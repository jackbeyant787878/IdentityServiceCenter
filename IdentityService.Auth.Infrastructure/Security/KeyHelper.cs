using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Auth.Infrastructure.Security
{
    public static class KeyHelper
    {
        public static (RsaSecurityKey SigningKey, RsaSecurityKey EncryptionKey) LoadKeys(IConfiguration configuration)
        {
            var keySettings = configuration.GetSection("KeySettings");
            var baseDirSetting = keySettings["BaseDirectory"] ?? "keys";

            string ResolvePath(string path)
            {
                if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Key path not configured.");
                if (Path.IsPathRooted(path)) return path;
                return Path.Combine(AppContext.BaseDirectory, path);
            }

            var keysBaseDir = ResolvePath(baseDirSetting);

            // 加载签名
            var signingSection = keySettings.GetSection("Signing");
            var signingPrivatePath = Path.Combine(keysBaseDir, signingSection["PrivateKeyFile"] ?? "signing/private.pem");
            var signingKidPath = Path.Combine(keysBaseDir, signingSection["KeyIdFile"] ?? "signing/kid.txt");

            if (!File.Exists(signingPrivatePath))
                throw new FileNotFoundException($"Signing private key not found: {signingPrivatePath}");
            if (!File.Exists(signingKidPath))
                throw new FileNotFoundException($"Signing KeyId file not found: {signingKidPath}");

            var signingRsa = RSA.Create();
            signingRsa.ImportFromPem(File.ReadAllText(signingPrivatePath));
            var signingKey = new RsaSecurityKey(signingRsa)
            {
                KeyId = File.ReadAllText(signingKidPath).Trim()
            };

            // 加载加密
            var encryptionSection = keySettings.GetSection("Encryption");
            var encryptionPrivatePath = Path.Combine(keysBaseDir, encryptionSection["PrivateKeyFile"] ?? "encryption/private.pem");
            var encryptionKidPath = Path.Combine(keysBaseDir, encryptionSection["KeyIdFile"] ?? "encryption/kid.txt");

            if (!File.Exists(encryptionPrivatePath))
                throw new FileNotFoundException($"Encryption private key not found: {encryptionPrivatePath}");
            if (!File.Exists(encryptionKidPath))
                throw new FileNotFoundException($"Encryption KeyId file not found: {encryptionKidPath}");

            var encryptionRsa = RSA.Create();
            encryptionRsa.ImportFromPem(File.ReadAllText(encryptionPrivatePath));
            var encryptionKey = new RsaSecurityKey(encryptionRsa)
            {
                KeyId = File.ReadAllText(encryptionKidPath).Trim()
            };

            return (signingKey, encryptionKey);
        }


    }
}
