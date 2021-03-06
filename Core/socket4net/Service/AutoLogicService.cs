﻿#region MIT
//  /*The MIT License (MIT)
// 
//  Copyright 2016 lizs lizs4ever@163.com
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//   * */
#endregion
using System;
using System.Threading;

#if NET45
using System.Collections.Concurrent;
#endif

namespace socket4net
{
    /// <summary>
    ///     自动逻辑服务
    ///     定时器刷新以及Jobs队列的刷新完全由本模块自理
    /// </summary>
    public class AutoLogicService : LogicServiceBase
    {
        private Thread _workingThread;
        private BlockingCollection<IJob> _workingQueue;

        protected override void OnStart()
        {
            base.OnStart();

            if (_workingQueue != null
                || _workingThread != null)
            {
                Logger.Ins.Fatal("AutoLogicService being start more than once");
                return;
            }

            DoStartup();
            Logger.Ins.Debug("Auto logic service started!");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_workingThread == null
                || _workingQueue == null)
            {
                Logger.Ins.Fatal("AutoLogicService not yet been started");
                return;
            }

            StopWorking = true;
            _workingThread.Join();
            Logger.Ins.Debug("auto-LogicService stopped!");
        }

        public override event Action Idle;

        /// <summary>
        /// Specify the work items count currently in working queue.
        /// </summary>
        public override int Jobs
        {
            get { return _workingQueue.Count; }
        }
        
        /// <summary>
        /// External(e.g. the TCP socket thread) call this method to push
        /// a work item into the working queue. The work item must not
        /// be null.
        /// </summary>
        /// <param name="w">the work item object, must not be null</param>
        public override void Enqueue(IJob w)
        {
            _workingQueue.Add(w);
        }
   
        /// <summary>
        /// do actually startup job
        /// </summary>
        private void DoStartup()
        {
            _workingQueue = new BlockingCollection<IJob>(QueueCapacity);
            _workingThread = new Thread(WorkingProcedure) { Name = "AutoLogicService", IsBackground = true};
            // use background thread
            // see http://msdn.microsoft.com/en-us/library/h339syd0.aspx

            _workingThread.Start();
        }
        
        /// <summary>
        /// working thread's working procedure
        /// </summary>
        private void WorkingProcedure()
        {
            Watch.Start();

            while (!StopWorking)
            {
                var periodCounter = StopWatchDivider;
                var tick = Environment.TickCount;

                var t1 = Watch.ElapsedMilliseconds;

                do
                {
                    try
                    {
                        IJob item;
                        if (_workingQueue.TryTake(out item, Period))
                        {
                            item.Do();
                            CalcPerformance();

                            periodCounter--;

                            if (periodCounter < 1)
                            {
                                if ((Watch.ElapsedMilliseconds - t1) >= Period)
                                {
                                    break;
                                }
                                periodCounter = StopWatchDivider;
                            }
                        }
                        else
                            break;
                    }
                    catch (Exception ex)
                    {   
                        Logger.Ins.Fatal("{0} : {1}", ex.Message, ex.StackTrace);
                    }

                }
                while ((Environment.TickCount - tick) < Period);

                WiElapsed = Watch.ElapsedMilliseconds - t1;
                if (Idle == null) continue;

                var t2 = Watch.ElapsedMilliseconds;
                try
                {
                    Idle();
                }
                catch (Exception ex)
                {
                    Logger.Ins.Fatal(string.Format("{0} : {1}", ex.Message, ex.StackTrace));
                }

                IdleCallbackElapsed = Watch.ElapsedMilliseconds - t2;
            }

            
            IJob leftItem;
            while (_workingQueue.TryTake(out leftItem, Period * 10))
            {
                leftItem.Do();
            }

            Watch.Stop();
        }
    }
}
