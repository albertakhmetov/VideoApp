﻿/*  Copyright © 2025, Albert Akhmetov <akhmetov@live.com>   
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
namespace VideoApp.Converters;

using Microsoft.UI.Xaml;
using VideoApp.Core.Models;

internal static class Helpers
{
    public static string ToVolumeIcon(int value)
    {
        if (value == 0)
        {
            return "\uE74F";
        }
        else if (value < 25)
        {
            return "\uE992";
        }
        else if (value < 50)
        {
            return "\uE993";
        }
        else if (value < 75)
        {
            return "\uE994";
        }
        else
        {
            return "\uE995";
        }
    }

    public static string ToIcon(PlaybackState value) => value switch
    {
        PlaybackState.Playing => "\uF5B0",
        PlaybackState.Paused => "\uF8AE",
        _ => ""
    };

    public static string ToString(int value)
    {
        return TimeSpan.FromSeconds(value).ToString();
    }

    public static bool Not(bool value)
    {
        return !value;
    }

    public static Visibility VisibleIf(bool value)
    {
        return value == true ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility VisibleIfNot(bool value)
    {
        return value == false ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility VisibleIf(double value)
    {
        return Math.Abs(value) > 0.001 ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility VisibleIfNot(double value)
    {
        return Math.Abs(value) < 0.001 ? Visibility.Visible : Visibility.Collapsed;
    }
}
