using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SQLitePCL;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using WinUI3Localizer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls;

public enum SortingOrder
{
    None = 0,
    Descending = 1,
    Ascending = 2,
}

public sealed partial class ColumnHeaderButton : UserControl, IDisposable
{
    public bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }
    public SolidColorBrush PointerHoverColor
    {
        get => (SolidColorBrush)GetValue(PointerHoverColorProperty);
        set => SetValue(PointerHoverColorProperty, value);
    }
    public TextAlignment HorizontalTextAlignment
    {
        get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
        set => SetValue(HorizontalTextAlignmentProperty, value);
    }
    public SortingOrder SortingOrder
    {
        get => (SortingOrder)GetValue(SortingOrderProperty);
        set => SetValue(SortingOrderProperty, value);
    }
    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
    public object CommandParameter
    {
        get => (object)GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public string Text
    {
        get
        {
            if (GetValue(TextProperty).GetType().Name == "Int32")
            {
                return string.Empty;
            }
            return (string)GetValue(TextProperty);
        }
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register("IsEnabled", typeof(bool), typeof(ColumnHeaderButton), null);
    public static readonly DependencyProperty PointerHoverColorProperty = DependencyProperty.Register("PointerHoverColor", typeof(SolidColorBrush), typeof(ColumnHeaderButton), null);
    public static readonly DependencyProperty HorizontalTextAlignmentProperty = DependencyProperty.Register("HorizontalTextAlignment", typeof(TextAlignment), typeof(ColumnHeaderButton), null);
    public static readonly DependencyProperty SortingOrderProperty = DependencyProperty.Register("SortingOrder", typeof(SortingOrder), typeof(ColumnHeaderButton), new PropertyMetadata(0, new PropertyChangedCallback(SortingOrderChangedCallBack)));
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(ColumnHeaderButton), null);
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register("CommandParameter", typeof(object), typeof(ColumnHeaderButton), null);
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ColumnHeaderButton), new PropertyMetadata(0, new PropertyChangedCallback(TextChangedCallBack)));

    private string _text;
    private static List<ColumnHeaderButton> _buttons = new();
    private static Brush _originalBrush;
    private bool _isClicked;
    private bool _disposing;

    public ColumnHeaderButton()
    {
        InitializeComponent();
        SortingOrder = SortingOrder.None;
        _buttons.Add(this);
    }


    
    private static void SortingOrderChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColumnHeaderButton btn && btn.SortingOrder is not SortingOrder.None && !btn._isClicked && !btn._disposing)
        {
            switch (btn.SortingOrder)
            {
                case SortingOrder.Ascending:
                    {
                        btn._text = btn.Text;
                        btn.Text = btn._text + $" ↑";
                        btn.innerText.Foreground = btn.PointerHoverColor;
                        break;
                    }
                case SortingOrder.Descending:
                    {
                        btn._text = btn.Text;
                        btn.Text = btn._text + $" ↓";
                        btn.innerText.Foreground = btn.PointerHoverColor;
                        break;
                    }
                
            }
        }
    }
    private static void TextChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColumnHeaderButton btn && !btn._disposing)
        {

        }
    }

    private void btn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && IsEnabled)
        {
            _isClicked = true;
            switch (SortingOrder)
            {
                case SortingOrder.None:
                    {
                        if (_originalBrush is null)
                        {
                            _originalBrush = innerText.Foreground;
                        }
                        ResetAllButtons();
                        _text = Text;
                        SortingOrder = SortingOrder.Ascending;
                        Text = _text + $" ↑";
                        innerText.Foreground = PointerHoverColor;
                        
                        break;
                    }
                case SortingOrder.Descending:
                    {
                        SortingOrder = SortingOrder.Ascending;
                        Text = _text + $" ↑";
                        innerText.Foreground = PointerHoverColor;
                        
                        break;
                    }
                case SortingOrder.Ascending:
                    {
                        SortingOrder = SortingOrder.Descending;
                        Text = _text + $" ↓";
                        innerText.Foreground = PointerHoverColor;
                       
                        break;
                    }
            }
        }
        _isClicked = false;

    }

    private void ResetAllButtons()
    {
        foreach (ColumnHeaderButton btn in _buttons)
        {
            if (btn.SortingOrder is not SortingOrder.None)
            {
                btn.Text = btn._text ?? btn.Text;
                btn.SortingOrder = SortingOrder.None;
                btn.innerText.Foreground = _originalBrush;
            }
                
        }
    }

    public void Dispose()
    {
        _disposing = true;

        Command = null;
    }
}
