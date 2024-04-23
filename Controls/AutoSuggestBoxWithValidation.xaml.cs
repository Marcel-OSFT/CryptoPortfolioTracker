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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls
{
    public sealed partial class AutoSuggestBoxWithValidation : UserControl    //, INotifyPropertyChanged
    {

        public AutoSuggestBoxWithValidation()
        {
            this.InitializeComponent();
            //innerASBox.TextChanged += TextEntryChanged;
            //** PointerPressedEvent needs to be added is this 'special' way.
            //This because its handled earlier in the system and not passed through.
            innerASBox.AddHandler(TextBox.PointerPressedEvent, new PointerEventHandler(innerASBox_PointerPressed), true);
            innerASBox.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler(innerASBox_KeyDown), true);
            innerASBox.Visibility = Visibility.Collapsed;
        }

        

        //private static AutoSuggestBox asb;
        public bool AnimateBorder { get; set; } = true;
        public bool IsPlaceholderSet { get; set; } = false;


        public event EventHandler TextChanged;
        public event EventHandler<AutoSuggestBoxSuggestionChosenEventArgs> SuggestionChosen;
        public event EventHandler<KeyRoutedEventArgs> KeyDown;

        public bool IsEntryMatched
        {
            get
            {
                return (bool)GetValue(IsEntryMatchedProperty);
            }
            set
            {
                SetValue(IsEntryMatchedProperty, value);
            }
        }
        public List<string> ItemsSource
        {
            get
            {
                return (List<string>)GetValue(ItemsSourceProperty);
            }
            set
            {
                SetValue(ItemsSourceProperty, value);
            }
        }
        public string MyText
        {
            get
            {
                return (string)GetValue(MyTextProperty);
            }
            set
            {
                SetValue(MyTextProperty, value);
            }
        }

        public string Header
        {
            get
            {
                return (string)GetValue(HeaderProperty);
            }
            set
            {
                SetValue(HeaderProperty, value);
            }
        }
        public string MyPlaceholderText
        {
            get
            {
                return (string)GetValue(MyPlaceholderTextProperty);
            }
            set
            {
                SetValue(MyPlaceholderTextProperty, value);
            }
        }

        public static readonly DependencyProperty IsEntryMatchedProperty = DependencyProperty.Register("IsEntryMatched", typeof(bool), typeof(AutoSuggestBoxWithValidation), new PropertyMetadata(0, new PropertyChangedCallback(IsEntryMatchedChangedCallBack)));
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(List<string>), typeof(AutoSuggestBoxWithValidation), new PropertyMetadata(0, new PropertyChangedCallback(ItemsSourceChangedCallBack)));
        public static readonly DependencyProperty MyTextProperty = DependencyProperty.Register("MyText", typeof(string), typeof(AutoSuggestBoxWithValidation), new PropertyMetadata(0, new PropertyChangedCallback(TextEntryChangedCallBack)));
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(string), typeof(AutoSuggestBoxWithValidation), null);
        public static readonly DependencyProperty MyPlaceholderTextProperty = DependencyProperty.Register("MyPlaceholderText", typeof(string), typeof(AutoSuggestBoxWithValidation), new PropertyMetadata(0, new PropertyChangedCallback(PlaceholderTextChangedCallBack)));

        private void innerASBox_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            innerASBox.IsEnabled = true;
            innerASBox.IsSuggestionListOpen = true;
        }
        
        private void innerASBox_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            IsEntryMatched = true;

            SuggestionChosen?.Invoke(this, args);

        }
        private static void PlaceholderTextChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AutoSuggestBoxWithValidation thisInstance = (AutoSuggestBoxWithValidation)d;

            if (thisInstance.MyPlaceholderText != null && thisInstance.MyPlaceholderText != "Select item..." & thisInstance.MyPlaceholderText != "No items available!") thisInstance.IsPlaceholderSet = true;
        }
        private static void TextEntryChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AutoSuggestBox inner = ((AutoSuggestBoxWithValidation)d).innerASBox;
            AutoSuggestBoxWithValidation thisInstance = (AutoSuggestBoxWithValidation)d;

            if (inner.Visibility == Visibility.Collapsed && (string)e.NewValue == "") inner.Visibility = Visibility.Visible;

            inner.IsSuggestionListOpen = false;
            thisInstance.PopulateSuitableItems();
            thisInstance.IsEntryMatched = thisInstance.DoesEntryMatch();

            thisInstance.TextChanged?.Invoke(thisInstance, EventArgs.Empty);

        }
        private static void ItemsSourceChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            List<string> result = (List<string>)d.GetValue(ItemsSourceProperty);
            AutoSuggestBoxWithValidation thisInstance = (AutoSuggestBoxWithValidation)d; 
            AutoSuggestBox inner = (d as AutoSuggestBoxWithValidation).innerASBox;

            if (inner.ItemsSource == null || !inner.ItemsSource.Equals(d.GetValue(ItemsSourceProperty))) inner.ItemsSource = d.GetValue(ItemsSourceProperty);

            if (result.Count == 1)
            {
                d.SetValue(MyTextProperty, result.First().ToString());
                thisInstance.TextChanged?.Invoke(thisInstance, EventArgs.Empty);
            }


            if (!thisInstance.IsPlaceholderSet)
            {
                inner.PlaceholderText = inner.Items.Count > 0 ? "Select item..." : "No items available!";
            }
            else inner.PlaceholderText = thisInstance.MyPlaceholderText;

        }

        private static void IsEntryMatchedChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((d as AutoSuggestBoxWithValidation).AnimateBorder)
            {
                (d as AutoSuggestBoxWithValidation).SetBorderColor();
            }
        }


        private bool DoesEntryMatch()
        {
            return ItemsSource.Contains(MyText);
        }

        private void PopulateSuitableItems()
        {

            var suitableItems = new List<string>();
            var splitText = innerASBox.Text.ToLower().Split(" ");
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
                    if (MyText.ToLower() == item.ToLower() && MyText != item) MyText = item;
                }
            }
            if (suitableItems.Count == 0)
            {
                suitableItems.Add("No results found");
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
            else innerASBox.BorderBrush = new SolidColorBrush(Colors.Red);
        }

        private void innerASBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                Debug.WriteLine("enter-key");
                IsEntryMatched = DoesEntryMatch();
                KeyDown?.Invoke(this, e);
            }
        }
    }
}
