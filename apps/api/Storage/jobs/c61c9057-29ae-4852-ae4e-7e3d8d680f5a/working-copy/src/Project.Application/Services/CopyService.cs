using Project.Application.Common.Dtos;
using Project.Application.Common.Exceptions;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Project.Application.Authentication;

namespace Project.Application.Services;

/// <summary>
/// Provides business operations for physical book copies.
/// </summary>
public class CopyService : ICopyService
{
    private readonly ICopyRepository _copyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopyService"/> class.
    /// </summary>
    /// <param name="copyRepository">
    /// Repository used to access copy data.
    /// </param>
    public CopyService(ICopyRepository copyRepository)
    {
        _copyRepository = copyRepository;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CopyReadDto>> GetByBookIdAsync(int bookId)
    {
        var copies = await _copyRepository.GetByBookIdAsync(bookId);

        return copies.Select(MapToReadDto);
    }

    /// <inheritdoc />
    public async Task<CopyReadDto> GetByIdAsync(int id)
    {
        var copy = await _copyRepository.GetByIdAsync(id);

        if (copy is null)
        {
            throw new NotFoundException(
                $"Copy with ID {id} not found.");
        }

        return MapToReadDto(copy);
    }

    /// <summary>
    /// Gets all active physical book copies.
    /// </summary>
    /// <returns>
    /// A collection of active copy DTOs.
    /// </returns>
    public async Task<IEnumerable<CopyReadDto>> GetAllAsync()
    {
        var copies = await _copyRepository.GetAllAsync();

        return copies.Select(MapToReadDto);
    }

    /// <inheritdoc />
    public async Task<CopyReadDto> CreateAsync(
        int bookId,
        CopyCreateDto copyDto)
    {
        var copy = new Copy
        {
            BookId = bookId,
            Location = copyDto.Location,
            Status = "Available",
            Version = 1,
            IsDeleted = false
        };

        // First save generates the database ID.
        await _copyRepository.AddAsync(copy);
        await _copyRepository.SaveChangesAsync();

        // Generate barcode using the generated ID.
        copy.Barcode = $"LIB-{copy.Id:D6}";

        // Save the generated barcode.
        await _copyRepository.UpdateAsync(copy);
        await _copyRepository.SaveChangesAsync();

        // Reload the copy so its Book navigation property
        // is populated before mapping to the DTO.
        var createdCopy = await _copyRepository.GetByIdAsync(copy.Id);

        if (createdCopy is null)
        {
            throw new NotFoundException(
                $"Copy with ID {copy.Id} could not be loaded after creation.");
        }

        return MapToReadDto(createdCopy);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        int id,
        CopyUpdateDto copyDto,
        int version)
    {
        var copy = await _copyRepository.GetByIdAsync(id);

        if (copy is null)
        {
            throw new NotFoundException(
                $"Copy with ID {id} not found.");
        }

        if (copy.Version != version)
        {
            throw new ConcurrencyException(
                "The copy has been modified by another user.");
        }

        copy.Location = copyDto.Location;
        copy.Status = copyDto.Status;
        copy.Version++;

        await _copyRepository.UpdateAsync(copy);
        await _copyRepository.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        int id,
        int version)
    {
        var copy = await _copyRepository.GetByIdAsync(id);

        if (copy is null)
        {
            throw new NotFoundException(
                $"Copy with ID {id} not found.");
        }

        if (copy.Version != version)
        {
            throw new ConcurrencyException(
                "The copy has been modified by another user.");
        }

        // A copy that is currently on loan cannot be deleted.
        if (copy.Status == "OnLoan")
        {
            throw new ConflictException(
                "A copy that is currently on loan cannot be deleted.");
        }

        copy.IsDeleted = true;
        copy.Version++;

        await _copyRepository.UpdateAsync(copy);
        await _copyRepository.SaveChangesAsync();
    }

    /// <summary>
    /// Maps a Copy entity to a CopyReadDto.
    /// </summary>
    /// <param name="copy">
    /// The copy entity.
    /// </param>
    /// <returns>
    /// The mapped copy DTO.
    /// </returns>
    private static CopyReadDto MapToReadDto(Copy copy)
    {
        return new CopyReadDto
        {
            Id = copy.Id,
            BookId = copy.BookId,
            BookTitle = copy.Book.Title,
            Barcode = copy.Barcode,
            Location = copy.Location,
            Status = copy.Status,
            Version = copy.Version
        };
    }
}