using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }

    [Fact]
    public async Task GetLowStock_FiltersByThresholdAndOrdersByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-B001", stock: 8);
        TestSetup.AddProduct(db, sku: "SKU-B002", stock: 3);
        TestSetup.AddProduct(db, sku: "SKU-B003", stock: 20); // 高於門檻，不應出現

        var items = await service.GetLowStockAsync(10);

        Assert.Equal(2, items.Count);
        Assert.Equal("SKU-B002", items[0].Sku); // 庫存量升冪：3 在 8 前面
        Assert.Equal("SKU-B001", items[1].Sku);
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-C001", stock: 2, isActive: false);
        TestSetup.AddProduct(db, sku: "SKU-C002", stock: 2, isActive: true);

        var items = await service.GetLowStockAsync(10);

        Assert.Single(items);
        Assert.Equal("SKU-C002", items[0].Sku);
    }

    [Fact]
    public async Task GetLowStock_Sold30Days_ExcludesCancelledAndOldOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, sku: "SKU-D001", stock: 5);

        db.Orders.AddRange(
            new Order // 30 天內、未取消 -> 應計入
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Confirmed,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Items = { new OrderItem { ProductId = product.Id, Quantity = 4, UnitPriceSnapshot = product.UnitPrice } }
            },
            new Order // 30 天內、但已取消 -> 不應計入
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Cancelled,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                Items = { new OrderItem { ProductId = product.Id, Quantity = 100, UnitPriceSnapshot = product.UnitPrice } }
            },
            new Order // 超過 30 天 -> 不應計入
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Confirmed,
                CreatedAt = DateTime.UtcNow.AddDays(-40),
                Items = { new OrderItem { ProductId = product.Id, Quantity = 200, UnitPriceSnapshot = product.UnitPrice } }
            });
        db.SaveChanges();

        var items = await service.GetLowStockAsync(10);

        var row = Assert.Single(items);
        Assert.Equal(4, row.Sold30Days);
    }
}
