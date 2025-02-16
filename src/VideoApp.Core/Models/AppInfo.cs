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

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

public sealed class AppInfo
{
    public AppInfo(FileInfo fileInfo)
    {
        fileInfo.NotNull();

        var info = FileVersionInfo.GetVersionInfo(fileInfo.FullName);

        ProductName = info.ProductName ?? string.Empty;
        ProductVersion = GetProductVersion(info.ProductVersion ?? string.Empty);
        Comments = info.Comments ?? string.Empty;
        Copyright = info.LegalCopyright ?? string.Empty;
        Runtime = $"{Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName}";

        FileVersion = new Version(
            info.FileMajorPart,
            info.FileMinorPart,
            info.FileBuildPart,
            info.FilePrivatePart);

        IsPreRelease = Regex.IsMatch(ProductVersion, "[a-zA-Z]");
    }

    public string ProductName { get; }

    public string ProductVersion { get; }

    public string Comments { get; }

    public string Copyright { get; }

    public string Runtime { get; }

    public Version FileVersion { get; }

    public bool IsPreRelease { get; }

    private string GetProductVersion(string productVersion)
    {
        var plusIndex = productVersion.IndexOf("+");

        if (plusIndex > -1)
        {
            return productVersion.Substring(0, plusIndex);
        }
        else
        {
            return productVersion;
        }
    }
}
