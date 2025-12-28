using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using TemperatureMonitor.Enums;

namespace TemperatureMonitor.Controls
{
    public sealed partial class DaySelectorControl : UserControl
    {
        public DaySelectorControl()
        {
            this.InitializeComponent();
            // initialize to sensible defaults
            SelectedMode = DaySelectorMode.Nu;
            SelectedDate = DateTime.Now.Date;
        }

        // SelectedMode DP
        public static readonly DependencyProperty SelectedModeProperty =
            DependencyProperty.Register(nameof(SelectedMode), typeof(DaySelectorMode), typeof(DaySelectorControl),
                new PropertyMetadata(DaySelectorMode.Nu, OnSelectedModeChanged));

        public DaySelectorMode SelectedMode
        {
            get => (DaySelectorMode)GetValue(SelectedModeProperty);
            set => SetValue(SelectedModeProperty, value);
        }
        private static void OnSelectedModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaySelectorControl ctrl && e.NewValue is DaySelectorMode newMode)
            {
                ctrl.OnModeChanged(newMode);
            }
        }
        // SelectedDate DP (changed to DateTime)
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime), typeof(DaySelectorControl),
                new PropertyMetadata(DateTime.Now.Date, OnSelectedDateChanged));

        public DateTime SelectedDate
        {
            get => (DateTime)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (DaySelectorControl)d;
            ctrl.UpdateNavButtons();
            ctrl.RaiseSelectedDateChanged();
        }

        // Optional events
        public event EventHandler<DaySelectorMode>? ModeChanged;
        public event EventHandler<DateTime>? SelectedDateChanged;

        private void OnModeChanged(DaySelectorMode newMode)
        {
            if (newMode == DaySelectorMode.Dag)
            {
                if (SelectedDate == default)
                    SelectedDate = DateTime.Now.Date;
            }

            UpdateNavButtons();
            ModeChanged?.Invoke(this, newMode);
        }

        private void UpdateNavButtons()
        {
            var today = DateTime.Now.Date;
            bool isToday = SelectedDate.Date == today;
            if (NextButton != null)
                NextButton.IsEnabled = !isToday;
        }

        private void RaiseSelectedDateChanged()
        {
            SelectedDateChanged?.Invoke(this, SelectedDate);
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e) => SelectedDate = SelectedDate.AddDays(-1);

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var newDate = SelectedDate.AddDays(1);
            var today = DateTime.Now.Date;
            if (newDate.Date > today) newDate = today;
            SelectedDate = newDate;
        }

        private void JumpToLatestButton_Click(object sender, RoutedEventArgs e) => SelectedDate = DateTime.Now.Date;

        private void ModeToggle_Toggled(object sender, RoutedEventArgs e) => SelectedMode = (ModeToggle.IsOn) ? DaySelectorMode.Dag :  DaySelectorMode.Nu;

        //private void DayToggle_Click(object sender, RoutedEventArgs e) => SelectedMode = DaySelectorMode.Dag;
    }
}