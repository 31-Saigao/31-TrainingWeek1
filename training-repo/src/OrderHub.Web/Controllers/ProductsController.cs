using Microsoft.AspNetCore.Mvc;
using OrderHub.Core.Services;
using OrderHub.Web.ViewModels;

namespace OrderHub.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllAsync();

        var vm = new ProductListViewModel
        {
            Products = products.Select(p => new ProductRowViewModel
            {
                Sku = p.Sku,
                Name = p.Name,
                UnitPrice = p.UnitPrice,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> LowStock(int? threshold)
    {
        var vm = new LowStockViewModel { Threshold = threshold ?? 10 };

        if (!TryValidateModel(vm))
            return View(vm);

        var items = await _productService.GetLowStockAsync(vm.Threshold);

        vm.Items = items
            .Select(p => new LowStockRowViewModel
            {
                Sku = p.Sku,
                Name = p.Name,
                StockQuantity = p.StockQuantity,
                Sold30Days = p.Sold30Days
            })
            .ToList();

        return View(vm);
    }
}

