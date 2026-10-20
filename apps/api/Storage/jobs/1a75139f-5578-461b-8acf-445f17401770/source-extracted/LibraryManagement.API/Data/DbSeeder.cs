using LibraryManagement.API.Enums;
using LibraryManagement.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Data;

/// <summary>
/// Seeds required system users into the database.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Creates the default Admin and Librarian accounts if they do not exist.
    /// </summary>
    public static async Task SeedAsync(
        AppDbContext context,
        IPasswordHasher<User> passwordHasher)
    {
        await context.Database.MigrateAsync();

        // Seed Admin
        await CreateUserIfNotExistsAsync(
            context,
            passwordHasher,
            "Admin",
            "User",
            "admin@library.com",
            "Admin@12345",
            UserRole.Admin);

        // Seed Librarian
        await CreateUserIfNotExistsAsync(
            context,
            passwordHasher,
            "Library",
            "Librarian",
            "librarian@library.com",
            "Librarian@12345",
            UserRole.Librarian);
    }

    private static async Task CreateUserIfNotExistsAsync(
        AppDbContext context,
        IPasswordHasher<User> passwordHasher,
        string firstName,
        string lastName,
        string email,
        string password,
        UserRole role)
    {
        var normalizedEmail = email.Trim().ToLower();

        var exists = await context.Users
            .AnyAsync(u => u.Email == normalizedEmail);

        if (exists)
        {
            return;
        }

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = normalizedEmail,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            password);

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
    }
}
