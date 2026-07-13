using OrderHub.Core.Domain;

namespace OrderHub.Core.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
}
