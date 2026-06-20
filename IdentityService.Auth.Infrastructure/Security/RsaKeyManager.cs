using IdentityService.Auth.Application.Security;
using IdentityService.Auth.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

public class RsaKeyManager : IRsaKeyManager
{
    private readonly string _keysBaseDir;
    private readonly object _lockObj = new();

    public RsaKeyManager(IConfiguration configuration)
    {
        var baseDir = configuration["KeySettings:BaseDirectory"] ?? "keys";
        _keysBaseDir = Path.IsPathRooted(baseDir)
            ? baseDir
            : Path.Combine(AppContext.BaseDirectory, baseDir);

        // 启动时确保根目录存在（递归创建所有父目录，Linux兼容）
        Directory.CreateDirectory(_keysBaseDir);
    }

    #region 1. 初始化：无密钥自动生成
    public Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        lock (_lockObj)
        {
            EnsureKeySetExists("signing");
            EnsureKeySetExists("encryption");
        }
        return Task.CompletedTask;
    }

    private void EnsureKeySetExists(string keyType)
    {
        // 全程使用 Path.Combine，自动适配 Linux / 分隔符
        var activeDir = Path.Combine(_keysBaseDir, keyType);
        var privateKeyPath = Path.Combine(activeDir, "private.pem");
        var publicKeyPath = Path.Combine(activeDir, "public.pem");
        var kidPath = Path.Combine(activeDir, "kid.txt");

        // 关键文件都存在则跳过
        if (File.Exists(privateKeyPath) && File.Exists(kidPath))
            return;

        try
        {
            // 递归创建目录，Linux下自动处理权限继承
            Directory.CreateDirectory(activeDir);

            // 生成 RSA 密钥对（2048位，跨平台标准算法）
            using var rsa = RSA.Create(2048);

            // 标准 PEM 格式导出，Linux openssl 完全兼容
            var privatePem = PemEncoding.Write("RSA PRIVATE KEY", rsa.ExportRSAPrivateKey());
            var publicPem = PemEncoding.Write("PUBLIC KEY", rsa.ExportSubjectPublicKeyInfo());

            // 统一 UTC 时间，避免时区差异导致 kid 混乱
            var kid = $"{keyType}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid()}";

            // 写入文件，使用 UTF-8 无 BOM 编码，Linux 原生兼容
            File.WriteAllText(privateKeyPath, privatePem, System.Text.Encoding.UTF8);
            File.WriteAllText(publicKeyPath, publicPem, System.Text.Encoding.UTF8);
            File.WriteAllText(kidPath, kid, System.Text.Encoding.UTF8);

            Console.WriteLine($"[KeyManager] {keyType} 密钥生成成功，Kid: {kid}，路径: {activeDir}");
        }
        catch (UnauthorizedAccessException ex)
        {
            // 专门捕获权限不足，给出明确排查提示
            throw new UnauthorizedAccessException(
                $"密钥目录写入权限不足，路径: {activeDir}。" +
                "请检查 PVC 挂载权限或容器运行用户是否有写入权限。", ex);
        }
    }
    #endregion

    #region 2. 密钥轮转：旧密钥归档，生成新密钥
    public Task RotateKeysAsync(CancellationToken cancellationToken = default)
    {
        lock (_lockObj)
        {
            RotateKeySet("signing");
            RotateKeySet("encryption");
        }
        return Task.CompletedTask;
    }

    private void RotateKeySet(string keyType)
    {
        var activeDir = Path.Combine(_keysBaseDir, keyType);
        var historyRootDir = Path.Combine(_keysBaseDir, "history", keyType);

        if (!Directory.Exists(activeDir) || !File.Exists(Path.Combine(activeDir, "private.pem")))
        {
            // 没有活跃密钥，直接生成新的
            EnsureKeySetExists(keyType);
            return;
        }

        // 1. 创建归档目录：按时间戳命名
        var archiveDirName = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var archiveDir = Path.Combine(historyRootDir, archiveDirName);
        Directory.CreateDirectory(archiveDir);

        // 2. 将当前活跃密钥移动到历史目录
        foreach (var fileName in new[] { "private.pem", "public.pem", "kid.txt" })
        {
            var src = Path.Combine(activeDir, fileName);
            if (File.Exists(src))
            {
                File.Move(src, Path.Combine(archiveDir, fileName));
            }
        }

        // 3. 生成全新活跃密钥
        EnsureKeySetExists(keyType);

        Console.WriteLine($"[{keyType}] 密钥轮转完成，旧密钥已归档到: {archiveDir}");
    }
    #endregion

    #region 3. 加载所有密钥
    public (List<RsaSecurityKey> SigningKeys, List<RsaSecurityKey> EncryptionKeys) LoadAllKeys()
    {
        return KeyHelper.LoadKeys(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["KeySettings:BaseDirectory"] = _keysBaseDir
            })
            .Build());
    }
    #endregion
}