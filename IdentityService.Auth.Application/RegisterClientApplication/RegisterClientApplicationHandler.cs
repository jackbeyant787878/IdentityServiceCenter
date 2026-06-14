using IdentityService.Auth.Application.IRepositories;
using IdentityService.Auth.Domain.Aggregates;
using MediatR;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Auth.Application.RegisterClientApplication
{
    public record RegisterClientApplicationCommand(string AppName, int ClientType, string? TenantId)
     : IRequest<ClientRegisterResultDto>;

    public class RegisterClientApplicationHandler : IRequestHandler<RegisterClientApplicationCommand, ClientRegisterResultDto>
    {
        private readonly IClientApplicationRepository _repository;

        public RegisterClientApplicationHandler(IClientApplicationRepository repository)
        {
            _repository = repository;
        }

        public async Task<ClientRegisterResultDto> Handle(RegisterClientApplicationCommand request, CancellationToken cancellationToken)
        {
            // 1. 动态生成符合安全规范的凭证
            // SaaS 应用可以用 app_ 前缀，内部微服务可以用 srv_ 前缀
            string prefix = request.ClientType == 1 ? "srv_" : "app_";
            string clientId = prefix + Guid.NewGuid().ToString("N")[..12];

            // 生成 32 位强随机明文密码供第一次返回
            string clientSecret = "secret_" + RandomNumberGenerator.GetHexString(32);

            // 2. 运用 DDD 领域行为构建聚合根
            ClientApplication app = request.ClientType == 1
                ? ClientApplication.CreateInternalService(request.AppName)
                : ClientApplication.CreateSaaSApp(clientId, request.AppName, request.TenantId!);

            // 3. 调用仓储写入数据库
            // 在仓储实现里，会调用 openIddictManager.CreateAsync() 最终插入到 OpenIddictApplications 表
            await _repository.AddAsync(app, clientSecret, cancellationToken);

            // 4. 返回明文凭证
            return new ClientRegisterResultDto(app.ClientId, clientSecret, app.DisplayName);
        }
    }

    public record ClientRegisterResultDto(string ClientId, string ClientSecret, string DisplayName);
}
