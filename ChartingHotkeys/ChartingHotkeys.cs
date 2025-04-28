using System.Collections.Generic;
using System.Linq;

namespace ATAS.Indicators.AlphaVector;

using ATAS.Indicators;
using OFT.Attributes;
using OFT.Rendering.Context;
using OFT.Rendering.Settings;
using OFT.Rendering.Tools;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Windows.Input;

[DisplayName("Charting Hotkeys")]
[Category("AlphaVector")]
[HelpLink("https://github.com/alpha-vector-trade/atas-indicators")]
public class ChartingHotkeys : Indicator
{
    #region Fields

    // Track the current layout
    private int _currentLayoutNumber = 1;
    private const int MaxLayoutCount = 5;

    #endregion

    #region Properties

    #region General Settings

    [Display(GroupName = "General", Name = "Enable Hotkeys", Order = 10)]
    public bool EnableHotkeys { get; set; } = true;

    [Display(GroupName = "General", Name = "Global Modifier 1", Order = 20)]
    public Key GlobalModifier1 { get; set; } = Key.LeftCtrl;

    [Display(GroupName = "General", Name = "Global Modifier 2", Order = 30)]
    public Key GlobalModifier2 { get; set; } = Key.None;

    #endregion

    #region Layout Cycle

    [Display(GroupName = "Cycle Through Layouts", Name = "Cycle Up", Order = 40)]
    public Key LayoutCycleUpHotkey { get; set; } = Key.PageUp;

    [Display(GroupName = "Cycle Through Layouts", Name = "Cycle Down", Order = 50)]
    public Key LayoutCycleDownHotkey { get; set; } = Key.PageDown;

    #endregion

    #region Footprint Layout 1

    [Display(GroupName = "Footprint Layout 1", Name = "Enabled", Order = 100)]
    public bool Layout1Enabled { get; set; } = true;

    [Display(GroupName = "Footprint Layout 1", Name = "Hotkey", Order = 110)]
    public Key Layout1Hotkey { get; set; } = Key.D1;

    [Display(GroupName = "Footprint Layout 1", Name = "Visual Mode", Order = 120)]
    public FootprintVisualModes Layout1FootprintVisualMode { get; set; } = FootprintVisualModes.FullRow;

    [Display(GroupName = "Footprint Layout 1", Name = "Content Mode", Order = 130)]
    public FootprintContentModes Layout1FootprintContentMode { get; set; } = FootprintContentModes.BidXAsk;

    [Display(GroupName = "Footprint Layout 1", Name = "Color Scheme", Order = 140)]
    public FootprintColorSchemes Layout1FootprintColorScheme { get; set; } = FootprintColorSchemes.Delta;

    #endregion

    #region Footprint Footprint Layout 2

    [Display(GroupName = "Footprint Layout 2", Name = "Enabled", Order = 200)]
    public bool Layout2Enabled { get; set; } = true;

    [Display(GroupName = "Footprint Layout 2", Name = "Hotkey", Order = 210)]
    public Key Layout2Hotkey { get; set; } = Key.D2;

    [Display(GroupName = "Footprint Layout 2", Name = "Visual Mode", Order = 220)]
    public FootprintVisualModes Layout2FootprintVisualMode { get; set; } = FootprintVisualModes.FullRow;

    [Display(GroupName = "Footprint Layout 2", Name = "Content Mode", Order = 230)]
    public FootprintContentModes Layout2FootprintContentMode { get; set; } = FootprintContentModes.BidXAsk;

    [Display(GroupName = "Footprint Layout 2", Name = "Color Scheme", Order = 240)]
    public FootprintColorSchemes Layout2FootprintColorScheme { get; set; } = FootprintColorSchemes.Delta;

    #endregion

    #region Footprint Layout 3

    [Display(GroupName = "Footprint Layout 3", Name = "Enabled", Order = 300)]
    public bool Layout3Enabled { get; set; } = true;

    [Display(GroupName = "Footprint Layout 3", Name = "Hotkey", Order = 310)]
    public Key Layout3Hotkey { get; set; } = Key.D3;

    [Display(GroupName = "Footprint Layout 3", Name = "Visual Mode", Order = 320)]
    public FootprintVisualModes Layout3FootprintVisualMode { get; set; } = FootprintVisualModes.FullRow;

    [Display(GroupName = "Footprint Layout 3", Name = "Content Mode", Order = 330)]
    public FootprintContentModes Layout3FootprintContentMode { get; set; } = FootprintContentModes.BidXAsk;

    [Display(GroupName = "Footprint Layout 3", Name = "Color Scheme", Order = 340)]
    public FootprintColorSchemes Layout3FootprintColorScheme { get; set; } = FootprintColorSchemes.Delta;

    #endregion

    #region Footprint Layout 4

    [Display(GroupName = "Footprint Layout 4", Name = "Enabled", Order = 400)]
    public bool Layout4Enabled { get; set; } = true;

    [Display(GroupName = "Footprint Layout 4", Name = "Hotkey", Order = 410)]
    public Key Layout4Hotkey { get; set; } = Key.D4;

    [Display(GroupName = "Footprint Layout 4", Name = "Visual Mode", Order = 420)]
    public FootprintVisualModes Layout4FootprintVisualMode { get; set; } = FootprintVisualModes.FullRow;

    [Display(GroupName = "Footprint Layout 4", Name = "Content Mode", Order = 430)]
    public FootprintContentModes Layout4FootprintContentMode { get; set; } = FootprintContentModes.BidXAsk;

    [Display(GroupName = "Footprint Layout 4", Name = "Color Scheme", Order = 440)]
    public FootprintColorSchemes Layout4FootprintColorScheme { get; set; } = FootprintColorSchemes.Delta;

    #endregion

    #region Footprint Layout 5

    [Display(GroupName = "Footprint Layout 5", Name = "Enabled", Order = 500)]
    public bool Layout5Enabled { get; set; } = true;

    [Display(GroupName = "Footprint Layout 5", Name = "Hotkey", Order = 510)]
    public Key Layout5Hotkey { get; set; } = Key.D5;

    [Display(GroupName = "Footprint Layout 5", Name = "Visual Mode", Order = 520)]
    public FootprintVisualModes Layout5FootprintVisualMode { get; set; } = FootprintVisualModes.FullRow;

    [Display(GroupName = "Footprint Layout 5", Name = "Content Mode", Order = 530)]
    public FootprintContentModes Layout5FootprintContentMode { get; set; } = FootprintContentModes.BidXAsk;

    [Display(GroupName = "Footprint Layout 5", Name = "Color Scheme", Order = 540)]
    public FootprintColorSchemes Layout5FootprintColorScheme { get; set; } = FootprintColorSchemes.Delta;

    #endregion

    #endregion

    #region ctor

    public ChartingHotkeys() : base(true)
    {
        DenyToChangePanel = true;

        DataSeries[0].IsHidden = true;
        SubscribeToDrawingEvents(DrawingLayouts.Final);
    }

    #endregion

    #region Public Methods

    public override bool ProcessKeyDown(KeyEventArgs e)
    {
        if (!EnableHotkeys)
            return false;

        // Check if modifiers match
        bool modifier1Pressed = GlobalModifier1 == Key.None || Keyboard.IsKeyDown(GlobalModifier1);
        bool modifier2Pressed = GlobalModifier2 == Key.None || Keyboard.IsKeyDown(GlobalModifier2);

        if (!modifier1Pressed || !modifier2Pressed)
            return false;

        // Apply the appropriate layout based on the key pressed
        switch (e.Key)
        {
            case var key when key == Layout1Hotkey && Layout1Enabled:
                _currentLayoutNumber = 1;
                ApplyLayout(_currentLayoutNumber);
                break;

            case var key when key == Layout2Hotkey && Layout2Enabled:
                _currentLayoutNumber = 2;
                ApplyLayout(_currentLayoutNumber);
                break;

            case var key when key == Layout3Hotkey && Layout3Enabled:
                _currentLayoutNumber = 3;
                ApplyLayout(_currentLayoutNumber);
                break;

            case var key when key == Layout4Hotkey && Layout4Enabled:
                _currentLayoutNumber = 4;
                ApplyLayout(_currentLayoutNumber);
                break;

            case var key when key == Layout5Hotkey && Layout4Enabled:
                _currentLayoutNumber = 5;
                ApplyLayout(_currentLayoutNumber);
                break;

            case var key when key == LayoutCycleUpHotkey:
                CycleLayoutUp();
                break;

            case var key when key == LayoutCycleDownHotkey:
                CycleLayoutDown();
                break;

            default:
                return false;
        }

        return true;
    }

    #endregion

    #region Protected Methods

    protected override void OnCalculate(int bar, decimal value)
    {
        // This indicator doesn't perform any calculations
    }

    #endregion

    #region Private Methods

    private void CycleLayoutUp()
    {
        int nextLayout = GetNextEnabledLayout(_currentLayoutNumber);
        if (nextLayout != -1)
        {
            _currentLayoutNumber = nextLayout;
            ApplyLayout(_currentLayoutNumber);
        }
    }

    private void CycleLayoutDown()
    {
        int prevLayout = GetPreviousEnabledLayout(_currentLayoutNumber);
        if (prevLayout != -1)
        {
            _currentLayoutNumber = prevLayout;
            ApplyLayout(_currentLayoutNumber);
        }
    }

    private int GetNextEnabledLayout(int currentLayout)
    {
        // Get all enabled layouts
        var enabledLayouts = GetEnabledLayouts();

        if (enabledLayouts.Count == 0)
            return -1;

        int index = enabledLayouts.IndexOf(currentLayout);
        if (index == -1)
            return enabledLayouts.First();

        // Get next layout or wrap around to the first
        return enabledLayouts[(index + 1) % enabledLayouts.Count];
    }

    private int GetPreviousEnabledLayout(int currentLayout)
    {
        // Get all enabled layouts
        var enabledLayouts = GetEnabledLayouts();
        if (enabledLayouts.Count == 0)
            return -1;

        int index = enabledLayouts.IndexOf(currentLayout);
        if (index == -1)
            return enabledLayouts.Last();

        // Get previous layout or wrap around to the last
        return enabledLayouts[(index - 1 + enabledLayouts.Count) % enabledLayouts.Count];
    }

    private List<int> GetEnabledLayouts()
    {
        var enabledLayouts = new List<int>();

        if (Layout1Enabled) enabledLayouts.Add(1);
        if (Layout2Enabled) enabledLayouts.Add(2);
        if (Layout3Enabled) enabledLayouts.Add(3);
        if (Layout4Enabled) enabledLayouts.Add(4);
        if (Layout5Enabled) enabledLayouts.Add(5);

        return enabledLayouts;
    }


    private void ApplyLayout(int layoutNumber)
    {
        FootprintVisualModes footprintMode;
        FootprintContentModes contentMode;
        FootprintColorSchemes colorScheme;

        switch (layoutNumber)
        {
            case 1:
                footprintMode = Layout1FootprintVisualMode;
                contentMode = Layout1FootprintContentMode;
                colorScheme = Layout1FootprintColorScheme;
                break;
            case 2:
                footprintMode = Layout2FootprintVisualMode;
                contentMode = Layout2FootprintContentMode;
                colorScheme = Layout2FootprintColorScheme;
                break;
            case 3:
                footprintMode = Layout3FootprintVisualMode;
                contentMode = Layout3FootprintContentMode;
                colorScheme = Layout3FootprintColorScheme;
                break;
            case 4:
                footprintMode = Layout4FootprintVisualMode;
                contentMode = Layout4FootprintContentMode;
                colorScheme = Layout4FootprintColorScheme;
                break;
            case 5:
                footprintMode = Layout5FootprintVisualMode;
                contentMode = Layout5FootprintContentMode;
                colorScheme = Layout5FootprintColorScheme;
                break;
            default:
                return;
        }

        // Apply the settings to the chart
        ChartInfo.FootprintVisualMode = footprintMode;
        ChartInfo.FootprintContentMode = contentMode;
        ChartInfo.FootprintColorScheme = colorScheme;

        // Force chart redraw
        RedrawChart();
    }

    #endregion
}
