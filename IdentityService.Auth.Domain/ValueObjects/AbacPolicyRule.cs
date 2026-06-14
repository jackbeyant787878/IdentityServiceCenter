using IdentityService.Auth.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Domain.ValueObjects
{
    /// <summary>
    /// 高级 ABAC 阶梯动态风控规则值对象
    /// </summary>
    public class AbacPolicyRule
    {
        public decimal MaxAllowedAmount { get; set; } = decimal.MaxValue;
        public string? AllowedIpRange { get; set; }
        public bool EnforceMerchantIsolation { get; set; } = true;

        /// <summary>
        /// 核心风控断言引擎：只接受基础类型和同层的领域实体
        /// </summary>
        public bool IsSatisfiedBy(decimal requestAmount,string clientRealIp,long targetMerchantId,long operatorMerchantId)
        {
            // 1. 阶梯限额校验
            if (requestAmount > MaxAllowedAmount) return false;
            // 2. 环境风控安全隔离 (IP段校验)
            if (!string.IsNullOrEmpty(AllowedIpRange) && clientRealIp != AllowedIpRange) return false;
            // 3. 商户物理水平隔离（强力防越权）
            if (EnforceMerchantIsolation && operatorMerchantId != targetMerchantId) return false;
            return true;
        }
    }
}
