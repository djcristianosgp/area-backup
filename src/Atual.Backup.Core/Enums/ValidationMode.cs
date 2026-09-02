namespace Atual.Backup.Core.Enums;

/// <summary>
/// Defines the depth of backup integrity verification.
/// </summary>
public enum ValidationMode
{
    /// <summary>
    /// Verifies archive integrity, header, manifest structure, format versions, and entry counts.
    /// </summary>
    Quick = 0,

    /// <summary>
    /// Reads and computes streaming SHA-256 hashes of every file in the package and validates entire dependency chain.
    /// </summary>
    Full = 1
}
