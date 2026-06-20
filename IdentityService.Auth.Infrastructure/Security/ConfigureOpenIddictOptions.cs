using IdentityService.Auth.Application.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using OpenIddict.Validation;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Infrastructure.Security
{
    

    public class ConfigureOpenIddictServerOptions :
        IConfigureOptions<OpenIddictServerOptions>,
        IConfigureOptions<OpenIddictValidationOptions>
    {
        private readonly IRsaKeyManager _keyManager;

        public ConfigureOpenIddictServerOptions(IRsaKeyManager keyManager)
        {
            _keyManager = keyManager;
        }
        /// <summary>
        /// Server 端配置 (用于签发和加密)
        /// </summary>
        /// <param name="options"></param>
        public void Configure(OpenIddictServerOptions options)
        {
            // 动态从磁盘扫描并加载所有密钥（活跃 + 历史）
            var (signingKeys, encryptionKeys) = _keyManager.LoadAllKeys();

           // 2. 映射并注入签名凭证（OpenIddict 对 RSA 默认使用 RsaSha256）
            foreach (var signingKey in signingKeys)
            {
                options.SigningCredentials.Add(new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256));
            }

            // 3. 映射并注入加密凭证（OpenIddict 对 RSA 默认使用 RsaOaep 包装与 Aes256CbcHmacSha512 加密）
            foreach (var encryptionKey in encryptionKeys)
            {
                options.EncryptionCredentials.Add(new EncryptingCredentials(
                    encryptionKey, 
                    SecurityAlgorithms.RsaOaepKeyWrap, 
                    SecurityAlgorithms.Aes256CbcHmacSha512));
            }
        }
        /// <summary>
        /// Validation 端配置 (用于验签和解密)
        /// </summary>
        /// <param name="options"></param>
        public void Configure(OpenIddictValidationOptions options)
        {
            var (signingKeys, encryptionKeys) = _keyManager.LoadAllKeys();

            // 针对验证端：签名密钥直接塞给 JwtBearer 的 TokenValidationParameters
            options.TokenValidationParameters.IssuerSigningKeys = signingKeys;

            // 针对验证端：解密密钥直接加入到 EncryptionCredentials 集合
            foreach (var encryptionKey in encryptionKeys)
            {
                options.EncryptionCredentials.Add(new EncryptingCredentials(
                    encryptionKey,
                    SecurityAlgorithms.RsaOaepKeyWrap,
                    SecurityAlgorithms.Aes256CbcHmacSha512));
            }
        }
    }
}
