﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.Log;

#if !NET35
using System.Collections.Concurrent;
#else
using Core.Concurrent;
#endif

namespace Core.Net.TCP
{
    public class SessionMgr : IDisposable
    {
        public int Count { get { return _sessions.Count; } }
        public IPeer Host { get; private set; }

        private readonly ConcurrentDictionary<long, ISession> _sessions = new ConcurrentDictionary<long, ISession>();
        private Action<ISession> _openCb;
        private Action<ISession, SessionCloseReason> _closeCb;

        public SessionMgr(IPeer host, Action<ISession> openCb, Action<ISession, SessionCloseReason> closeCb)
        {
            Host = host;
            _openCb = openCb;
            _closeCb = closeCb;
        }

        public void Add(ISession session)
        {
            if (_sessions.TryAdd(session.Id, session))
            {
                Host.PerformInLogic(()=>_openCb(session));
            }
            else
                Logger.Instance.Warn("Add session failed for id : " + session.Id);
        }

        public void Remove(long id, SessionCloseReason reason)
        {
            ISession session;
            if (_sessions.TryRemove(id, out session))
            {
                Host.PerformInLogic(() =>_closeCb(session, reason));
            }
            else if (_sessions.ContainsKey(id))
                Logger.Instance.Warn("Remove session failed for id : " + id);
            else
                Logger.Instance.Warn("Remove session failed for id :  cause of it doesn't exist" + id);
        }

        public ISession Get(long id)
        {
            ISession session;
            return _sessions.TryGetValue(id, out session) ? session : null;
        }

        public void Clear()
        {
            foreach (var session in Sessions)
            {
                Remove(session.Id, SessionCloseReason.ClosedByMyself);
            }
        }

        public IEnumerable<ISession> Sessions
        {
            get
            {
                return _sessions.Select(x => x.Value);
            }
        }

        public void Dispose()
        {
            Clear();

            _openCb = null;
            _closeCb = null;
            Host = null;
        }
    }
}
