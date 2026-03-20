using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using OpenDash.WheelOverlay.Models;
using OpenDash.WheelOverlay.ViewModels;

namespace OpenDash.WheelOverlay.Views
{
    public partial class DialLayout : System.Windows.Controls.UserControl
    {
        private OverlayViewModel? _viewModel;
        private readonly List<TextBlock> _labelBlocks = new();
        private Storyboard? _flashStoryboard;

        /// <summary>
        /// Maps user-facing scale (1–10) to internal render fraction (0.3–0.75).
        /// </summary>
        private static double UserScaleToInternal(double userScale)
        {
            double clamped = Math.Max(1.0, Math.Min(10.0, userScale));
            return 0.3 + (clamped - 1.0) * 0.05; // 1→0.3, 10→0.75
        }

        private double GetInternalScale()
        {
            double userScale = _viewModel?.Settings?.ActiveProfile?.DialKnobScale ?? 5.0;
            return UserScaleToInternal(userScale);
        }

        public DialLayout()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            DialCanvas.SizeChanged += OnDialCanvasSizeChanged;
        }

        private void OnDialCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            PositionKnobGraphic();
            PositionLabelsOnCanvas();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModel = DataContext as OverlayViewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                RebuildLabels();
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(OverlayViewModel.Settings):
                case nameof(OverlayViewModel.PopulatedPositionLabels):
                case nameof(OverlayViewModel.DisplayItems):
                    RebuildLabels();
                    break;
                case nameof(OverlayViewModel.CurrentPosition):
                case nameof(OverlayViewModel.CurrentItem):
                    UpdateSelection();
                    RotateKnobToActivePosition();
                    break;
                case nameof(OverlayViewModel.IsFlashing):
                    UpdateFlashAnimation();
                    break;
            }
        }

        private void RebuildLabels()
        {
            for (int i = DialCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (DialCanvas.Children[i] is TextBlock)
                    DialCanvas.Children.RemoveAt(i);
            }
            _labelBlocks.Clear();

            if (_viewModel == null) return;
            var profile = _viewModel.Settings?.ActiveProfile;
            if (profile == null) return;

            int positionCount = profile.PositionCount;
            var angles = DialPositionConfig.GetAngles(positionCount);
            var labels = profile.TextLabels;

            for (int i = 0; i < positionCount; i++)
            {
                int positionIndex = i + 1;
                if (!angles.ContainsKey(positionIndex)) continue;
                string text = (i < labels.Count) ? labels[i] : string.Empty;
                var tb = CreateLabelTextBlock(text, i);
                _labelBlocks.Add(tb);
                DialCanvas.Children.Add(tb);
            }

            UpdateSelection();
            ComputeAndSetCanvasSize();
            PositionKnobGraphic();
            RotateKnobToActivePosition();
            PositionLabelsOnCanvas();
        }

        private void ComputeAndSetCanvasSize()
        {
            if (_viewModel == null || _labelBlocks.Count == 0) return;
            var profile = _viewModel.Settings?.ActiveProfile;
            if (profile == null) return;

            double scale = GetInternalScale();

            double maxLabelWidth = 0;
            double maxLabelHeight = 0;
            foreach (var tb in _labelBlocks)
            {
                tb.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                maxLabelWidth = Math.Max(maxLabelWidth, tb.DesiredSize.Width);
                maxLabelHeight = Math.Max(maxLabelHeight, tb.DesiredSize.Height);
            }

            // Proportional gap from profile setting
            double gapPercent = (_viewModel?.Settings?.ActiveProfile?.DialLabelGapPercent ?? 15) / 100.0;
            double knobSizeEstimate = 200 * scale;
            double gap = (knobSizeEstimate / 2.0) * gapPercent;
            gap = Math.Max(gap, 2); // minimum 2px
            double maxLabelExtent = Math.Max(maxLabelWidth, maxLabelHeight);

            // canvasSize/2 = (canvasSize * scale / 2) * 0.975 + gap + maxLabelExtent
            // canvasSize * (1 - scale * 0.975) / 2 = gap + maxLabelExtent
            double denominator = 1.0 - scale * 0.975;
            if (denominator <= 0.01) denominator = 0.01;

            double requiredSize = 2.0 * (gap + maxLabelExtent) / denominator;
            requiredSize = Math.Max(requiredSize, 150);

            DialCanvas.Width = Math.Round(requiredSize);
            DialCanvas.Height = Math.Round(requiredSize);
        }

        private TextBlock CreateLabelTextBlock(string text, int positionIndex)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Tag = positionIndex,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 2,
                    ShadowDepth = 1,
                    Opacity = 0.8
                }
            };

            if (_viewModel != null)
            {
                var profile = _viewModel.Settings?.ActiveProfile;
                var settings = _viewModel.Settings;

                tb.FontSize = settings?.FontSize ?? 20;
                tb.FontFamily = new System.Windows.Media.FontFamily(settings?.FontFamily ?? "Segoe UI");

                if (profile != null)
                {
                    try
                    {
                        var converter = new FontWeightConverter();
                        var weight = converter.ConvertFromString(profile.FontWeight);
                        if (weight is FontWeight fw)
                            tb.FontWeight = fw;
                    }
                    catch { tb.FontWeight = FontWeights.Bold; }
                }

                if (profile != null && Enum.TryParse<TextRenderingMode>(profile.TextRenderingMode, true, out var renderMode))
                    TextOptions.SetTextRenderingMode(tb, renderMode);
                else
                    TextOptions.SetTextRenderingMode(tb, TextRenderingMode.Aliased);
            }

            return tb;
        }

        private void UpdateSelection()
        {
            if (_viewModel == null) return;
            var settings = _viewModel.Settings;
            int currentPos = _viewModel.CurrentPosition;
            var selectedBrush = BrushFromHex(settings.SelectedTextColor);
            var nonSelectedBrush = BrushFromHex(settings.NonSelectedTextColor);

            foreach (var tb in _labelBlocks)
            {
                int posIndex = (int)tb.Tag;
                tb.Foreground = (posIndex == currentPos) ? selectedBrush : nonSelectedBrush;
            }
        }

        private void RotateKnobToActivePosition()
        {
            if (_viewModel == null) return;
            var profile = _viewModel.Settings?.ActiveProfile;
            if (profile == null) return;

            int positionIndex = _viewModel.CurrentPosition + 1;
            var angles = DialPositionConfig.GetAngles(profile.PositionCount);
            if (!angles.ContainsKey(positionIndex)) return;

            KnobGraphic.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            KnobGraphic.RenderTransform = new RotateTransform(angles[positionIndex]);
        }

        private void PositionKnobGraphic()
        {
            double cw = DialCanvas.ActualWidth;
            double ch = DialCanvas.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            double scale = GetInternalScale();
            double knobSize = Math.Min(cw, ch) * scale;
            KnobGraphic.Width = knobSize;
            KnobGraphic.Height = knobSize;
            Canvas.SetLeft(KnobGraphic, (cw - knobSize) / 2.0);
            Canvas.SetTop(KnobGraphic, (ch - knobSize) / 2.0);
        }

        private void PositionLabelsOnCanvas()
        {
            if (_viewModel == null || _labelBlocks.Count == 0) return;

            double cw = DialCanvas.ActualWidth;
            double ch = DialCanvas.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            double centerX = cw / 2.0;
            double centerY = ch / 2.0;

            double scale = GetInternalScale();
            double knobSize = Math.Min(cw, ch) * scale;
            double cogTipRadius = (knobSize / 2.0) * 0.975;
            double gapPercent = (_viewModel?.Settings?.ActiveProfile?.DialLabelGapPercent ?? 15) / 100.0;
            double gap = (knobSize / 2.0) * gapPercent;
            gap = Math.Max(gap, 2);
            double labelRadius = cogTipRadius + gap;

            var profile = _viewModel?.Settings?.ActiveProfile;
            if (profile == null) return;
            var angles = DialPositionConfig.GetAngles(profile.PositionCount);

            for (int i = 0; i < _labelBlocks.Count; i++)
            {
                int positionIndex = i + 1;
                if (!angles.ContainsKey(positionIndex)) continue;

                var tb = _labelBlocks[i];
                tb.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                double tbW = tb.DesiredSize.Width;
                double tbH = tb.DesiredSize.Height;

                // Estimate the width of a single character for anchor offset.
                // The arrow should point to the middle of the first character
                // (right side) or the middle of the last character (left side).
                string labelText = tb.Text ?? "";
                double charW = labelText.Length > 0 ? tbW / labelText.Length : 0;
                double halfChar = charW / 2.0;

                double angleDeg = angles[positionIndex];
                var (x, y) = DialPositionConfig.AngleToPoint(angleDeg, labelRadius);

                double left, top;
                double a = ((angleDeg % 360) + 360) % 360;

                if (a >= 350 || a <= 10)
                {
                    // Top (12 o'clock): center text horizontally
                    left = centerX + x - tbW / 2.0;
                    top = centerY + y - tbH;
                }
                else if (a >= 170 && a <= 190)
                {
                    // Bottom (6 o'clock): center text horizontally
                    left = centerX + x - tbW / 2.0;
                    top = centerY + y;
                }
                else if (a > 10 && a < 170)
                {
                    // Right side: arrow points to middle of first character
                    left = centerX + x - halfChar;
                    top = centerY + y - tbH / 2.0;
                }
                else
                {
                    // Left side: arrow points to middle of last character
                    left = centerX + x - tbW + halfChar;
                    top = centerY + y - tbH / 2.0;
                }

                Canvas.SetLeft(tb, left);
                Canvas.SetTop(tb, top);
            }
        }

        private static SolidColorBrush BrushFromHex(string hex)
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch { return System.Windows.Media.Brushes.White; }
        }

        private void UpdateFlashAnimation()
        {
            if (_viewModel == null) return;
            if (_viewModel.IsFlashing) StartFlashAnimation();
            else StopFlashAnimation();
        }

        private void StartFlashAnimation()
        {
            StopFlashAnimation();
            var animation = new DoubleAnimation
            {
                From = 1.0, To = 0.3,
                Duration = TimeSpan.FromMilliseconds(125),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            _flashStoryboard = new Storyboard();
            _flashStoryboard.Children.Add(animation);
            Storyboard.SetTarget(animation, DialCanvas);
            Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));
            _flashStoryboard.Begin();
        }

        private void StopFlashAnimation()
        {
            if (_flashStoryboard != null)
            {
                _flashStoryboard.Stop();
                _flashStoryboard = null;
            }
            DialCanvas.Opacity = 1.0;
        }
    }
}
