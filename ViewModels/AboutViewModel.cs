
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Services;
using Microsoft.EntityFrameworkCore.Storage;
using System;

namespace CryptoPortfolioTracker.ViewModels;

public sealed partial class AboutViewModel : BaseViewModel, IDisposable
{
    [ObservableProperty] private string helpText;
    private readonly IPreferencesService _preferencesService;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static AboutViewModel Current;

    public AboutViewModel(IPreferencesService preferencesService) : base(preferencesService)
    {
        Current = this;
        _preferencesService = preferencesService;
    }

    public void Dispose()
    {
        Current = null;
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

}

