﻿namespace Core.RPC
{  
    /// <summary>
    /// Rpc类型
    /// C->S or S->C
    /// </summary>
    public enum RpcType
    {
        /// <summary>
        /// 通知
        /// 无需等待对端响应
        /// CS SC
        /// </summary>
        Notify,
        /// <summary>
        /// 请求
        /// 需要等待对端响应才能完成本次Rpc
        /// CS
        /// </summary>
        Request,
        /// <summary>
        /// 响应Request
        /// SC
        /// </summary>
        Response,
    }
}