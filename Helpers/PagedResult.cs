namespace KABILIN.Helpers
{
    public interface IPaged
    {
        int Page { get; }
        int PageSize { get; }
        int Total { get; }
        int TotalPages { get; }
        int From { get; }
        int To { get; }
    }

    public class PagedResult<T> : IPaged
    {
        public List<T> Items { get; init; } = new();
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public int Total { get; init; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
        public int From => Total == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Total, Page * PageSize);

        public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> query, int page, int pageSize)
        {
            page = Math.Max(1, page);
            var total = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(query);
            var maxPage = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Min(page, maxPage);
            var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                query.Skip((page - 1) * pageSize).Take(pageSize));
            return new PagedResult<T> { Items = items, Page = page, PageSize = pageSize, Total = total };
        }
    }
}
