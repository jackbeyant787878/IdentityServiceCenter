using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Domain.Entities
{
    public class SysUserRole
    {
        public Guid UserId { get; set; }
        public SysUser User { get; set; } = null!;
        public Guid RoleId { get; set; }
        public SysRole Role { get; set; } = null!;
    }
}
