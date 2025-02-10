/*  Copyright © 2025, Albert Akhmetov <akhmetov@live.com>   
 *
 *  This file is part of VideoApp.
 *
 *  VideoApp is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  VideoApp is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with VideoApp. If not, see <https://www.gnu.org/licenses/>.   
 *
 */
namespace VideoApp.Controls;

using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;

[TemplatePart(Name = "PART_DECREASE", Type = typeof(Rectangle))]
[TemplatePart(Name = "PART_TRACK", Type = typeof(Rectangle))]
[TemplatePart(Name = "PART_THUMB", Type = typeof(Thumb))]
public class AppSlider : Control
{
    public static readonly DependencyProperty ThumbTipValueConverterProperty = DependencyProperty.Register(
        nameof(ThumbTipValueConverter),
        typeof(IValueConverter),
        typeof(AppSlider),
        new PropertyMetadata(null, OnThumbTipValueConverterPropertyChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(int),
        typeof(AppSlider),
        new PropertyMetadata(0, OnValuePropertyChanged));

    public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
        nameof(MaxValue),
        typeof(int),
        typeof(AppSlider),
        new PropertyMetadata(100, OnMaxValuePropertyChanged));

    public static readonly DependencyProperty StepFrequencyProperty = DependencyProperty.Register(
        nameof(StepFrequency),
        typeof(int),
        typeof(AppSlider),
        new PropertyMetadata(1, OnStepFrequencyPropertyChanged));

    public static readonly DependencyProperty NavigationStepFrequencyProperty = DependencyProperty.Register(
        nameof(NavigationStepFrequency),
        typeof(int),
        typeof(AppSlider),
        new PropertyMetadata(1, null));

    public static readonly DependencyProperty PositionCommandProperty = DependencyProperty.Register(
        nameof(PositionCommand),
        typeof(ICommand),
        typeof(AppSlider),
        new PropertyMetadata(null, null));

    private static void OnThumbTipValueConverterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AppSlider slider)
        {
            slider.SetToolTip();
        }
    }

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AppSlider slider && e.NewValue is int newValue)
        {
            if (newValue < 0)
            {
                slider.Value = 0;
            }
            else if (newValue > slider.MaxValue)
            {
                slider.Value = slider.MaxValue;
            }
            else
            {
                slider.UpdateThumbPosition();
                slider.SetToolTip();
            }
        }
    }

    private static void OnMaxValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AppSlider slider && e.NewValue is int newMaxValue)
        {
            if (slider.Value > newMaxValue)
            {
                slider.Value = newMaxValue;
            }
        }
    }

    private static void OnStepFrequencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AppSlider slider && e.NewValue is int newStepFrequency)
        {
            if (slider.Value != slider.MaxValue && slider.Value % newStepFrequency != 0)
            {
                slider.Value = slider.Value / newStepFrequency * newStepFrequency;
            }
        }
    }

    private Thumb? thumb;
    private Rectangle? decrease, track;

    private double thumbPosition;

    public AppSlider()
    {
        this.DefaultStyleKey = typeof(AppSlider);
    }

    public IValueConverter ThumbTipValueConverter
    {
        get => (IValueConverter)GetValue(ThumbTipValueConverterProperty);
        set => SetValue(ThumbTipValueConverterProperty, value);
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int MaxValue
    {
        get => (int)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public int StepFrequency
    {
        get => (int)GetValue(StepFrequencyProperty);
        set => SetValue(StepFrequencyProperty, value);
    }

    public int NavigationStepFrequency
    {
        get => (int)GetValue(NavigationStepFrequencyProperty);
        set => SetValue(NavigationStepFrequencyProperty, value);
    }

    public ICommand? PositionCommand
    {
        get => (ICommand?)GetValue(PositionCommandProperty);
        set => SetValue(PositionCommandProperty, value);
    }

    protected override void OnApplyTemplate()
    {
        if (track != null)
        {
            track.SizeChanged -= Track_SizeChanged;
        }

        if (thumb != null)
        {
            thumb.DragStarted -= Thumb_DragStarted;
            thumb.DragCompleted -= Thumb_DragCompleted;
            thumb.DragDelta -= Thumb_DragDelta;
        }

        base.OnApplyTemplate();

        decrease = GetTemplateChild("PART_DECREASE") as Rectangle;
        track = GetTemplateChild("PART_TRACK") as Rectangle;
        thumb = GetTemplateChild("PART_THUMB") as Thumb;

        UpdateThumbPosition();

        if (track != null)
        {
            track.SizeChanged += Track_SizeChanged;
        }

        if (thumb != null)
        {
            thumb.DragStarted += Thumb_DragStarted;
            thumb.DragCompleted += Thumb_DragCompleted;
            thumb.DragDelta += Thumb_DragDelta;
        }

        SetToolTip();
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Left:
                    Value -= NavigationStepFrequency;
                    PositionCommand?.Execute(Value);
                    break;

                case Windows.System.VirtualKey.Right:
                    Value += NavigationStepFrequency;
                    PositionCommand?.Execute(Value);
                    break;
            }
        }
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        if (track != null && thumb != null)
        {
            var pos = e.GetCurrentPoint(this).Position.X;

            SetValue(MaxValue * (pos / (track.ActualWidth - thumb.ActualWidth)));
        }
    }

    private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        thumbPosition = decrease?.ActualWidth ?? 0;

        if (GetTemplateChild("PART_THUMB_TOOLTIP") is ToolTip toolTip)
        {
            toolTip.IsOpen = false;
        }
    }

    private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        thumbPosition = decrease?.ActualWidth ?? 0;
    }

    private void Track_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateThumbPosition();
    }

    private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is Thumb t && !t.IsDragging)
        {
            return;
        }

        if (decrease != null && track != null && thumb != null)
        {
            thumbPosition += e.HorizontalChange;
            SetValue(MaxValue * (thumbPosition / (track.ActualWidth - thumb.ActualWidth)));
        }
    }

    private void SetValue(double value)
    {
        var newValue = Convert.ToInt32(value);
        Value = newValue == MaxValue ? newValue : (newValue / StepFrequency * StepFrequency);
        PositionCommand?.Execute(Value);
        ReOpenToolTip();
    }

    private void UpdateThumbPosition()
    {
        if (decrease != null && track != null && thumb != null && track.ActualWidth > 0)
        {
            decrease.SetValue(Rectangle.WidthProperty, Value * 1d / MaxValue * (track.ActualWidth - thumb.ActualWidth));
        }
    }

    private void ReOpenToolTip()
    {
        if (GetTemplateChild("PART_THUMB_TOOLTIP") is ToolTip toolTip)
        {
            toolTip.IsOpen = false;
            toolTip.IsOpen = true;
        }
    }

    private void SetToolTip()
    {
        if (GetTemplateChild("PART_THUMB_TOOLTIP") is ToolTip toolTip)
        {
            toolTip.Content = ThumbTipValueConverter == null
                ? Value.ToString()
                : ThumbTipValueConverter.Convert(Value, typeof(string), null, null);
        }
    }
}
