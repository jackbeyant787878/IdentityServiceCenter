using IdentityService.Auth.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Application.IRepositories
{
    public interface ISysUserRepository
    {
        Task<SysUser?> GetByUserNameAsync(string username);
        Task<SysUser?> GetByIdAsync(Guid id);
        Task<SysUser?> GetWithPermissionsAsync(Guid id); // 处理用户及其权限树
    }
}
