using System.Linq;

namespace StockManufactura.Shared
{
    public sealed class Result : IResult
    {
        public bool IsSuccess { get; }
        public string[] Errors { get; }

        private Result(bool isSuccess, string[] errors)
        {
            IsSuccess = isSuccess;
            Errors = errors;
        }

        public static Result Success() => new Result(true, Array.Empty<string>());

        public static Result Failure(params string[] errors)
        {
            var distinctErrors = errors?.Where(error => !string.IsNullOrWhiteSpace(error)).Distinct().ToArray() ?? Array.Empty<string>();
            return new Result(false, distinctErrors);
        }
    }
}
