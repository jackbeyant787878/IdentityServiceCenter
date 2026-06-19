using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace IdentityService.Auth.Infrastructure.Security
{
    public static class KeyHelper
    {
        /// <summary>
        /// 加载签名密钥和加密密钥，支持活跃 Key + 历史兼容 Key 的链式加载
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static (List<RsaSecurityKey> SigningKeys, List<RsaSecurityKey> EncryptionKeys) LoadKeys(IConfiguration configuration)
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

            // 分别加载签名密钥池与加密密钥池
            var signingKeys = LoadKeyCollection(keysBaseDir, keySettings.GetSection("Signing"), "signing");
            var encryptionKeys = LoadKeyCollection(keysBaseDir, keySettings.GetSection("Encryption"), "encryption");

            return (signingKeys, encryptionKeys);
        }

        /// <summary>
        /// 核心链式加载器：加载1个活跃Key + N个历史兼容Key
        /// </summary>
        private static List<RsaSecurityKey> LoadKeyCollection(string keysBaseDir, IConfigurationSection section, string defaultFolder)
        {
            var keyList = new List<RsaSecurityKey>();

            // ---------------- 1️⃣加载当前活跃的主密钥 ----------------
            var privateKeyFile = section["PrivateKeyFile"] ?? $"{defaultFolder}/private.pem";
            var kidFile = section["KeyIdFile"] ?? $"{defaultFolder}/kid.txt";

            var activePrivatePath = Path.Combine(keysBaseDir, privateKeyFile);
            var activeKidPath = Path.Combine(keysBaseDir, kidFile);

            if (!File.Exists(activePrivatePath))
                throw new FileNotFoundException($"致命错误: 活跃 {defaultFolder} 私钥文件丢失: {activePrivatePath}");
            if (!File.Exists(activeKidPath))
                throw new FileNotFoundException($"致命错误: 活跃 {defaultFolder} KeyId文件丢失: {activeKidPath}");

            var activeRsa = RSA.Create();
            activeRsa.ImportFromPem(File.ReadAllText(activePrivatePath));

            // 默认第一个装载的为 Active Key
            keyList.Add(new RsaSecurityKey(activeRsa)
            {
                KeyId = File.ReadAllText(activeKidPath).Trim()
            });


            // ---------------- 2️⃣ 自动、安全地扫描【历史】兼容密钥 ----------------
            // 基于当前活跃密钥所在目录，向下寻找 "history" 文件夹
            var activeDir = Path.GetDirectoryName(activePrivatePath);
            if (activeDir != null)
            {
                var historyDir = Path.Combine(activeDir, "history");

                if (Directory.Exists(historyDir))
                {
                    // 遍历 history 下的所有子文件夹（例如：v1, v2 等）
                    foreach (var subDir in Directory.GetDirectories(historyDir))
                    {
                        try
                        {
                            // 历史目录中通常固定使用 private.pem 和 kid.txt
                            var historyPrivatePath = Path.Combine(subDir, "private.pem");
                            var historyKidPath = Path.Combine(subDir, "kid.txt");

                            if (File.Exists(historyPrivatePath) && File.Exists(historyKidPath))
                            {
                                var historyRsa = RSA.Create();
                                historyRsa.ImportFromPem(File.ReadAllText(historyPrivatePath));

                                keyList.Add(new RsaSecurityKey(historyRsa)
                                {
                                    KeyId = File.ReadAllText(historyKidPath).Trim()
                                });

                                Console.WriteLine($"成功兼容历史旧 {defaultFolder} Key. Kid: {File.ReadAllText(historyKidPath).Trim()}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // 🧱 防御性容错：历史密钥损坏绝对不能导致服务启动崩溃
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"历史密钥加载跳过，目录 [{subDir}] 解析失败: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                }
            }

            return keyList;
        }
    }
}