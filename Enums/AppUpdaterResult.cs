using Microsoft.UI.Xaml.Markup;
using System;

namespace CryptoPortfolioTracker.Enums;

public enum AppUpdaterResult
{
    CheckingError = 0,
    UpToDate = 1,
    NeedUpdate = 2,
    UpdateSuccesfull = 3,
    DownloadSuccesfull = 4,
    DownloadError = 5,
    UpdateError = 6,
}