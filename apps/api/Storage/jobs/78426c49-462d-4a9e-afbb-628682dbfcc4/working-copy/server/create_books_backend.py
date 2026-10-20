from pathlib import Path

ROOT = Path(".")
SERVER = ROOT / "server"

# ==========================
# CREATE FOLDERS
# ==========================

folders = [
    "Controllers",
    "DTOs",
    "Repositories",
    "Repositories/Interfaces",
    "Services",
    "Services/Interfaces",
]

for folder in folders:
    (SERVER / folder).mkdir(parents=True, exist_ok=True)

print("Folders created successfully!")

# ==========================
# FILE CONTENTS
# ==========================

files = {

# =====================================================
# DTOs
# =====================================================

"DTOs/CreateBookDto.cs": """
namespace LibraryManagement.Api.DTOs;

public class CreateBookDto
{
    public string Title { get; set; } = string.Empty;

    public string ISBN { get; set; } = string.Empty;

    public int AuthorId { get; set; }
}
""",

"DTOs/UpdateBookDto.cs": """
namespace LibraryManagement.Api.DTOs;

public class UpdateBookDto
{
    public string Title { get; set; } = string.Empty;

    public string ISBN { get; set; } = string.Empty;

    public int AuthorId { get; set; }
}
""",

# =====================================================
# REPOSITORY INTERFACE
# =====================================================

"Repositories/Interfaces/IBookRepository.cs": """
using LibraryManagement.Api.Models;

namespace LibraryManagement.Api.Repositories.Interfaces;

public interface IBookRepository
{
    Task<IEnumerable<Book>> GetAllAsync();

    Task<Book?> GetByIdAsync(int id);

    Task<Book> AddAsync(Book book);

    Task UpdateAsync(Book book);

    Task DeleteAsync(Book book);
}
""",

# =====================================================
# REPOSITORY
# =====================================================

"Repositories/BookRepository.cs": """
using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Repositories;

public class BookRepository : IBookRepository
{
    private readonly AppDbContext _context;

    public BookRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Book>> GetAllAsync()
    {
        return await _context.Books
            .Include(b => b.Author)
            .ToListAsync();
    }

    public async Task<Book?> GetByIdAsync(int id)
    {
        return await _context.Books
            .Include(b => b.Author)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Book> AddAsync(Book book)
    {
        _context.Books.Add(book);

        await _context.SaveChangesAsync();

        return book;
    }

    public async Task UpdateAsync(Book book)
    {
        _context.Books.Update(book);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Book book)
    {
        _context.Books.Remove(book);

        await _context.SaveChangesAsync();
    }
}
""",

# =====================================================
# SERVICE INTERFACE
# =====================================================

"Services/Interfaces/IBookService.cs": """
using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Services.Interfaces;

public interface IBookService
{
    Task<IEnumerable<BookDto>> GetAllAsync();

    Task<BookDto?> GetByIdAsync(int id);

    Task<BookDto> CreateAsync(CreateBookDto dto);

    Task<bool> UpdateAsync(int id, UpdateBookDto dto);

    Task<bool> DeleteAsync(int id);
}
""",

# =====================================================
# SERVICE
# =====================================================

"Services/BookService.cs": """
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories.Interfaces;
using LibraryManagement.Api.Services.Interfaces;

namespace LibraryManagement.Api.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _repository;

    public BookService(IBookRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BookDto>> GetAllAsync()
    {
        return new List<BookDto>();
    }

    public async Task<BookDto?> GetByIdAsync(int id)
    {
        return null;
    }

    public async Task<BookDto> CreateAsync(CreateBookDto dto)
    {
        var book = new Book
        {
            Title = dto.Title,
            ISBN = dto.ISBN,
            AuthorId = dto.AuthorId
        };

        await _repository.AddAsync(book);

        return new BookDto
        {
            Id = book.Id,
            Title = book.Title
        };
    }

    public async Task<bool> UpdateAsync(int id, UpdateBookDto dto)
    {
        var book = await _repository.GetByIdAsync(id);

        if (book == null)
            return false;

        book.Title = dto.Title;
        book.ISBN = dto.ISBN;
        book.AuthorId = dto.AuthorId;

        await _repository.UpdateAsync(book);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var book = await _repository.GetByIdAsync(id);

        if (book == null)
            return false;

        await _repository.DeleteAsync(book);

        return true;
    }
}
""",

# =====================================================
# CONTROLLER
# =====================================================

"Controllers/BooksController.cs": """
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _service;

    public BooksController(IBookService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var book = await _service.GetByIdAsync(id);

        if (book == null)
            return NotFound();

        return Ok(book);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBookDto dto)
    {
        var book = await _service.CreateAsync(dto);

        return Ok(book);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateBookDto dto)
    {
        var success = await _service.UpdateAsync(id, dto);

        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeleteAsync(id);

        if (!success)
            return NotFound();

        return NoContent();
    }
}
"""
}

# ==========================
# CREATE FILES
# ==========================

for path, content in files.items():

    file_path = SERVER / path

    file_path.parent.mkdir(
        parents=True,
        exist_ok=True
    )

    file_path.write_text(
        content.strip(),
        encoding="utf-8"
    )

print("Books backend scaffold generated successfully!")