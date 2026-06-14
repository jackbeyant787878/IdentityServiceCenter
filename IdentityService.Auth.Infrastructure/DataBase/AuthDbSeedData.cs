using IdentityService.Auth.Domain.Entities;
using IdentityService.Auth.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Infrastructure.DataBase
{

    public static class AuthDbSeedData
    {
        private static readonly Guid PermModifyKeyId = Guid.Parse("01111111-1111-1111-1111-111111111111");
        private static readonly Guid PermRefundTier2Id = Guid.Parse("02222222-2222-2222-2222-222222222222");
        private static readonly Guid PermRefundTier1Id = Guid.Parse("03333333-3333-3333-3333-333333333333");
        private static readonly Guid PermViewAuditId = Guid.Parse("04444444-4444-4444-4444-444444444444");

        private static readonly Guid RolePlatAdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid RolePlatAuditorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid RoleMchCfoId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid RoleMchCashierId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        private static readonly Guid UserPlatRootId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        private static readonly Guid UserAuditorId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        private static readonly Guid UserWandaCfoId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        private static readonly Guid UserWandaCashierId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        public static void InjectionHeavySeeds(ModelBuilder modelBuilder)
        {
            // 1. 灌入系统核心功能树（Action 字符串升级为 AllowedActions 位掩码）
            modelBuilder.Entity<SysPermission>().HasData(
                new SysPermission
                {
                    Id = PermModifyKeyId,
                    Resource = "pay_config",
                    AllowedActions = PayActions.ModifyConfig, 
                    PermissionName = "修改通道核心核心支付私钥密钥",
                    CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    AbacRule = new AbacPolicyRule
                    {
                        MaxAllowedAmount = 0.00m,
                        AllowedIpRange = "127.0.0.1",
                        EnforceMerchantIsolation = true
                    }
                },
                new SysPermission
                {
                    Id = PermRefundTier2Id,
                    Resource = "refund_order",
                    AllowedActions = PayActions.AuditTier2, 
                    PermissionName = "大额无上限资金退款审批权",
                    CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    AbacRule = new AbacPolicyRule
                    {
                        MaxAllowedAmount = 500000.00m,
                        AllowedIpRange = "10.10.10.100",
                        EnforceMerchantIsolation = true
                    }
                },
                new SysPermission
                {
                    Id = PermRefundTier1Id,
                    Resource = "refund_order",
                    AllowedActions = PayActions.AuditTier1, 
                    PermissionName = "门店小额日常高频退款操作权",
                    CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    AbacRule = new AbacPolicyRule
                    {
                        MaxAllowedAmount = 5000.00m,
                        AllowedIpRange = null,
                        EnforceMerchantIsolation = true
                    }

                },
                new SysPermission
                {
                    Id = PermViewAuditId,
                    Resource = "balance",
                    AllowedActions = PayActions.Query, 
                    PermissionName = "全网多租户金流对账全局透视",
                    CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    AbacRule = new AbacPolicyRule
                    {
                        MaxAllowedAmount = 0.00m,
                        AllowedIpRange = null,
                        EnforceMerchantIsolation = false
                    }
                }
            );

     

            // 2. 灌入预设的核心系统角色
            modelBuilder.Entity<SysRole>().HasData(
                new SysRole { Id = RolePlatAdminId, RoleName = "平台超级金流管控专员", RoleCode = "ROLE_PLATFORM_SUPER_ADMIN", DataScope = DataScope.PlatformGlobal, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new SysRole { Id = RolePlatAuditorId, RoleName = "中央风控合规审计官", RoleCode = "ROLE_PLATFORM_AUDITOR", DataScope = DataScope.PlatformGlobal, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new SysRole { Id = RoleMchCfoId, RoleName = "商户集团首席财务官", RoleCode = "ROLE_MERCHANT_GROUP_CFO", DataScope = DataScope.MerchantLevel, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new SysRole { Id = RoleMchCashierId, RoleName = "门店标准前台收银员", RoleCode = "ROLE_MERCHANT_CASHIER", DataScope = DataScope.StoreLevel, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // 3. 灌入精准隔离的分区测试人员（密码：P@ssword2026 保持强对齐）
            const string pwdHash = "AQAAAAIAAYagAAAAEJxR...[MOCK_CRYPTO_HASH]...";
            modelBuilder.Entity<SysUser>().HasData(
                // 平台系统级
                new SysUser { Id = UserPlatRootId, Username = "pc_central_admin", PasswordHash = pwdHash, RealName = "张总（中央金流总裁）", Mobile = "13800000001", UserType = UserType.PlatformSuperAdmin, BelongMerchantId = null, BelongStoreId = null, IsActive = true, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new SysUser { Id = UserAuditorId, Username = "pc_audit_compliance", PasswordHash = pwdHash, RealName = "李四（合规审计总监）", Mobile = "13800000002", UserType = UserType.PlatformAuditor, BelongMerchantId = null, BelongStoreId = null, IsActive = true, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                // 商户专属级（万达集团 ID 强对齐）
                new SysUser { Id = UserWandaCfoId, Username = "wanda_group_cfo", PasswordHash = pwdHash, RealName = "王华（万达商管CFO）", Mobile = "13911112222", UserType = UserType.MerchantAdmin, BelongMerchantId = 1805123456789012345, BelongStoreId = null, IsActive = true, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                // 门店基层级（蛇口店 ID 强对齐）
                new SysUser { Id = UserWandaCashierId, Username = "wanda_sk_cashier01", PasswordHash = pwdHash, RealName = "小刘（蛇口店01号收银员）", Mobile = "13533334444", UserType = UserType.MerchantOperator, BelongMerchantId = 1805123456789012345, BelongStoreId = 1905123456789000001, IsActive = true, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // 4. 组装网格多对多关系：角色强绑功能位定义
            modelBuilder.Entity<SysRolePermission>().HasData(
                // 平台超管：拥有全部 4 个物理权限定义块
                new SysRolePermission { RoleId = RolePlatAdminId, PermissionId = PermModifyKeyId },
                new SysRolePermission { RoleId = RolePlatAdminId, PermissionId = PermRefundTier2Id },
                new SysRolePermission { RoleId = RolePlatAdminId, PermissionId = PermRefundTier1Id },
                new SysRolePermission { RoleId = RolePlatAdminId, PermissionId = PermViewAuditId },

                // 中央审计官：仅拥有对账透视块（内部已被赋予 PayActions.Query 位）
                new SysRolePermission { RoleId = RolePlatAuditorId, PermissionId = PermViewAuditId },

                // 商户CFO：同时拥有大额终审块与日常小额块
                new SysRolePermission { RoleId = RoleMchCfoId, PermissionId = PermRefundTier2Id },
                new SysRolePermission { RoleId = RoleMchCfoId, PermissionId = PermRefundTier1Id },

                // 门店收银员：仅拥有日常小额块
                new SysRolePermission { RoleId = RoleMchCashierId, PermissionId = PermRefundTier1Id }
            );

            // 5. 组装网格多对多关系：用户穿戴专属角色
            modelBuilder.Entity<SysUserRole>().HasData(
                new SysUserRole { UserId = UserPlatRootId, RoleId = RolePlatAdminId },
                new SysUserRole { UserId = UserAuditorId, RoleId = RolePlatAuditorId },
                new SysUserRole { UserId = UserWandaCfoId, RoleId = RoleMchCfoId },
                new SysUserRole { UserId = UserWandaCashierId, RoleId = RoleMchCashierId }
            );
        }
    }
}
