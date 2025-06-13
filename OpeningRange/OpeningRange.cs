using SharedResources.Properties;

namespace ATAS.Indicators.AlphaVector;

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

using ATAS.Indicators;
using ATAS.Indicators.Drawing;

using Color = System.Drawing.Color;

[DisplayName("Opening Range")]
[Category("AlphaVector")]
[HelpLink("https://github.com/alpha-vector-trade/atas-indicators")]
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
    private int _dailyMinutes = 30;
    private bool _showHistoricalRanges = true;
    private bool _showFormationMarkers = true;
    private bool _renderOnlyAfterFormation;
    private int _lookBackDays = 60;
    private bool _showText = true;
    private bool _showPrice = true;

    private decimal _currentHigh;
    private decimal _currentLow;
    private int _currentHighBar;
    private int _currentLowBar;
    private int _currentRangeStartBar;
    private int _currentRangeEndBar;
    private DateTime _currentRangeDate;
    private bool _customSession;
    private TimeSpan _startTime = TimeSpan.FromHours(0);

    private bool _rangeFormed;
    private bool _rangeStarted;
    private DateTime _rangeStartTime = DateTime.MaxValue;

    private int _lastHighAlertBar = -1;
    private int _lastLowAlertBar = -1;
    private bool _highAlertTriggeredForRange;
    private bool _lowAlertTriggeredForRange;

    // Dictionary to store historical ranges
    private readonly System.Collections.Generic.Dictionary<DateTime, (decimal high, decimal low, int highBar, int lowBar, int startBar, int endBar)> _historicalRanges =
        new System.Collections.Generic.Dictionary<DateTime, (decimal high, decimal low, int highBar, int lowBar, int startBar, int endBar)>();
    private readonly PriceSelectionDataSeries _formationMarkersDataSeries = new("FormationMarkers");

    #endregion

    #region Properties

    [Display(ResourceType = typeof(Resources), Name = "TimeFrame", GroupName = "Settings", Order = 10)]
    public TimeFrameType RangeTimeFrame
    {
        get => _timeFrame;
        set
        {
            _timeFrame = value;
            RecalculateValues();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "DailyMinutes", GroupName = "Daily", Order = 10)]
    [Range(1, 1440)]
    public int DailyMinutes
    {
        get => _dailyMinutes;
        set
        {
            _dailyMinutes = value;
            RecalculateValues();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "CustomSession", GroupName = "Daily", Order = 20)]
    public bool CustomSession
    {
        get => _customSession;
        set
        {
            _customSession = value;
            RecalculateValues();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "StartTime", GroupName = "Daily", Order = 30)]
    public TimeSpan StartTime
    {
        get => _startTime;
        set
        {
            _startTime = value;
            RecalculateValues();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "DaysLookBack", GroupName = "Calculation", Order = int.MaxValue)]
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

    [Display(ResourceType = typeof(Resources), Name = "Line", GroupName = "HighRange", Order = 100)]
    public PenSettings HighPen { get; set; } = new() { Color = DefaultColors.Green.Convert(), Width = 2 };

    [Display(ResourceType = typeof(Resources), Name = "Text", GroupName = "HighRange", Order = 120)]
    public string HighText { get; set; } = "Range High";

    [Display(ResourceType = typeof(Resources), Name = "Line", GroupName = "LowRange", Order = 130)]
    public PenSettings LowPen { get; set; } = new() { Color = DefaultColors.Red.Convert(), Width = 2 };

    [Display(ResourceType = typeof(Resources), Name = "Text", GroupName = "LowRange", Order = 140)]
    public string LowText { get; set; } = "Range Low";

    [Description("Hide lines and area during formation of the range")]
    [Display(ResourceType = typeof(Resources), Name = "RenderAfterFormation", GroupName = "Formation", Order = 200)]
    public bool RenderOnlyAfterFormation
    {
        get => _renderOnlyAfterFormation;
        set
        {
            _renderOnlyAfterFormation = value;
            RedrawChart();
        }
    }

    [Description("Display markers at the high and low during formation of the range")]
    [Display(ResourceType = typeof(Resources), Name = "DisplayMarkers", GroupName = "Formation", Order = 210)]
    public bool ShowFormationMarkers
    {
        get => _showFormationMarkers;
        set
        {
            _showFormationMarkers = value;
            RedrawChart();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "Type", GroupName = "Formation", Order = 220)]
    public ObjectType FormationMarkerType { get; set; } = ObjectType.Diamond;

    [Display(ResourceType = typeof(Resources), Name = "Size", GroupName = "Formation", Order = 230)]
    [Range(1, 20)]
    public int FormationMarkerSize { get; set; } = 8;

    [Display(ResourceType = typeof(Resources), Name = "ShowHistoricalRanges", GroupName = "Drawing", Order = 240)]
    public bool ShowHistoricalRanges
    {
        get => _showHistoricalRanges;
        set
        {
            _showHistoricalRanges = value;
            RedrawChart();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "ShowText", GroupName = "Drawing", Order = 250)]
    public bool ShowText
    {
        get => _showText;
        set
        {
            _showText = value;
            RedrawChart();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "ShowPrice", GroupName = "Drawing", Order = 260)]
    public bool ShowPrice
    {
        get => _showPrice;
        set
        {
            _showPrice = value;
            RedrawChart();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "ExtendLines", GroupName = "Drawing", Order = 270)]
    public bool ExtendLines { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "ShowArea", GroupName = "Drawing", Order = 280)]
    public bool ShowArea { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "AreaColor", GroupName = "Drawing", Order = 290)]
    public Color AreaColor { get; set; } = Color.FromArgb(80, Color.Blue);

    [Display(ResourceType = typeof(Resources), Name = "ShowAboveChart", GroupName = "Drawing", Order = 300)]
    public bool ShowAboveChart
    {
        get => DrawAbovePrice;
        set
        {
            DrawAbovePrice = value;
            RedrawChart();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "HighLineAlert", GroupName = "Alerts", Order = 400)]
    public bool UseHighAlert { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "LowLineAlert", GroupName = "Alerts", Order = 410)]
    public bool UseLowAlert { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "OncePerRange", GroupName = "Alerts", Order = 420)]
    public bool OncePerRange { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "OmitConsecutiveAlerts", GroupName = "Alerts", Order = 430)]
    public bool OmitConsecutiveAlerts { get; set; } = true;

    [Display(ResourceType = typeof(Resources), Name = "ApproximationAlert", GroupName = "Alerts", Order = 440)]
    public bool UseApproximationAlert { get; set; } = false;

    [Display(ResourceType = typeof(Resources), Name = "ApproximationFilter", GroupName = "Alerts", Order = 450)]
    [Range(1, 100)]
    public int ApproximationFilter { get; set; } = 3;

    [Display(ResourceType = typeof(Resources), Name = "AlertFile", GroupName = "Alerts", Order = 460)]
    public string AlertFile { get; set; } = "alert1";

    [Display(ResourceType = typeof(Resources), Name = "FontColor", GroupName = "Alerts", Order = 470)]
    public Color AlertForeColor { get; set; } = Color.FromArgb(255, 247, 249, 249);

    [Display(ResourceType = typeof(Resources), Name = "Background", GroupName = "Alerts", Order = 480)]
    public Color AlertBgColor { get; set; } = Color.FromArgb(255, 75, 72, 72);

    #endregion

    #region ctor

    public OpeningRange()
        : base(true)
    {
        DenyToChangePanel = true;
        EnableCustomDrawing = true;
        SubscribeToDrawingEvents(DrawingLayouts.LatestBar);

        DrawAbovePrice = ShowAboveChart;

        _formationMarkersDataSeries.IsHidden = true;
        DataSeries[0] = _formationMarkersDataSeries;
    }

    #endregion

    #region Protected methods

    protected override void OnCalculate(int bar, decimal value)
    {
        if (bar == 0)
        {
            ResetFields();
            InitializeCalculation();
        }

        var candle = GetCandle(bar);
        var isLastBar = bar == CurrentBar - 1;

        // Determine current state
        var candleTime = GetAdjustedCandleTime(candle);
        var isNewRangePeriod = IsNewRangePeriod(bar);
        var shouldStartCustomSession = CustomSession && !_rangeStarted && candleTime >= _rangeStartTime;

        // Handle new range periods or custom session starts
        if (isNewRangePeriod || shouldStartCustomSession)
        {
            HandleRangeStart(bar, candle, candleTime, isNewRangePeriod);
        }
        // Handle existing range updates
        else if (_currentRangeStartBar > 0)
        {
            HandleExistingRange(bar, candle);
        }

        // Check for alerts only if the range has formed
        if (isLastBar && _rangeFormed)
        {
            CheckForAlerts(bar, candle);
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

        if (ShowFormationMarkers && _currentRangeStartBar > 0 && !_rangeFormed)
        {
            DrawFormationMarkers(context, _currentHigh, _currentLow, _currentHighBar, _currentLowBar);
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
        _rangeStarted = false;
        _historicalRanges.Clear();

        ResetAlertTracking();
    }

    private void ResetAlertTracking()
    {
        _lastHighAlertBar = -1;
        _lastLowAlertBar = -1;
        _highAlertTriggeredForRange = false;
        _lowAlertTriggeredForRange = false;
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

    private DateTime GetAdjustedCandleTime(IndicatorCandle candle)
    {
        var diff = InstrumentInfo.TimeZone;
        return candle.Time.AddHours(diff);
    }

    private void HandleRangeStart(int bar, IndicatorCandle candle, DateTime candleTime, bool isNewRangePeriod)
    {
        if (isNewRangePeriod)
        {
            PrepareForNewRangePeriod(candleTime);
            SaveCurrentRangeIfFormed();

            if (!CustomSession)
                StartNewRange(bar, candle);
        }
        else // Custom session start
        {
            _rangeStarted = true;
            StartNewRange(bar, candle);
        }
    }

    private void PrepareForNewRangePeriod(DateTime candleTime)
    {
        _rangeStarted = false;
        _rangeStartTime = CalculateRangeStartTime(candleTime);
    }

    private DateTime CalculateRangeStartTime(DateTime candleTime)
    {
        var baseDate = candleTime.Date.Add(StartTime);
        return baseDate > candleTime ? baseDate : baseDate.AddDays(1);
    }

    private void SaveCurrentRangeIfFormed()
    {
        if (_rangeFormed && _currentRangeStartBar > 0)
        {
            _historicalRanges[_currentRangeDate] = (
                _currentHigh,
                _currentLow,
                _currentHighBar,
                _currentLowBar,
                _currentRangeStartBar,
                _currentRangeEndBar
            );
        }
    }

    private void HandleExistingRange(int bar, IndicatorCandle candle)
    {
        if (!_rangeFormed)
        {
            if (IsWithinFormationPeriod(bar, candle))
            {
                UpdateRangeLevels(bar, candle);
            }
            else
            {
                FinalizeRangeFormation(bar);
            }
        }
    }

    private void FinalizeRangeFormation(int bar)
    {
        _rangeFormed = true;
        _currentRangeEndBar = bar;
        _formationMarkersDataSeries.Clear();
    }

    private bool IsNewRangePeriod(int bar)
    {
        if (bar == 0)
            return true;

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
                return diff >= 0 && diff <= _dailyMinutes;

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

        ResetAlertTracking();
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

        // Get Y coordinates for high and low
        var yHigh = ChartInfo.PriceChartContainer.GetYByPrice(high, false);
        var yLow = ChartInfo.PriceChartContainer.GetYByPrice(low, false);

        // Draw area between high and low if enabled
        if (ShowArea)
        {
            var startX = ChartInfo.PriceChartContainer.GetXByBar(startBar, false);
            var rect = new Rectangle(startX, yHigh, endX - startX, yLow - yHigh);
            context.FillRectangle(AreaColor, rect);
        }

        // Draw high line
        context.DrawLine(HighPen.RenderObject, ChartInfo.PriceChartContainer.GetXByBar(startBar, false), yHigh, endX,
            yHigh);

        if (ShowText)
        {
            var highText = string.IsNullOrEmpty(HighText) ? $"{timeFrameLabel} Range High" : HighText;
            DrawStringAtLineEnd(context, highText, yHigh, endX, HighPen.RenderObject.Color);
        }

        // Draw low line
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

    private void DrawFormationMarkers(RenderContext context, decimal high, decimal low, int highBar, int lowBar)
    {
        _formationMarkersDataSeries.Clear();

        // Only draw if the bars are valid
        if (highBar < 0 || lowBar < 0)
            return;

        // Draw high marker
        if (highBar >= FirstVisibleBarNumber && highBar <= LastVisibleBarNumber)
        {
            var highValue = new PriceSelectionValue(high)
            {
                VisualObject = FormationMarkerType,
                Size = FormationMarkerSize,
                ObjectColor = HighPen.Color,
                PriceSelectionColor = Color.Transparent.Convert()
            };

            _formationMarkersDataSeries[highBar].Add(highValue);
        }

        // Draw low marker
        if (lowBar >= FirstVisibleBarNumber && lowBar <= LastVisibleBarNumber)
        {
            var lowValue = new PriceSelectionValue(low)
            {
                VisualObject = FormationMarkerType,
                Size = FormationMarkerSize,
                ObjectColor = LowPen.Color,
                PriceSelectionColor = Color.Transparent.Convert()
            };

            _formationMarkersDataSeries[lowBar].Add(lowValue);
        }
    }

    private void CheckForAlerts(int bar, IndicatorCandle candle)
    {
        if (bar <= 0)
            return;

        var prevCandle = GetCandle(bar - 1);

        // Check for high and low level crossings
        CheckLevelCrossing(bar, candle, prevCandle, _currentHigh, ref _lastHighAlertBar,
            ref _highAlertTriggeredForRange, UseHighAlert, "Opening Range High");

        CheckLevelCrossing(bar, candle, prevCandle, _currentLow, ref _lastLowAlertBar,
            ref _lowAlertTriggeredForRange, UseLowAlert, "Opening Range Low");
    }

    private void CheckLevelCrossing(int bar, IndicatorCandle candle, IndicatorCandle prevCandle,
        decimal level, ref int lastAlertBar, ref bool alertTriggeredForRange, bool useAlert, string levelName)
    {
        if (!useAlert || lastAlertBar == bar || (OncePerRange && alertTriggeredForRange))
            return;

        bool triggered = false;
        string message = $"{levelName} crossed: {level}";

        // Direct crossing check
        bool directCrossing = (candle.Close >= level && prevCandle.Close <= level) ||
                              (candle.Close <= level && prevCandle.Close >= level);

        if (directCrossing)
        {
            triggered = true;
        }
        // Approximation check if enabled
        else if (UseApproximationAlert)
        {
            decimal approximationDistance = ApproximationFilter * InstrumentInfo.TickSize;

            // Check if price is within the approximation range
            bool isWithinRange = Math.Abs(candle.Close - level) <= approximationDistance &&
                                 Math.Abs(prevCandle.Close - level) > approximationDistance;

            if (isWithinRange)
            {
                triggered = true;
                message = $"{levelName} approximation ({ApproximationFilter} ticks): {level}";
            }
        }

        if (triggered)
        {
            if (!OmitConsecutiveAlerts || (OmitConsecutiveAlerts && lastAlertBar < bar - 1))
            {
                AddAlert(AlertFile, InstrumentInfo.Instrument, message, AlertBgColor.Convert(), AlertForeColor.Convert());
            }

            lastAlertBar = bar;
            alertTriggeredForRange = true;
        }
    }

    #endregion
}
