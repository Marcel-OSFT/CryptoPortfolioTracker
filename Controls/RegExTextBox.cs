using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using CommunityToolkit.WinUI.UI;
using System.Xml.Linq;
using CryptoPortfolioTracker.Converters;
using Microsoft.UI.Xaml.Shapes;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using System.Windows.Markup;
using System.Reflection.Metadata;
using System.Buffers.Binary;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using CryptoPortfolioTracker;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls
{

    public enum MyEnum
    {
        RegExDecimal,
        RegExPositiveDecimal,
        RegExInt,
        RegExPositiveInt,
        RegExEmail,
        RegExPhone
    }

    public sealed class RegExTextBox : TextBox, INotifyPropertyChanged
    {

        public event EventHandler TextChanged;
        public event EventHandler<PointerRoutedEventArgs> PointerPressed;

        public event PropertyChangedEventHandler PropertyChanged;
        public RegExTextBox()
        {
            this.DefaultStyleKey = typeof(TextBox);
            this.AddHandler(TextBox.PointerPressedEvent, new PointerEventHandler(This_PointerPressed), true);

        }

        protected override void OnApplyTemplate()
        {
            if (CustomRegEx != null && CustomRegEx != "") regExChoosen = CustomRegEx;
            //Text = "0";
            //TextBoxExtensions.SetRegex(this, regExChoosen);
            TextBoxExtensions.SetValidationMode(this, TextBoxExtensions.ValidationMode.Forced);

            (this as TextBox).TextChanged += TextEntryChanged;
            if (AnimateBorder) InitBorderBrush();

            base.OnApplyTemplate();
        }


        //private bool isEntryValid;
        public bool AnimateBorder { get; set; } = true;
        public bool IsZeroAllowed { get; set; } = true;
        private static string regExChoosen;

        public bool IsEntryValid
        {
            get { return (bool)GetValue(IsEntryValidProperty); }
            set { SetValue(IsEntryValidProperty, value); }
        }
        public MyEnum RegEx
        {
            get { return (MyEnum)GetValue(RegExProperty); }
            set { SetValue(RegExProperty, value); }
        }
        private string customRegEx;
        public string CustomRegEx
        {
            get { return customRegEx; }
            set
            {
                if (value == customRegEx) return;
                customRegEx = value;
                TextBoxExtensions.SetRegex(this, customRegEx);
                OnPropertyChanged(nameof(CustomRegEx));
            }
        }
        public string MyText
        {
            get { return (string)GetValue(MyTextProperty); }
            set
            {
                SetValue(MyTextProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for Ownenum.  This enables animation, styling, binding, etc...  
        public static readonly DependencyProperty RegExProperty = DependencyProperty.Register("RegEx", typeof(MyEnum), typeof(RegExTextBox), new PropertyMetadata(0, new PropertyChangedCallback(RegExChangedCallBack)));
        public static readonly DependencyProperty IsEntryValidProperty = DependencyProperty.Register("IsEntryValid", typeof(bool), typeof(RegExTextBox), new PropertyMetadata(0, new PropertyChangedCallback(IsEntryValidChangedCallBack)));
        public static readonly DependencyProperty MyTextProperty = DependencyProperty.Register("MyText", typeof(string), typeof(RegExTextBox), new PropertyMetadata(0, new PropertyChangedCallback(MyTextEntryChangedCallBack)));


        private static MyEnum SelectedRegEx;

        private static void MyTextEntryChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            
        }

        private static void IsEntryValidChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((d as RegExTextBox).AnimateBorder)
            {
                (d as RegExTextBox).SetBorderColor();
            }
        }

        

        private void TextEntryChanged(object sender, RoutedEventArgs e)
        {
            // Raise an event on the custom control when 'like' is clicked
            
            if ((this.Text == "" || ( this.Text == "0" && !IsZeroAllowed)) && (SelectedRegEx != MyEnum.RegExEmail && SelectedRegEx != MyEnum.RegExPhone))
            {
                IsEntryValid = false;
                Debug.WriteLine("GetIsValid(1) - " + TextBoxExtensions.GetIsValid(this).ToString());
            }
            else
            {
                IsEntryValid = TextBoxExtensions.GetIsValid(this);
                Debug.WriteLine("GetIsValid(2) - " + TextBoxExtensions.GetIsValid(this).ToString());

            }
            Debug.WriteLine("GetRegEx - " + TextBoxExtensions.GetRegex(this).ToString());
            Debug.WriteLine("Text is now - " + this.Text);
            OnPropertyChanged(nameof(this.Text));
            TextChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetBorderColor()
        {
            if (IsEntryValid)
            {
                this.BorderBrush = new SolidColorBrush(Colors.Green);
            }
            else this.BorderBrush = new SolidColorBrush(Colors.Red);
        }
        private void InitBorderBrush()
        {
            if ((this.Text == "0" && !IsZeroAllowed) && (SelectedRegEx != MyEnum.RegExEmail && SelectedRegEx != MyEnum.RegExPhone))
            {
                this.BorderBrush = new SolidColorBrush(Colors.Green);
            }
        }

        private static void RegExChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {   // process logic  
            var control = d as RegExTextBox;

            SelectedRegEx = (MyEnum)e.NewValue;
            switch (SelectedRegEx)
            {
                case MyEnum.RegExPositiveDecimal:
                    regExChoosen = App.Current.Resources["RegExPositiveDecimal"] as string;
                    break;
                case MyEnum.RegExPositiveInt:
                    regExChoosen = App.Current.Resources["RegExPositiveInt"] as string;
                    break;
                case MyEnum.RegExEmail:
                    regExChoosen = App.Current.Resources["RegExEmail"] as string;
                    break;
                case MyEnum.RegExPhone:
                    // To-Do -> regExChoosen = @"^([0 - 9]) * (\.[0 - 9]+)?$";
                    break;
                case MyEnum.RegExDecimal:
                    regExChoosen = App.Current.Resources["RegExDecimal"] as string;
                    break;
                case MyEnum.RegExInt:
                    regExChoosen = App.Current.Resources["RegExInt"] as string;
                    break;
                default:
                    break;
            }
            TextBoxExtensions.SetRegex(control, regExChoosen);

        }

        private void This_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PointerPressed?.Invoke(this, e);
        }
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}