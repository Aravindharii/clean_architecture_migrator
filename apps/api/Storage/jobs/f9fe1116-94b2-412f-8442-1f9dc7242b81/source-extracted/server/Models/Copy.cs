using LibraryManagement.Api.Models.Common;

namespace LibraryManagement.Api.Models;

public class Copy : ISoftDelete
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public Book? Book { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public string Status { get; set; } = "Available";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
