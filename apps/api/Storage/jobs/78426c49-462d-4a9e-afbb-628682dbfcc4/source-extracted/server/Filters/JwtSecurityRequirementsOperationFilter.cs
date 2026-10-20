using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LibraryManagement.Api.Filters;

public class JwtSecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Security ??= new List<OpenApiSecurityRequirement>();

        var schemeRef = new OpenApiSecuritySchemeReference("Bearer", null);

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [schemeRef] = new List<string>()
        });
    }
}
