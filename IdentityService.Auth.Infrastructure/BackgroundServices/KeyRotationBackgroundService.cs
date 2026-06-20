using IdentityService.Auth.Application.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Infrastructure.BackgroundServices
{
    public class KeyRotationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _checkInterval;

        public KeyRotationBackgroundService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            // 检查频率：每天检查一次是否需要轮转
            var checkDays = int.Parse(configuration["KeySettings:CheckIntervalDays"] ?? "1");
            _checkInterval = TimeSpan.FromDays(checkDays);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 启动延迟：服务启动 5 分钟后执行首次检查
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 定时任务必须创建独立 Scope，避免单例生命周期问题
                    using var scope = _scopeFactory.CreateScope();
                    var keyRotationService = scope.ServiceProvider.GetRequiredService<IRsaKeyManager>();

                    await keyRotationService.RotateKeysAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // 仅做日志记录，定时任务异常不能导致服务崩溃
                    Console.WriteLine($"密钥轮转定时任务执行失败: {ex.Message}");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
