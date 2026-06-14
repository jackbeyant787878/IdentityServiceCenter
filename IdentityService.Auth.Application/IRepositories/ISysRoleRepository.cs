using IdentityService.Auth.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Application.IRepositories
{
    public interface ISysRoleRepository
    {
        Task<List<SysRole>> GetActiveRolesAsync();
        Task UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds);
    }
}
