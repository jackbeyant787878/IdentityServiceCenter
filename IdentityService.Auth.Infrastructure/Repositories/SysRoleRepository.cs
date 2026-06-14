using IdentityService.Auth.Application.IRepositories;
using IdentityService.Auth.Domain.Entities;
using IdentityService.Auth.Infrastructure.DataBase;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Infrastructure.Repositories
{
    public class SysRoleRepository : ISysRoleRepository
    {
        private readonly AuthDbContext _db;

        public SysRoleRepository(AuthDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 只读查询：获取所有激活的角色（强加 AsNoTracking 压榨极限性能）
        /// </summary>
        public async Task<List<SysRole>> GetActiveRolesAsync()
        {
            return await _db.SysRoles
                .AsNoTracking()
                // 假设实体有 IsActive 状态，如果没有可以去掉此处的 Where 过滤
                //.Where(r => r.IsActive)
                .ToListAsync();
        }

        /// <summary>
        /// 事务性写操作：一键更替角色的权限矩阵
        /// </summary>
        public async Task UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds)
        {
            // 1. 采用高效率的物理清除，先抹除该角色旧的全部权限映射
            var oldMappings = _db.SysRolePermissions.Where(rp => rp.RoleId == roleId);
            _db.SysRolePermissions.RemoveRange(oldMappings);

            // 2. 批量组装新的全量权限映射
            if (permissionIds != null && permissionIds.Any())
            {
                var newMappings = permissionIds.Select(pId => new SysRolePermission
                {
                    RoleId = roleId,
                    PermissionId = pId
                    // 如果你使用了雪花 ID 拦截器，这里不需要手动赋主键 ID
                });

                await _db.SysRolePermissions.AddRangeAsync(newMappings);
            }

            // 3. 在仓储主场内部持久化！业务层（Application）只需要调用此方法，完全不需要感知 SaveChanges
            await _db.SaveChangesAsync();
        }
    }
}
