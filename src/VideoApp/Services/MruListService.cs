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
namespace VideoApp.Services;

using System;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VideoApp.Core;
using VideoApp.Core.Models;
using VideoApp.Core.Services;

class MruListService : IMruListService
{
    private readonly BehaviorSubject<ImmutableArray<FileItem>> itemsSubject;

    public MruListService()
    {
        itemsSubject = new BehaviorSubject<ImmutableArray<FileItem>>([.. LoadItems()]);

        Items = itemsSubject.AsObservable();
    }

    public int MaxCount { get; } = 10;

    public IObservable<ImmutableArray<FileItem>> Items { get; }

    public void Add(string fileName)
    {
        var item = itemsSubject.Value.FirstOrDefault(x => x.FullPath.Equals(fileName));

        var items = itemsSubject.Value.ToList();
        if (item != null)
        {
            items.Remove(item);
        }

        items.Insert(0, new FileItem(fileName));

        itemsSubject.OnNext([.. items.Take(MaxCount)]);

        SaveItems();
    }

    public void Remove(string fileName)
    {
        var item = itemsSubject.Value.FirstOrDefault(x => x.FullPath.Equals(fileName));

        if (item != null)
        {
            var items = itemsSubject.Value.ToList();
            items.Remove(item);

            itemsSubject.OnNext([.. items.Take(MaxCount)]);
        }

        SaveItems();
    }

    private IEnumerable<FileItem> LoadItems()
    {
        var r = new List<FileItem>();

        try
        {
            using var reader = File.OpenText("./mru.txt");

            var line = default(string);
            while ((line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrEmpty(line) && File.Exists(line))
                {
                    r.Add(new FileItem(line));
                }
            }
        }
        catch (Exception)
        {
            return [];
        }

        return r.Take(MaxCount);
    }

    private void SaveItems()
    {
        using var writer = File.CreateText("./mru.txt");

        foreach (var i in itemsSubject.Value.Take(MaxCount))
        {
            writer.WriteLine(i.FullPath);
        }
    }
}
