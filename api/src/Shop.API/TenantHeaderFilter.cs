using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shop.API;

public class TenantHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";
        if (path.StartsWith("api/platform/") || path.StartsWith("api/health"))
            return;

        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Tenant-Id",
            In = ParameterLocation.Header,
            Description = "테넌트 Slug (예: catholia)",
            Required = true,
            Schema = new OpenApiSchema { Type = "string", Default = new Microsoft.OpenApi.Any.OpenApiString("catholia") }
        });
    }
}
