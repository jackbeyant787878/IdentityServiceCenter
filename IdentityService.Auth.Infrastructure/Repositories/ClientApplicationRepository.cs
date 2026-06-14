using IdentityService.Auth.Application.IRepositories;
using IdentityService.Auth.Domain.Aggregates;
using IdentityService.Auth.Domain.ValueObjects;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace IdentityService.Auth.Infrastructure.Repositories
{
    public class ClientApplicationRepository : IClientApplicationRepository
    {
        private readonly IOpenIddictApplicationManager _applicationManager;

        public ClientApplicationRepository(IOpenIddictApplicationManager applicationManager)
        {
            _applicationManager = applicationManager;
        }

        public async Task AddAsync(ClientApplication app, string cleanSecret, CancellationToken cancellationToken)
        {

            var tenantJsonElement = JsonSerializer.SerializeToElement(app.TenantId ?? string.Empty);

            // 组装 OpenIddict 要求的标准数据描述符
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = app.ClientId,
                ClientSecret = cleanSecret, // 传入明文，OpenIddict 内部会自动对其进行 Hash 加密后入库
                DisplayName = app.DisplayName
            };

            descriptor.Properties.Add("TenantId", tenantJsonElement);


            // 根据不同的客户端类型，动态分配向 OpenIddictApplications 表插入时的权限字段 (Permissions)
            if (app.ClientType == ClientType.InternalMicroservice)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "internal_api");
            }
            else
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken); 
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "openid");
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "profile");
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access"); 
            }

            // 核心：执行此方法后，数据就正式 INSERT 到 OpenIddictApplications 表中了
            await _applicationManager.CreateAsync(descriptor, cancellationToken);
        }
    }
}
