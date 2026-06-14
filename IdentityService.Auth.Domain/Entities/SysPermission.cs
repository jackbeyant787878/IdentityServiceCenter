using IdentityService.Auth.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Domain.Entities
{
    public class SysPermission
    {
        public Guid Id { get; set; }= Guid.NewGuid();

        /// <summary>
        /// 资源域，例如：pay_order, mch_config, merchant_balance
        /// </summary>
        public required string Resource { get; set; } = null!;

        /// <summary>
        /// 核心进化：该资源当前被分配的二进制权限位合集
        /// </summary>
        public PayActions AllowedActions { get; set; }

        public required string PermissionName { get; set; } = null!;

        // 依然保留高级 ABAC 阶梯风控规则（完美兼得 RBAC + 位运算 + ABAC）
        public AbacPolicyRule AbacRule { get; set; } = new();

        public DateTime CreatedAtUtc { get; set; }
        public ICollection<SysRolePermission> RolePermissions { get; set; } = new List<SysRolePermission>();
    }
}
