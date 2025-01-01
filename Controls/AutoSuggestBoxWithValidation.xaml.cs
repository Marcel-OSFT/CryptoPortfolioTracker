using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using WinUI3Localizer;
using static WinUI3Localizer.LanguageDictionary;

namespace CryptoPortfolioTracker.Controls;

public sealed partial class AutoSuggestBoxWithValidation : UserControl 
{
    private static readonly ILocalizer loc = Localizer.Get();
    public bool AnimateBorder { get; set; } = true;
    public bool IsPlaceholderSet { get; set; } = false;

    public event EventHandler? TextChanged;
    public event EventHandler<AutoSuggestBoxSuggestionChosenEventArgs>? SuggestionChosen;
    public event EventHandler<KeyRoutedEventArgs>? KeyDown;
    
    public bool IsEntryMatched
    {
        get
        {
            if (GetValue(IsEntryMatchedProperty).GetType().Name == "Int32")
            {
                return false;
            }
            return (bool)GetValue(IsEntryMatchedProperty);
        }
        set => SetValue(IsEntryMatchedProperty, value);
    }
    public List<string> ItemsSource
    {
        get
        {
            if (GetValue(ItemsSourceProperty).GetType().Name == "Int32")
            {
                return new List<string>();
            }
            return (List<string>)GetValue(ItemsSourceProperty);
        }
        set => SetValue(ItemsSourceProperty, value);
    }
    public string MyText
    {
        get
        {
            if (GetValue(MyTextProperty).GetType().Name == "Int32")
            {
                return string.Empty;
            }
            return (string)GetValue(MyTextProperty);
        }
        set => SetValue(MyTextProperty, value);
    }

    public string Header
    {
        get
        {
            if (GetValue(HeaderProperty).GetType().Name == "Int32")
            {
                return string.Empty;
            }
            return (string)GetValue(HeaderProperty);
        }
        set => SetValue(HeaderProperty, value);
    }
    public string MyPlaceholderText
    {
        get
        {
            if (GetValue(MyPlaceholderTextProperty).GetType().Name == "Int32")
            {
                return string.Empty;
            }
            return (string)GetValue(MyPlaceholderTextProperty);
        }
        set => SetValue(MyPlaceholderTextProperty, value);
    }

    public static readonly DependencyProperty IsEntryMatchedProperty = DependencyProperty.Register("IsEntryMatched", typeof(bool), typeof(AutoSuggestBoxWithValidation), new PropertyMetadata(0, new PropertyChangedCallback(IsEntryMatchedChangedCallBack)));
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(List<string>), typeof(AutoSuggestBoxWithValidation), new PropertyMetadata(0, new PropertyChangedCallback(ItemsSourceChangedCallBack)));
    public static readonly DependencyProperty MyTextProperty = DependencyProperty.Register("MyText", typeof(string), typeof(AutoSuggestBoxWithValidation), new PropertyMetadata(0, new PropertyChangedCallback(TextEntryChangedCallBack)));
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(string), typeof(AutoSuggestBoxWithValidation), null);
    public static readonly DependencyProperty MyPlaceholderTextProperty = DependencyProperty.Register("MyPlaceholderText", typeof(string), typeof(AutoSuggestBoxWithValidation), new PropertyMetadata(0, new PropertyChangedCallback(PlaceholderTextChangedCallBack)));

    public AutoSuggestBoxWithValidation()
    {
        InitializeComponent();
        //** PointerPressedEvent needs to be added is this 'special' way.
        //This because its handled earlier in the system and not passed through.
        innerASBox.AddHandler(TextBox.PointerPressedEvent, new PointerEventHandler(InnerASBox_PointerPressed), true);
        innerASBox.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler(InnerASBox_KeyDown), true);
        innerASBox.Visibility = Visibility.Collapsed;
    }

    private static void PlaceholderTextChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var thisBox = (AutoSuggestBoxWithValidation)d;

        if (thisBox.MyPlaceholderText != null 
            && thisBox.MyPlaceholderText != loc.GetLocalizedString("ASBox_SelectItem_Msg") 
            && thisBox.MyPlaceholderText != loc.GetLocalizedString("ASBox_NoItems_Msg"))
        {
            thisBox.IsPlaceholderSet = true;
        }
    }

    private static void TextEntryChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var thisBox = (AutoSuggestBoxWithValidation)d;

        if (thisBox.innerASBox.Visibility == Visibility.Collapsed && (string)e.NewValue == "")
        {
            thisBox.innerASBox.Visibility = Visibility.Visible;
        }
        thisBox.PopulateSuitableItems();
        thisBox.IsEntryMatched = thisBox.DoesEntryMatch();
        thisBox.TextChanged?.Invoke(thisBox, EventArgs.Empty);
    }
    private static void ItemsSourceChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d.GetValue(ItemsSourceProperty).GetType().FullName == "System.Int32")
        {
            return;
        }

        var list = (List<string>)d.GetValue(ItemsSourceProperty);
        var thisBox = (AutoSuggestBoxWithValidation)d; 
        if (thisBox.innerASBox.ItemsSource == null ||  !thisBox.innerASBox.ItemsSource.Equals(list))
        {
            thisBox.innerASBox.ItemsSource = list;
        }

        if (list.Count > 0 && thisBox.MyText != string.Empty)
        {
            thisBox.IsEntryMatched = thisBox.DoesEntryMatch();
        }

        if (!thisBox.IsPlaceholderSet)
        {
            thisBox.innerASBox.PlaceholderText = thisBox.innerASBox.Items.Count > 0 
                ? loc.GetLocalizedString("ASBox_SelectItem_Msg") 
                : loc.GetLocalizedString("ASBox_NoItems_Msg");
        }
        else
        {
            thisBox.innerASBox.PlaceholderText = thisBox.MyPlaceholderText;
        }
        thisBox.innerASBox.IsSuggestionListOpen = false;
    }

    private static void IsEntryMatchedChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AutoSuggestBoxWithValidation thisBox && thisBox.AnimateBorder)
        {
            thisBox.SetBorderColor();
        }
    }
    private void InnerASBox_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        innerASBox.IsEnabled = true;
        innerASBox.IsSuggestionListOpen = true;
    }

    private void InnerASBox_SuggestionChosen(object _, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        IsEntryMatched = true;
        SuggestionChosen?.Invoke(this, args);
    }
    private void InnerASBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            IsEntryMatched = DoesEntryMatch();
            KeyDown?.Invoke(this, e);
        }
    }
    private bool DoesEntryMatch()
    {
        return ItemsSource.Contains(MyText);
    }

    private void PopulateSuitableItems()
    {
        var suitableItems = new List<string>();

        //*** first check for exact match
        var exactMatch = ItemsSource.Where(x => x.ToLower() == MyText.ToLower()).FirstOrDefault();

        if (exactMatch != null)
        {
            suitableItems.Add(exactMatch);
            MyText = exactMatch;
        }
        else
        {
            //*** check for partial matches
            var splitText = MyText.ToLower().Split(" ");
            foreach (var item in ItemsSource.ToList())
            {
                var found = splitText.All((key) =>
                {
                    if (item.ToLower().Contains(key))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });
                if (found)
                {
                    suitableItems.Add(item);
                    if (MyText.ToLower() == item.ToLower() && MyText != item)
                    {
                        MyText = item;
                    }
                }
            }
        }
        
        if (suitableItems.Count == 0)
        {
            suitableItems.Add(loc.GetLocalizedString("ASBox_NoSuitableItems_Msg"));
        }
        innerASBox.ItemsSource = suitableItems;

        if (ItemsSource.Contains(MyText))
        {
            IsEntryMatched = true;
        }
        else
        {
            IsEntryMatched = false;
        }
        
    }
    private void SetBorderColor()
    {
        if (IsEntryMatched)
        {
            innerASBox.BorderBrush = new SolidColorBrush(Colors.Green);
        }
        else
        {
            innerASBox.BorderBrush = new SolidColorBrush(Colors.Red);
        }
    }
    

}
