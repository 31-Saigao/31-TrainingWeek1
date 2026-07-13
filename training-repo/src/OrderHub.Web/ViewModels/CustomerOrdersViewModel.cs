using OrderHub.Core.Domain;

namespace OrderHub.Web.ViewModels;

public class CustomerOrdersViewModel
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public CustomerTier Tier { get; set; }
    public IReadOnlyList<OrderRowViewModel> Orders { get; set; } = Array.Empty<OrderRowViewModel>();
}
