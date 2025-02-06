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
namespace VideoApp.Core.Commands;

using System;
using System.Windows.Input;

public abstract class CommandBase : ObservableObject, ICommand
{
    private string text;
    private bool isEnabled = true;

    protected CommandBase(string? text = null)
    {
        this.text = text ?? string.Empty;
    }

    public string Text
    {
        get => text;
        private set => Set(ref text, value);
    }

    public bool IsEnabled
    {
        get => isEnabled;
        private set
        {
            if (Set(ref isEnabled, value))
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? CanExecuteChanged;

    public virtual bool CanExecute(object? parameter)
    {
        return IsEnabled;
    }

    public abstract void Execute(object? parameter);
}