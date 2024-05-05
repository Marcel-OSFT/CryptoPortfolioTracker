using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace CryptoPortfolioTracker.Controls;


public enum MyEnum
{
    RegExDecimal,
    RegExPositiveDecimal,
    RegExInt,
    RegExPositiveInt,
    RegExEmail,
    RegExPhone
}

public class RegExTextBox : TextBox, INotifyPropertyChanged
{
    public event EventHandler? TextChanged;
    //public event EventHandler<KeyRoutedEventArgs>? KeyDown;
    public event EventHandler<PointerRoutedEventArgs>? PointerPressed;

    public RegExTextBox()
    {
        DefaultStyleKey = typeof(TextBox);
        AddHandler(TextBox.PointerPressedEvent, new PointerEventHandler(This_PointerPressed), true);
        customRegEx = string.Empty;
    }

    protected override void OnApplyTemplate()
    {
        if (CustomRegEx != null && CustomRegEx != string.Empty)
        {
            regExChoosen = CustomRegEx;
        }

        TextBoxExtensions.SetValidationMode(this, TextBoxExtensions.ValidationMode.Forced);

        (this as TextBox).TextChanged += TextEntryChanged;
        

        if (AnimateBorder)
        {
            InitBorderBrush();
        }

        base.OnApplyTemplate();
    }


    public bool AnimateBorder { get; set; } = true;
    public bool IsZeroAllowed { get; set; } = true;
    private string? regExChoosen;

    private static MyEnum SelectedRegEx;
    private static bool invalidKeyEntered;
    
    public bool IsEntryValid
    {
        get => (bool)GetValue(IsEntryValidProperty);
        set => SetValue(IsEntryValidProperty, value);
    }
    public MyEnum RegEx
    {
        get => (MyEnum)GetValue(RegExProperty);
        set => SetValue(RegExProperty, value);
    }
    private string customRegEx;
    public string CustomRegEx
    {
        get => customRegEx;
        set
        {
            if (value == customRegEx)
            {
                return;
            }

            customRegEx = value;
            TextBoxExtensions.SetRegex(this, customRegEx);
            OnPropertyChanged();
        }
    }

    // Using a DependencyProperty as the backing store for Ownenum.  This enables animation, styling, binding, etc...  
    public static readonly DependencyProperty RegExProperty = DependencyProperty.Register("RegEx", typeof(MyEnum), typeof(RegExTextBox), new PropertyMetadata(0, new PropertyChangedCallback(RegExChangedCallBack)));
    public static readonly DependencyProperty IsEntryValidProperty = DependencyProperty.Register("IsEntryValid", typeof(bool), typeof(RegExTextBox), new PropertyMetadata(0, new PropertyChangedCallback(IsEntryValidChangedCallBack)));


    private static void IsEntryValidChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if ((d is RegExTextBox tbox) && tbox.AnimateBorder)
        {
            tbox.SetBorderColor();
        }
    }

    private void TextEntryChanged(object sender, RoutedEventArgs e)
    {
       if (invalidKeyEntered)
        {
            var cursorPosition = SelectionStart - 1;
            if (cursorPosition >= 0) 
            {
                Text = Text.Remove(cursorPosition, 1);
                SelectionStart = cursorPosition ;
                invalidKeyEntered = false;
            }
            return;
        }
        
        if ((Text == "" || (Text == "0" && !IsZeroAllowed)) 
            && SelectedRegEx != MyEnum.RegExEmail && SelectedRegEx != MyEnum.RegExPhone)
        {
            IsEntryValid = false;
        }
        else
        {
            IsEntryValid = TextBoxExtensions.GetIsValid(this);
        }
        OnPropertyChanged(nameof(Text));
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetBorderColor()
    {
        if (IsEntryValid)
        {
            BorderBrush = new SolidColorBrush(Colors.Green);
        }
        else
        {
            BorderBrush = new SolidColorBrush(Colors.Red);
        }
    }
    private void InitBorderBrush()
    {
        if (Text == "0" && !IsZeroAllowed && SelectedRegEx != MyEnum.RegExEmail && SelectedRegEx != MyEnum.RegExPhone)
        {
            BorderBrush = new SolidColorBrush(Colors.Green);
        }
    }

    private static void RegExChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {   // process logic  
        if (d is not RegExTextBox thisInstance)
        {
            return;
        }

        SelectedRegEx = (MyEnum)e.NewValue;
        switch (SelectedRegEx)
        {
            case MyEnum.RegExPositiveDecimal:
                thisInstance.regExChoosen = App.userPreferences.NumberFormat.NumberDecimalSeparator == "." 
                    ? App.Current.Resources["RegExPositiveDecimalEn"] as string 
                    : App.Current.Resources["RegExPositiveDecimalNl"] as string;
                thisInstance.KeyDown += (sender, e) => KeyDownNumeric(sender, e, true, true);
                
                break;
            case MyEnum.RegExPositiveInt:
                thisInstance.regExChoosen = App.userPreferences.NumberFormat.NumberDecimalSeparator == "." 
                    ? App.Current.Resources["RegExPositiveIntEn"] as string 
                    : App.Current.Resources["RegExPositiveIntNl"] as string;
                thisInstance.KeyDown += (sender, e) => KeyDownNumeric(sender, e, false, true);
                break;
            case MyEnum.RegExEmail:
                thisInstance.regExChoosen = App.Current.Resources["RegExEmail"] as string;
                break;
            case MyEnum.RegExPhone:
                // To-Do -> regExChoosen = @"^([0 - 9]) * (\.[0 - 9]+)?$";
                break;
            case MyEnum.RegExDecimal:
                thisInstance.regExChoosen = App.userPreferences.NumberFormat.NumberDecimalSeparator == "." 
                    ? App.Current.Resources["RegExDecimalEn"] as string 
                    : App.Current.Resources["RegExDecimalNl"] as string;
                thisInstance.KeyDown += (sender, e) => KeyDownNumeric(sender, e, true, false);
                break;
            case MyEnum.RegExInt:
                thisInstance.regExChoosen = App.userPreferences.NumberFormat.NumberDecimalSeparator == "." 
                    ? App.Current.Resources["RegExIntEn"] as string 
                    : App.Current.Resources["RegExIntNl"] as string;
                thisInstance.KeyDown += (sender, e) => KeyDownNumeric(sender, e, false, false);
               
                break;
            default:
                break;
        }
        TextBoxExtensions.SetRegex(thisInstance, thisInstance.regExChoosen);
    }

    private void This_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        PointerPressed?.Invoke(this, e);
    }

   
    private static void KeyDownNumeric(object sender, KeyRoutedEventArgs e, bool _isDecimal, bool _isOnlyPositive)
    {
        if (sender is not RegExTextBox thisInstance)
        {
            return;
        }
        // Initialize the flag to false.
        invalidKeyEntered = false;
        var decimalChar = App.userPreferences.NumberFormat.NumberDecimalSeparator;
        var decimalKey = decimalChar == "." ? 190 : 188;

        // Determine whether the keystroke is a number from the top of the keyboard.
        if (e.Key < VirtualKey.Number0 || e.Key > VirtualKey.Number9)
        {
            // Determine whether the keystroke is a number from the keypad.
            if (e.Key < VirtualKey.NumberPad0 || e.Key > VirtualKey.NumberPad9)
            {
                // Determine whether the keystroke is a backspace.
                if (e.Key != VirtualKey.Back)
                {   
                    //Determine wether the keystroke is the decimal key
                    var tbox = sender as TextBox ?? new();
                    if (!_isDecimal
                            || (tbox.Text.Contains(decimalChar)) 
                            || Convert.ToInt32(e.Key) != decimalKey)
                    {
                        //Determine wether the keystroke is the '-' key
                        //check if '-' already exists, allow the '-' key if all tekst is selected and will be overwritten
                        //'-' sign can only occur at the beginning at position 0
                        if (thisInstance.SelectionStart != 0 || _isOnlyPositive 
                            || (tbox.Text.Contains('-') && thisInstance.SelectedText.Length != thisInstance.Text.Length) 
                            || (e.Key != VirtualKey.Subtract 
                            && Convert.ToInt32(e.Key) != 189))
                        {
                            // Finally the reuslt is that A non-numerical keystroke was pressed.
                            // Set the flag to true and take action in TextChanged event.
                            invalidKeyEntered = true;
                        }
                    }
                }
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

}
