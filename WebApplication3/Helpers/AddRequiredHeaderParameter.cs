using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Helpers
{
    public class AddRequiredHeaderParameter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
            {
                operation.Parameters = new List<IParameter>();
            }

            var authorizeAttributes = context.ApiDescription
                .ControllerAttributes()
                .Union(context.ApiDescription.ActionAttributes())
                .OfType<AuthorizeAttribute>();
            var allowAnonymousAttributes = context.ApiDescription.ActionAttributes().OfType<AllowAnonymousAttribute>();

            if (!authorizeAttributes.Any() && !allowAnonymousAttributes.Any())
            {
                return;
            }
            var parameter = new NonBodyParameter
            {
                Name = "Content-Type",
                In = "header",
                Description = "application/json",
                Required = true,
                Type = "string"
            };
            var parameter1 = new NonBodyParameter
            {
                Name = "Authorization",
                In = "header",
                Description = "The bearer token",
                Required = true,
                Type = "string"

            };

            operation.Parameters.Add(parameter);
            operation.Parameters.Add(parameter1);

        }
    }
}
