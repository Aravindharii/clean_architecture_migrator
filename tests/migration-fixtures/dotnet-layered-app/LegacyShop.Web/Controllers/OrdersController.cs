using LegacyShop.Data;
using LegacyShop.Models;
using LegacyShop.Services;
using Microsoft.AspNetCore.Mvc;

namespace LegacyShop.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderRepository _repository; // ARCHITECTURAL VIOLATION: Direct coupling to concrete repository
    private readonly OrderService _orderService;

    public OrdersController(OrderRepository repository, OrderService orderService)
    {
        _repository = repository;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await _repository.GetAllOrdersAsync();
        return Ok(orders);
    }
}
