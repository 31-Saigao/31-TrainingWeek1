using OrderHub.Core.Domain;

namespace OrderHub.Web.ViewModels;

public class OrderDetailsViewModel
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public CustomerTier CustomerTier { get; set; }
    public int CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public IReadOnlyList<OrderItemRowViewModel> Items { get; set; } = Array.Empty<OrderItemRowViewModel>();

    public decimal Subtotal { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    public bool CanCancel => Status is OrderStatus.Pending or OrderStatus.Confirmed;
}

public class OrderItemRowViewModel
{
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
