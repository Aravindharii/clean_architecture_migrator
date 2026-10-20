using Project.Application.Common.Dtos;
using Project.Application.Common.Exceptions;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Project.Application.Authentication;

namespace Project.Application.Services;

/// <summary>
/// Provides business operations for authors.
/// </summary>
public class AuthorService : IAuthorService
{
    private readonly IAuthorRepository _authorRepository;


    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="AuthorService"/> class.
    /// </summary>
    /// <param name="authorRepository">
    /// Repository used to access author data.
    /// </param>
    public AuthorService(
        IAuthorRepository authorRepository)
    {
        _authorRepository = authorRepository;
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
public async Task<IEnumerable<AuthorReadDto>> GetAllAsync(
    string? q)
{
    var authors = await _authorRepository.GetAllAsync(q);

    return authors.Select(MapToReadDto);
}


    /// <summary>
    /// Retrieves an author by identifier.
    /// </summary>
    public async Task<AuthorReadDto?> GetByIdAsync(int id)
    {
        var author =
            await _authorRepository.GetByIdAsync(id);

        if (author == null)
        {
            throw new NotFoundException(
                $"Author with ID '{id}' was not found.");
        }

        return MapToReadDto(author);
    }

    /// <summary>
/// Creates a new author.
/// </summary>
/// <param name="authorDto">
/// Data required to create the author.
/// </param>
/// <returns>
/// The created author.
/// </returns>
public async Task<AuthorReadDto> CreateAsync(
    AuthorCreateDto authorDto)
{
    var author = new Author
    {
        Name = authorDto.Name.Trim(),
        Biography = authorDto.Biography
    };

    await _authorRepository.AddAsync(author);

    var createdAuthor =
        await _authorRepository.GetByIdAsync(author.Id);

    return MapToReadDto(createdAuthor!);
}

/// <summary>
/// Updates an existing author using optimistic concurrency.
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
public async Task<AuthorReadDto> UpdateAsync(
    int id,
    AuthorUpdateDto authorDto,
    int version)
{
    var author =
        await _authorRepository.GetByIdAsync(id);

    if (author == null)
    {
        throw new NotFoundException(
            $"Author with ID '{id}' was not found.");
    }

    if (author.Version != version)
    {
        throw new ConcurrencyException(
            "The author was modified by another user. Please refresh and try again.");
    }

    author.Name = authorDto.Name.Trim();
    author.Biography = authorDto.Biography;

    author.Version++;

    await _authorRepository.UpdateAsync(
        author,
        version);

    return MapToReadDto(author);
}

/// <summary>
/// Deletes an author if the author has no associated books.
/// </summary>
/// <param name="id">
/// Author identifier.
/// </param>
/// <returns>
/// True when the author is successfully deleted.
/// </returns>
public async Task<bool> DeleteAsync(int id)
{
    var author =
        await _authorRepository.GetByIdAsync(id);

    if (author == null)
    {
        throw new NotFoundException(
            $"Author with ID '{id}' was not found.");
    }

    if (await _authorRepository.HasBooksAsync(id))
    {
        throw new ConflictException(
            "Cannot delete author because the author is associated with one or more books.");
    }

    await _authorRepository.DeleteAsync(author);

    return true;
}


    /// <summary>
    /// Maps an Author entity to AuthorReadDto.
    /// </summary>
    private static AuthorReadDto MapToReadDto(
        Author author)
    {
        return new AuthorReadDto
        {
            Id = author.Id,
            Name = author.Name,
            Biography = author.Biography,
            Version = author.Version
        };
 
   }
   /// <summary>
/// Retrieves books written by an author.
/// </summary>
/// <param name="authorId">
/// Author identifier.
/// </param>
/// <returns>
/// Collection of book summaries.
/// </returns>
public async Task<IEnumerable<BookSummaryDto>> GetBooksByAuthorIdAsync(
    int authorId)
{
    var books =
        await _authorRepository.GetBooksByAuthorIdAsync(authorId);

    return books.Select(book => new BookSummaryDto
    {
        Id = book.Id,
        Title = book.Title
    });
}
}