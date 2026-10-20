using LibraryManagement.API.DTOs.Inventory;
using LibraryManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LibraryManagement.API.Controllers;

/// <summary>
/// Provides API endpoints for inventory availability.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;


    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="InventoryController"/> class.
    /// </summary>
    /// <param name="inventoryService">
    /// Service used to access inventory information.
    /// </param>
    public InventoryController(
        IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }


    /// <summary>
    /// Retrieves inventory information by book identifier.
    /// </summary>
    /// <param name="bookId">
    /// Book identifier.
    /// </param>
    /// <returns>
    /// Availability information for the book.
    /// </returns>
    [HttpGet("book/{bookId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryReadDto>> GetByBookId(
        int bookId)
    {
        var inventory =
            await _inventoryService.GetByBookIdAsync(bookId);

        return Ok(inventory);
    }


    /// <summary>
    /// Retrieves inventory information by ISBN.
    /// </summary>
    /// <param name="isbn">
    /// Book ISBN.
    /// </param>
    /// <returns>
    /// Availability information for the book.
    /// </returns>
    [HttpGet("isbn/{isbn}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryReadDto>> GetByIsbn(
        string isbn)
    {
        var inventory =
            await _inventoryService.GetByIsbnAsync(isbn);

        return Ok(inventory);
    }
}