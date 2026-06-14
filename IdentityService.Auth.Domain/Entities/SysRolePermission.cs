using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Domain.Entities
{
    public class SysRolePermission
    {
        public Guid RoleId { get; set; }
        public SysRole Role { get; set; } = null!;
        public Guid PermissionId { get; set; }
        public SysPermission Permission { get; set; } = null!;
    }
}
