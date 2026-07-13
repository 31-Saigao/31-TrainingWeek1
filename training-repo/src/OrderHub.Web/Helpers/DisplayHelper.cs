using OrderHub.Core.Domain;

namespace OrderHub.Web.Helpers;

public static class DisplayHelper
{
    public static string StatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "待處理",
        OrderStatus.Confirmed => "已確認",
        OrderStatus.Shipped => "已出貨",
        OrderStatus.Cancelled => "已取消",
        _ => status.ToString()
    };

    public static string StatusBadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-warning text-dark",
        OrderStatus.Confirmed => "bg-primary",
        OrderStatus.Shipped => "bg-success",
        OrderStatus.Cancelled => "bg-secondary",
        _ => "bg-light text-dark"
    };

    public static string TierLabel(CustomerTier tier) => tier switch
    {
        CustomerTier.Standard => "一般會員",
        CustomerTier.Silver => "銀卡會員",
        CustomerTier.Gold => "金卡會員",
        _ => tier.ToString()
    };

    public static string TierBadgeClass(CustomerTier tier) => tier switch
    {
        CustomerTier.Gold => "bg-warning text-dark",
        CustomerTier.Silver => "bg-secondary",
        _ => "bg-light text-dark border"
    };

    public static string Money(decimal amount) => $"NT$ {amount:N2}";

    public static string LocalTime(DateTime utc) => utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}
