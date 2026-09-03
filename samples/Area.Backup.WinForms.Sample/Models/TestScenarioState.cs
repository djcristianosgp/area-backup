using System.Security.Cryptography;
using Area.Backup.Core.Models;

namespace Area.Backup.WinForms.Sample.Models;

/// <summary>
/// Manages state and helper operations for the Test Lab (Dummy data generation, mutation simulation, restore verification).
/// </summary>
public sealed class TestScenarioState
{
    public string TestRootFolder { get; set; }
    public string SourceFolder { get; set; }
    public string RepositoryFolder { get; set; }
    public string SandboxRestoreFolder { get; set; }

    public int TotalGeneratedFiles { get; set; }
    public int ModifiedFilesCount { get; set; }
    public int AddedFilesCount { get; set; }
    public int DeletedFilesCount { get; set; }

    public TestScenarioState()
    {
        TestRootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AreaBackupTestLab");
        SourceFolder = Path.Combine(TestRootFolder, "Source_ERP");
        RepositoryFolder = Path.Combine(TestRootFolder, "Repository");
        SandboxRestoreFolder = Path.Combine(TestRootFolder, "Sandbox_Restored");
    }

    /// <summary>
    /// Creates a complete dummy ERP dataset with structured folders and synthetic files.
    /// </summary>
    public void GenerateInitialDataset(int fileCount = 40)
    {
        if (Directory.Exists(TestRootFolder))
        {
            try { Directory.Delete(TestRootFolder, recursive: true); } catch { }
        }

        Directory.CreateDirectory(SourceFolder);
        Directory.CreateDirectory(RepositoryFolder);
        Directory.CreateDirectory(SandboxRestoreFolder);

        // Subfolders
        var docDir = Path.Combine(SourceFolder, "Documentos_PDF");
        var xmlDir = Path.Combine(SourceFolder, "NFe_XML");
        var dataDir = Path.Combine(SourceFolder, "BaseDados_Dat");
        var tempDir = Path.Combine(SourceFolder, "Temp"); // To test exclusion!

        Directory.CreateDirectory(docDir);
        Directory.CreateDirectory(xmlDir);
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(tempDir);

        // Temp excluded file
        File.WriteAllText(Path.Combine(tempDir, "temp_lock.tmp"), "Temporario para teste de exclusão");
        File.WriteAllText(Path.Combine(SourceFolder, "debug.log"), "Log file para teste de exclusão *.log");

        // Generate synthetic files
        var rnd = new Random(42);
        for (int i = 1; i <= fileCount / 3; i++)
        {
            File.WriteAllText(Path.Combine(docDir, $"Contrato_{i:D4}.pdf"), $"CONTRATO #{i:D4}\nCliente: Empresa Teste {i}\nData: {DateTime.Now:yyyy-MM-dd}\nValor: R$ {rnd.Next(100, 50000):N2}");
        }

        for (int i = 1; i <= fileCount / 3; i++)
        {
            File.WriteAllText(Path.Combine(xmlDir, $"NFe_352609_{i:D6}.xml"), $"<nfeProc versao=\"4.00\"><NFe><infNFe Id=\"NFe352609{i:D6}\"><total><vNF>{rnd.Next(50, 9999):N2}</vNF></total></infNFe></NFe></nfeProc>");
        }

        for (int i = 1; i <= fileCount / 3; i++)
        {
            byte[] bytes = new byte[1024 * rnd.Next(2, 10)]; // 2 to 10 KB
            rnd.NextBytes(bytes);
            File.WriteAllBytes(Path.Combine(dataDir, $"Tabela_{i:D3}.dat"), bytes);
        }

        TotalGeneratedFiles = Directory.GetFiles(SourceFolder, "*.*", SearchOption.AllDirectories).Length;
        ModifiedFilesCount = 0;
        AddedFilesCount = 0;
        DeletedFilesCount = 0;
    }

    /// <summary>
    /// Simulates real ERP mutations (modifying existing files, creating new invoices, deleting old drafts).
    /// </summary>
    public (int added, int modified, int deleted) SimulateMutations()
    {
        if (!Directory.Exists(SourceFolder))
        {
            GenerateInitialDataset(20);
        }

        int added = 0;
        int modified = 0;
        int deleted = 0;

        var xmlDir = Path.Combine(SourceFolder, "NFe_XML");
        var docDir = Path.Combine(SourceFolder, "Documentos_PDF");
        var dataDir = Path.Combine(SourceFolder, "BaseDados_Dat");

        // 1. Add new files
        var rnd = new Random();
        int newId = rnd.Next(1000, 9999);
        File.WriteAllText(Path.Combine(xmlDir, $"NFe_NOVA_EMISSAO_{newId}.xml"), $"<nfeProc><infNFe Id=\"NOVA_{newId}\"><vNF>1500.00</vNF></infNFe></nfeProc>");
        File.WriteAllText(Path.Combine(docDir, $"Aditivo_Contratual_{newId}.pdf"), $"ADITIVO CONTRATO #{newId}\nData: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        added += 2;

        // 2. Modify existing files
        var existingDocs = Directory.GetFiles(docDir, "*.pdf");
        if (existingDocs.Length > 0)
        {
            var targetDoc = existingDocs[0];
            File.AppendAllText(targetDoc, $"\n[ALTERADO EM {DateTime.Now:HH:mm:ss}] - Cláusula adicional adicionada.");
            modified++;
        }

        var existingDat = Directory.GetFiles(dataDir, "*.dat");
        if (existingDat.Length > 0)
        {
            var targetDat = existingDat[0];
            var bytes = File.ReadAllBytes(targetDat);
            bytes[0] = (byte)(bytes[0] ^ 0xFF); // mutate first byte
            File.WriteAllBytes(targetDat, bytes);
            modified++;
        }

        // 3. Delete 1 file to test deletion tracking
        if (existingDocs.Length > 2)
        {
            var targetToDelete = existingDocs[^1];
            try
            {
                File.Delete(targetToDelete);
                deleted++;
            }
            catch { }
        }

        AddedFilesCount += added;
        ModifiedFilesCount += modified;
        DeletedFilesCount += deleted;
        TotalGeneratedFiles = Directory.GetFiles(SourceFolder, "*.*", SearchOption.AllDirectories).Length;

        return (added, modified, deleted);
    }

    /// <summary>
    /// Compares all files in the source directory against the sandbox restored directory using SHA-256 bit-by-bit comparison.
    /// </summary>
    public (int matched, int mismatched, int missingInRestore, List<string> errors) VerifySandboxParity()
    {
        var errors = new List<string>();
        if (!Directory.Exists(SandboxRestoreFolder))
        {
            errors.Add("A pasta de sandbox para restauração não existe.");
            return (0, 0, 0, errors);
        }

        // Find the restored source root (since backup places sources under source tags or direct paths)
        var restoredFiles = Directory.GetFiles(SandboxRestoreFolder, "*.*", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(SandboxRestoreFolder, f).Replace('/', '\\'), f => f, StringComparer.OrdinalIgnoreCase);

        var sourceFiles = Directory.GetFiles(SourceFolder, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".log", StringComparison.OrdinalIgnoreCase) && !f.Contains("\\Temp\\"))
            .ToList();

        int matched = 0;
        int mismatched = 0;
        int missing = 0;

        using var sha = SHA256.Create();

        foreach (var srcFile in sourceFiles)
        {
            var relPath = Path.GetRelativePath(SourceFolder, srcFile).Replace('/', '\\');

            // Find matching restored file (may be prefixed with source tag or relative path)
            var matchingRestored = restoredFiles.FirstOrDefault(kvp => 
                kvp.Key.Equals(relPath, StringComparison.OrdinalIgnoreCase) || 
                kvp.Key.EndsWith(relPath, StringComparison.OrdinalIgnoreCase));

            if (matchingRestored.Value == null)
            {
                missing++;
                errors.Add($"Arquivo ausente na restauração: {relPath}");
                continue;
            }

            var srcHash = ComputeSha256(srcFile);
            var resHash = ComputeSha256(matchingRestored.Value);

            if (srcHash == resHash)
            {
                matched++;
            }
            else
            {
                mismatched++;
                errors.Add($"Divergência de Hash SHA-256 no arquivo: {relPath} (Origem: {srcHash[..8]}... != Restore: {resHash[..8]}...)");
            }
        }

        return (matched, mismatched, missing, errors);
    }

    private static string ComputeSha256(string filePath)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexStringLower(hash);
    }
}
