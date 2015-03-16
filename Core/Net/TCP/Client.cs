﻿using System;
using System.Net;
using System.Net.Sockets;
using Core.Log;
using Core.RPC;
using Core.Service;

namespace Core.Net.TCP
{
    public interface IClient<out TSession, out TLogicService, out TNetService> : IPeer<TSession, TLogicService, TNetService>
        where TSession : class, ISession, new()
        where TNetService : IService, new()
        where TLogicService : ILogicService, new()
    {
        void Send(byte[] data);
        void Send<T>(T proto);
    }

    public class Client<TSession, TLogicService, TNetService> : IClient<TSession, TLogicService, TNetService>
        where TSession : class, ISession, new()
        where TNetService : class ,IService, new()
        where TLogicService : class, ILogicService, new()
    {
        public event Action<TSession, SessionCloseReason> EventSessionClosed;
        public event Action<TSession> EventSessionEstablished;
        public event Action EventPeerClosing;

        public string Ip { get; private set; }
        public ushort Port { get; private set; }
        public IPAddress Address { get; private set; }
        public EndPoint EndPoint { get; private set; }
        public SessionMgr SessionMgr { get; private set; }
        public bool IsLogicServiceShared { get; private set; }
        public bool IsNetServiceShared { get; private set; }
        public TLogicService LogicService { get; private set; }
        public TNetService NetService { get; private set; }

        public TSession Session
        {
            get
            {
                var ret = SessionMgr.FirstOrDefault();
                return ret != null ? ret as TSession : null;
            }
        }

        private Socket _underlineSocket;
        private SessionFactory<TSession> _sessionFactory;
        private SocketAsyncEventArgs _connectEvent;

        public Client(string ip, ushort port)
        {
            Ip = ip;
            Port = port;

            Address = IPAddress.Parse(Ip);
            EndPoint = new IPEndPoint(Address, Port);
        }

        public void Start(IService net, IService logic)
        {
            _sessionFactory = new SessionFactory<TSession>();

            SessionMgr = new SessionMgr(this,
                session => { if (EventSessionEstablished != null) EventSessionEstablished(session as TSession); },
                (session, reason) => { if (EventSessionClosed != null) EventSessionClosed(session as TSession, reason); });

            _connectEvent = new SocketAsyncEventArgs();
            _connectEvent.Completed += OnConnectCompleted;

            _underlineSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IsLogicServiceShared = (logic != null);
            IsNetServiceShared = (net != null);

            LogicService = (logic as TLogicService) ?? new TLogicService { Capacity = 10000, Period = 10 };
            if (!IsLogicServiceShared) LogicService.Start();

            NetService = (net as TNetService) ?? new TNetService { Capacity = 10000, Period = 10 };
            if (!IsNetServiceShared) NetService.Start();

            Connect();
        }

        public void Stop()
        {
            SessionMgr.Dispose();
            _connectEvent.Completed -= OnConnectCompleted;

            LogicService.Perform(() =>
            {
                if (EventPeerClosing != null)
                    EventPeerClosing();

                if (!IsLogicServiceShared) LogicService.Stop();
                if (!IsNetServiceShared) NetService.Stop();
            });
        }

        public void PerformInLogic(Action action)
        {
            LogicService.Perform(action);
        }

        public void PerformInLogic<TParam>(Action<TParam> action, TParam param)
        {
            LogicService.Perform(action, param);
        }

        public void PerformInNet(Action action)
        {
            NetService.Perform(action);
        }

        public void PerformInNet<TParam>(Action<TParam> action, TParam param)
        {
            NetService.Perform(action, param);
        }
        
        public void Send<TP>(TP proto)
        {
            if (Session != null) Session.Send(proto);
        }

        public void Send(byte[] data)
        {
            if (Session != null) Session.Send(data);
        }

        public void Connect(byte[] info = null)
        {
            _connectEvent.RemoteEndPoint = EndPoint;
            if (info != null)
                _connectEvent.SetBuffer(info, 0, info.Length);

            if (!_underlineSocket.ConnectAsync(_connectEvent))
                HandleConnection(_underlineSocket);
        }

        private void HandleConnection(Socket sock)
        {
            var session = _sessionFactory.Create(sock, this);
            SessionMgr.Add(session);

            Session.Start();
        }
        
        private void OnConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            if(e.SocketError == SocketError.Success)
                HandleConnection(_underlineSocket);
            else
                Logger.Instance.Error(e.SocketError);
        }
    }


    public class Client : Client<RpcSession, LogicService, NetService>
    {
        public Client(string ip, ushort port) : base(ip, port)
        {
        }
    }

    public class UnityClient : Client<RpcSession, LogicService4Unity, NetService>
    {
        public UnityClient(string ip, ushort port) : base(ip, port)
        {
        }
    }
}