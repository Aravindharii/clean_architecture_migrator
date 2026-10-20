from pathlib import Path

ROOT = Path(".")
SERVER = ROOT / "server"

# ==========================
# CREATE FOLDERS
# ==========================

folders = [
    "Controllers",
    "Models",
    "DTOs",
    "Data",
    "Repositories",
    "Repositories/Interfaces",
    "Services",
    "Services/Interfaces",
    "Common",
    "Common/Exceptions",
    "Common/Responses",
    "Mappings",
    "Extensions",
    "Configurations",
]

for folder in folders:
    (SERVER / folder).mkdir(parents=True, exist_ok=True)

print("Folders created successfully!")

# ==========================
# FILE CONTENTS
# ==========================

files = {

# ==========================
# DATA
# ==========================

"Data/AppDbContext.cs": """
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // DbSets will be added feature by feature.
}
""",

# ==========================
# COMMON RESPONSE
# ==========================

"Common/Responses/ApiResponse.cs": """
namespace LibraryManagement.Api.Common.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }
}
""",

# ==========================
# BASE REPOSITORY
# ==========================

"Repositories/Interfaces/IRepository.cs": """
namespace LibraryManagement.Api.Repositories.Interfaces;

public interface IRepository<T>
{
}
""",

# ==========================
# BASE SERVICE
# ==========================

"Services/Interfaces/IService.cs": """
namespace LibraryManagement.Api.Services.Interfaces;

public interface IService
{
}
""",

# ==========================
# AUTOMAPPER PROFILE
# ==========================

"Mappings/MappingProfile.cs": """
namespace LibraryManagement.Api.Mappings;

public class MappingProfile
{
}
""",

# ==========================
# EXTENSIONS
# ==========================

"Extensions/ServiceCollectionExtensions.cs": """
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Register repositories

        // Register services

        return services;
    }
}
""",

# ==========================
# GLOBAL EXCEPTION PLACEHOLDER
# ==========================

"Common/Exceptions/AppException.cs": """
namespace LibraryManagement.Api.Common.Exceptions;

public class AppException : Exception
{
    public AppException(string message)
        : base(message)
    {
    }
}
""",

# ==========================
# README
# ==========================

"README_BACKEND.md": """
# Library Management Backend

Generated backend architecture.

Current phase:

✔ Folder structure

✔ AppDbContext

✔ Repository base

✔ Service base

✔ Mapping placeholder

✔ Extension placeholder

Next:

create_catalogue_backend.py
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

print("Backend base scaffold generated successfully!")