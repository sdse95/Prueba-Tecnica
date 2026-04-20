namespace Patients.Api.Features.Patients.Contracts;

public class PagedResponse<T>
{
    public required IReadOnlyCollection<T> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
