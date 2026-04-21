using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VenueOps.Models.Common;

namespace VenueOps.ViewModels.Common;

/// <summary>
/// Reusable pagination controller that can be composed into any ViewModel.
/// Holds the current page of items, tracks pagination state, and exposes
/// Next / Previous / Refresh commands.
///
/// Usage:
///   public PaginatedSection&lt;MyModel&gt; Items { get; }
///   Items = new PaginatedSection&lt;MyModel&gt;((page, size, ct) => service.GetAsync(page, size, ct));
///   await Items.LoadAsync();
/// </summary>
public partial class PaginatedSection<T> : ObservableObject
{
    private readonly Func<int, int, CancellationToken, Task<PagedResult<T>?>> _fetchFunc;

    // ── Items ─────────────────────────────────────────────────────────────
    public ObservableCollection<T> Items { get; } = [];

    // ── Pagination state ──────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPrevious))]
    [NotifyPropertyChangedFor(nameof(PageLabel))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    public partial int CurrentPage { get; private set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(PageLabel))]
    public partial int TotalItems { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial bool HasMore { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    public partial bool IsLoading { get; private set; }

    public int  PageSize      { get; }
    public bool IsNotLoading  => !IsLoading;
    public bool HasPrevious   => CurrentPage > 1;
    public int  TotalPages  => TotalItems == 0 ? 1 : (int)Math.Ceiling((double)TotalItems / PageSize);
    public string PageLabel => $"Page {CurrentPage} of {TotalPages}";

    public PaginatedSection(
        Func<int, int, CancellationToken, Task<PagedResult<T>?>> fetchFunc,
        int pageSize = 5)
    {
        _fetchFunc = fetchFunc;
        PageSize   = pageSize;
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Loads the first page. Call on page initialization.</summary>
    public Task LoadAsync(CancellationToken ct = default)
        => LoadPageAsync(1, ct);

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task NextPageAsync() => LoadPageAsync(CurrentPage + 1);

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private Task PreviousPageAsync() => LoadPageAsync(CurrentPage - 1);

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private Task RefreshAsync() => LoadPageAsync(CurrentPage);

    private bool CanGoNext()     => HasMore && !IsLoading;
    private bool CanGoPrevious() => HasPrevious && !IsLoading;
    private bool CanRefresh()    => !IsLoading;

    // ── Core load ─────────────────────────────────────────────────────────

    private async Task LoadPageAsync(int page, CancellationToken ct = default)
    {
        if (IsLoading) return;

        IsLoading = true;
        Items.Clear(); // Wipe immediately so stale data never shows behind the spinner
        try
        {
            var result = await _fetchFunc(page, PageSize, ct);
            if (result is null) return;

            foreach (var item in result.Items)
                Items.Add(item);

            CurrentPage = result.Page;
            TotalItems  = result.Total;
            HasMore     = result.HasMore;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
