using IdentityService.Auth.Application.IRepositories;
using IdentityService.Auth.Domain.Entities;
using IdentityService.Auth.Infrastructure.DataBase;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Infrastructure.Repositories
{
    public class SysUserRepository : ISysUserRepository
    {
        private readonly AuthDbContext _db;
        public SysUserRepository(AuthDbContext db) => _db = db;
        public async Task<SysUser?> GetByUserNameAsync(string username) => await _db.SysUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);

        public async Task<SysUser?> GetByIdAsync(Guid id) => await _db.SysUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id ==id);

        public async Task<SysUser?> GetWithPermissionsAsync(Guid id)
        {
            return await _db.SysUsers
                .AsNoTracking()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(rp => rp.RolePermissions).ThenInclude(p => p.Permission)
                .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
