using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace StockManufactura.Api.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException validationException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";

                var payload = JsonSerializer.Serialize(new { errors = validationException.Errors.Select(error => error.ErrorMessage) });
                await context.Response.WriteAsync(payload);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Unhandled exception occurred.");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(new { errors = new[] { "An unexpected error occurred." } });
                await context.Response.WriteAsync(payload);
            }
        }
    }
}
