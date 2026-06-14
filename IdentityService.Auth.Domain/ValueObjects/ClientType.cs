using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Domain.ValueObjects
{
    public enum ClientType
    {
        /// <summary>
        /// 内部微服务（高信任，走 Client Credentials 模式）
        /// </summary>
        InternalMicroservice = 1,

        /// <summary>
        /// SaaS 商户自建应用（走 Authorization Code + PKCE 或 Password 模式）
        /// </summary>
        SaaSMerchantApp = 2,

        /// <summary>
        /// 三方生态合作伙伴（走授权码模式，需严格限流与审计）
        /// </summary>
        ThirdPartyPartner = 3
    }
}
