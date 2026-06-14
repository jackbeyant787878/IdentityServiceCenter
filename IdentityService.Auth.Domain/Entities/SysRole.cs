using IdentityService.Auth.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Domain.Entities
{
    public class SysRole
    {
        public Guid Id { get; set; }= Guid.NewGuid();
        public string RoleName { get; set; } = null!;
        public string RoleCode { get; set; } = null!; // 如: ROLE_MCH_CFO, ROLE_CASHIER
        public DataScope DataScope { get; set; }       // 核心：纵向切割数据隔离线
        public long? BelongMerchantId { get; set; }    // 允许商户自定义自身的专属角色
        public DateTime CreatedAtUtc { get; set; }

        public ICollection<SysUserRole> UserRoles { get; set; } = new List<SysUserRole>();
        public ICollection<SysRolePermission> RolePermissions { get; set; } = new List<SysRolePermission>();
    }
}
