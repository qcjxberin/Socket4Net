﻿using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace socket4net
{
    [ProtoContract]
    public class ProtoWrapper<T>
    {
        [ProtoMember(1)]
        public T Item { get; set; }
    }

    /// <summary>
    ///     对接Redis key-value
    /// </summary>
    public class RedisEntry
    {
        public string Feild { get; set; }
        public byte[] Data { get; set; }
    }

    /// <summary>
    ///     实体信息
    ///     一个实体由 类型、Id、属性定义
    /// </summary>
    public class EntityEntry
    {
        public Type Type { get; set; }
        public Guid Id { get; set; }
        public IReadOnlyCollection<RedisEntry> Blocks { get; set; }
    }

    /// <summary>
    ///     实体管理器参数
    ///     包括：
    ///     1、 属性块创建器
    ///     2、 属性Feild格式化器
    ///     3、 属性Feild萃取器
    /// </summary>
    public class EntityMgrArg : UniqueMgrArg
    {
        public Func<short, IBlock> BlockMaker { get; private set; }
        public Func<short, string> BlockFeildFormatter { get; private set; }
        public Func<string, short> BlockFeildExtractor { get; private set; }

        public EntityMgrArg(IObj parent, Func<short, IBlock> blockMaker, Func<short, string> formatter,
            Func<string, short> extractor) : base(parent)
        {
            BlockMaker = blockMaker;
            BlockFeildFormatter = formatter;
            BlockFeildExtractor = extractor;
        }
    }
    
    /// <summary>
    ///     实体管理器
    /// </summary>
    public class EntityMgr : UniqueMgr<Guid, Entity>
    {
        private Func<short, IBlock> _blockMaker;
        private Func<short, string> _blockFeildFormatter;
        private Func<string, short> _blockFeildExtractor;

        public event Action<Entity> EventEntityExtracted;
        public event Action<Entity> EventEntityCreated;

        protected override void OnInit(ObjArg arg)
        {
            base.OnInit(arg);
            var more = arg.As<EntityMgrArg>();

            _blockMaker = more.BlockMaker;
            _blockFeildFormatter = more.BlockFeildFormatter;
            _blockFeildExtractor = more.BlockFeildExtractor;

            if (_blockMaker == null || _blockFeildFormatter == null || _blockFeildExtractor == null)
                throw new ArgumentException("arg");
        }

        #region 提取

        public T Extract<T>(EntityEntry entry) where T : Entity, new()
        {
            var entity = Create<T>(entry.Type, new EntityArg(this, entry.Id));
            if (entity == null) return null;

            if (entry.Blocks.IsNullOrEmpty()) return entity;
            var blocks = entry.Blocks.Select(ExtractBlock).Where(block => block != null).ToArray();
            entity.Apply(blocks);

            return entity;
        }

        public IEnumerable<T> Extract<T>(IReadOnlyCollection<EntityEntry> entries)
            where T : Entity, new()
        {
            return entries.IsNullOrEmpty() ? null : entries.Select(Extract<T>);
        }

        #endregion

        #region 输出

        /// <summary>
        ///     格式化输出一个实体
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public IReadOnlyCollection<RedisEntry> FormatEntity(Entity entity)
        {
            return entity.Blocks.Select(FormatBlock).ToArray();
        }

        /// <summary>
        ///     格式化输出一个属性块
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public RedisEntry FormatBlock(IBlock block)
        {
            return new RedisEntry {Feild = _blockFeildFormatter(block.Id), Data = block.Serialize()};
        }

        #endregion


        #region helper
        private string FormatEntityFeild(EntityEntry entry)
        {
            return string.Format("{0}:{1}", entry.Type.Name, entry.Id);
        }

        private EntityEntry ExtractEntityFeild(string feild)
        {
            var fileds = feild.Split(':');
            return new EntityEntry { Type = Type.GetType(fileds[0]), Id = Guid.Parse(fileds[1]) };
        }

        private IBlock ExtractBlock(RedisEntry entry)
        {
            var pid = _blockFeildExtractor(entry.Feild);
            var block = _blockMaker(pid);
            return block.Deserialize(entry.Data) ? block : null;
        }

        private bool ExtractBlock(RedisEntry entry, ref IBlock item)
        {
            return item.Deserialize(entry.Data);
        }

        private RedisEntry SerializeBlock(IBlock block)
        {
            var feild = _blockFeildFormatter(block.Id);
            var data = block is IDifferentialSerializable
                ? (block as IDifferentialSerializable).SerializeDifference()
                : block.Serialize();

            return new RedisEntry { Feild = feild, Data = data };
        }
        #endregion
    }
}