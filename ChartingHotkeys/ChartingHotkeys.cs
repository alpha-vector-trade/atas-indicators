namespace ATAS.Indicators.Technical;

using ATAS.Indicators.Technical;
using ATAS.Indicators;
using OFT.Attributes;
using OFT.Rendering.Context;
using OFT.Rendering.Settings;
using OFT.Rendering.Tools;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Input;
using System;
using Utils.Common.Logging;

[DisplayName("Charting Hotkeys")]
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

        [Display(GroupName = "Cycle Through Layouts", Name = "Cycle Up", Order = 10)]
        public Key LayoutCycleUpHotkey { get; set; } = Key.PageUp;

        [Display(GroupName = "Cycle Through Layouts", Name = "Cycle Down", Order = 20)]
        public Key LayoutCycleDownHotkey { get; set; } = Key.PageDown;

        #endregion

        #region Footprint Layout 1

        [Display(GroupName = "Footprint Layout 1", Name = "Hotkey", Order = 10)]
        public Key Layout1Hotkey { get; set; } = Key.D1;

        [Display(GroupName = "Footprint Layout 1", Name = "Visual Mode", Order = 20)]
        public FootprintVisualModes Layout1FootprintVisualMode { get; set; } = FootprintVisualModes.FullRow;

        [Display(GroupName = "Footprint Layout 1", Name = "Content Mode", Order = 30)]
        public FootprintContentModes Layout1FootprintContentMode { get; set; } = FootprintContentModes.BidAsk;

        [Display(GroupName = "Footprint Layout 1", Name = "Color Scheme", Order = 40)]
        public FootprintColorSchemes Layout1FootprintColorScheme { get; set; } = FootprintColorSchemes.Delta;

        #endregion

        #region Footprint Footprint Layout 2

        [Display(GroupName = "Footprint Layout 2", Name = "Hotkey", Order = 10)]
        public Key Layout2Hotkey { get; set; } = Key.D2;

        [Display(GroupName = "Footprint Layout 2", Name = "Visual Mode", Order = 20)]
        public FootprintVisualModes Layout2FootprintVisualMode { get; set; } = FootprintVisualModes.FullRow;

        [Display(GroupName = "Footprint Layout 2", Name = "Content Mode", Order = 30)]
        public FootprintContentModes Layout2FootprintContentMode { get; set; } = FootprintContentModes.BidAsk;

        [Display(GroupName = "Footprint Layout 2", Name = "Color Scheme", Order = 40)]
        public FootprintColorSchemes Layout2FootprintColorScheme { get; set; } = FootprintColorSchemes.Delta;

        #endregion

        #region Footprint Layout 3

        [Display(GroupName = "Footprint Layout 3", Name = "Hotkey", Order = 10)]
        public Key Layout3Hotkey { get; set; } = Key.D3;

        [Display(GroupName = "Footprint Layout 3", Name = "Visual Mode", Order = 20)]
        public FootprintVisualModes Layout3FootprintVisualMode { get; set; } = FootprintVisualModes.FullRow;

        [Display(GroupName = "Footprint Layout 3", Name = "Content Mode", Order = 30)]
        public FootprintContentModes Layout3FootprintContentMode { get; set; } = FootprintContentModes.BidAsk;

        [Display(GroupName = "Footprint Layout 3", Name = "Color Scheme", Order = 40)]
        public FootprintColorSchemes Layout3FootprintColorScheme { get; set; } = FootprintColorSchemes.Delta;

        #endregion

        #region Footprint Layout 4

        [Display(GroupName = "Footprint Layout 4", Name = "Hotkey", Order = 10)]
        public Key Layout4Hotkey { get; set; } = Key.D4;

        [Display(GroupName = "Footprint Layout 4", Name = "Visual Mode", Order = 20)]
        public FootprintVisualModes Layout4FootprintVisualMode { get; set; } = FootprintVisualModes.FullRow;

        [Display(GroupName = "Footprint Layout 4", Name = "Content Mode", Order = 30)]
        public FootprintContentModes Layout4FootprintContentMode { get; set; } = FootprintContentModes.BidAsk;

        [Display(GroupName = "Footprint Layout 4", Name = "Color Scheme", Order = 40)]
        public FootprintColorSchemes Layout4FootprintColorScheme { get; set; } = FootprintColorSchemes.Delta;

        #endregion

        #region Footprint Layout 5

        [Display(GroupName = "Footprint Layout 5", Name = "Hotkey", Order = 10)]
        public Key Layout5Hotkey { get; set; } = Key.D5;

        [Display(GroupName = "Footprint Layout 5", Name = "Visual Mode", Order = 20)]
        public FootprintVisualModes Layout5FootprintVisualMode { get; set; } = FootprintVisualModes.FullRow;

        [Display(GroupName = "Footprint Layout 5", Name = "Content Mode", Order = 30)]
        public FootprintContentModes Layout5FootprintContentMode { get; set; } = FootprintContentModes.BidAsk;

        [Display(GroupName = "Footprint Layout 5", Name = "Color Scheme", Order = 40)]
        public FootprintColorSchemes Layout5FootprintColorScheme { get; set; } = FootprintColorSchemes.Delta;

        #endregion

        #endregion

        #region ctor

        public ChartingHotkeys() : base(true)
        {
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

            // Isolate the key without modifiers
            Key keyPressed = e.Key;

            // Apply the appropriate layout based on the key pressed
            switch (e.Key)
            {
                case var key when key == Layout1Hotkey:
                    _currentLayoutNumber = 1;
                    ApplyLayout(_currentLayoutNumber);
                    break;

                case var key when key == Layout2Hotkey:
                    _currentLayoutNumber = 2;
                    ApplyLayout(_currentLayoutNumber);
                    break;

                case var key when key == Layout3Hotkey:
                    _currentLayoutNumber = 3;
                    ApplyLayout(_currentLayoutNumber);
                    break;

                case var key when key == Layout4Hotkey:
                    _currentLayoutNumber = 4;
                    ApplyLayout(_currentLayoutNumber);
                    break;

                case var key when key == Layout5Hotkey:
                    _currentLayoutNumber = 5;
                    ApplyLayout(_currentLayoutNumber);
                    break;

                case var key when key == LayoutCycleUpHotkey:
                    CycleLayoutUp();
                    break;

                case var key when key == LayoutCycleDownHotkey:
                    CycleLayoutDown();
                    break;
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
            _currentLayoutNumber = (_currentLayoutNumber % MaxLayoutCount) + 1;
            ApplyLayout(_currentLayoutNumber);
        }

        private void CycleLayoutDown()
        {
            _currentLayoutNumber = (_currentLayoutNumber > 1) ? _currentLayoutNumber - 1 : MaxLayoutCount;
            ApplyLayout(_currentLayoutNumber);
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
