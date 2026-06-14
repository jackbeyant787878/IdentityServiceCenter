using IdentityService.Auth.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Domain.Aggregates
{
    public class ClientApplication
    {
        public string Id { get; private set; } = Guid.NewGuid().ToString("N");
        public string ClientId { get; private set; }
        public string DisplayName { get; private set; }
        public ClientType ClientType { get; private set; }

        // DDD 核心：SaaS 模式下的租户隔离标识（内部微服务此项为 Null）
        public string? TenantId { get; private set; }
        public bool IsActive { get; private set; }

        // 隐藏构造函数，强迫通过工厂或领域行为创建
        private ClientApplication() { }

        // 领域行为：创建内部微服务凭证
        public static ClientApplication CreateInternalService(string serviceName)
        {
            return new ClientApplication
            {
                ClientId = $"srv-{serviceName.ToLower()}",
                DisplayName = $"内部微服务-{serviceName}",
                ClientType = ClientType.InternalMicroservice,
                IsActive = true
            };
        }

        // 领域行为：创建 SaaS 商户应用
        public static ClientApplication CreateSaaSApp(string clientId, string appName, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("SaaS应用必须绑定租户ID");

            return new ClientApplication
            {
                ClientId = clientId,
                DisplayName = appName,
                ClientType = ClientType.SaaSMerchantApp,
                TenantId = tenantId,
                IsActive = true
            };
        }
    }
}
