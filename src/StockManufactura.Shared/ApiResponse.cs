namespace StockManufactura.Shared
{
    public sealed class ApiResponse<T>
    {
        public bool Success { get; }
        public T? Data { get; }
        public string[] Errors { get; }

        public ApiResponse(T data)
        {
            Success = true;
            Data = data;
            Errors = Array.Empty<string>();
        }

        public ApiResponse(params string[] errors)
        {
            Success = false;
            Data = default;
            Errors = errors ?? Array.Empty<string>();
        }
    }
}
