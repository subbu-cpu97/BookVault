namespace BookVault.Domain.Common;

// PagedResult<T> is the universal response wrapper for every list endpoint
// Interview answer: "Why a generic PagedResult instead of just returning a list?"
// A plain list gives the client zero context — how many total records are there?
// Am I on the last page? PagedResult answers all those questions in one response.
// Making it generic means ONE class works for books, authors, users — any entity.
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    // Computed — derived from the other four values, never stored
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPrevPage => Page > 1;

    // Static factory — clean construction from raw data
    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int page,
        int pageSize) => new()
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
}
