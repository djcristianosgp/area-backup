using Area.Backup.Core.Enums;

namespace Area.Backup.Core.Models;

/// <summary>
/// Settings for backup integrity validation.
/// </summary>
public sealed class ValidationOptions
{
    /// <summary>
    /// Depth of verification (Quick checks header/manifest/structure, Full computes all payload hashes).
    /// </summary>
    public ValidationMode Mode { get; init; } = ValidationMode.Quick;

    /// <summary>
    /// Automatically validate backup package immediately after creation.
    /// </summary>
    public bool ValidateAfterBackup { get; init; } = true;

    /// <summary>
    /// Perform a simulated temporary restore test to guarantee restorability.
    /// </summary>
    public bool PerformTestRestore { get; init; } = false;
}
