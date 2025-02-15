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
using Microsoft.UI.Xaml.Input;

[TemplatePart(Name = "PART_BORDER", Type = typeof(Border))]
public class AppItemContainer : ContentControl
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(AppItemContainer),
        new PropertyMetadata(null, null));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter),
        typeof(object),
        typeof(AppItemContainer),
        new PropertyMetadata(null, null));

    public AppItemContainer()
    {
        DefaultStyleKey = typeof(AppItemContainer);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "PointerOver", true);

        base.OnPointerEntered(e);
    }

    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "Normal", true);

        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "Pressed", true);

        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "PointerOver", true);

        base.OnPointerReleased(e);

        if (!e.Handled)
        {
            Command?.Execute(CommandParameter);
        }
    }
}
