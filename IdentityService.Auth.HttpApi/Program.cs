using IdentityService.Auth.Application.Authentication.Commands;
using IdentityService.Auth.Application.IRepositories;
using IdentityService.Auth.Infrastructure.DataBase;
using IdentityService.Auth.Infrastructure.Repositories;
using IdentityService.Auth.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using PaymentCenter.Auth.Application.Security;
using System.Diagnostics;
using System.Security.Cryptography;
var builder = WebApplication.CreateBuilder(args);


// 1. 数据库底座：注入 SQL Server 2022
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    // 注入 OpenIddict 专属的 EF 核心实体映射
    options.UseOpenIddict();
});


// 2. 【.NET 10 核心】注入一体化混合多级缓存 HybridCache

builder.Services.AddHybridCache(options =>
{
    // L1 (内存级) 默认强类型最大缓存时长
    options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(30),
        LocalCacheExpiration = TimeSpan.FromMinutes(5) // L1 内存级留存5分钟，防止高并发雪崩
    };
});

// 注入 Redis 作为 HybridCache 的底层 L2 分布式逃生舱
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:Configuration"];
});


// 3. 注入风控核心：二进制位运算与 ABAC 复合安全拦截器
// 严格遵循 SOLID 独立注入的仓储矩阵

builder.Services.AddScoped<ISysUserRepository, SysUserRepository>();
builder.Services.AddScoped<ISysRoleRepository, SysRoleRepository>();
builder.Services.AddScoped<ISysPermissionRepository, SysPermissionRepository>();
builder.Services.AddScoped<IClientApplicationRepository, ClientApplicationRepository>();
builder.Services.AddScoped<DistributedMerchantAbacGuard>();


// 4. 骨架组装：OpenIddict 核心认证服务器配置

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        // 配置 OpenIddict 使用我们自定义的 AuthDbContext 存储 Token 和客户端凭证
        options.UseEntityFrameworkCore().UseDbContext<AuthDbContext>();
    })
    .AddServer(options =>
    {
        // 显式激活密码流（Password Flow）与刷新 Token 流（Refresh Token）
        options.SetTokenEndpointUris("/api/connect/token");
        options.AllowPasswordFlow()
              .AllowClientCredentialsFlow() 
              .AllowRefreshTokenFlow();

       
        // 加载签名和加密密钥
        var (signingKeys, encryptionKeys) = KeyHelper.LoadKeys(builder.Configuration);

        // 从配置读取是否禁用加密（默认禁用，仅开发环境）
        var disableEncryption = builder.Configuration.GetValue<bool>("OpenIddict:DisableEncryption", true);
        if (disableEncryption)
        {
            options.DisableAccessTokenEncryption();
        }

        // 注册签名和加密密钥
        // 3. 循环注册所有签名密钥（第一个加入的会自动成为签发 Key，后续的仅用于验签）
        foreach (var signingKey in signingKeys)
        {
            options.AddSigningKey(signingKey);
        }

        // 4. 循环注册所有加密密钥（第一个加入的会自动成为加密 Key，后续的仅用于解密）
        foreach (var encryptionKey in encryptionKeys)
        {
            options.AddEncryptionKey(encryptionKey);
        }

        // 强迫 OIDC 服务接管 ASP.NET Core 的认证管线响应
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough();

        options.RegisterScopes(OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.Profile, OpenIddictConstants.Scopes.OfflineAccess);


        // 读取生命周期配置（从 appsettings 或环境变量）
        var accessTokenLifetime = builder.Configuration.GetValue<int>("OpenIddictSettings:AccessTokenLifetimeMinutes", 60);
        var refreshTokenLifetime = builder.Configuration.GetValue<int>("OpenIddictSettings:RefreshTokenLifetimeDays", 14);

        // 设置 Access Token 有效期（分钟）
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(accessTokenLifetime));
        // 设置 Refresh Token 有效期（天）
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(refreshTokenLifetime));


        // （可选）设置 Issuer，建议显式指定，避免从请求中自动推断导致不一致
        var issuer = builder.Configuration["OpenIddictSettings:Issuer"];
        if (!string.IsNullOrEmpty(issuer))
        {
            options.SetIssuer(new Uri(issuer));
        }



    }) 
    .AddValidation(options =>
    {
        // 允许本地微服务直接在进程内本地解析和验签 OpenIddict 下发的 Access Token
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();
builder.Services.AddControllers();
// 通过反射直接抓取整个 Application 程序集对象
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ExchangeTokenCommand).Assembly));





// 5. Swagger 联调与 OAuth2 锁扣配置

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "支付中心高密级核心鉴权网关", Version = "v1.0.0_Bitwise" });

    // 在 Swagger 中加入 Bearer Token 安全锁扣
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "请在下方直接粘贴由 OpenIddict 分发的 Access Token (不需要加 'Bearer ' 前缀)",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            // 将当前的 document 锚点泵入引用器，让 OpenAPI 2.0 引擎在运行时动态绑定
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});


// 6. 中间件管线路由建立（构建请求洋葱模型）
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PaymentCenter Auth API v1"));
}

app.UseHttpsRedirection();
app.UseRouting();

// 核心安全双中间件，顺序绝对不能颠倒
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();