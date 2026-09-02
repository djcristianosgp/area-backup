using System.IO.Enumeration;
using Area.Backup.Core.Models;

namespace Area.Backup.Infrastructure.Scanning;

/// <summary>
/// Fast, case-insensitive matcher for backup exclusions (globs, directory names, extensions, exact paths).
/// </summary>
public sealed class ExclusionMatcher
{
    private readonly List<string> _wildcardPatterns = new();
    private readonly HashSet<string> _directoryNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _extensions = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _exactPaths = new(StringComparer.OrdinalIgnoreCase);

    public ExclusionMatcher(IEnumerable<BackupExclusion>? exclusions)
    {
        if (exclusions == null) return;

        foreach (var item in exclusions)
        {
            if (string.IsNullOrWhiteSpace(item.Pattern)) continue;
            var pattern = item.Pattern.Trim();

            switch (item.Type)
            {
                case ExclusionType.DirectoryName:
                    _directoryNames.Add(pattern.TrimEnd('\\', '/'));
                    break;

                case ExclusionType.Extension:
                    _extensions.Add(pattern.StartsWith('.') ? pattern : $".{pattern}");
                    break;

                case ExclusionType.ExactPath:
                    _exactPaths.Add(Path.GetFullPath(pattern));
                    break;

                case ExclusionType.Pattern:
                default:
                    if (pattern.StartsWith("*.") && !pattern.Contains('/') && !pattern.Contains('\\'))
                    {
                        _extensions.Add(pattern[1..]); // e.g. ".tmp"
                    }
                    else if (!pattern.Contains('*') && !pattern.Contains('?') && !pattern.Contains('/') && !pattern.Contains('\\'))
                    {
                        _directoryNames.Add(pattern);
                    }
                    else
                    {
                        _wildcardPatterns.Add(pattern);
                    }
                    break;
            }
        }
    }

    public bool IsDirectoryExcluded(string directoryPath, string directoryName)
    {
        if (_directoryNames.Contains(directoryName)) return true;

        var fullDir = Path.GetFullPath(directoryPath);
        if (_exactPaths.Contains(fullDir)) return true;

        foreach (var pattern in _wildcardPatterns)
        {
            if (FileSystemName.MatchesSimpleExpression(pattern, directoryName, ignoreCase: true))
                return true;

            if (FileSystemName.MatchesSimpleExpression(pattern, directoryPath, ignoreCase: true))
                return true;
        }

        return false;
    }

    public bool IsFileExcluded(string filePath, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(ext) && _extensions.Contains(ext)) return true;

        var fullPath = Path.GetFullPath(filePath);
        if (_exactPaths.Contains(fullPath)) return true;

        foreach (var pattern in _wildcardPatterns)
        {
            if (FileSystemName.MatchesSimpleExpression(pattern, fileName, ignoreCase: true))
                return true;

            if (FileSystemName.MatchesSimpleExpression(pattern, filePath, ignoreCase: true))
                return true;
        }

        return false;
    }
}
