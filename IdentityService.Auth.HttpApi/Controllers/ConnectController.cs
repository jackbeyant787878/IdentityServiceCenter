using IdentityService.Auth.Application.Authentication.Commands;
using IdentityService.Auth.Application.Authentication.Queries;
using IdentityService.Auth.Application.IRepositories;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;

namespace IdentityService.Auth.HttpApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectController : ControllerBase
    {
      
        private readonly IMediator _mediator;

        public ConnectController(IMediator mediator)
        {
            _mediator = mediator;

        }

        /// <summary>
        /// 授权码模式端点：用于商户管理员/收银员在 Web 浏览器端扫码或表单登录
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [HttpPost("token"), Produces("application/json")]
        [Consumes("application/x-www-form-urlencoded")] //强校验必须为标准的 OIDC 表单提交
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("当前非 OIDC 规范的合法认证请求");
            var result = await _mediator.Send(new ExchangeTokenCommand(request));
            if (result.IsSuccess) return SignIn(result.Principal,OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (result.ErrorType == "Forbid") return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            return BadRequest(new { Error = result.ErrorCode, Error_Description = result.ErrorDescription });

        }


        /// <summary>
        /// 标准 UserInfo 端点：支付网关或下游服务持 Access Token 换取高密级明细
        /// </summary>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("userinfo"), HttpPost("userinfo")]
        [Produces("application/json")]
        public async Task<IActionResult> UserInfo()
        {
            // 此时 Access Token 已被 OpenIddict 自动解包并注入进 User.Identity
            var userIdStr = User.GetClaim(OpenIddictConstants.Claims.Subject);
            if (string.IsNullOrEmpty(userIdStr)) return Challenge();


            var user = await _mediator.Send(new GetUserInfoQuery(Guid.Parse(userIdStr)));
            if (user == null || !user.isActive)
            {
                return BadRequest(new { error = "invalid_user", description = "操作员不存在或已被风控冻结" });
            }

            // 返回最纯净、无任何冗余的网关标准 Claims 字典
            return Ok(new
            {
                sub = user.userId,
                username = user.userName,
                user_type = user.userType.ToString(),
                merchant_id = user.merchantId,
                store_id = user.storeId,
                status = "Active"
            });
        }

        /// <summary>
        /// 3. 令牌撤销端点 (Revocation)：支付风控系统的“紧急红色按钮”
        /// 当商户操作员密钥泄露、终端被盗或商户欠款清退时，清算网关通过此接口直接宣告 Token 失效
        /// </summary>
        /// <returns></returns>
        [HttpPost("revoke")]
        [Produces("application/json")]
        public async Task<IActionResult> Revoke()
        {
            // 该端点由 OpenIddict 核心引擎全权承接状态机
            // 它会接收 token, token_type_hint (access_token/refresh_token) 并将其从分布式缓存/DB中直接抹去
            return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// 4. 安全登出端点 (Logout / EndSession)
        /// </summary>
        /// <returns></returns>
        [HttpGet("logout"), HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // 1. 擦除本地 Cookie 状态机
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. 触发 OIDC 联动通知，通知 OpenIddict 销毁单点登录主凭证
            return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }


    }
}
