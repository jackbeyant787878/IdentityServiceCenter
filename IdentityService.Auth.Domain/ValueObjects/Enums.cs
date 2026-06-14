using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Domain.ValueObjects
{
    /// <summary>
    /// 严密的用户分区类型：彻底划分平台最高监管权、商户核心管理权与门店基层操作权
    /// </summary>
    public enum UserType
    {
        PlatformSuperAdmin = 1,  // 平台超级管理员（跨商户掌控全网清算、通道分拨与洗钱风控）
        PlatformAuditor = 2,     // 平台风控合规审计员（仅有跨商户只读对账权限）
        MerchantAdmin = 3,       // 商户集团CFO/管理员（掌控本商户集团下属全门店、结算账户）
        MerchantOperator = 4     // 商户基层收银员/店长（仅限本门店日常高频收银与小额退款）
    }

    /// <summary>
    /// 纵向数据切割范围（结合支付业务的数据可见性防线）
    /// </summary>
    public enum DataScope
    {
        PlatformGlobal = 1,      // 跨商户全网透视
        MerchantLevel = 2,       // 商户集团级（可见该商户下所有门店流水）
        StoreLevel = 3,          // 单门店级（仅可见自身所在的物理门店）
        TerminalLevel = 4        // 单终端级（最低收银员，只能看自己手上那台设备）
    }




    [Flags]
    public enum PayActions : long
    {
        None = 0,

        // 1. 基础读写权 (1, 2, 4, 8)
        Query = 1 << 0,          // 1   - 只读查询（流水分看、对账单下载）
        CreateOrder = 1 << 1,    // 2   - 正向交易下单权
        RefundApply = 1 << 2,    // 4   - 提交退款申请权
        ExportData = 1 << 3,     // 8   - 敏感金融数据导出权

        // 2. 核心敏感风控权 (16, 32, 64)
        ModifyConfig = 1 << 4,   // 16  - 通道参数/支付密钥修改权（高危）
        AuditTier1 = 1 << 5,     // 32  - 门店级小额退款自动/人工初审权
        AuditTier2 = 1 << 6,     // 64  - 集团级大额无上限资金退款终审权
        RiskRelease = 1 << 7,    // 128 - 风控黑名单/商户解冻挂起权

        // 3. 组合权快捷定义（位或运算）
        StandardCashier = Query | CreateOrder | RefundApply, // 1 + 2 + 4 = 7 (标准收银员)
        MerchantCFO = Query | RefundApply | AuditTier1 | AuditTier2 // 集团财务核心
    }

}
