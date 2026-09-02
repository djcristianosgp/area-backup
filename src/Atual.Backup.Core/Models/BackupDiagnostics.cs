namespace Atual.Backup.Core.Models;

/// <summary>
/// Non-fatal warning occurring during backup execution (e.g. skipped locked file when configured).
/// </summary>
public sealed record BackupWarning(string Code, string Message, string? Path = null);

/// <summary>
/// Fatal or item-level error occurring during backup execution.
/// </summary>
public sealed record BackupError(string Code, string Message, string? Path = null, Exception? Exception = null);
