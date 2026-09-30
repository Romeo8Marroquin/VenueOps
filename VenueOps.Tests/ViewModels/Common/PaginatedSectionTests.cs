using VenueOps.Models.Common;
using VenueOps.ViewModels.Common;

namespace VenueOps.Tests.ViewModels.Common;

public class PaginatedSectionTests
{
    private readonly List<(int Page, int PageSize)> _calls = [];

    private static PagedResult<string> Page(int page, int total, bool hasMore, params string[] items) =>
        new() { Items = [.. items], Page = page, Total = total, PageSize = 5, HasMore = hasMore };

    /// <summary>Section whose fetch returns the next queued result and records its arguments.</summary>
    private PaginatedSection<string> CreateSut(Queue<PagedResult<string>?> results, int pageSize = 5) =>
        new((page, size, _) =>
        {
            _calls.Add((page, size));
            return Task.FromResult(results.Dequeue());
        }, pageSize);

    private PaginatedSection<string> CreateSut(params PagedResult<string>?[] results) => CreateSut(new Queue<PagedResult<string>?>(results));

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void InitialState_IsFirstPageWithNoItems()
    {
        PaginatedSection<string> sut = CreateSut();

        Assert.Empty(sut.Items);
        Assert.Equal(1, sut.CurrentPage);
        Assert.Equal(0, sut.TotalItems);
        Assert.Equal(1, sut.TotalPages);
        Assert.Equal("Page 1 of 1", sut.PageLabel);
        Assert.False(sut.HasMore);
        Assert.False(sut.HasPrevious);
        Assert.False(sut.IsLoading);
        Assert.True(sut.IsNotLoading);
        Assert.Equal(5, sut.PageSize);
    }

    [Fact]
    public void PageSize_UsesConstructorValue()
    {
        Assert.Equal(10, CreateSut(new Queue<PagedResult<string>?>(), pageSize: 10).PageSize);
    }

    [Fact]
    public void InitialCommands_OnlyRefreshCanExecute()
    {
        PaginatedSection<string> sut = CreateSut();

        Assert.False(sut.NextPageCommand.CanExecute(null));
        Assert.False(sut.PreviousPageCommand.CanExecute(null));
        Assert.True(sut.RefreshCommand.CanExecute(null));
    }

    // ── LoadAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_RequestsFirstPageAndAppliesResult()
    {
        PaginatedSection<string> sut = CreateSut(Page(1, 12, true, "a", "b", "c", "d", "e"));

        await sut.LoadAsync();

        Assert.Equal([(1, 5)], _calls);
        Assert.Equal(["a", "b", "c", "d", "e"], sut.Items);
        Assert.Equal(1, sut.CurrentPage);
        Assert.Equal(12, sut.TotalItems);
        Assert.True(sut.HasMore);
        Assert.False(sut.IsLoading);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 1)]
    [InlineData(6, 2)]
    [InlineData(11, 3)]
    public async Task TotalPages_RoundsUpPartialPages(int total, int expectedPages)
    {
        PaginatedSection<string> sut = CreateSut(Page(1, total, false, "a"));

        await sut.LoadAsync();

        Assert.Equal(expectedPages, sut.TotalPages);
        Assert.Equal($"Page 1 of {expectedPages}", sut.PageLabel);
    }

    [Fact]
    public async Task LoadAsync_ForwardsCancellationToken()
    {
        using CancellationTokenSource cts = new();
        CancellationToken received = default;
        PaginatedSection<string> sut = new((_, _, ct) =>
        {
            received = ct;
            return Task.FromResult<PagedResult<string>?>(Page(1, 0, false));
        });

        await sut.LoadAsync(cts.Token);

        Assert.Equal(cts.Token, received);
    }

    [Fact]
    public async Task LoadAsync_ClearsItemsAndKeepsPagingState_WhenResultIsNull()
    {
        PaginatedSection<string> sut = CreateSut(Page(1, 8, true, "a", "b"), null);
        await sut.LoadAsync();

        await sut.LoadAsync();

        Assert.Empty(sut.Items);
        Assert.Equal(1, sut.CurrentPage);
        Assert.Equal(8, sut.TotalItems);
        Assert.True(sut.HasMore);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_ClearsStaleItemsAndSetsLoading_BeforeFetchCompletes()
    {
        TaskCompletionSource<PagedResult<string>?> pending = new();
        int fetches = 0;
        PaginatedSection<string> sut = new((_, _, _) =>
            ++fetches == 1 ? Task.FromResult<PagedResult<string>?>(Page(1, 2, false, "stale")) : pending.Task);
        await sut.LoadAsync();

        Task load = sut.LoadAsync();

        Assert.True(sut.IsLoading);
        Assert.False(sut.IsNotLoading);
        Assert.Empty(sut.Items);
        pending.SetResult(Page(1, 1, false, "fresh"));
        await load;
        Assert.Equal(["fresh"], sut.Items);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_IsIgnored_WhileAnotherLoadIsInProgress()
    {
        TaskCompletionSource<PagedResult<string>?> pending = new();
        int fetches = 0;
        PaginatedSection<string> sut = new((_, _, _) =>
        {
            fetches++;
            return pending.Task;
        });

        Task first = sut.LoadAsync();
        await sut.LoadAsync();
        pending.SetResult(Page(1, 1, false, "a"));
        await first;

        Assert.Equal(1, fetches);
        Assert.Equal(["a"], sut.Items);
    }

    [Fact]
    public async Task LoadAsync_ResetsLoadingAndPropagates_WhenFetchThrows()
    {
        PaginatedSection<string> sut = new((_, _, _) => throw new HttpRequestException("offline"));

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.LoadAsync());

        Assert.False(sut.IsLoading);
        Assert.True(sut.RefreshCommand.CanExecute(null));
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task NextPageCommand_LoadsFollowingPage_WhenMoreResultsExist()
    {
        PaginatedSection<string> sut = CreateSut(Page(1, 7, true, "a"), Page(2, 7, false, "f"));
        await sut.LoadAsync();
        Assert.True(sut.NextPageCommand.CanExecute(null));

        await sut.NextPageCommand.ExecuteAsync(null);

        Assert.Equal([(1, 5), (2, 5)], _calls);
        Assert.Equal(["f"], sut.Items);
        Assert.Equal(2, sut.CurrentPage);
        Assert.True(sut.HasPrevious);
        Assert.Equal("Page 2 of 2", sut.PageLabel);
        Assert.False(sut.NextPageCommand.CanExecute(null));
    }

    [Fact]
    public async Task PreviousPageCommand_LoadsPriorPage_WhenNotOnFirstPage()
    {
        PaginatedSection<string> sut = CreateSut(Page(1, 7, true, "a"), Page(2, 7, false, "f"), Page(1, 7, true, "a"));
        await sut.LoadAsync();
        await sut.NextPageCommand.ExecuteAsync(null);
        Assert.True(sut.PreviousPageCommand.CanExecute(null));

        await sut.PreviousPageCommand.ExecuteAsync(null);

        Assert.Equal(1, _calls[^1].Page);
        Assert.Equal(1, sut.CurrentPage);
        Assert.False(sut.HasPrevious);
        Assert.False(sut.PreviousPageCommand.CanExecute(null));
    }

    [Fact]
    public async Task RefreshCommand_ReloadsCurrentPage()
    {
        PaginatedSection<string> sut = CreateSut(Page(1, 7, true, "a"), Page(2, 7, false, "f"), Page(2, 8, false, "f", "g"));
        await sut.LoadAsync();
        await sut.NextPageCommand.ExecuteAsync(null);

        await sut.RefreshCommand.ExecuteAsync(null);

        Assert.Equal((2, 5), _calls[^1]);
        Assert.Equal(["f", "g"], sut.Items);
        Assert.Equal(8, sut.TotalItems);
    }

    [Fact]
    public async Task Commands_CannotExecute_WhileLoading()
    {
        TaskCompletionSource<PagedResult<string>?> pending = new();
        Queue<Task<PagedResult<string>?>> responses = new(
        [
            Task.FromResult<PagedResult<string>?>(Page(1, 20, true, "a")),
            Task.FromResult<PagedResult<string>?>(Page(2, 20, true, "f")),
            pending.Task
        ]);
        PaginatedSection<string> sut = new((_, _, _) => responses.Dequeue());
        await sut.LoadAsync();
        await sut.NextPageCommand.ExecuteAsync(null); // page 2: HasMore and HasPrevious both true

        Task refresh = sut.RefreshCommand.ExecuteAsync(null);

        Assert.False(sut.NextPageCommand.CanExecute(null));
        Assert.False(sut.PreviousPageCommand.CanExecute(null));
        Assert.False(sut.RefreshCommand.CanExecute(null));
        pending.SetResult(Page(2, 20, true, "f"));
        await refresh;
        Assert.True(sut.NextPageCommand.CanExecute(null));
        Assert.True(sut.PreviousPageCommand.CanExecute(null));
        Assert.True(sut.RefreshCommand.CanExecute(null));
    }

    // ── Notifications ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_RaisesDependentPropertyAndCanExecuteNotifications()
    {
        PaginatedSection<string> sut = CreateSut(Page(2, 12, true, "a"));
        List<string?> raised = [];
        int nextCanExecuteChanged = 0;
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
        sut.NextPageCommand.CanExecuteChanged += (_, _) => nextCanExecuteChanged++;

        await sut.LoadAsync();

        Assert.Contains(nameof(sut.IsNotLoading), raised);
        Assert.Contains(nameof(sut.HasPrevious), raised);
        Assert.Contains(nameof(sut.TotalPages), raised);
        Assert.Contains(nameof(sut.PageLabel), raised);
        Assert.Contains(nameof(sut.HasMore), raised);
        Assert.True(nextCanExecuteChanged > 0);
    }
}
