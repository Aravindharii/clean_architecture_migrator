using LibraryManagement.Api.Common;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Repositories;

public class SoftDeleteRepository<T> where T : class, ISoftDelete
{
    protected readonly AppDbContext _context;
    protected readonly ILogger _logger;
    protected readonly DbSet<T> _dbSet;

    public SoftDeleteRepository(AppDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<IEnumerable<T>> GetAllDeletedAsync()
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Where(e => e.IsDeleted)
            .ToListAsync();
    }

    public virtual async Task<T?> GetByIdIncludingDeletedAsync(int id)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
    }

    public async Task SoftDeleteAsync(T entity, string? deletedBy = null)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = deletedBy;
        _dbSet.Update(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error soft-deleting {Entity} {Id}", typeof(T).Name, GetEntityId(entity));
            throw;
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "soft-deleting", typeof(T).Name, _logger);
        }
    }

    public async Task RestoreAsync(T entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        _dbSet.Update(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error restoring {Entity} {Id}", typeof(T).Name, GetEntityId(entity));
            throw;
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "restoring", typeof(T).Name, _logger);
        }
    }

    public async Task HardDeleteAsync(T entity)
    {
        _dbSet.Remove(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error hard-deleting {Entity} {Id}", typeof(T).Name, GetEntityId(entity));
            throw;
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "hard-deleting", typeof(T).Name, _logger);
        }
    }

    private static int GetEntityId(T entity)
    {
        var prop = typeof(T).GetProperty("Id");
        return prop?.GetValue(entity) is int id ? id : 0;
    }
}
