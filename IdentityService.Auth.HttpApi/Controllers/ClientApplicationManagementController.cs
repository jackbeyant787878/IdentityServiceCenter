using IdentityService.Auth.Application.RegisterClientApplication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Auth.HttpApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //线上环境必须加严格的权限控制：只有系统超级管理员或内部受信任凭证才能调用
    //[Authorize(Policy = "PlatformAdminPolicy")]
    public class ClientApplicationManagementController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ClientApplicationManagementController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// 独立接口：供 SaaS 商户或系统管理员 注册新应用
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterApp([FromBody] RegisterAppRequest request)
        {
            // 1. 转交应用层 Command 处理
            var command = new RegisterClientApplicationCommand(
                request.AppName,
                request.ClientType,
                request.TenantId // 如果是内部微服务，此项传 null 或 "SYSTEM"
            );

            var result = await _mediator.Send(command);

            // 2. 返回生成的凭证给前端（Secret 只有这一次展示机会）
            return Ok(result);
        }
    }

    // 接收前端或运维脚本的 DTO
    public record RegisterAppRequest(string AppName, int ClientType, string? TenantId);
}
