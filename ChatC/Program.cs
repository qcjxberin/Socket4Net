﻿using System;
using System.Threading;
using Core.Log;
using Core.Net.TCP;
using Core.RPC;

namespace ChatC
{
    internal class Program
    {
        private static Chater _chater;

        private static void Main(string[] args)
        {
            var ip = "127.0.0.1";
            ushort port = 5000;

            if (args.Length > 0)
            {
                ip = args[0];
                if (args.Length > 1)
                    port = ushort.Parse(args[1]);
            }

            // 初始Logger
            Logger.Instance = new DefaultLogger();

            //ClientSample(ip, port);
            UnityClientSample(ip, port);
        }

        private static void ClientSample(string ip, ushort port)
        {
            // 客户端示例
            Chater chater = null;

            // 创建客户端
            var client = new Client(ip, port);

            // 监听事件
            client.EventSessionClosed +=
                (session, reason) => Logger.Instance.InfoFormat("{0} disconnected by {1}", session.Id, reason);
            client.EventSessionEstablished +=
                session =>
                {
                    Logger.Instance.InfoFormat("{0} connected", session.Id);
                    chater = new Chater(session);
                    chater.Boot();  // 需要Boot才能处理rpc
                };

            // 启动客户端
            // 注意：该客户端拥有自己独立的网络服务和逻辑服务，故传入参数为null
            client.Start(null, null);

            // 结束服务器
            while (true)
            {
                var cmd = Console.ReadLine();
                switch (cmd.ToUpper())
                {
                    case "QUIT":
                    case "EXIT":
                        {
                            client.Stop();
                        }
                        break;

                    case "REQUEST":
                        {
                            chater.RequestCommand(
                                "This is a RPC response, indicate that server response your request success when you receive this message!");
                        }
                        continue;

                    default:
                        {
                            chater.NotifyMessage(cmd);
                        }
                        continue;
                }
            }
        }

        private static void UnityClientSample(string ip, ushort port)
        {
            // 客户端示例
            Chater chater = null;

            // 创建客户端
            var client = new UnityClient(ip, port);

            // 监听事件
            client.EventSessionClosed +=
                (session, reason) => Logger.Instance.InfoFormat("{0} disconnected by {1}", session.Id, reason);
            client.EventSessionEstablished +=
                session =>
                {
                    Logger.Instance.InfoFormat("{0} connected", session.Id);
                    chater = new Chater(session);
                };

            // 启动客户端
            // 注意：该客户端拥有自己独立的网络服务和逻辑服务，故传入参数为null
            // 超级警告：由于UnityClient直接使用Unity的逻辑线程作为自己的逻辑服务
            // 线程，所以需要在某个MonoBehaviour的Update或FixedUpdate中调用
            // client.LogicService.Update(delta)来驱动逻辑服务
            client.Start(null, null);

            // 结束客户端
            var stop = false;
            while (!stop)
            {
                Thread.Sleep(1);

                // 使用主线程来模拟unity逻辑线程
                client.LogicService.Update(.0f);

                ThreadPool.QueueUserWorkItem(state =>
                {
                    while (!stop)
                    {
                        string cmd = Console.ReadLine();
                        switch (cmd.ToUpper())
                        {
                            case "QUIT":
                            case "EXIT":
                                {
                                    client.PerformInLogic(() =>
                                    {
                                        client.Stop();
                                        stop = true;
                                    });
                                }
                                break;

                            case "REQUEST":
                                {
                                    client.PerformInLogic(() => chater.RequestCommand(
                                        "This is a rpc request, specified server response success when you receive this message!"));
                                }
                                break;

                            default:
                                {
                                    client.PerformInLogic(() => chater.NotifyMessage(cmd));
                                }
                                break;
                        }
                    }
                });
            }
        }
    }
}
