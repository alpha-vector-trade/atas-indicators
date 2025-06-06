﻿namespace ATAS.Indicators.AlphaVector;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Media;

using ATAS.Indicators;
using ATAS.Indicators.Drawing;
using SharedResources.Properties;

using OFT.Attributes;
using OFT.Rendering.Context;
using OFT.Rendering.Settings;
using OFT.Rendering.Tools;

using Utils.Common.Logging;

using Color = System.Drawing.Color;

[DisplayName("Daily Lines Extended")]
[Category("AlphaVector")]
[HelpLink("https://github.com/alpha-vector-trade/atas-indicators")]
public class DailyLinesExtended : Indicator
{
    #region Nested types

    [Serializable]
    [Obfuscation(Feature = "renaming", ApplyToMembers = true, Exclude = true)]
    public enum PeriodType
    {
        [Display(ResourceType = typeof(Resources), Name = "CurrentDay")]
        CurrentDay,

        [Display(ResourceType = typeof(Resources), Name = "PreviousDay")]
        PreviousDay,

        [Display(ResourceType = typeof(Resources), Name = "CurrentWeek")]
        CurrenWeek,

        [Display(ResourceType = typeof(Resources), Name = "PreviousWeek")]
        PreviousWeek,

        [Display(ResourceType = typeof(Resources), Name = "CurrentMonth")]
        CurrentMonth,

        [Display(ResourceType = typeof(Resources), Name = "PreviousMonth")]
        PreviousMonth,
    }

    [Obfuscation(Feature = "renaming", ApplyToMembers = true, Exclude = true)]
    [Serializable]
    public enum MiddleClusterType
    {
	    [Display(ResourceType = typeof(Resources), Name = "Bid")]
	    Bid,

	    [Display(ResourceType = typeof(Resources), Name = "Ask")]
	    Ask,

	    [Display(ResourceType = typeof(Resources), Name = "Delta")]
	    Delta,

	    [Display(ResourceType = typeof(Resources), Name = "Volume")]
	    Volume,

	    [Display(ResourceType = typeof(Resources), Name = "Ticks")]
	    Tick,
    }

    #region Candle

	public class DynamicCandle
	{
		#region Nested types

		private class PriceInfo
		{
			#region Properties

			public decimal Price { get; }

			public decimal Volume { get; set; }

			public decimal Value { get; set; }

			#endregion

			#region ctor

			public PriceInfo(decimal price)
			{
				Price = price;
			}

			#endregion
		}

		#endregion

		#region Fields

		private readonly SortedList<decimal, PriceInfo> _allPrice = new();

		private decimal _cachedVah;
		private decimal _cachedVal;
		private decimal _cachedVol;
		private decimal _maxPrice;

		private PriceInfo _maxPriceInfo = new(0);

		public MiddleClusterType Type = MiddleClusterType.Volume;

		#endregion

		#region Properties

		public decimal MaxValue { get; set; }

		public decimal TrueMaxValue { get; set; }

		public decimal MaxValuePrice { get; set; }

		public decimal High { get; set; }

		public decimal Low { get; set; }

		public decimal Open { get; set; }

		public decimal Close { get; set; }

		public decimal Volume { get; set; }

		#endregion

		#region Public methods

		public void AddCandle(IndicatorCandle candle, decimal tickSize)
		{
			if (Open == 0)
				Open = candle.Open;
			Close = candle.Close;

			for (var price = candle.High; price >= candle.Low; price -= tickSize)
			{
				var info = candle.GetPriceVolumeInfo(price);

				if (info == null)
					continue;

				if (price > High)
					High = price;

				if (price < Low || Low == 0)
					Low = price;

				Volume += info.Volume;

				if (!_allPrice.TryGetValue(price, out var priceInfo))
				{
					priceInfo = new PriceInfo(price);
					_allPrice.Add(price, priceInfo);
				}

				priceInfo.Volume += info.Volume;

				if (priceInfo.Volume > _maxPriceInfo.Value)
				{
					_maxPrice = price;
					_maxPriceInfo = priceInfo;
				}

				switch (Type)
				{
					case MiddleClusterType.Bid:
					{
						priceInfo.Value += info.Bid;
						break;
					}
					case MiddleClusterType.Ask:
					{
						priceInfo.Value += info.Ask;
						break;
					}
					case MiddleClusterType.Delta:
					{
						priceInfo.Value += info.Ask - info.Bid;
						break;
					}
					case MiddleClusterType.Volume:
					{
						priceInfo.Value += info.Volume;
						break;
					}
					case MiddleClusterType.Tick:
					{
						priceInfo.Value += info.Ticks;
						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (Math.Abs(priceInfo.Value) > MaxValue)
				{
					MaxValue = Math.Abs(priceInfo.Value);
					TrueMaxValue = priceInfo.Value;
					MaxValuePrice = price;
				}
			}
		}

		public void AddTick(MarketDataArg tick)
		{
			if (tick.DataType != MarketDataType.Trade)
				return;

			var price = tick.Price;

			if (price < Low || Low == 0)
				Low = price;

			if (price > High)
				High = price;

			Volume += tick.Volume;
			var volume = tick.Volume;
			var bid = 0m;
			var ask = 0m;

			if (tick.Direction == TradeDirection.Buy)
				ask = volume;
			else if (tick.Direction == TradeDirection.Sell)
				bid = volume;

			if (!_allPrice.TryGetValue(price, out var priceInfo))
			{
				priceInfo = new PriceInfo(price);
				_allPrice.Add(price, priceInfo);
			}

			priceInfo.Volume += volume;

			if (priceInfo.Volume > _maxPriceInfo.Value)
			{
				_maxPrice = price;
				_maxPriceInfo = priceInfo;
			}

			switch (Type)
			{
				case MiddleClusterType.Bid:
				{
					priceInfo.Value += bid;
					break;
				}
				case MiddleClusterType.Ask:
				{
					priceInfo.Value += ask;
					break;
				}
				case MiddleClusterType.Delta:
				{
					priceInfo.Value += ask - bid;
					break;
				}
				case MiddleClusterType.Volume:
				{
					priceInfo.Value += volume;
					break;
				}
				case MiddleClusterType.Tick:
				{
					priceInfo.Value++;
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (Math.Abs(priceInfo.Value) > MaxValue)
			{
				MaxValue = Math.Abs(priceInfo.Value);
				TrueMaxValue = priceInfo.Value;
				MaxValuePrice = price;
			}
		}

		public (decimal, decimal) GetValueArea(decimal tickSize, int valueAreaPercent)
		{
			if (Volume == _cachedVol)
				return (_cachedVah, _cachedVal);

			var vah = 0m;
			var val = 0m;

			if (High != 0 && Low != 0)
			{
				var k = valueAreaPercent / 100.0m;
				vah = val = _maxPrice;
				var vol = _maxPriceInfo.Volume;
				var valueAreaVolume = Volume * k;

				var upperPrice = 0m;
				var lowerPrice = 0m;
				var upperIndex = 0;
				var lowerIndex = 0;

				while (vol <= valueAreaVolume)
				{
					if (vah >= High && val <= Low)
						break;

					var upperVol = 0m;
					var lowerVol = 0m;

					var newVah = upperPrice != vah;
					var newVal = lowerPrice != val;

					upperPrice = vah;
					lowerPrice = val;
					var c = 2;

					var count = _allPrice.Count;

					if (newVah)
						upperIndex = _allPrice.IndexOfKey(upperPrice);

					var upLoopIdx = upperIndex;
					var upLoopPrice = upperPrice;

					for (var i = 0; i <= c; i++)
					{
						if (upLoopIdx + 1 >= count)
							break;

						upLoopIdx++;

						var info = _allPrice.Values[upLoopIdx];
						upLoopPrice = info.Price;

						upperVol += info.Volume;
					}

					if (newVal)
						lowerIndex = _allPrice.IndexOfKey(lowerPrice);

					var downLoopIdx = lowerIndex;
					var downLoopPrice = lowerPrice;

					for (var i = 0; i <= c; i++)
					{
						if (downLoopIdx - 1 < 0)
							break;

						downLoopIdx--;

						var info = _allPrice.Values[downLoopIdx];
						downLoopPrice = info.Price;

						lowerVol += info.Volume;
					}

					if (upperVol == lowerVol && upperVol == 0)
					{
						vah = Math.Min(upLoopPrice, High);
						val = Math.Max(downLoopPrice, Low);
					}
					else if (upperVol >= lowerVol)
					{
						vah = upLoopPrice;
						vol += upperVol;
					}
					else
					{
						val = downLoopPrice;
						vol += lowerVol;
					}

					if (vol >= valueAreaVolume)
						break;
				}
			}

			_cachedVol = Volume;
			_cachedVah = vah;
			_cachedVal = val;

			return (vah, val);
		}

		public void Clear()
		{
			_allPrice.Clear();
			MaxValue = High = Low = Volume = _cachedVol = _cachedVah = _cachedVal = _maxPrice = 0;
			_maxPriceInfo = new PriceInfo(0);
		}

		#endregion
	}

	#endregion

    #endregion

    #region Fields

    private readonly RenderFont _font = new("Arial", 8);

    private int _closeBar;
    private decimal _currentClose;
    private decimal _currentHigh;
    private decimal _currentLow;

    private decimal _currentOpen;
    private bool _customSession;
    private int _days = 60;
    private bool _drawFromBar;
    private TimeSpan _endTime;
    private int _highBar;
    private int _lastSession;
    private int _lowBar;
    private int _openBar;
    private PeriodType _per = PeriodType.PreviousDay;
    private decimal _prevClose;
    private int _prevCloseBar;
    private decimal _prevHigh;
    private int _prevHighBar;
    private decimal _prevLow;
    private int _prevLowBar;
    private decimal _prevOpen;
    private int _prevOpenBar;
    private bool _showText = true;
    private bool _showPrice = true;
    private TimeSpan _startTime;
    private int _targetBar;

    private readonly DynamicCandle _currentPeriodCandle = new();
    private readonly DynamicCandle _previousPeriodCandle = new();

    private decimal _currentPoc;
    private decimal _currentVah;
    private decimal _currentVal;
    private int _currentPocBar;

    private decimal _prevPoc;
    private decimal _prevVah;
    private decimal _prevVal;
    private int _prevPocBar;

    private int _lastPocAlertBar = -1;
    private int _lastVahAlertBar = -1;
    private int _lastValAlertBar = -1;

    private int _lastOpenAlertBar = -1;
    private int _lastCloseAlertBar = -1;
    private int _lastHighAlertBar = -1;
    private int _lastLowAlertBar = -1;

    #endregion

    #region Properties

    [Display(ResourceType = typeof(Resources), GroupName = "Calculation", Name = "DaysLookBack", Order = int.MaxValue, Description = "DaysLookBackDescription")]
    [Range(1, 1000)]
    public int Days
    {
        get => _days;
        set
        {
            _days = value;
            RecalculateValues();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "Period", GroupName = "Filters", Order = 110)]
    public PeriodType Period
    {
        get => _per;
        set
        {
            _per = value;

            bool isPreviousPeriod = value is PeriodType.PreviousDay or PeriodType.PreviousWeek or PeriodType.PreviousMonth;
            UseOpenAlert.Enabled = UseCloseAlert.Enabled = UseHighAlert.Enabled = UseLowAlert.Enabled =
	            UsePocAlert.Enabled = UseVahAlert.Enabled = UseValAlert.Enabled = isPreviousPeriod;

            RecalculateValues();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "CustomSession", GroupName = "Filters", Order = 120)]
    public bool CustomSession
    {
        get => _customSession;
        set
        {
            _customSession = value;
            RecalculateValues();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "SessionBegin", GroupName = "Filters", Order = 120)]
    public TimeSpan StartTime
    {
        get => _startTime;
        set
        {
            _startTime = value;
            RecalculateValues();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "SessionEnd", GroupName = "Filters", Order = 120)]
    public TimeSpan EndTime
    {
        get => _endTime;
        set
        {
            _endTime = value;
            RecalculateValues();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "Text", GroupName = "Show", Order = 200)]
    public bool ShowText
    {
        get => _showText;
        set
        {
            _showText = value;
            RecalculateValues();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "PriceLocation", GroupName = "Show", Order = 210)]
    public bool ShowPrice
    {
	    get => _showPrice;
	    set
	    {
		    _showPrice = value;
		    RecalculateValues();
	    }
    }

    [Display(ResourceType = typeof(Resources), Name = "FirstBar", GroupName = "Drawing", Order = 300)]
    public bool DrawFromBar
    {
        get => _drawFromBar;
        set
        {
            _drawFromBar = value;
            RecalculateValues();
        }
    }

    [Display(ResourceType = typeof(Resources), Name = "ShowAboveChart", GroupName = "Drawing", Order = 3310)]
    public bool ShowAboveChart
    {
	    get => DrawAbovePrice;
	    set
	    {
		    DrawAbovePrice = value;
		    RedrawChart();
	    }
    }

    // Open
    [Display(ResourceType = typeof(Resources), Name = "Line", GroupName = "Open", Order = 310)]
    public PenSettings OpenPen { get; set; } = new() { Color = DefaultColors.Red.Convert(), Width = 2 };

    [Display(ResourceType = typeof(Resources), Name = "Text", GroupName = "Open", Order = 315)]
    public string OpenText { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "CrossAlert", GroupName = "Open", Order = 316)]
    public FilterBool UseOpenAlert { get; set; } = new(false);

    // Close
    [Display(ResourceType = typeof(Resources), Name = "Line", GroupName = "Close", Order = 320)]
    public PenSettings ClosePen { get; set; } = new() { Color = DefaultColors.Red.Convert(), Width = 2 };

    [Display(ResourceType = typeof(Resources), Name = "Text", GroupName = "Close", Order = 325)]
    public string CloseText { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "CrossAlert", GroupName = "Close", Order = 326)]
    public FilterBool UseCloseAlert { get; set; } = new(false);

    // High
    [Display(ResourceType = typeof(Resources), Name = "Line", GroupName = "High", Order = 330)]
    public PenSettings HighPen { get; set; } = new() { Color = DefaultColors.Red.Convert(), Width = 2 };

    [Display(ResourceType = typeof(Resources), Name = "Text", GroupName = "High", Order = 335)]
    public string HighText { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "CrossAlert", GroupName = "High", Order = 336)]
    public FilterBool UseHighAlert { get; set; } = new(false);

    // Low
    [Display(ResourceType = typeof(Resources), Name = "Line", GroupName = "Low", Order = 340)]
    public PenSettings LowPen { get; set; } = new() { Color = DefaultColors.Red.Convert(), Width = 2 };

    [Display(ResourceType = typeof(Resources), Name = "Text", GroupName = "Low", Order = 345)]
    public string LowText { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "CrossAlert", GroupName = "Low", Order = 346)]
    public FilterBool UseLowAlert { get; set; } = new(false);

    // POC
    [Display(ResourceType = typeof(Resources), Name = "Line", GroupName = "Poc", Order = 350)]
    public PenSettings PocPen { get; set; } = new() { Color = DefaultColors.Red.Convert(), Width = 2 };

    [Display(ResourceType = typeof(Resources), GroupName = "Poc", Name = "Type", Order = 356)]
    public MiddleClusterType Type { get; set; } = MiddleClusterType.Volume;

    [Display(ResourceType = typeof(Resources), Name = "Text", GroupName = "Poc", Order = 356)]
    public string PocText { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "CrossAlert", GroupName = "Poc", Order = 357)]
    public FilterBool UsePocAlert { get; set; } = new(false);

    // VAH
    [Display(ResourceType = typeof(Resources), Name = "Line", GroupName = "Vah", Order = 360)]
    public PenSettings VahPen { get; set; } = new() { Color = DefaultColors.Red.Convert(), Width = 2 };

    [Display(ResourceType = typeof(Resources), Name = "Text", GroupName = "Vah", Order = 365)]
    public string VahText { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "CrossAlert", GroupName = "Vah", Order = 366)]
    public FilterBool UseVahAlert { get; set; } = new(false);

    // VAL
    [Display(ResourceType = typeof(Resources), Name = "Line", GroupName = "Val", Order = 370)]
    public PenSettings ValPen { get; set; } = new() { Color = DefaultColors.Red.Convert(), Width = 2 };

    [Display(ResourceType = typeof(Resources), Name = "Text", GroupName = "Val", Order = 375)]
    public string ValText { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "CrossAlert", GroupName = "Val", Order = 376)]
    public FilterBool UseValAlert { get; set; } = new(false);

    [Display(ResourceType = typeof(Resources), Name = "ApproximationAlert", GroupName = "Alerts", Order = 380)]
    public bool UseApproximationAlert { get; set; } = false;

    [Display(ResourceType = typeof(Resources), Name = "ApproximationFilter", GroupName = "Alerts", Order = 385)]
    [Range(1, 100)]
    public int ApproximationTicks { get; set; } = 3;

    [Display(ResourceType = typeof(Resources), Name = "OmitConsecutiveAlerts", GroupName = "Alerts", Order = 386)]
    public bool OmitConsecutiveAlerts { get; set; } = true;

    // Alerts
    [Display(ResourceType = typeof(Resources), Name = "AlertFile", GroupName = "Alerts", Order = 400)]
    public string AlertFile { get; set; } = "alert1";

    [Display(ResourceType = typeof(Resources), Name = "FontColor", GroupName = "Alerts", Order = 405)]
    public System.Windows.Media.Color AlertForeColor { get; set; } = System.Windows.Media.Color.FromArgb(255, 247, 249, 249);

    [Display(ResourceType = typeof(Resources), Name = "BackGround", GroupName = "Alerts", Order = 410)]
    public System.Windows.Media.Color AlertBGColor { get; set; } = System.Windows.Media.Color.FromArgb(255, 75, 72, 72);

    #endregion

    #region ctor

    public DailyLinesExtended()
        : base(true)
    {
        DenyToChangePanel = true;
        EnableCustomDrawing = true;
        SubscribeToDrawingEvents(DrawingLayouts.LatestBar);

        DataSeries[0].IsHidden = true;
        ((ValueDataSeries)DataSeries[0]).VisualType = VisualMode.Hide;

        DrawAbovePrice = ShowAboveChart;

        bool isPreviousPeriod = _per is PeriodType.PreviousDay or PeriodType.PreviousWeek or PeriodType.PreviousMonth;
        UseOpenAlert.Enabled = UseCloseAlert.Enabled = UseHighAlert.Enabled = UseLowAlert.Enabled =
	        UsePocAlert.Enabled = UseVahAlert.Enabled = UseValAlert.Enabled = isPreviousPeriod;
    }

    #endregion

    #region Public methods

    public override string ToString()
    {
        return "Daily Lines Extended";
    }

    #endregion

    #region Protected methods

    protected override void OnRender(RenderContext context, DrawingLayouts layout)
	{
	    if (ChartInfo is null)
	        return;

	    string periodStr = GetPeriodString();
	    bool isCurrentPeriod = Period is PeriodType.CurrentDay or PeriodType.CurrenWeek or PeriodType.CurrentMonth;

	    // Get values based on period
	    var open = isCurrentPeriod ? _currentOpen : _prevOpen;
	    var close = isCurrentPeriod ? _currentClose : _prevClose;
	    var high = isCurrentPeriod ? _currentHigh : _prevHigh;
	    var low = isCurrentPeriod ? _currentLow : _prevLow;
	    var poc = isCurrentPeriod ? _currentPoc : _prevPoc;
	    var vah = isCurrentPeriod ? _currentVah : _prevVah;
	    var val = isCurrentPeriod ? _currentVal : _prevVal;

	    // Get bars based on period
	    var openBar = isCurrentPeriod ? _openBar : _prevOpenBar;
	    var closeBar = isCurrentPeriod ? _closeBar : _prevCloseBar;
	    var highBar = isCurrentPeriod ? _highBar : _prevHighBar;
	    var lowBar = isCurrentPeriod ? _lowBar : _prevLowBar;
	    var pocStartBar = isCurrentPeriod ? _openBar : _prevOpenBar;

	    // Draw all lines
	    DrawLevelLine(context, open, openBar, OpenPen, string.IsNullOrEmpty(OpenText) ? periodStr + "Open" : OpenText);

	    if (!isCurrentPeriod)
	        DrawLevelLine(context, close, closeBar, ClosePen, string.IsNullOrEmpty(CloseText) ? periodStr + "Close" : CloseText);

	    DrawLevelLine(context, high, highBar, HighPen, string.IsNullOrEmpty(HighText) ? periodStr + "High" : HighText);
	    DrawLevelLine(context, low, lowBar, LowPen, string.IsNullOrEmpty(LowText) ? periodStr + "Low" : LowText);

	    if (poc > 0)
	        DrawLevelLine(context, poc, pocStartBar, PocPen, string.IsNullOrEmpty(PocText) ? periodStr + "POC" : PocText);

	    if (vah > 0)
	        DrawLevelLine(context, vah, pocStartBar, VahPen, string.IsNullOrEmpty(VahText) ? periodStr + "VAH" : VahText);

	    if (val > 0)
	        DrawLevelLine(context, val, pocStartBar, ValPen, string.IsNullOrEmpty(ValText) ? periodStr + "VAL" : ValText);

	    // Draw price labels if enabled
	    if (ShowPrice)
	    {
	        var bounds = context.ClipBounds;
	        context.ResetClip();
	        context.SetTextRenderingHint(RenderTextRenderingHint.Aliased);

	        DrawPriceLabels(context, isCurrentPeriod, open, close, high, low, poc, vah, val, openBar, closeBar, highBar, lowBar, pocStartBar);

	        context.SetTextRenderingHint(RenderTextRenderingHint.AntiAlias);
	        context.SetClip(bounds);
	    }
	}

    protected override void OnRecalculate()
    {
        ResetFields();
    }

    protected override void OnCalculate(int bar, decimal value)
    {
	    try
	    {
		    if (bar == 0)
			    InitializeCalculation();

		    if (bar < _targetBar)
			    return;

		    var candle = GetCandle(bar);
		    bool isNewSession = IsNewPeriodSession(bar);

		    if (isNewSession && (_lastSession != bar || bar == 0))
		    {
			    HandleNewPeriod(bar, candle);
		    }
		    else
		    {
			    if (CustomSession && !InsideSession(bar) && Period is PeriodType.CurrentDay or PeriodType.PreviousDay)
				    return;

			    UpdateLevels(bar);
			    UpdateVolumeProfile(candle, bar);
		    }

		    // Check for alerts on the last bar
		    if (bar == CurrentBar - 1 && bar > 0)
		    {
			    var prevCandle = GetCandle(bar - 1);
			    CheckForAlerts(bar, candle, prevCandle);
		    }
	    }
	    catch (Exception e)
	    {
		    this.LogError("Daily lines error ", e);
	    }
    }

    #endregion

    #region Private methods

    private void ResetFields()
    {
        _lastSession = 0;
        _targetBar = 0;

        _prevClose = 0;
        _prevCloseBar = 0;
        _prevHigh = 0;
        _prevHighBar = 0;
        _prevLow = 0;
        _prevLowBar = 0;
        _prevOpen = 0;
        _prevOpenBar = 0;

        _highBar = 0;
        _lowBar = 0;
        _openBar = 0;

        _closeBar = 0;
        _currentClose = 0;
        _currentHigh = 0;
        _currentLow = 0;

        _currentOpen = 0;

        _currentPeriodCandle.Clear();
        _previousPeriodCandle.Clear();

        _currentPoc = 0;
        _currentVah = 0;
        _currentVal = 0;
        _currentPocBar = 0;

        _prevPoc = 0;
        _prevVah = 0;
        _prevVal = 0;
        _prevPocBar = 0;

        _lastOpenAlertBar = -1;
        _lastCloseAlertBar = -1;
        _lastHighAlertBar = -1;
        _lastLowAlertBar = -1;
        _lastPocAlertBar = -1;
        _lastVahAlertBar = -1;
        _lastValAlertBar = -1;
    }

    private void InitializeCalculation()
    {
	    _openBar = _closeBar = _highBar = _lowBar = -1;
	    _lastSession = 0;

	    var days = 0;
	    for (var i = CurrentBar - 1; i >= 0; i--)
	    {
		    _targetBar = i;
		    if (!IsNewSession(i))
			    continue;
		    days++;
		    if (days == _days)
			    break;
	    }
    }

    private void UpdateLevels(int bar)
    {
        var candle = GetCandle(bar);

        _currentClose = candle.Close;
        _closeBar = bar;

        if (_currentHigh < candle.High)
        {
            _currentHigh = candle.High;
            _highBar = bar;
        }

        if (_currentLow > candle.Low)
        {
            _currentLow = candle.Low;
            _lowBar = bar;
        }
    }

    private bool InsideSession(int bar)
    {
        var diff = InstrumentInfo.TimeZone;
        var candle = GetCandle(bar);
        var time = candle.Time.AddHours(diff);

        if (_startTime < _endTime)
            return time.TimeOfDay <= EndTime && time.TimeOfDay >= StartTime;

        return (time.TimeOfDay >= EndTime && time.TimeOfDay >= StartTime && time.TimeOfDay <= new TimeSpan(23, 23, 59))
            || (time.TimeOfDay <= _startTime && time.TimeOfDay <= EndTime && time.TimeOfDay >= TimeSpan.Zero);
    }

    private bool IsNewCustomSession(int bar)
    {
        var candle = GetCandle(bar);

        var candleStart = candle.Time
            .AddHours(InstrumentInfo.TimeZone)
            .TimeOfDay;

        var candleEnd = candle.LastTime
            .AddHours(InstrumentInfo.TimeZone)
            .TimeOfDay;

        if (bar == 0)
        {
            if (_startTime < _endTime)
            {
                return (candleStart <= _startTime && candleEnd >= _endTime)
                    || (candleStart >= _startTime && candleEnd <= _endTime)
                    || (candleStart < _startTime && candleEnd > _startTime && candleEnd <= _endTime);
            }

            return candleStart >= _startTime || candleStart <= _endTime;
        }

        var diff = InstrumentInfo.TimeZone;

        var prevCandle = GetCandle(bar - 1);
        var prevTime = prevCandle.LastTime.AddHours(diff);

        var time = candle.LastTime.AddHours(diff);

        if (_startTime < _endTime)
        {
            return time.TimeOfDay >= _startTime && time.TimeOfDay <= EndTime &&
                !(prevTime.TimeOfDay >= _startTime && prevTime.TimeOfDay <= EndTime);
        }

        return time.TimeOfDay >= _startTime && time.TimeOfDay >= EndTime && time.TimeOfDay <= new TimeSpan(23, 23, 59)
            && !((prevTime.TimeOfDay >= _startTime && prevTime.TimeOfDay >= EndTime && prevTime.TimeOfDay <= new TimeSpan(23, 23, 59))
                ||
                (time.TimeOfDay <= _startTime && time.TimeOfDay <= EndTime && time.TimeOfDay >= TimeSpan.Zero))
            && !(prevTime.TimeOfDay <= _startTime && prevTime.TimeOfDay <= EndTime && prevTime.TimeOfDay >= TimeSpan.Zero);
    }

    private bool IsNewPeriodSession(int bar)
    {
	    return (((IsNewSession(bar) && !CustomSession) || (IsNewCustomSession(bar) && CustomSession)) &&
	            Period is PeriodType.CurrentDay or PeriodType.PreviousDay)
	           || (Period is PeriodType.CurrenWeek or PeriodType.PreviousWeek && IsNewWeek(bar))
	           || (Period is PeriodType.CurrentMonth or PeriodType.PreviousMonth && IsNewMonth(bar))
	           || bar == _targetBar;
    }

    private void HandleNewPeriod(int bar, IndicatorCandle candle)
    {
	    // Save previous period values
	    _prevOpenBar = _openBar;
	    _prevCloseBar = _closeBar;
	    _prevHighBar = _highBar;
	    _prevLowBar = _lowBar;

	    _prevOpen = _currentOpen;
	    _prevClose = _currentClose;
	    _prevHigh = _currentHigh;
	    _prevLow = _currentLow;

	    // Initialize new period
	    _openBar = _closeBar = _highBar = _lowBar = bar;
	    _currentOpen = candle.Open;
	    _currentClose = candle.Close;
	    _currentHigh = candle.High;
	    _currentLow = candle.Low;

	    // Calculate POC, VAH, VAL for the previous period before resetting
	    SavePreviousPeriodVolumeProfile();

	    // Reset current period candle
	    ResetCurrentPeriodCandle(candle, bar);

	    _lastSession = bar;
    }

    private void SavePreviousPeriodVolumeProfile()
    {
	    if (_currentPeriodCandle.Volume > 0)
	    {
		    _previousPeriodCandle.Clear();
		    _previousPeriodCandle.Type = Type;

		    // Copy the current period data to previous
		    _prevPoc = _currentPoc;
		    var valueArea = _currentPeriodCandle.GetValueArea(InstrumentInfo.TickSize, PlatformSettings.ValueAreaPercent);
		    _prevVah = valueArea.Item1;
		    _prevVal = valueArea.Item2;
		    _prevPocBar = _currentPocBar;
	    }
    }

    private void ResetCurrentPeriodCandle(IndicatorCandle candle, int bar)
    {
	    _currentPeriodCandle.Clear();
	    _currentPeriodCandle.Type = Type;
	    _currentPoc = 0;
	    _currentVah = 0;
	    _currentVal = 0;
	    _currentPocBar = bar;

	    // Add the first candle of the new period
	    _currentPeriodCandle.AddCandle(candle, InstrumentInfo.TickSize);
    }

    private void CheckForAlerts(int bar, IndicatorCandle candle, IndicatorCandle prevCandle)
    {
	    bool isCurrentPeriod = Period is PeriodType.CurrentDay or PeriodType.CurrenWeek or PeriodType.CurrentMonth;
	    if (bar <= 0 || isCurrentPeriod)
		    return;

	    // Check for level crossings
	    CheckLevelCrossing(bar, candle, prevCandle, _prevOpen, ref _lastOpenAlertBar, UseOpenAlert, "Previous Open");
	    CheckLevelCrossing(bar, candle, prevCandle, _prevClose, ref _lastCloseAlertBar, UseCloseAlert, "Previous Close");
	    CheckLevelCrossing(bar, candle, prevCandle, _prevHigh, ref _lastHighAlertBar, UseHighAlert, "Previous High");
	    CheckLevelCrossing(bar, candle, prevCandle, _prevLow, ref _lastLowAlertBar, UseLowAlert, "Previous Low");
	    CheckLevelCrossing(bar, candle, prevCandle, _prevPoc, ref _lastPocAlertBar, UsePocAlert, "Previous POC");
	    CheckLevelCrossing(bar, candle, prevCandle, _prevVah, ref _lastVahAlertBar, UseVahAlert, "Previous VAH");
	    CheckLevelCrossing(bar, candle, prevCandle, _prevVal, ref _lastValAlertBar, UseValAlert, "Previous VAL");
    }

    private void CheckLevelCrossing(int bar, IndicatorCandle candle, IndicatorCandle prevCandle,
	    decimal level, ref int lastAlertBar, FilterBool useAlert, string levelName)
    {
	    if (!useAlert.Value || lastAlertBar == bar || level <= 0)
		    return;

	    bool triggered = false;
	    string message = $"{levelName} reached: {level}";

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
		    decimal approximationDistance = ApproximationTicks * InstrumentInfo.TickSize;

		    // Check if price is within the approximation range
		    bool isWithinRange = Math.Abs(candle.Close - level) <= approximationDistance &&
		                         Math.Abs(prevCandle.Close - level) > approximationDistance;

		    if (isWithinRange)
		    {
			    triggered = true;
			    message = $"{levelName} approximation ({ApproximationTicks} ticks): {level}";
		    }
	    }

	    if (triggered)
	    {
		    if (!OmitConsecutiveAlerts || (OmitConsecutiveAlerts && lastAlertBar < bar - 1))
		    {
			    AddAlert(AlertFile, InstrumentInfo.Instrument, message, AlertBGColor, AlertForeColor);
		    }

		    lastAlertBar = bar;
	    }
    }

    private void UpdateVolumeProfile(IndicatorCandle candle, int bar)
    {
	    // Add this candle to the current period
	    _currentPeriodCandle.AddCandle(candle, InstrumentInfo.TickSize);

	    // Update POC
	    if (_currentPeriodCandle.MaxValue > 0)
	    {
		    _currentPoc = _currentPeriodCandle.MaxValuePrice;
		    _currentPocBar = bar;

		    // Get VAH and VAL
		    var valueArea = _currentPeriodCandle.GetValueArea(InstrumentInfo.TickSize, PlatformSettings.ValueAreaPercent);
		    _currentVah = valueArea.Item1;
		    _currentVal = valueArea.Item2;
	    }
    }

    private void DrawString(RenderContext context, string renderText, int yPrice, Color color)
    {
        var textSize = context.MeasureString(renderText, _font);
        context.DrawString(renderText, _font, color, Container.Region.Right - textSize.Width - 5, yPrice - textSize.Height);
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

    private string GetPeriodString()
	{
	    return Period switch
	    {
	        PeriodType.CurrentDay => "Curr. Day ",
	        PeriodType.PreviousDay => "Prev. Day ",
	        PeriodType.CurrenWeek => "Curr. Week ",
	        PeriodType.PreviousWeek => "Prev. Week ",
	        PeriodType.CurrentMonth => "Curr. Month ",
	        PeriodType.PreviousMonth => "Prev. Month ",
	        _ => throw new ArgumentOutOfRangeException()
	    };
	}

	private void DrawLevelLine(RenderContext context, decimal price, int startBar, PenSettings pen, string text)
	{
	    if (price <= 0)
	        return;

	    var y = ChartInfo.PriceChartContainer.GetYByPrice(price, false);

	    if (DrawFromBar && startBar >= 0 && startBar <= LastVisibleBarNumber)
	    {
	        var x = ChartInfo.PriceChartContainer.GetXByBar(startBar, false);
	        context.DrawLine(pen.RenderObject, x, y, Container.Region.Right, y);
	    }
	    else if (!DrawFromBar)
	    {
	        context.DrawLine(pen.RenderObject, Container.Region.Left, y, Container.Region.Right, y);
	    }
	    else
	    {
	        return; // Don't draw text if we're not drawing the line
	    }

	    if (ShowText)
	        DrawString(context, text, y, pen.RenderObject.Color);
	}

	private void DrawPriceLabels(RenderContext context, bool isCurrentPeriod, decimal open, decimal close,
	    decimal high, decimal low, decimal poc, decimal vah, decimal val,
	    int openBar, int closeBar, int highBar, int lowBar, int pocStartBar)
	{
	    // Check if we should draw the price label based on DrawFromBar setting and bar visibility
	    bool ShouldDrawPrice(int bar, decimal price) =>
	        ((bar >= 0 && bar <= LastVisibleBarNumber) || !DrawFromBar) && price > 0;

	    if (ShouldDrawPrice(openBar, open))
	        DrawPrice(context, open, OpenPen.RenderObject);

	    if (!isCurrentPeriod && ShouldDrawPrice(closeBar, close))
	        DrawPrice(context, close, ClosePen.RenderObject);

	    if (ShouldDrawPrice(highBar, high))
	        DrawPrice(context, high, HighPen.RenderObject);

	    if (ShouldDrawPrice(lowBar, low))
	        DrawPrice(context, low, LowPen.RenderObject);

	    if (ShouldDrawPrice(pocStartBar, poc))
	        DrawPrice(context, poc, PocPen.RenderObject);

	    if (ShouldDrawPrice(pocStartBar, vah))
	        DrawPrice(context, vah, VahPen.RenderObject);

	    if (ShouldDrawPrice(pocStartBar, val))
	        DrawPrice(context, val, ValPen.RenderObject);
	}

    #endregion
}
