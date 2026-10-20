using Project.Application.Common.Responses;
using Project.Application.Validators;
using Project.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Common.Dtos;

namespace Project.API.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] bool? available,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? sort = null)
    {
        var result = await _bookService.GetAllAsync(q, category, available, page, size, sort);

        return Ok(new ApiResponse<PagedResponse<BookDto>>
        {
            Success = true,
            Message = "Books retrieved successfully.",
            Data = result
        });
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var books = await _bookService.GetAllAsync();

        return Ok(new ApiResponse<IEnumerable<BookDto>>
        {
            Success = true,
            Message = "Books retrieved successfully.",
            Data = books
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var book = await _bookService.GetByIdAsync(id);

        return Ok(new ApiResponse<BookDto>
        {
            Success = true,
            Message = "Book retrieved successfully.",
            Data = book
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Create([FromBody] BookCreateValidator dto)
    {
        var book = await _bookService.CreateAsync(dto);

        return CreatedAtAction(nameof(GetById), new { id = book.Id }, new ApiResponse<BookDto>
        {
            Success = true,
            Message = "Book created successfully.",
            Data = book
        });
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Update(int id, [FromBody] BookUpdateValidator dto)
    {
        await _bookService.UpdateAsync(id, dto);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Book updated successfully."
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Delete(int id)
    {
        await _bookService.DeleteAsync(id);

        return NoContent();
    }

    [HttpGet("{id}/delete-check")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> DeleteCheck(int id)
    {
        var result = await _bookService.CheckDeletionEligibilityAsync(id);

        return Ok(new ApiResponse<BookDeleteCheckResult>
        {
            Success = true,
            Message = "Deletion check completed.",
            Data = result
        });
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeleted()
    {
        var books = await _bookService.GetDeletedAsync();

        return Ok(new ApiResponse<IEnumerable<BookDto>>
        {
            Success = true,
            Message = "Deleted books retrieved successfully.",
            Data = books
        });
    }

    [HttpPost("{id}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restore(int id)
    {
        await _bookService.RestoreAsync(id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Book restored successfully."
        });
    }

    [HttpDelete("{id}/hard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HardDelete(int id)
    {
        await _bookService.HardDeleteAsync(id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Book permanently deleted."
        });
    }
}
