using IdentityService.Auth.Domain.Entities;
using IdentityService.Auth.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
namespace IdentityService.Auth.Infrastructure.DataBase
{

    public class AuthDbContext : DbContext
    {
        // 高并发雪花算法引擎，用于非 Guid 类型的 BIGINT 物理主键自增（工作机器ID: 1, 数据中心ID: 1）
        private static readonly SnowflakeIdEngine IdWorker = new(1, 1);

        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        // 核心安全身份网格数据集
        public DbSet<SysUser> SysUsers => Set<SysUser>();
        public DbSet<SysRole> SysRoles => Set<SysRole>();
        public DbSet<SysPermission> SysPermissions => Set<SysPermission>();
        public DbSet<SysUserRole> SysUserRoles => Set<SysUserRole>();
        public DbSet<SysRolePermission> SysRolePermissions => Set<SysRolePermission>();

      
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. 挂载 OpenIddict 核心 14 张高安全 OIDC 体系表底座
            modelBuilder.UseOpenIddict();

            // 2. 运营/运维及商户用户表配置（主键采用高强度 Guid 连续有序生成）
            modelBuilder.Entity<SysUser>(b =>
            {
                b.ToTable("AuthUsers","dbo");
                b.HasKey(u => u.Id);
                // 保护磁盘物理文件页连续追加，防范高并发下的数据库“页分裂”开销
                b.Property(u => u.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
                b.Property(u => u.Username).HasMaxLength(100).IsRequired();
                b.HasIndex(u => u.Username).IsUnique();
            });

            // 3. 角色表配置
            modelBuilder.Entity<SysRole>(b =>
            {
                b.ToTable("AuthRoles","dbo");
                b.HasKey(r => r.Id);
                b.Property(r => r.RoleCode).HasMaxLength(50).IsRequired();
                b.HasIndex(r => r.RoleCode).IsUnique();
            });

            // 4. 【核心重构点】权限功能资源位表配置
            modelBuilder.Entity<SysPermission>(b =>
            {
                b.ToTable("AuthPermissions","dbo");
                b.HasKey(p => p.Id);
                b.Property(p => p.Resource).HasMaxLength(100).IsRequired();

                // 将长整型 Flags 枚举映射为 SQL Server 的 BIGINT 物理列
                b.Property(p => p.AllowedActions).IsRequired();

                // 索引升级：由“资源+动作字符串”进化为“资源+二进制动作位”的复合唯一索引
                b.HasIndex(p => new { p.Resource, p.AllowedActions }).IsUnique();

                b.Property(p => p.AbacRule)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                      v => JsonSerializer.Deserialize<AbacPolicyRule>(v, JsonSerializerOptions.Default) ?? new AbacPolicyRule()
                  )
                  .HasColumnType("nvarchar(max)")
                  .IsRequired();
            });


            // 5. 显式配置多对多交叉网格的复合主键
            modelBuilder.Entity<SysUserRole>(b =>
            {
                b.ToTable("AuthUserRoles", "dbo");
                b.HasKey(r => new { r.UserId, r.RoleId });

                // 显式声明外键关系
                b.HasOne(ur => ur.User)
                 .WithMany(u => u.UserRoles)
                 .HasForeignKey(ur => ur.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(ur => ur.Role)
                 .WithMany(r => r.UserRoles)
                 .HasForeignKey(ur => ur.RoleId)
                 .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<SysRolePermission>(b =>
            {
                b.ToTable("AuthRolePermissions", "dbo");
                b.HasKey(rp => new { rp.RoleId, rp.PermissionId });

                // 显式声明外键关系
                b.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(rp => rp.Permission)
                 .WithMany(p => p.RolePermissions)
                 .HasForeignKey(rp => rp.PermissionId)
                 .OnDelete(DeleteBehavior.Cascade);

            });


            // 6. 强行挂载刚刚升级完位运算的庞大静态种子数据引擎
            AuthDbSeedData.InjectionHeavySeeds(modelBuilder);
        }

        #region 拦截器机制：高并发分布式雪花主键自动泵入

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ProcessSnowflakeIds();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ProcessSnowflakeIds();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// 自动扫描全网进入 EF 状态机的追踪对象，凡是遭遇 BIGINT 且未显式赋值的主键，自动注入雪花ID
        /// </summary>
        private void ProcessSnowflakeIds()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State != EntityState.Added) continue;

                var primaryKey = entry.Metadata.FindPrimaryKey();
                if (primaryKey == null) continue;

                foreach (var property in primaryKey.Properties)
                {
                    // 专门针对业务流水表或关联组件中需要雪花长整型 ID 的主键进行拦截注入
                    if (property.ClrType == typeof(long) && (long)entry.CurrentValues[property.Name]! == 0L)
                    {
                        entry.CurrentValues[property.Name] = IdWorker.NextId();
                    }
                }
            }
        }

        #endregion
    }

}
