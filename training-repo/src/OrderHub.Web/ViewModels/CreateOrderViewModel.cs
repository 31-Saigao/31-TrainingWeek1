using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OrderHub.Web.ViewModels;

public class CreateOrderViewModel
{
    [Required(ErrorMessage = "請選擇客戶")]
    [Display(Name = "客戶")]
    public int? CustomerId { get; set; }

    [MinLength(1, ErrorMessage = "訂單至少需要一項商品")]
    public List<CreateOrderLineViewModel> Lines { get; set; } = new();

    public IReadOnlyList<SelectListItem> CustomerOptions { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> ProductOptions { get; set; } = Array.Empty<SelectListItem>();
}

public class CreateOrderLineViewModel
{
    [Required(ErrorMessage = "請選擇商品")]
    [Display(Name = "商品")]
    public int? ProductId { get; set; }

    [Range(1, 999, ErrorMessage = "數量需介於 1 到 999")]
    [Display(Name = "數量")]
    public int Quantity { get; set; } = 1;
}
