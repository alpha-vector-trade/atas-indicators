namespace ATAS.Indicators.Technical;

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using OFT.Attributes;
using OFT.Rendering.Context;
using OFT.Rendering.Settings;
using OFT.Rendering.Tools;
using Utils.Common.Logging;

using ATAS.Indicators.Drawing;

using Color = System.Drawing.Color;

[DisplayName("Opening Range")]
public class OpeningRange : Indicator
{
    #region Nested types

    [Serializable]
    [Obfuscation(Feature = "renaming", ApplyToMembers = true, Exclude = true)]
    public enum TimeFrameType
    {
        [Display(Name = "Daily")]
        Daily,

        [Display(Name = "Weekly")]
        Weekly
    }

    #endregion

    #region Fields

    private readonly RenderFont _font = new("Arial", 8);

    private TimeFrameType _timeFrame = TimeFrameType.Weekly;
    private FilterInt _dailyMinutes = new(false) { Value = 30 };
    private bool _showHistoricalRanges = true;
    private bool _renderOnlyAfterFormation = false;
    private int _lookBackDays = 60;

    private decimal _currentHigh;
    private decimal _currentLow;
    private int _currentHighBar;
    private int _currentLowBar;
    private int _currentRangeStartBar;
    private int _currentRangeEndBar;
    private DateTime _currentRangeDate;

    private bool _rangeFormed = false;

    // Dictionary to store historical ranges
    private readonly System.Collections.Generic.Dictionary<DateTime, (decimal high, decimal low, int highBar, int lowBar, int startBar, int endBar)> _historicalRanges =
        new System.Collections.Generic.Dictionary<DateTime, (decimal high, decimal low, int highBar, int lowBar, int startBar, int endBar)>();

    #endregion

    #region Properties

    [Display(Name = "Time Frame", GroupName = "General", Order = 10)]
    public TimeFrameType RangeTimeFrame
    {
        get => _timeFrame;
        set
        {
            _timeFrame = value;
            DailyMinutes.Enabled = value == TimeFrameType.Daily;

            RecalculateValues();
        }
    }

    [Display(Name = "Daily Minutes", GroupName = "General", Order = 20)]
    [Range(1, 1440)]
    public FilterInt DailyMinutes
    {
        get => _dailyMinutes;
        set
        {
            _dailyMinutes = value;
            RecalculateValues();
        }
    }

    [Display(Name = "Show Historical Ranges", GroupName = "Display", Order = 30)]
    public bool ShowHistoricalRanges
    {
        get => _showHistoricalRanges;
        set
        {
            _showHistoricalRanges = value;
            RecalculateValues();
        }
    }

    [Display(Name = "Render Only After Formation", GroupName = "Display", Order = 40)]
    public bool RenderOnlyAfterFormation
    {
        get => _renderOnlyAfterFormation;
        set
        {
            _renderOnlyAfterFormation = value;
            RecalculateValues();
        }
    }

    [Display(Name = "Days Look Back", GroupName = "General", Order = 50)]
    [Range(1, 1000)]
    public int LookBackDays
    {
        get => _lookBackDays;
        set
        {
            _lookBackDays = value;
            RecalculateValues();
        }
    }

    [Display(Name = "Show Text", GroupName = "Display", Order = 60)]
    public bool ShowText { get; set; } = true;

    [Display(Name = "Show Price", GroupName = "Display", Order = 70)]
    public bool ShowPrice { get; set; } = true;

    [Display(Name = "Extend Lines", GroupName = "Display", Order = 75)]
    public bool ExtendLines { get; set; } = false;

    [Display(Name = "High Line", GroupName = "High Range", Order = 80)]
    public PenSettings HighPen { get; set; } = new() { Color = DefaultColors.Green.Convert(), Width = 2 };

    [Display(Name = "High Text", GroupName = "High Range", Order = 90)]
    public string HighText { get; set; } = "Range High";

    [Display(Name = "Low Line", GroupName = "Low Range", Order = 100)]
    public PenSettings LowPen { get; set; } = new() { Color = DefaultColors.Red.Convert(), Width = 2 };

    [Display(Name = "Low Text", GroupName = "Low Range", Order = 110)]
    public string LowText { get; set; } = "Range Low";

    #endregion

    #region ctor

    public OpeningRange()
        : base(true)
    {
        DenyToChangePanel = true;
        EnableCustomDrawing = true;
        SubscribeToDrawingEvents(DrawingLayouts.Final);

        DataSeries[0].IsHidden = true;
        ((ValueDataSeries)DataSeries[0]).VisualType = VisualMode.Hide;

        DailyMinutes.Enabled = _timeFrame == TimeFrameType.Daily;
    }

    #endregion

    #region Protected methods

    protected override void OnCalculate(int bar, decimal value)
    {
        try
        {
            if (bar == 0)
            {
                ResetFields();
                InitializeCalculation();
            }

            var candle = GetCandle(bar);
            var isLastBar = bar == CurrentBar - 1;

            // Check if we're in a new range period
            var isNewRange = IsNewRangePeriod(bar);

            if (isNewRange)
            {
                // If we have a current range and it's formed, save it to historical ranges
                if (_rangeFormed && _currentRangeStartBar > 0)
                {
                    _historicalRanges[_currentRangeDate] = (_currentHigh, _currentLow, _currentHighBar, _currentLowBar,
                        _currentRangeStartBar, _currentRangeEndBar);
                }

                // Start a new range
                StartNewRange(bar, candle);
            }
            else if (_currentRangeStartBar > 0)
            {
                // Check if we're still within the formation period
                if (!_rangeFormed && IsWithinFormationPeriod(bar, candle))
                {
                    UpdateRangeLevels(bar, candle);
                }
                else if (!_rangeFormed)
                {
                    // Mark the range as formed once we exit the formation period
                    _rangeFormed = true;
                    _currentRangeEndBar = bar;
                }
            }

            // For debugging: at the last bar, you could add some diagnostic output
            if (isLastBar)
            {
                // Optional: You could log the current range status here
            }
        }
        catch (Exception e)
        {
            this.LogError("Weekly Opening Range error: ", e);
        }
    }

    protected override void OnRender(RenderContext context, DrawingLayouts layout)
    {
        if (ChartInfo is null)
            return;

        // Draw current range if it exists
        if (_currentRangeStartBar > 0 && (!RenderOnlyAfterFormation || _rangeFormed))
        {
            DrawRangeLines(context, _currentHigh, _currentLow, _currentHighBar, _currentLowBar, _currentRangeStartBar);
        }

        // Draw historical ranges if enabled
        if (ShowHistoricalRanges)
        {
            foreach (var range in _historicalRanges)
            {
                if (range.Value.startBar >= 0 && range.Value.startBar <= LastVisibleBarNumber)
                {
                    DrawRangeLines(context, range.Value.high, range.Value.low, range.Value.highBar, range.Value.lowBar, range.Value.startBar);
                }
            }
        }
    }

    #endregion

    #region Private methods

    private void ResetFields()
    {
        _currentHigh = 0;
        _currentLow = 0;
        _currentHighBar = -1;
        _currentLowBar = -1;
        _currentRangeStartBar = -1;
        _currentRangeEndBar = -1;
        _rangeFormed = false;
        _historicalRanges.Clear();
    }

    private void InitializeCalculation()
    {
        // Find starting point for calculation based on look back days
        var days = 0;
        for (var i = CurrentBar - 1; i >= 0; i--)
        {
            if (IsNewSession(i))
            {
                days++;
                if (days == _lookBackDays)
                    break;
            }
        }
    }

    private bool IsNewRangePeriod(int bar)
    {
        if (bar == 0)
            return true;

        var candle = GetCandle(bar);
        var prevCandle = GetCandle(bar - 1);

        switch (_timeFrame)
        {
            case TimeFrameType.Weekly:
                // Check if this is the first day of a new week
                return IsNewWeek(bar);

            case TimeFrameType.Daily:
                // Check if this is the start of a new trading day
                return IsNewSession(bar);

            default:
                return false;
        }
    }

    private bool IsWithinFormationPeriod(int bar, IndicatorCandle candle)
    {
        // Basic checks: Ensure we have a start bar and the current bar is not before it.
        if (_currentRangeStartBar < 0 || bar < _currentRangeStartBar)
            return false;

        switch (_timeFrame)
        {
            case TimeFrameType.Weekly:
                // For the Weekly range, the formation period is the *entire first session* of the week.
                if (bar > _currentRangeStartBar && IsNewSession(bar))
                {
                    // We've hit the start of the second session of the week. Formation is over.
                    return false;
                }
                // Otherwise, we are still within the first session of the week.
                return true;

            case TimeFrameType.Daily:
                // For the Daily range, check if we're within the configured number of minutes
                // from the session start time (which corresponds to _currentRangeStartBar).
                var startCandle = GetCandle(_currentRangeStartBar);
                var diff = (candle.Time - startCandle.Time).TotalMinutes;
                // Ensure the difference calculation is meaningful (bar should be >= start bar)
                return diff >= 0 && diff <= _dailyMinutes.Value;

            default:
                return false;
        }
    }


    private void StartNewRange(int bar, IndicatorCandle candle)
    {
        _currentRangeStartBar = bar;
        _currentHigh = candle.High;
        _currentLow = candle.Low;
        _currentHighBar = bar;
        _currentLowBar = bar;
        _currentRangeDate = candle.Time.Date;
        _rangeFormed = false;
    }

    private int FindEndBarForRange(int startBar)
    {
        // Default to the last bar if no next period is found
        int endBar = CurrentBar - 1;

        // Look for the next range period start
        for (int bar = startBar + 1; bar < CurrentBar; bar++)
        {
            if (IsNewRangePeriod(bar))
            {
                endBar = bar - 1;
                break;
            }
        }

        return endBar;
    }

    private void UpdateRangeLevels(int bar, IndicatorCandle candle)
    {
        if (candle.High > _currentHigh)
        {
            _currentHigh = candle.High;
            _currentHighBar = bar;
        }

        if (candle.Low < _currentLow)
        {
            _currentLow = candle.Low;
            _currentLowBar = bar;
        }
    }

    private void DrawRangeLines(RenderContext context, decimal high, decimal low, int highBar, int lowBar, int startBar)
    {
        var timeFrameLabel = _timeFrame == TimeFrameType.Weekly ? "Weekly" : "Daily";

        // Determine the end X position for the lines
        int endX;

        if (ExtendLines)
        {
            // Extend to the right edge of the chart
            endX = Container.Region.Right;
        }
        else
        {
            // Find the end bar for this range period
            int endBar = FindEndBarForRange(startBar);
            endX = ChartInfo.PriceChartContainer.GetXByBar(endBar, false);
        }

        // Draw high line
        var yHigh = ChartInfo.PriceChartContainer.GetYByPrice(high, false);
        context.DrawLine(HighPen.RenderObject, ChartInfo.PriceChartContainer.GetXByBar(startBar, false), yHigh, endX,
            yHigh);

        if (ShowText)
        {
            var highText = string.IsNullOrEmpty(HighText) ? $"{timeFrameLabel} Range High" : HighText;
            DrawStringAtLineEnd(context, highText, yHigh, endX, HighPen.RenderObject.Color);
        }

        // Draw low line
        var yLow = ChartInfo.PriceChartContainer.GetYByPrice(low, false);
        context.DrawLine(LowPen.RenderObject, ChartInfo.PriceChartContainer.GetXByBar(startBar, false), yLow, endX,
            yLow);

        if (ShowText)
        {
            var lowText = string.IsNullOrEmpty(LowText) ? $"{timeFrameLabel} Range Low" : LowText;
            DrawStringAtLineEnd(context, lowText, yLow, endX, LowPen.RenderObject.Color);
        }

        // Draw price labels if enabled
        if (ShowPrice && ExtendLines)
        {
            var bounds = context.ClipBounds;
            context.ResetClip();
            context.SetTextRenderingHint(RenderTextRenderingHint.Aliased);

            DrawPrice(context, high, HighPen.RenderObject);
            DrawPrice(context, low, LowPen.RenderObject);

            context.SetTextRenderingHint(RenderTextRenderingHint.AntiAlias);
            context.SetClip(bounds);
        }
    }

    private void DrawString(RenderContext context, string renderText, int yPrice, Color color)
    {
        var textSize = context.MeasureString(renderText, _font);
        context.DrawString(renderText, _font, color, Container.Region.Right - textSize.Width - 5, yPrice - textSize.Height);
    }

    private void DrawStringAtLineEnd(RenderContext context, string renderText, int yPrice, int xPosition, Color color)
    {
        var textSize = context.MeasureString(renderText, _font);
        // Position text at the end of the line, with a small offset to avoid overlapping
        context.DrawString(renderText, _font, color, xPosition - textSize.Width - 5, yPrice - textSize.Height);
    }

    private void DrawPrice(RenderContext context, decimal price, RenderPen pen)
    {
        var y = ChartInfo.GetYByPrice(price, false);

        var renderText = price.ToString(CultureInfo.InvariantCulture);
        var textWidth = context.MeasureString(renderText, _font).Width;

        if (y + 8 > Container.Region.Height)
            return;

        var polygon = new Point[]
        {
            new(Container.Region.Right, y),
            new(Container.Region.Right + 6, y - 7),
            new(Container.Region.Right + textWidth + 8, y - 7),
            new(Container.Region.Right + textWidth + 8, y + 8),
            new(Container.Region.Right + 6, y + 8)
        };

        context.FillPolygon(pen.Color, polygon);
        context.DrawString(renderText, _font, Color.White, Container.Region.Right + 6, y - 6);
    }

    #endregion
}
