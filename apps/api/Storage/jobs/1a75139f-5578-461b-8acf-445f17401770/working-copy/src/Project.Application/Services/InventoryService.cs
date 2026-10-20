using Project.Application.Common.Dtos;
using Project.Application.Common.Exceptions;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Project.Application.Authentication;

namespace Project.Application.Services;

/// <summary>
/// Provides business operations related to inventory.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;


    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="InventoryService"/> class.
    /// </summary>
    /// <param name="inventoryRepository">
    /// Repository used to access inventory data.
    /// </param>
    public InventoryService(
        IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }


    /// <summary>
    /// Retrieves inventory information by book identifier.
    /// </summary>
    /// <param name="bookId">
    /// Book identifier.
    /// </param>
    /// <returns>
    /// Inventory availability details.
    /// </returns>
    public async Task<InventoryReadDto> GetByBookIdAsync(
        int bookId)
    {
        var inventory =
            await _inventoryRepository.GetByBookIdAsync(bookId);

        if (inventory == null)
        {
            throw new NotFoundException(
                $"Inventory for book ID '{bookId}' was not found.");
        }

        return MapToDto(inventory);
    }


    /// <summary>
    /// Retrieves inventory information by ISBN.
    /// </summary>
    /// <param name="isbn">
    /// Book ISBN.
    /// </param>
    /// <returns>
    /// Inventory availability details.
    /// </returns>
    public async Task<InventoryReadDto> GetByIsbnAsync(
        string isbn)
    {
        var inventory =
            await _inventoryRepository.GetByIsbnAsync(isbn);

        if (inventory == null)
        {
            throw new NotFoundException(
                $"Inventory for ISBN '{isbn}' was not found.");
        }

        return MapToDto(inventory);
    }


    /// <summary>
    /// Maps inventory entity to read DTO.
    /// </summary>
    private static InventoryReadDto MapToDto(
        Inventory inventory)
    {
        return new InventoryReadDto
        {
            BookId = inventory.BookId,
            Title = inventory.Book.Title,
            ISBN = inventory.Book.ISBN,

            TotalCopies = inventory.TotalCopies,

            AvailableCopies = inventory.AvailableCopies,

            LoanedCopies =
                inventory.TotalCopies -
                inventory.AvailableCopies
        };
    }
}