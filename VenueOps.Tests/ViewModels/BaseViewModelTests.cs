using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

public class BaseViewModelTests
{
    // Derived type that keeps the default (no-op) OnBusyStateChanged hook.
    private sealed class PlainViewModel : BaseViewModel;

    // Derived type that records every call to the OnBusyStateChanged hook.
    private sealed class RecordingViewModel : BaseViewModel
    {
        public List<bool> BusyStates { get; } = [];

        protected override void OnBusyStateChanged(bool isBusy) => BusyStates.Add(isBusy);
    }

    [Fact]
    public void Defaults_AreNotBusyWithEmptyTitle()
    {
        PlainViewModel sut = new();

        Assert.False(sut.IsBusy);
        Assert.True(sut.IsNotBusy);
        Assert.Equal(string.Empty, sut.Title);
    }

    [Fact]
    public void IsBusy_UpdatesIsNotBusyAndRaisesBothNotifications()
    {
        PlainViewModel sut = new();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.IsBusy = true;

        Assert.True(sut.IsBusy);
        Assert.False(sut.IsNotBusy);
        Assert.Equal([nameof(BaseViewModel.IsBusy), nameof(BaseViewModel.IsNotBusy)], raised);
    }

    [Fact]
    public void IsBusy_InvokesOnBusyStateChangedHook_OnEveryChange()
    {
        RecordingViewModel sut = new();

        sut.IsBusy = true;
        sut.IsBusy = true; // unchanged value: no hook call
        sut.IsBusy = false;

        Assert.Equal([true, false], sut.BusyStates);
    }

    [Fact]
    public void Title_RaisesPropertyChanged_WhenSet()
    {
        PlainViewModel sut = new();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.Title = "Fictional Title";

        Assert.Equal("Fictional Title", sut.Title);
        Assert.Equal([nameof(BaseViewModel.Title)], raised);
    }
}
