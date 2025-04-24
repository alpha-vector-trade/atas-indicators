namespace ATAS.Indicators.AlphaVector;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Media;

using ATAS.Indicators.Drawing;

using OFT.Attributes;
using OFT.Rendering.Context;
using OFT.Rendering.Settings;
using OFT.Rendering.Tools;

using Utils.Common.Logging;

using Color = System.Drawing.Color;

[DisplayName("Timeframe Marker")]
[Category("AlphaVector")]
[HelpLink("https://github.com/alpha-vector-trade/atas-indicators")]
public class TimeFrameMarker : Indicator
{
    public enum PeriodType
    {
        [Display(Name = "Day")]
        Day,

        [Display(Name = "Week")]
        Week,

        [Display(Name = "Month")]
        Month
    }

    private PeriodType _periodType = PeriodType.Day;
    private int _lastSession;

    // Use PenSettings for line appearance
    private PenSettings _linePen = new() { Color = Colors.Black, Width = 2 };

    // Track marker bars independently of DataSeries
    private readonly HashSet<int> _periodMarkers = new();

    public TimeFrameMarker() : base(true)
    {
        DenyToChangePanel = true;
        EnableCustomDrawing = true;
        SubscribeToDrawingEvents(DrawingLayouts.Final);

        DataSeries[0].IsHidden = true;
    }

    [Display(Name = "Period Type", Order = 100)]
    public PeriodType Period
    {
        get => _periodType;
        set
        {
            _periodType = value;
            RecalculateValues();
        }
    }

    [Display(Name = "Line", Order = 200, GroupName = "Line")]
    public PenSettings LinePen
    {
        get => _linePen;
        set
        {
            _linePen = value;
            RecalculateValues();
        }
    }

    protected override void OnCalculate(int bar, decimal value)
    {
        if (bar == 0)
        {
            _lastSession = 0;
            _periodMarkers.Clear();
            return;
        }

        var isNewPeriod = Period switch
        {
            PeriodType.Day => IsNewSession(bar),
            PeriodType.Week => IsNewWeek(bar),
            PeriodType.Month => IsNewMonth(bar),
            _ => throw new ArgumentOutOfRangeException()
        };

        if (isNewPeriod && _lastSession != bar)
        {
            _periodMarkers.Add(bar);
            _lastSession = bar;
        }
    }

    protected override void OnRender(RenderContext context, DrawingLayouts layout)
    {
        var pen = _linePen.RenderObject;

        // Draw vertical lines where marked in _periodMarkers
        foreach (var bar in _periodMarkers)
        {
            if (bar < FirstVisibleBarNumber || bar > LastVisibleBarNumber)
                continue;

            var x = ChartInfo.GetXByBar(bar);
            context.DrawLine(pen, x, Container.Region.Top, x, Container.Region.Bottom);
        }
    }
}
