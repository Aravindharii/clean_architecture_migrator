using LibraryManagement.API.DTOs.Copies;
using LibraryManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers;

/// <summary>
/// Provides endpoints for managing physical book copies.
/// </summary>
[ApiController]
[Route("api")]
[Authorize(Roles = "Admin,Librarian")]
public class CopyController : ControllerBase
{
    private readonly ICopyService _copyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopyController"/> class.
    /// </summary>
    /// <param name="copyService">
    /// Service used to perform copy operations.
    /// </param>
    public CopyController(ICopyService copyService)
    {
        _copyService = copyService;
    }

    /// <summary>
    /// Gets all active physical book copies.
    /// </summary>
    /// <returns>
    /// A collection of active physical book copies.
    /// </returns>
    [HttpGet("copies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CopyReadDto>>> GetAll()
    {
        var copies = await _copyService.GetAllAsync();

        return Ok(copies);
    }

    /// <summary>
    /// Gets all active copies belonging to a book.
    /// </summary>
    /// <param name="bookId">
    /// The identifier of the book.
    /// </param>
    /// <returns>
    /// The copies belonging to the book.
    /// </returns>
    [HttpGet("books/{bookId:int}/copies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CopyReadDto>>> GetByBookId(
        int bookId)
    {
        var copies = await _copyService.GetByBookIdAsync(bookId);

        return Ok(copies);
    }

    /// <summary>
    /// Gets a specific physical book copy.
    /// </summary>
    /// <param name="id">
    /// The identifier of the copy.
    /// </param>
    /// <returns>
    /// The requested copy.
    /// </returns>
    [HttpGet("copies/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CopyReadDto>> GetById(int id)
    {
        var copy = await _copyService.GetByIdAsync(id);

        return Ok(copy);
    }

    /// <summary>
    /// Adds a physical copy to an existing book.
    /// </summary>
    /// <param name="bookId">
    /// The identifier of the book.
    /// </param>
    /// <param name="copyDto">
    /// The copy data.
    /// </param>
    /// <returns>
    /// The newly created copy.
    /// </returns>
    [HttpPost("books/{bookId:int}/copies")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CopyReadDto>> Create(
        int bookId,
        CopyCreateDto copyDto)
    {
        var copy = await _copyService.CreateAsync(
            bookId,
            copyDto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = copy.Id },
            copy);
    }

    /// <summary>
    /// Updates a physical book copy using optimistic concurrency.
    /// </summary>
    /// <param name="id">
    /// The identifier of the copy.
    /// </param>
    /// <param name="ifMatchHeader">
    /// The expected copy version from the If-Match header.
    /// </param>
    /// <param name="copyDto">
    /// The updated copy data.
    /// </param>
    /// <returns>
    /// No content when the update succeeds.
    /// </returns>
    [HttpPut("copies/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromHeader(Name = "If-Match")] string? ifMatchHeader,
        CopyUpdateDto copyDto)
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

        await _copyService.UpdateAsync(
            id,
            copyDto,
            version);

        Response.Headers.ETag = $"\"{version + 1}\"";

        return NoContent();
    }

    /// <summary>
    /// Soft-deletes a physical book copy using optimistic concurrency.
    /// </summary>
    /// <param name="id">
    /// The identifier of the copy.
    /// </param>
    /// <param name="ifMatchHeader">
    /// The expected copy version from the If-Match header.
    /// </param>
    /// <returns>
    /// No content when the deletion succeeds.
    /// </returns>
    [HttpDelete("copies/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        int id,
        [FromHeader(Name = "If-Match")] string? ifMatchHeader)
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

        await _copyService.DeleteAsync(id, version);

        return NoContent();
    }
}