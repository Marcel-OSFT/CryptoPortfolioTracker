using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.ViewModels;

public partial class BaseViewModel : ObservableObject, IRecipient<FontSizeChangedMessage>
{
    private protected ILogger Logger { get; set; }
    protected readonly ILocalizer loc = Localizer.Get();

    [ObservableProperty] private double? fontLevel1;
    [ObservableProperty] private double? fontLevel2;
    [ObservableProperty] private double? fontLevel3;

    [ObservableProperty]
    protected bool isScrollBarsExpanded;
    protected readonly IDialogService _dialogService;

    protected readonly Settings _appSettings;


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public BaseViewModel(Settings appSettings, IDialogService dialogService, IMessenger messenger)
    {
        _dialogService = dialogService; 
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        IsScrollBarsExpanded = _appSettings.IsScrollBarsExpanded;
        messenger.Register<FontSizeChangedMessage>(this);
        SetFontLevels();
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public void Receive(FontSizeChangedMessage message)
    {
        SetFontLevels();
    }



    protected void SetFontLevels()
    {
        switch (_appSettings.FontSize.ToString())
        {
            case "Small":
                {
                    FontLevel1 = 14;
                    FontLevel2 = 12;
                    FontLevel3 = 10;
                    break;
                }
            case "Normal":
                {
                    FontLevel1 = 16;
                    FontLevel2 = 14;
                    FontLevel3 = 12;
                    break;
                }
            case "Large":
                {
                    FontLevel1 = 18;
                    FontLevel2 = 16;
                    FontLevel3 = 14;
                    break;
                }
            default: break;
        }
    }


    protected void ReplaceOrAddUpdatedItems<T>(ObservableCollection<T> collection, IEnumerable<T> updatedItems, Func<T, int> keySelector)
    {
        var updatedList = updatedItems.ToList();

        foreach (var updated in updatedList)
        {
            var key = keySelector(updated);

            var existingIndex = collection
                .Select((item, index) => new { item, index })
                .FirstOrDefault(x => keySelector(x.item) == key)?.index;

            if (existingIndex.HasValue)
            {
                collection[existingIndex.Value] = updated;
            }
            else
            {
                collection.Add(updated);
            }
        }
    }

    

    


}