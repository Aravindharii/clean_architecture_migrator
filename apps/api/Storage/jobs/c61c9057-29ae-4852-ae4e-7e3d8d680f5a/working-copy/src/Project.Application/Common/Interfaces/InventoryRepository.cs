using Project.Domain.Entities;
using Project.Application.Common.Exceptions;
using Project.Application.Validators;
using Project.Application.Common.Dtos;

namespace Project.Application.Common.Interfaces;

public interface InventoryRepository
{
    Task<Inventory?> GetByBookIdAsync(
        int bookId);
    Task<Inventory?> GetByIsbnAsync(
        string isbn);
}
