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
namespace VideoApp.Core;

using System.Collections;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;

public static class Extensions
{
    public static void IsTrue(this bool result, string? errorMessage = null)
    {
        if (!result)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    public static T NotNull<T>(
        this T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
        string? errorMessage = null) where T : class
    {
        if (value == null)
        {
            throw new ArgumentNullException(paramName, errorMessage);
        }

        return value;
    }

    public static Delegate NotNull(
        this Delegate value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
        string? errorMessage = null)
    {
        if (value == null)
        {
            throw new ArgumentNullException(paramName, errorMessage);
        }

        return value;
    }


    public static void NotEmpty(
        this object value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
        string? errorMessage = null)
    {
        if (value is IList list && list.Count == 0)
        {
            throw new ArgumentException(paramName, errorMessage);
        }
    }

    public static void Equal<T>(
        this T value, T expectedValue,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
        string? errorMessage = null) where T : IComparable<T>
    {
        if (value.CompareTo(expectedValue) != 0)
        {
            throw new ArgumentException(paramName, errorMessage);
        }
    }

    public static void InRange<T>(
        this T value, T start, T end,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
    string? errorMessage = null) where T : IComparable<T>
    {
        if (value.CompareTo(start) < 0 || value.CompareTo(end) > 0)
        {
            throw new ArgumentOutOfRangeException(paramName, errorMessage);
        }
    }

    public static int ToInt(
        this long value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
        string? errorMessage = null)
    {
        if (value > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(paramName, errorMessage);
        }

        return (int)value;
    }

    public static int ToInt(
        this ulong value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
        string? errorMessage = null)
    {
        if (value > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(paramName, errorMessage);
        }

        return (int)value;
    }

    public static T DisposeWith<T>(
        this T obj,
        CompositeDisposable compositeDisposable) where T : IDisposable
    {
        compositeDisposable.Add(obj);

        return obj;
    }
}

