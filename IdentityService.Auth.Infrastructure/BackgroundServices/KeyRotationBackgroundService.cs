using IdentityService.Auth.Application.Security;
using IdentityService.Auth.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Infrastructure.BackgroundServices
{
    public class KeyRotationBackgroundService 
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ReloadableOptions<OpenIddictServerOptions> _serverOptions;

        private readonly ILogger<KeyRotationBackgroundService> _logger;

        public KeyRotationBackgroundService(IServiceScopeFactory scopeFactory,  ReloadableOptions<OpenIddictServerOptions> serverOptions, ILogger<KeyRotationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _serverOptions = serverOptions;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {

            try
            {
                _logger.LogInformation("密钥轮转定时任务开始执行...");

                // 定时任务必须创建独立 Scope，避免单例生命周期问题
                using var scope = _scopeFactory.CreateScope();
                var keyRotationService = scope.ServiceProvider.GetRequiredService<IRsaKeyManager>();

                // 1. 轮转磁盘密钥
                await keyRotationService.RotateKeysAsync();
                // 2. 触发内存热加载原子替换
                _serverOptions.Reload();

                _logger.LogInformation("密钥轮转定时任务执行完成。");
            }
            catch (Exception ex)
            {
                // 仅做日志记录，定时任务异常不能导致服务崩溃
                Console.WriteLine($"密钥轮转定时任务执行失败: {ex.Message}");
            }


        }
    }
}
