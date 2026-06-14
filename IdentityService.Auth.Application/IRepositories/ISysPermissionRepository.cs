using IdentityService.Auth.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Application.IRepositories
{
    public interface ISysPermissionRepository
    {
        Task<List<SysPermission>> GetPermissionsByResourceAsync(string resource);
    }
}
