using IdentityService.Auth.Application.Security;
using IdentityService.Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/key-management")]
public class KeyManagementController : ControllerBase
{
    private readonly IRsaKeyManager _keyManager;

    public KeyManagementController(IRsaKeyManager keyManager)
    {
        _keyManager = keyManager;
    }

    /// <summary>
    /// 手动触发密钥轮转
    /// </summary>
    [HttpPost("rotate")]
    public async Task<IActionResult> Rotate()
    {
        await _keyManager.RotateKeysAsync();

        // 轮转后需要重启服务才能加载新密钥（测试环境可接受，生产建议热重载）
        return Ok(new
        {
            Message = "密钥轮转完成，请重启服务生效",
            Tip = "重启后旧密钥自动进入历史兼容列表"
        });
    }
}