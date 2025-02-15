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
namespace VideoApp.Core.Models;

public sealed class FileItem : IEquatable<FileItem>, IEquatable<string>
{
    public FileItem(string fileName)
    {
        FullPath = fileName;
        Directory = Path.GetDirectoryName(fileName) ?? string.Empty;
        Name = Path.GetFileNameWithoutExtension(fileName) ?? string.Empty;
    }

    public string FullPath { get; }

    public string Directory { get; }

    public string Name { get; }

    public bool Equals(FileItem? other)
    {
        return other != null && FullPath.Equals(other.FullPath, StringComparison.InvariantCultureIgnoreCase);
    }

    public bool Equals(string? other)
    {
        return FullPath.Equals(other, StringComparison.InvariantCultureIgnoreCase);
    }
}
