using LibraryManagement.API.DTOs.Authors;
using LibraryManagement.API.DTOs.Books;
using LibraryManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LibraryManagement.API.Controllers;

/// <summary>
/// Provides API endpoints for retrieving author information.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;


    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="AuthorsController"/> class.
    /// </summary>
    /// <param name="authorService">
    /// Service used to access author data.
    /// </param>
    public AuthorsController(
        IAuthorService authorService)
    {
        _authorService = authorService;
    }


    /// <summary>
/// Retrieves authors with optional search.
/// </summary>
/// <param name="q">
/// Optional search keyword used to search author names.
/// </param>
/// <returns>
/// Collection of authors.
/// </returns>
[HttpGet]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<ActionResult<IEnumerable<AuthorReadDto>>> GetAllAuthors(
    [FromQuery] string? q)
{
    var authors = await _authorService.GetAllAsync(q);

    return Ok(authors);
}


    /// <summary>
    /// Retrieves an author by identifier.
    /// </summary>
    /// <param name="id">
    /// Author identifier.
    /// </param>
    /// <returns>
    /// Author information.
    /// </returns>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthorReadDto>> GetAuthorById(
        int id)
    {
        var author =
            await _authorService.GetByIdAsync(id);

        return Ok(author);
    }

    /// <summary>
/// Retrieves books written by an author.
/// </summary>
/// <param name="id">
/// Author identifier.
/// </param>
/// <returns>
/// Collection of books written by the author.
/// </returns>
[HttpGet("{id}/books")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<IEnumerable<BookSummaryDto>>> GetBooksByAuthor(
    int id)
{
    var books =
        await _authorService.GetBooksByAuthorIdAsync(id);

    return Ok(books);
}

/// <summary>
/// Creates a new author.
/// </summary>
/// <param name="authorDto">
/// Data required to create the author.
/// </param>
/// <returns>
/// The newly created author.
/// </returns>
[HttpPost]
[Authorize(Roles = "Librarian,Admin")]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<AuthorReadDto>> CreateAuthor(
    AuthorCreateDto authorDto)
{
    var author =
        await _authorService.CreateAsync(authorDto);

    return CreatedAtAction(
        nameof(GetAuthorById),
        new { id = author.Id },
        author);
}

/// <summary>
/// Updates an existing author.
/// </summary>
/// <param name="id">
/// Author identifier.
/// </param>
/// <param name="authorDto">
/// Updated author data.
/// </param>
/// <param name="version">
/// Expected author version.
/// </param>
/// <returns>
/// The updated author.
/// </returns>
[HttpPut("{id}")]
[Authorize(Roles = "Librarian,Admin")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<AuthorReadDto>> UpdateAuthor(
    int id,
    AuthorUpdateDto authorDto,
    [FromQuery] int version)
{
    var author =
        await _authorService.UpdateAsync(
            id,
            authorDto,
            version);

    return Ok(author);
}

/// <summary>
/// Deletes an author if the author has no associated books.
/// </summary>
/// <param name="id">
/// Author identifier.
/// </param>
/// <returns>
/// No content when the author is successfully deleted.
/// </returns>
[HttpDelete("{id}")]
[Authorize(Roles = "Librarian,Admin")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public async Task<IActionResult> DeleteAuthor(int id)
{
    await _authorService.DeleteAsync(id);

    return NoContent();
}

}