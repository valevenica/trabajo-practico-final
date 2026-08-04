using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.DTOs
{
    public sealed class AuthenticationResult
    {
        public bool IsSuccess { get; init; }
        public bool IsLockedOut { get; init; }
        public bool RequiresPasswordChange { get; init; }
        public DateTime? LockoutEndUtc { get; init; }
        public string Message { get; init; } = string.Empty;
        public Usuario? Usuario { get; init; }

        public static AuthenticationResult Failed(string message)
        {
            return new AuthenticationResult { IsSuccess = false, Message = message };
        }
    }
}
