using IdentityService.Auth.Application.IRepositories;
using MediatR;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace IdentityService.Auth.Application.Authentication.Commands
{
    public class ExchangeTokenCommandHandler(ISysUserRepository userRepository): IRequestHandler<ExchangeTokenCommand, ExchangeTokenResult>
    {
      
        public async Task<ExchangeTokenResult> Handle(ExchangeTokenCommand request, CancellationToken cancellationToken)
        {
            // 从 HttpContext 中提取 OpenIddict 标准请求对象
            var oidcRequest = request.HttpContext
                ?? throw new InvalidOperationException("当前非 OIDC 规范的合法认证请求");

            // 1. 分流处理：账号密码登录模式 (Password Grant)
            if (oidcRequest.IsPasswordGrantType())
            {
                return await HandlePasswordGrantAsync(oidcRequest);
            }

            // 2. 分流处理：机器对机器/服务间调用模式 (Client Credentials)
            if (oidcRequest.IsClientCredentialsGrantType())
            {
                return await HandleClientCredentialsGrantAsync(oidcRequest);
            }

            return ExchangeTokenResult.BadRequest("unsupported_grant_type", "不支持的 Grant Type");
        }

        /// <summary>
        /// 处理密码模式：注入商户隔离专属特种 Claims
        /// </summary>
        private async Task<ExchangeTokenResult> HandlePasswordGrantAsync(OpenIddictRequest oidcRequest)
        {
            var user = await userRepository.GetByUserNameAsync(oidcRequest.Username!);
            if (user == null || user.PasswordHash != oidcRequest.Password) // 生产环境请使用 PasswordHasher 验证
            {
                return ExchangeTokenResult.Forbid();
            }

            var identity = new ClaimsIdentity("OpenIddict.Server.AspNetCore");
            identity.AddClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
            identity.AddClaim(OpenIddictConstants.Claims.Username, user.Username);


            var merchantId=  user.BelongMerchantId;

            // 将租户ID 注入 Token 声明，全网微服务据此做多租户数据隔离
            identity.AddClaim("tenant_id", merchantId.ToString());
            identity.AddClaim("client_type", "saas_user");

            // 注入商户隔离及网闸特种 Claims
            identity.AddClaim("belong_merchant_id", merchantId.ToString());
            identity.AddClaim("belong_store_id", user.BelongStoreId.ToString());
            identity.AddClaim("user_partition_type", ((int)user.UserType).ToString());

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(new[] { OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.Profile,OpenIddictConstants.Scopes.OfflineAccess });

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
            }

            return ExchangeTokenResult.Success(principal);
        }

        /// <summary>
        /// 处理凭证模式：根据调用者身份判定风控与租户隔离级别
        /// </summary>
        private async Task<ExchangeTokenResult> HandleClientCredentialsGrantAsync(OpenIddictRequest oidcRequest)
        {
            var clientId = oidcRequest.ClientId!;
            var identity = new ClaimsIdentity("OpenIddict.Server.AspNetCore");

            identity.AddClaim(OpenIddictConstants.Claims.Subject, clientId);
            identity.AddClaim("client_id", clientId);
            identity.AddClaim("client_type", "microservice");

            // --- 核心支付风控设计逻辑 ---
            if (clientId.StartsWith("internal-"))
            {
                // 场景 A：内部核心微服务（放开多租户隔离网闸）
                identity.AddClaim("user_partition_type", "InternalSystem");
                identity.AddClaim("merchant_isolation_bypass", "true");
            }
            else if (clientId.StartsWith("mch-openapi-"))
            {
                // 场景 B：外部商户直连网关（强制绑定多租户隔离标签）
                // 实际生产中此处应查库，这里保持你的业务原型
                long boundMerchantId = 10086;

                identity.AddClaim("user_partition_type", "ExternalOpenApi");
                identity.AddClaim("belong_merchant_id", boundMerchantId.ToString());
            }

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(oidcRequest.GetScopes());

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
            }

            return ExchangeTokenResult.Success(principal);
        }
    }
}
