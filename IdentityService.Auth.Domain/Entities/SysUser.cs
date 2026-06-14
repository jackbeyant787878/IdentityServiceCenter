using IdentityService.Auth.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Domain.Entities
{
    public class SysUser
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // 权限表维持 GUID 的高灵活性，底层采用连续 GUID
        public string Username { get; set; } 
        public string PasswordHash { get; set; } 
        public string RealName { get; set; } 
        public string Mobile { get; set; }
        public UserType UserType { get; set; } = UserType.PlatformSuperAdmin;

        // 支付系统多租户核心隔离纽带（弱耦合外部业务表的分布式 ID）
        public long? BelongMerchantId { get; set; }
        public long? BelongStoreId { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; }

        public ICollection<SysUserRole> UserRoles { get; set; } = new List<SysUserRole>();
    }
}
