using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Infrastructure.Consul
{

    public static class ConsulRegistryExtensions
    {
        /// <summary>
        /// 优雅集成 Consul 自动注册与原生健康检查联动
        /// </summary>
        public static IServiceCollection AddConsulRegistry(this IServiceCollection services, IConfiguration configuration)
        {
            var registryAddress = configuration["ConsulConfig:RegistryAddress"];

            // 安全红线：本地开发环境未配置时，直接拦截返回，保持 DI 容器纯净
            if (string.IsNullOrEmpty(registryAddress))
            {
                return services;
            }

            // 1. 注册 官方 Consul 客户端
            services.AddSingleton<IConsulClient>(sp => new ConsulClient(config =>
            {
                config.Address = new Uri(registryAddress);
            }));

            // 2. 注册 托管在后台的注册与心跳守护服务
            services.AddHostedService<ConsulRegisterHostedService>();
            return services;
        }

        /// <summary>
        /// 内部纯净的守护进程：融合 ASP.NET Core 原生 HealthCheck 与 Consul TTL
        /// </summary>
        internal class ConsulRegisterHostedService : IHostedService
        {
            private readonly IConsulClient _consulClient;
            private readonly IConfiguration _configuration;
            private readonly HealthCheckService _healthCheckService; // 注入 .NET 原生健康检查服务
            private readonly ILogger<ConsulRegisterHostedService> _logger;
            private string? _serviceId;
            private Timer? _heartbeatTimer;

            public ConsulRegisterHostedService(IConsulClient consulClient,IConfiguration configuration,HealthCheckService healthCheckService,ILogger<ConsulRegisterHostedService> logger)
            {
                _consulClient = consulClient;
                _configuration = configuration;
                _healthCheckService = healthCheckService;
                _logger = logger;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                var serviceName = _configuration["ConsulConfig:ServiceName"] ?? "IdentityServiceCenter";
                var serviceIp = _configuration["ConsulConfig:ServiceIP"] ?? "auth-svc.identity.svc.cluster.local";
                var servicePort = int.Parse(_configuration["ConsulConfig:ServicePort"] ?? "5008");
                _serviceId = $"{serviceName}-{Environment.MachineName}";

                var registration = new AgentServiceRegistration()
                {
                    ID = _serviceId,
                    Name = serviceName,
                    Address = serviceIp,
                    Port = servicePort,
                    Check = new AgentServiceCheck()
                    {
                        TTL = TimeSpan.FromSeconds(15), // 15秒不报活则标记为故障
                        DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1) // 故障1分钟后自动剔除
                    },
                    Tags=new[] { "anonymous_allowed" }//打上“允许匿名访问”的特权标签
                };

                _logger.LogInformation($"[生产环境] 开启自动注册管线: {_serviceId} -> {serviceIp}:{servicePort}");
                await _consulClient.Agent.ServiceRegister(registration, cancellationToken);

                // 每 5 秒执行一次复合健康检查并报活
                _heartbeatTimer = new Timer(async _ => await ExecuteHealthCheckAndReportAsync(cancellationToken), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            }

            private async Task ExecuteHealthCheckAndReportAsync(CancellationToken cancellationToken)
            {
                try
                {
                    // 核心优雅点：调用 .NET 内部所有已注册的检查项（如数据库连接、Redis 状态等）
                    var healthReport = await _healthCheckService.CheckHealthAsync(cancellationToken);

                    if (healthReport.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
                    {
                        // 内部组件全健康，向 Consul 宣告 Pass
                        await _consulClient.Agent.PassTTL($"service:{_serviceId}", "All internal micro-checks passed.", cancellationToken);
                    }
                    else
                    {
                        // 内部组件有沦陷（如数据库断开），主动向 Consul 投毒，阻止流量进来
                        var failedReasons = string.Join(", ", healthReport.Entries.Where(e => e.Value.Status != Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy).Select(e => e.Key));
                        _logger.LogWarning($"[健康度预警] 内部组件异常: {failedReasons}，正在向 Consul 报告故障状态。");

                        await _consulClient.Agent.FailTTL($"service:{_serviceId}", $"Internal micro-checks failed: {failedReasons}", cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "执行 Consul 健康检查或心跳上报时发生异常");
                }
            }

            public async Task StopAsync(CancellationToken cancellationToken)
            {
                if (_heartbeatTimer != null) await _heartbeatTimer.DisposeAsync();

                if (!string.IsNullOrEmpty(_serviceId))
                {
                    _logger.LogInformation($"正在优雅注销 Consul 服务节点: {_serviceId}");
                    try
                    {
                        await _consulClient.Agent.ServiceDeregister(_serviceId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "从 Consul 注销服务失败");
                    }
                }
            }
        }

    }
}
