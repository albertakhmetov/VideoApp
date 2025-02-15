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
namespace VideoApp.Core.ViewModels;

using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using VideoApp.Core.Commands;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

public class MruListViewModel : ViewModel, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private readonly IMruListService mruService;
    private ImmutableArray<FileItem> items= ImmutableArray<FileItem>.Empty;

    public MruListViewModel(IMruListService mruService)
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException();
        }

        this.mruService = mruService.NotNull();
        this.mruService
            .Items
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Items = x)
            .DisposeWith(disposable);
    }

    public ImmutableArray<FileItem> Items
    {
        get => items;
        private set => Set(ref items, value);
    }

    public void Dispose()
    {
        disposable.Dispose();
    }
}
