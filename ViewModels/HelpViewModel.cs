
using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoPortfolioTracker.ViewModels;

public sealed partial class HelpViewModel : BaseViewModel
{
    [ObservableProperty] private string helpText;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static HelpViewModel Current;

    public HelpViewModel()
    {
        Current = this;
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

}

