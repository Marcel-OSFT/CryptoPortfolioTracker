using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView.Painting;

namespace CryptoPortfolioTracker.Models;


public class AssetInfo : ObservableValue
{
    public AssetInfo(string name, int value, SolidColorPaint paint)
    {
        Name = name;

        // the ObservableValue.Value property is used by the chart
        Value = value;
    }

    public string Name { get; set; }
}


