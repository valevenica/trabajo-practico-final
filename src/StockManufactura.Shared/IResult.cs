namespace StockManufactura.Shared
{
    public interface IResult
    {
        bool IsSuccess { get; }
        string[] Errors { get; }
    }
}
