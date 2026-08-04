using System.Net;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace StockManufactura.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Serilog.ILogger _logger;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
            _logger = Log.ForContext<ExceptionMiddleware>();
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unhandled exception");
                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await httpContext.Response.WriteAsJsonAsync(new { error = "An error occurred" });
            }
        }
    }
}
