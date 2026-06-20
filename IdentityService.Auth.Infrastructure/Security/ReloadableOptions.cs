using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Infrastructure.Security
{
    public class ReloadableOptions<TOptions> : IOptions<TOptions> where TOptions : class, new() 
    {
        private readonly IOptionsFactory<TOptions> _factory;
        private TOptions _value;
        private readonly object _lock = new();

        public ReloadableOptions(IOptionsFactory<TOptions> factory)
        {
            _factory = factory;
            // 首次启动时构建初始 Options
            _value = _factory.Create(Options.DefaultName);
        }

        public TOptions Value => _value;

        // 当密钥更新后，后台任务调用此方法触发重新加载
        public void Reload()
        {
            lock (_lock)
            {
                // 重新执行系统所有注册的 IConfigureOptions<TOptions> 逻辑
                _value = _factory.Create(Options.DefaultName);
            }
        }
    }
}
