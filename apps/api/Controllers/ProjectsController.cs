using CleanArchitectureMigration.Api.Data;
using CleanArchitectureMigration.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitectureMigration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly MigrationDbContext _db;

    public ProjectsController(MigrationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectResponse>>> GetAllProjects()
    {
        var projects = await _db.Projects
            .Include(p => p.Jobs)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new ProjectResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CreatedAtUtc = p.CreatedAtUtc,
                LatestJobId = p.Jobs.OrderByDescending(j => j.CreatedAtUtc).Select(j => (Guid?)j.Id).FirstOrDefault(),
                LatestJobStatus = p.Jobs.OrderByDescending(j => j.CreatedAtUtc).Select(j => (JobStatus?)j.Status).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectResponse>> GetProjectById(Guid id)
    {
        var p = await _db.Projects
            .Include(p => p.Jobs)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (p == null) return NotFound(new { error = $"Project {id} not found." });

        var latestJob = p.Jobs.OrderByDescending(j => j.CreatedAtUtc).FirstOrDefault();

        return Ok(new ProjectResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            CreatedAtUtc = p.CreatedAtUtc,
            LatestJobId = latestJob?.Id,
            LatestJobStatus = latestJob?.Status
        });
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> CreateProject([FromBody] CreateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Project name is required." });
        }

        var project = new ProjectEntity
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, new ProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAtUtc = project.CreatedAtUtc
        });
    }
}
