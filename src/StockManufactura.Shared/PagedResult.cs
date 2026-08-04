using System.Collections.Generic;

namespace StockManufactura.Shared
{
    public sealed class PagedResult<T>
    {
        public IEnumerable<T> Items { get; }
        public int Page { get; }
        public int PageSize { get; }
        public long Total { get; }

        public PagedResult(IEnumerable<T> items, int page, int pageSize, long total)
        {
            Items = items;
            Page = page;
            PageSize = pageSize;
            Total = total;
        }
    }
}
