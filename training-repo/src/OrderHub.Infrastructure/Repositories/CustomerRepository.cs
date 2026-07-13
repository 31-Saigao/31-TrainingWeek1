using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly OrderHubDbContext _db;

    public CustomerRepository(OrderHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync() =>
        await _db.Customers.OrderBy(c => c.Name).ToListAsync();

    public Task<Customer?> GetByIdAsync(int id) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Id == id);
}
