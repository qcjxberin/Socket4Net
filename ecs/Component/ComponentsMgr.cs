﻿using System;
using socket4net;

namespace ecs
{
    public class ComponentsMgr : UniqueMgr<short, Component>
    {
        private void AddDependencies<T>() where T : Component, new()
        {
            AddDependencies(typeof(T));
        }

        private void AddDependencies(Type cpType)
        {
            var targets = ComponentDepedencyCache.Ins.Get(cpType);
            if (targets.IsNullOrEmpty()) return;

            foreach (var target in targets)
            {
                CreateComponent(target);
            }
        }

        private Component CreateComponent(Type cpType)
        {
            var id = ComponentIdCache.Ins.Get(cpType);
            if(Exist(id)) return Get(id);

            var cp = (Component)Create(cpType, new ComponentArg(this, id));
            Add(cp);
            return cp;
        }

        private T CreateComponent<T>() where T : Component, new()
        {
            var id = ComponentIdCache.Ins.Get(typeof(T));
            return Exist(id) ? Get<T>(id) : Create<T>(new ComponentArg(this, id));
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var id = ComponentIdCache.Ins.Get(typeof(T));
            if (Exist(id)) return Get<T>(id);

            AddDependencies<T>();
            return CreateComponent<T>();
        }

        public Component AddComponent(Type cpType)
        {
            var id = ComponentIdCache.Ins.Get(cpType);
            if (Exist(id)) return Get(id);

            AddDependencies(cpType);
            return CreateComponent(cpType);
        }

        public bool ExistComponent<T>() where T : Component
        {
            return ExistComponent(typeof (T));
        }

        public bool ExistComponent(Type cpType)
        {
            var id = ComponentIdCache.Ins.Get(cpType);
            return ExistComponent(id);
        }

        public bool ExistComponent(short cpId)
        {
            return Exist(cpId);
        }
    }
}