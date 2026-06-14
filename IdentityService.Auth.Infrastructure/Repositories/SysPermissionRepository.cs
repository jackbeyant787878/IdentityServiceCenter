using IdentityService.Auth.Application.IRepositories;
using IdentityService.Auth.Domain.Entities;
using IdentityService.Auth.Infrastructure.DataBase;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Infrastructure.Repositories
{
    public class SysPermissionRepository : ISysPermissionRepository
    {
        private readonly AuthDbContext _db;
        public SysPermissionRepository(AuthDbContext db) => _db = db;

        public async Task<List<SysPermission>> GetPermissionsByResourceAsync(string resource)
        {
            return await _db.SysPermissions
                .AsNoTracking()
                .Where(p => p.Resource == resource)
                .ToListAsync();
        }
    }
}
