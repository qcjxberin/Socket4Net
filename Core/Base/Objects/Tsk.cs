﻿using System;

namespace socket4net
{
    public abstract class Tsk : Obj
    {
        public event Action<int, int> EventProgressChanged;
        public event Action<string> EventFailed;
        public event Action EventCompleted;

        private bool _isDone;
        private bool _failed;
        int _progress;

        public int Progress
        {
            get { return _progress; }
            protected set
            {
                if (value == _progress) return;

                _progress = value;
                if (EventProgressChanged != null)
                    EventProgressChanged(Steps, Progress);
            }
        }

        public int Steps { get; protected set; }

        public bool IsDone
        {
            get { return _isDone; }
            protected set
            {
                if (value == _isDone) return;
                _isDone = value;
                if (_isDone && EventCompleted != null)
                    EventCompleted();
            }
        }

        public bool Failed
        {
            get { return _failed; }
            protected set
            {
                if (_failed == value) return;
                _failed = value;

                if (_failed && EventFailed != null)
                    EventFailed("");
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 清理事件
            EventCompleted = null;
            EventFailed = null;
            EventProgressChanged = null;
        }
    }
}