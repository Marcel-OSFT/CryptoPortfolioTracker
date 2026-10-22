using System;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Models;

public enum BubbleMapType
{
    Target,
    BreakEven,
    Rsi,
    Ma
}

public sealed class BubbleMap
{
    private const double BreakEvenMinimum = -1050;

    private readonly BubbleMapType _type;
    public BubbleMapType Type => _type;

    public int Index { get; }

    public string Title { get; }

    public Axis[] YAxes { get; }

    public RectangularSection[] Sections { get; }

    public double[] Separators { get; private set; } = Array.Empty<double>();

    public BubbleMap(
        int index,
        BubbleMapType type,
        string title,
        string axisTitleResourceKey,
        double minLimit,
        double maxLimit,
        SKColor sectionColor)
    {
        Index = index;
        _type = type;
        Title = title;

        YAxes =
        [
            new Axis
            {
                MinLimit = minLimit,
                MaxLimit = maxLimit,
                Name = Localizer.Get().GetLocalizedString(axisTitleResourceKey),
                NamePadding = new LiveChartsCore.Drawing.Padding(0, 0),
                NamePaint = CreatePaint(SKColors.DarkGoldenrod, 15),
                NameTextSize = 15,
                TextSize = 12,
                LabelsPaint = CreatePaint(SKColors.DarkGoldenrod, 12),
                Padding = new LiveChartsCore.Drawing.Padding(1),
                Labeler = value => $"{value:N0}",
                SeparatorsPaint = new SolidColorPaint
                {
                    Color = SKColors.DarkGoldenrod,
                    StrokeThickness = 1
                }
            }
        ];

        Sections =
        [
            new RectangularSection
            {
                Yi = 0,
                Yj = 0,
                Fill = new SolidColorPaint(sectionColor.WithAlpha(100))
            }
        ];
    }

    public void Configure(double withinRangePerc)
    {
        Separators = _type switch
        {
            BubbleMapType.Target =>
            [
                -100,
                -50,
                -withinRangePerc,
                0,
                100
            ],

            BubbleMapType.BreakEven =>
            [
                -1000,
                -500,
                -withinRangePerc,
                0,
                100
            ],

            BubbleMapType.Rsi =>
            [
                0,
                30,
                50,
                70,
                100
            ],

            BubbleMapType.Ma =>
            [
                -100,
                -0.5 * withinRangePerc,
                0,
                0.5 * withinRangePerc,
                100
            ],

            _ => Array.Empty<double>()
        };

        var section = Sections[0];

        switch (_type)
        {
            case BubbleMapType.Target:
            case BubbleMapType.BreakEven:
                section.Yi = Separators[2];
                section.Yj = Separators[3];
                break;

            case BubbleMapType.Rsi:
                section.Yi = Separators[1];
                section.Yj = Separators[3];
                break;

            case BubbleMapType.Ma:
                section.Yi = Separators[1];
                section.Yj = Separators[3];
                break;
        }

        YAxes[0].CustomSeparators = Separators;
    }

    public string FormatTooltip(HeatMapPoint point)
    {
        return _type switch
        {
            BubbleMapType.Target =>
                $"{point.Label} {point.Y:N2} % ({point.Weight:N1} % of Portfolio)",

            BubbleMapType.Rsi =>
                $"{point.Label} RSI: {point.Y:N2}",

            BubbleMapType.Ma =>
                $"{point.Label} Dist to EMA: {point.Y:N2} %",

            BubbleMapType.BreakEven =>
                $"{point.Label} Dist to BE: {FormatBreakEvenValue(point)} % ({point.Weight:N1} % of Portfolio)",

            _ => point.Label
        };
    }

    private static string FormatBreakEvenValue(HeatMapPoint point)
    {
        return point.Y <= BreakEvenMinimum
            ? $"{point.RealYValue:N0}"
            : $"{point.Y:N0}";
    }

    private static SolidColorPaint CreatePaint(SKColor color, double textSize)
    {
        return new SolidColorPaint
        {
            Color = color,
            FontFamily = "Times New Roman",
            SKFontStyle = new SKFontStyle(
                SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Italic)
        };
    }
}