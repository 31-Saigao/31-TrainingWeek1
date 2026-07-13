using OrderHub.Core.Domain;

namespace OrderHub.Web.ViewModels;

public class CustomerListViewModel
{
    public IReadOnlyList<CustomerRowViewModel> Customers { get; set; } = Array.Empty<CustomerRowViewModel>();
}

public class CustomerRowViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CustomerTier Tier { get; set; }
    public DateTime CreatedAt { get; set; }
}
