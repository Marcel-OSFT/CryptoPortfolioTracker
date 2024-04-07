using Microsoft.UI.Xaml.Markup;
using System;

namespace CryptoPortfolioTracker.Enums;

public enum AppUpdaterResult
{
    Error = 0,
    UpToDate = 1,
    NeedUpdate = 2,
    UpdateSuccesfull = 3,

}