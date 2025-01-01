using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Helpers;
using CryptoPortfolioTracker.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.MemorySink;
using Serilog.Extensions.Logging;
using Serilog.Sinks.WinUi3;

using Serilog.Sinks.WinUi3.LogViewModels;
using Serilog.Templates;

namespace CryptoPortfolioTracker;

[ObservableObject]
public sealed partial class LogWindow : Window
{
    [ObservableProperty] private LoggingLevelSwitch _levelSwitch;
    [ObservableProperty] private ItemsRepeaterLogBroker _logBroker;
    private readonly IPreferencesService _preferencesService;
    private ILogger Logger  { get; set; }


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public LogWindow(IPreferencesService preferencesService)
    {
        InitializeComponent();
        _preferencesService = preferencesService;
        ConfigureLogger();
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private void ConfigureLogger()
    {
        LevelSwitch = new LoggingLevelSwitch
        {
            MinimumLevel = LogEventLevel.Verbose
        };

        var levels = Enum.GetNames(typeof(LogEventLevel));

        foreach (var level in levels)
        {
            LevelSwitcher.Items.Add(level.ToString());
        }

        LevelSwitcher.SelectedIndex = 0;
        LevelSwitcher.SelectionChanged += (sender, e) =>
        {
            if (sender is ComboBox comboBox &&
                Enum.TryParse<LogEventLevel>(comboBox.SelectedItem.ToString(), out var level) is true)
            {
                LevelSwitch.MinimumLevel = level;
                
            }
        };

        App.Current.Resources.TryGetValue("DefaultTextForegroundThemeBrush", out var defaultTextForegroundBrush);

        LogBroker = new ItemsRepeaterLogBroker(
            LogViewer,
            LogScrollViewer,
            new  EmojiLogViewModelBuilder((defaultTextForegroundBrush as SolidColorBrush)?.Color)

                .SetTimestampFormat(new ExpressionTemplate("[{@t:yyyy-MM-dd HH:mm:ss.fff}]"))
                .SetLevelsFormat(new ExpressionTemplate("{@l:u3}"))
                .SetLevelForeground(LogEventLevel.Verbose, Colors.Gray)
                .SetLevelForeground(LogEventLevel.Debug, Colors.Gray)
                .SetLevelForeground(LogEventLevel.Warning, Colors.Yellow)
                .SetLevelForeground(LogEventLevel.Error, Colors.Red)
                .SetLevelForeground(LogEventLevel.Fatal, Colors.HotPink)
                .SetSourceContextFormat(new ExpressionTemplate("{Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}"))
                .SetMessageFormat(new ExpressionTemplate("{@m}"))
                .SetMessageForeground(LogEventLevel.Verbose, Colors.Gray)
                .SetMessageForeground(LogEventLevel.Debug, Colors.Gray)
                .SetMessageForeground(LogEventLevel.Warning, Colors.Yellow)
                .SetMessageForeground(LogEventLevel.Error, Colors.Red)
                .SetMessageForeground(LogEventLevel.Fatal, Colors.HotPink)
                .SetExceptionFormat(new ExpressionTemplate("{@x}"))
                .SetExceptionForeground(Colors.HotPink)
                );

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LevelSwitch)
            .WriteTo.WinUi3Control(LogBroker)

            .WriteTo.File(App.appDataPath + "\\log.txt",
                       rollingInterval: RollingInterval.Day,
                       retainedFileCountLimit: 3,
                       outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}]  {SourceContext:lj}  {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(MainWindow).Name.PadRight(22));

        _preferencesService.AttachLogger();

        LogBroker.IsAutoScrollOn = true;
    }

   
    private void AutoScrollToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch && LogBroker is not null)
        {
            LogBroker.IsAutoScrollOn = toggleSwitch.IsOn;
        }
    }

    private void UpdateToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch && LogBroker is not null)
        {
            LogViewer.Visibility = toggleSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;
        }
    }
    private void Window_Closed(object sender, WindowEventArgs args)
    {
        Log.CloseAndFlush();
    }
}