using Microsoft.AspNetCore.Builder;

namespace StockManufactura.Api.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app)
        {
            app.UseCors("DefaultCorsPolicy");
            return app;
        }
    }
}
