using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Application.Security
{
    public interface IRsaKeyManager
    {

        /// <summary>
        /// 初始化密钥：不存在则自动生成，存在则跳过
        /// </summary>
        Task EnsureInitializedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 手动轮转密钥：生成新活跃密钥，旧密钥归档到history
        /// </summary>
        Task RotateKeysAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有有效密钥（活跃+历史），直接供 OpenIddict 注册
        /// </summary>
        (List<RsaSecurityKey> SigningKeys, List<RsaSecurityKey> EncryptionKeys) LoadAllKeys();

    }
}
