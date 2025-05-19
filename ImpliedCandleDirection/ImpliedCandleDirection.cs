using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using OFT.Localization;
using SharedResources.Properties;

namespace ATAS.Indicators.AlphaVector;

[DisplayName("Implied Candle Direction")]
[Category("AlphaVector")]
public class ImpliedCandleDirection : Indicator
{
    #region Fields

    // Define a private enum for direction values
    private enum Direction
    {
        Bearish = -1,
        Neutral = 0,
        Bullish = 1
    }

    public enum LabelLocations
    {
        [Display(ResourceType = typeof(Strings), Name = nameof(Strings.AboveCandle))]
        Top,

        [Display(ResourceType = typeof(Strings), Name = nameof(Strings.BelowCandle))]
        Bottom,

        [Display(ResourceType = typeof(Strings), Name = nameof(Strings.ByCandleDirection))]
        CandleDirection
    }

    private int _offset = 5;
    private LabelLocations _labelLocation = LabelLocations.CandleDirection;

    #endregion

    #region Properties

    private readonly ValueDataSeries _negSeries = new(Resources.Down)
    {
        Color = Colors.Red,
        VisualType = VisualMode.Dots,
        Width = 3,
        ShowTooltip = false
    };

    private readonly ValueDataSeries _posSeries = new(Resources.Up)
    {
        Color = Colors.Green,
        VisualType = VisualMode.Dots,
        Width = 3,
        ShowTooltip = false
    };

    [Display(ResourceType = typeof(Strings), Name = nameof(Strings.LabelLocation), GroupName = nameof(Strings.Drawing),
        Description = nameof(Strings.LabelLocationDescription))]
    public LabelLocations LabelLocation
    {
        get => _labelLocation;
        set
        {
            _labelLocation = value;
            RecalculateValues();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "Offset", GroupName = "Drawing", Order = 100)]
    public int Offset
    {
        get => _offset;
        set {
            _offset = value;
            RecalculateValues();
        }
    }

    #endregion

    #region ctor

    /// <summary>
    /// Represents an indicator that determines the implied direction of a candle
    /// in a chart based on specific visual and data series settings. Provides
    /// configuration options for label positioning and offsets.
    /// </summary>
    public ImpliedCandleDirection()
    {
        DenyToChangePanel = true;

        DataSeries[0] = _posSeries;
        DataSeries.Add(_negSeries);
    }

    #endregion

    #region Lifecycle

    /// <summary>
    /// Applies the default colors for the positive and negative data series
    /// based on the chart's candle color settings.
    /// The positive series color is set to match the 'Up Candle' color,
    /// and the negative series color is set to match the 'Down Candle' color.
    /// </summary>
    protected override void OnApplyDefaultColors()
    {
        if (ChartInfo is null)
            return;

        _posSeries.Color = ChartInfo.ColorsStore.UpCandleColor.Convert();
        _negSeries.Color = ChartInfo.ColorsStore.DownCandleColor.Convert();
    }

    /// <summary>
    /// Calculates the implied direction of a candle based on its midpoint and the provided value.
    /// Updates the negative or positive series with the label position if the direction is bearish or bullish, respectively.
    /// </summary>
    /// <param name="bar">The index of the current bar being evaluated.</param>
    /// <param name="value">The numeric value used to determine the candle's implied direction.</param>
    protected override void OnCalculate(int bar, decimal value)
    {
        if (bar == 0)
            return;

        var candle = GetCandle(bar);

        var low = candle.Low;
        var high = candle.High;

        // Calculate the midpoint (50% level) between low and high
        var midpoint = low + (high - low) / 2;

        Direction direction;

        if (value > midpoint)
            direction = Direction.Bullish;
        else if (value < midpoint)
            direction = Direction.Bearish;
        else
            direction = Direction.Neutral;

        if (direction == Direction.Bearish)
            _negSeries[bar] = GetLabelPosition(candle);
        else
            _negSeries[bar] = 0;

        if (direction == Direction.Bullish)
            _posSeries[bar] = GetLabelPosition(candle);
        else
            _posSeries[bar] = 0;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Determines the label position for a given candle based on the configured label location settings.
    /// </summary>
    /// <param name="candle">The candle object containing high, low, open, and close price values.</param>
    /// <returns>A decimal value representing the calculated position of the label for the specified candle.</returns>
    private decimal GetLabelPosition(IndicatorCandle candle)
    {
        var topPosition = candle.High + InstrumentInfo.TickSize * _offset;
        var bottomPosition = candle.Low - InstrumentInfo.TickSize * _offset;

        return LabelLocation switch
        {
            LabelLocations.Top => topPosition,
            LabelLocations.Bottom => bottomPosition,
            LabelLocations.CandleDirection => candle.Close > candle.Open
                ? topPosition
                : bottomPosition,
            _ => 0
        };
    }

    #endregion
}
