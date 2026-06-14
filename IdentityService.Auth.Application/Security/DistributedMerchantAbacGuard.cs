using IdentityService.Auth.Application.IRepositories;
using IdentityService.Auth.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Hybrid;
using System.Security;
namespace PaymentCenter.Auth.Application.Security;
public record AbacSecurityGuardContext(string CurrentOperatorId,string Resource,PayActions Action,string ClientRealIp,decimal RequestAmount,long TargetMerchantId,long TargetStoreId);
public class DistributedMerchantAbacGuard
{
    private readonly HybridCache _hybridCache;
    private readonly ISysUserRepository _repository;

    public DistributedMerchantAbacGuard(HybridCache hybridCache, ISysUserRepository repository)
    {
        _hybridCache = hybridCache;
        _repository= repository;
    }

    public async Task<bool> IsActionAllowedAsync(AbacSecurityGuardContext ctx)
    {

        if (!Guid.TryParse(ctx.CurrentOperatorId, out var userIdGuid)) throw new UnauthorizedAccessException("[安全网闸拦截] 凭证主键非法！");


        // 1. 从 .NET 10 的 L1/L2 一体化混合多级缓存中瞬间提取全套风控规则矩阵（避免直接频繁硬洗数据库）
        string cacheKey = $"auth:matrix:user:{ctx.CurrentOperatorId}";
        var userMatrix = await _hybridCache.GetOrCreateAsync(cacheKey, async token =>
        {
            var userIdGuid = Guid.Parse(ctx.CurrentOperatorId);
             return await _repository.GetWithPermissionsAsync(userIdGuid);
        });

        if (userMatrix == null) return false;

        // 2. 第一道防线：纵向数据域隔离（DataScope 验证）
        // 平台Root不受约束，如果是商户CFO，必须保证其所属商户ID与目标修改资产的 MerchantId 强一致
        if (userMatrix.UserType == UserType.MerchantAdmin)
        {
            if (userMatrix.BelongMerchantId != ctx.TargetMerchantId) return false; // 越权横向跨商户偷刷！
        }
        else if (userMatrix.UserType == UserType.MerchantOperator)
        {
            if (userMatrix.BelongMerchantId != ctx.TargetMerchantId || userMatrix.BelongStoreId != ctx.TargetStoreId)
            {
                return false; // 基层收银员尝试跨分店或越权操作集团级资产！
            }
        }

        // 3. 第二道防线：精细化强类型高级 ABAC 风控规则拆包校验
        var matchedPermissions = userMatrix.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission)
            .Where(p => p.Resource == ctx.Resource && p.AllowedActions.HasFlag(ctx.Action))
            .ToList();

        if (!matchedPermissions.Any()) return false; // 根本没有该功能码的操作权

        long operatorMerchantId = userMatrix.BelongMerchantId ?? 0L;

        foreach (var perm in matchedPermissions)
        {
            //if (perm.AbacRule.IsSatisfiedBy(ctx.RequestAmount,ctx.ClientRealIp,ctx.TargetMerchantId,operatorMerchantId))
            //{
            //    return true; // 只要有一个权限放行，整体通过
            //}

        }

        return false;
    }
}