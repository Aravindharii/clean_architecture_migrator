from pathlib import Path

ROOT = Path(".")
SERVER = ROOT / "server"

# ==========================
# CREATE FOLDERS
# ==========================

folders = [
    "Models",
    "DTOs",
    "Controllers",
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

# ==========================
# MODELS
# ==========================

"Models/Author.cs": """
namespace LibraryManagement.Api.Models;

public class Author
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
""",

"Models/Book.cs": """
namespace LibraryManagement.Api.Models;

public class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ISBN { get; set; } = string.Empty;

    public int AuthorId { get; set; }

    public Author? Author { get; set; }

    public Inventory? Inventory { get; set; }
}
""",

"Models/Inventory.cs": """
namespace LibraryManagement.Api.Models;

public class Inventory
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public Book? Book { get; set; }

    public int TotalCopies { get; set; }

    public int AvailableCopies { get; set; }
}
""",

# ==========================
# DTOS
# ==========================

"DTOs/BookDto.cs": """
namespace LibraryManagement.Api.DTOs;

public class BookDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;
}
""",

"DTOs/AuthorDto.cs": """
namespace LibraryManagement.Api.DTOs;

public class AuthorDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
""",

"DTOs/InventoryDto.cs": """
namespace LibraryManagement.Api.DTOs;

public class InventoryDto
{
    public int BookId { get; set; }

    public int TotalCopies { get; set; }

    public int AvailableCopies { get; set; }
}
""",

# ==========================
# REPOSITORY INTERFACES
# ==========================

"Repositories/Interfaces/IBookRepository.cs": """
using LibraryManagement.Api.Models;

namespace LibraryManagement.Api.Repositories.Interfaces;

public interface IBookRepository
{
    Task<IEnumerable<Book>> GetAllAsync();

    Task<Book?> GetByIdAsync(int id);
}
""",

"Repositories/Interfaces/IAuthorRepository.cs": """
using LibraryManagement.Api.Models;

namespace LibraryManagement.Api.Repositories.Interfaces;

public interface IAuthorRepository
{
    Task<IEnumerable<Author>> GetAllAsync();

    Task<Author?> GetByIdAsync(int id);
}
""",

"Repositories/Interfaces/IInventoryRepository.cs": """
using LibraryManagement.Api.Models;

namespace LibraryManagement.Api.Repositories.Interfaces;

public interface IInventoryRepository
{
    Task<Inventory?> GetByBookIdAsync(int bookId);
}
""",

# ==========================
# REPOSITORIES
# ==========================

"Repositories/BookRepository.cs": """
using Microsoft.EntityFrameworkCore;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories.Interfaces;

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
            .FirstOrDefaultAsync(b => b.Id == id);
    }
}
""",

"Repositories/AuthorRepository.cs": """
using Microsoft.EntityFrameworkCore;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories.Interfaces;

namespace LibraryManagement.Api.Repositories;

public class AuthorRepository : IAuthorRepository
{
    private readonly AppDbContext _context;

    public AuthorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Author>> GetAllAsync()
    {
        return await _context.Authors.ToListAsync();
    }

    public async Task<Author?> GetByIdAsync(int id)
    {
        return await _context.Authors.FindAsync(id);
    }
}
""",

"Repositories/InventoryRepository.cs": """
using Microsoft.EntityFrameworkCore;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories.Interfaces;

namespace LibraryManagement.Api.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _context;

    public InventoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Inventory?> GetByBookIdAsync(int bookId)
    {
        return await _context.Inventories
            .FirstOrDefaultAsync(i => i.BookId == bookId);
    }
}
""",

# ==========================
# SERVICE INTERFACES
# ==========================

"Services/Interfaces/IBookService.cs": """
using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Services.Interfaces;

public interface IBookService
{
    Task<IEnumerable<BookDto>> GetAllAsync();

    Task<BookDto?> GetByIdAsync(int id);
}
""",

"Services/Interfaces/IAuthorService.cs": """
using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Services.Interfaces;

public interface IAuthorService
{
    Task<IEnumerable<AuthorDto>> GetAllAsync();
}
""",

# ==========================
# SERVICES
# ==========================

"Services/BookService.cs": """
using LibraryManagement.Api.DTOs;
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
}
""",

"Services/AuthorService.cs": """
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Repositories.Interfaces;
using LibraryManagement.Api.Services.Interfaces;

namespace LibraryManagement.Api.Services;

public class AuthorService : IAuthorService
{
    private readonly IAuthorRepository _repository;

    public AuthorService(IAuthorRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<AuthorDto>> GetAllAsync()
    {
        return new List<AuthorDto>();
    }
}
""",

# ==========================
# CONTROLLER
# ==========================

"Controllers/CatalogueController.cs": """
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/catalogue")]
public class CatalogueController : ControllerBase
{
    [HttpGet("books")]
    public IActionResult GetBooks()
    {
        return Ok("TODO: Get Books");
    }

    [HttpGet("authors")]
    public IActionResult GetAuthors()
    {
        return Ok("TODO: Get Authors");
    }
}
"""
}

# ==========================
# CREATE FILES
# ==========================

for path, content in files.items():
    file_path = SERVER / path
    file_path.parent.mkdir(parents=True, exist_ok=True)
    file_path.write_text(content.strip(), encoding="utf-8")

print("Catalogue backend scaffold generated successfully!")