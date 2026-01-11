using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace KitsuneMenu.Core;

internal static class MenuFileSystem
{
    private static string? _baseDirectory;

    public static void Initialize(string baseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(baseDirectory))
        {
            _baseDirectory = baseDirectory;
        }
    }

    public static string GetBaseDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_baseDirectory))
        {
            return _baseDirectory!;
        }

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var directory = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(directory))
            {
                return directory;
            }
        }

        return AppContext.BaseDirectory;
    }

    public static string Combine(params string[] parts)
    {
        var segments = new List<string> { GetBaseDirectory() };
        segments.AddRange(parts);
        return Path.Combine(segments.ToArray());
    }

    public static void EnsureDirectoryForFile(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
