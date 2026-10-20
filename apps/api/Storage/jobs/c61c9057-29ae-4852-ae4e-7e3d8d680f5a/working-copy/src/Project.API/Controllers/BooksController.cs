using Project.Application.Common.Dtos;
using Project.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Project.Application.Common.Interfaces;
using Project.Application.Authentication;

namespace Project.API.Controllers;

/// <summary>
/// Provides API endpoints for managing books.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

 /// <summary>
/// Retrieves a paginated list of books from the catalogue.
/// Supports optional search, filtering, sorting, and pagination.
/// </summary>
/// <param name="q">
/// Optional search keyword.
/// </param>
/// <param name="categoryId">
/// Optional category identifier used to filter books.
/// </param>
/// <param name="language">
/// Optional language filter.
/// </param>
/// <param name="publishedYear">
/// Optional published year filter.
/// </param>
/// <param name="available">
/// Optional availability filter.
/// Set to <c>true</c> to return books with available copies.
/// Set to <c>false</c> to return books with no available copies.
/// </param>
/// <param name="page">
/// Page number. Default is 1.
/// </param>
/// <param name="pageSize">
/// Number of books to return per page. Default is 10.
/// Maximum allowed value is 100.
/// </param>
/// <param name="sortBy">
/// Field used for sorting. Supported values are
/// <c>title</c>, <c>publishedYear</c>, and <c>available</c>.
/// </param>
/// <param name="sortOrder">
/// Sorting direction. Supported values are
/// <c>asc</c> and <c>desc</c>.
/// </param>
/// <returns>
/// A paginated collection of books matching the specified criteria.
/// </returns>
[Authorize]
[HttpGet]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]

public async Task<ActionResult<PagedResult<BookReadDto>>> GetAll(
    [FromQuery] string? q,
    [FromQuery] int? categoryId,
    [FromQuery] string? language,
    [FromQuery] int? publishedYear,
    [FromQuery] bool? available = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? sortBy = null,
    [FromQuery] string? sortOrder = null)
{
    // Validate pagination parameters
    if (page <= 0)
    {
        return BadRequest("Page number must be greater than 0.");
    }

    if (pageSize <= 0)
    {
        return BadRequest("Page size must be greater than 0.");
    }

    if (pageSize > 100)
    {
        return BadRequest("Page size cannot exceed 100.");
    }

    var result = await _bookService.GetAllAsync(
        q,
        categoryId,
        language,
        publishedYear,
        available,
        page,
        pageSize,
        sortBy,
        sortOrder);
    
    

    return Ok(result);
}
/// <summary>
/// Retrieves a book by its identifier.
/// </summary>
/// <param name="id">
/// Book identifier.
/// </param>
/// <returns>
/// The requested book details.
/// </returns>
[Authorize]
[HttpGet("{id}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<BookReadDto>> GetBookById(int id)
{
    var book = await _bookService.GetByIdAsync(id);

    Response.Headers.ETag = $"\"{book!.Version}\"";

    return Ok(book);
}


/// <summary>
/// Creates a new book in the catalogue.
/// </summary>
[Authorize(Roles = "Librarian,Admin")]
[HttpPost]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public async Task<ActionResult<BookReadDto>> CreateBook(
    BookCreateDto bookDto)
{
    var createdBook = await _bookService.CreateAsync(bookDto);

    return CreatedAtAction(
        nameof(GetBookById),
        new { id = createdBook.Id },
        createdBook);
}
/// <summary>
/// Partially updates an existing book.
/// </summary>
[Authorize(Roles = "Librarian,Admin")]
[HttpPatch("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public async Task<IActionResult> PatchBook(
    int id,
    [FromHeader(Name = "If-Match")] string? ifMatchHeader,
    BookPatchDto bookDto)
{
    if (string.IsNullOrWhiteSpace(ifMatchHeader))
    {
        return BadRequest("If-Match header is required.");
    }

    var versionValue = ifMatchHeader.Trim('"');

    if (!int.TryParse(versionValue, out var version))
    {
        return BadRequest("Invalid If-Match header.");
    }

    var updated = await _bookService.PatchAsync(
        id,
        bookDto,
        version);

    Response.Headers.ETag = $"\"{updated.Version}\"";

    return NoContent();
}

    /// <summary>
    /// Updates an existing book.
    /// </summary>
    [Authorize(Roles = "Librarian,Admin")]
    [HttpPut("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> UpdateBook(
    int id,
    [FromHeader(Name = "If-Match")] string? ifMatchHeader,
    BookUpdateDto bookDto)
{
    if (string.IsNullOrWhiteSpace(ifMatchHeader))
    {
        return BadRequest("If-Match header is required.");
    }

    var versionValue = ifMatchHeader.Trim('"');

    if (!int.TryParse(versionValue, out var version))
    {
        return BadRequest("Invalid If-Match header.");
    }

    var updated = await _bookService.UpdateAsync(
        id,
        bookDto,
        version);

    Response.Headers.ETag = $"\"{updated.Version}\"";

    return NoContent();
}
    /// <summary>
    /// Deletes a book from the catalogue.
    /// </summary>
    [Authorize(Roles = "Librarian,Admin")]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var deleted = await _bookService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}