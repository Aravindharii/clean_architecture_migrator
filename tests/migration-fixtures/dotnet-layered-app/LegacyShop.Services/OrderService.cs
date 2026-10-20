using LegacyShop.Data;
using LegacyShop.Models;

namespace LegacyShop.Services;

public class OrderService
{
    private readonly OrderRepository _repository;

    public OrderService(OrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Order>> GetRecentOrdersAsync()
    {
        return await _repository.GetAllOrdersAsync();
    }
}
