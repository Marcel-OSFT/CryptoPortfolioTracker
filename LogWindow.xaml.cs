using CryptoPortfolioTracker;
using CryptoPortfolioTracker.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.WinUi3;
using Serilog.Sinks.WinUi3.LogViewModels;
using Serilog.Templates;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker;

public sealed partial class LogWindow : Window
{
    private CancellationTokenSource? _cancellationTokenSource;
    private LoggingLevelSwitch _levelSwitch;
    private ItemsRepeaterLogBroker _logBroker;

    public LogWindow()
    {
        this.InitializeComponent();

       
        _cancellationTokenSource = new CancellationTokenSource();

        _levelSwitch = new LoggingLevelSwitch();
        _levelSwitch.MinimumLevel = LogEventLevel.Verbose;

        string[] levels = Enum.GetNames(typeof(LogEventLevel));

        foreach (string level in levels)
        {
            LevelSwitcher.Items.Add(level.ToString());
        }

        LevelSwitcher.SelectedIndex = 0;
        LevelSwitcher.SelectionChanged += ((sender, e) =>
        {
            if (sender is ComboBox comboBox &&
                Enum.TryParse<LogEventLevel>(comboBox.SelectedItem.ToString(), out LogEventLevel level) is true)
                _levelSwitch.MinimumLevel = level;
        });

        App.Current.Resources.TryGetValue("DefaultTextForegroundThemeBrush", out object defaultTextForegroundBrush);

        _logBroker = new ItemsRepeaterLogBroker(
            LogViewer,
            LogScrollViewer,
            new EmojiLogViewModelBuilder((defaultTextForegroundBrush as SolidColorBrush)?.Color)

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
            .MinimumLevel.ControlledBy(_levelSwitch)
            .WriteTo.WinUi3Control(_logBroker)
            .WriteTo.File(App.appDataPath + "\\log.txt",
                       rollingInterval: RollingInterval.Day,
                       retainedFileCountLimit: 3,
                       outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}]  {SourceContext:lj}  {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

//        Logger = Log.Logger.ForContext<MainWindow>();
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(MainWindow).Name.PadRight(22));

        App.userPreferences.AttachLogger();

        _logBroker.IsAutoScrollOn = true;
    }

    private ILogger Logger
    {
        get; set;
    }

    private void AutoScrollToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch && _logBroker is not null)
            _logBroker.IsAutoScrollOn = toggleSwitch.IsOn;
    }

    

   

    private void UpdateToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch && _logBroker is not null)
            LogViewer.Visibility = toggleSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Window_Closed(object sender, WindowEventArgs args)
    {
        Log.CloseAndFlush();
    }
}