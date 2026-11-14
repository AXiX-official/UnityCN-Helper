using UnityAsset.NET.BundleFiles;
using CompressionType = UnityAsset.NET.CompressionType;
using UnityAsset.NET.FileSystem;
using UnityAsset.NET.FileSystem.DirectFileSystem;

namespace UnityCN_Helper;

internal static class BundleProcessor
{
    internal static bool DecryptSingle(string inFile, string outFile, string key, CompressionType compressionType)
    {
        using var file = new DirectFileInfo(inFile);
        if (file.FileType != FileType.BundleFile) return false;
        BundleFile inBundleFile = new BundleFile(file, key: key);
        inBundleFile.Serialize(outFile, compressionType, compressionType);
        return true;
    }

    internal static bool EncryptSingle(string inFile, string outFile, string key, CompressionType compressionType)
    {
        using var file = new DirectFileInfo(inFile);
        if (file.FileType != FileType.BundleFile) return false;
        BundleFile inBundleFile = new BundleFile(file);
        inBundleFile.Serialize(outFile, compressionType, compressionType, key);
        return true;
    }
    
    internal static void ProcessFolder(
        string inFolder,
        string outFolderRoot,
        string key,
        CompressionType compression,
        Func<string, string, string, CompressionType, bool> processSingle,
        string statusText,
        ConsoleFileProgress progress)
    {
        if (!Directory.Exists(inFolder))
        {
            Console.WriteLine("Folder not found.");
            return;
        }

        var files = Directory.EnumerateFiles(inFolder, "*", SearchOption.AllDirectories).ToList();
        int total = files.Count;
        if (total == 0)
        {
            Console.WriteLine($"No files found in {inFolder}.");
            return;
        }

        for (int i = 0; i < total; i++)
        {
            string file = files[i];

            var outFolder = Path.GetDirectoryName(file)!.Replace(inFolder, outFolderRoot);
            if (!Directory.Exists(outFolder))
                Directory.CreateDirectory(outFolder);

            string targetFile = Path.Combine(outFolder, Path.GetFileName(file));

            try
            {
                if (!processSingle(file, targetFile, key, compression))
                    Console.WriteLine($"{Path.GetRelativePath(inFolder, file)} skipped (not a BundleFile).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Path.GetRelativePath(inFolder, file)} failed: {ex.Message}");
            }

            progress.Report(new FileProgress(statusText, total, i + 1, Path.GetRelativePath(inFolder, file)));
        }

        progress.Complete();
    }
}

sealed record FileProgress(
    string StatusText,
    int Total,
    int Processed,
    string? RelativePath)
{
    public double Percentage => Total == 0 ? 0 : (double)Processed / Total * 100;
}

internal sealed class ConsoleFileProgress : IProgress<FileProgress>
{
    private readonly bool _interactive = !Console.IsOutputRedirected;

    public void Report(FileProgress p)
    {
        string line = $"{p.StatusText} {p.Processed}/{p.Total} ({p.Percentage:F1}%) {p.RelativePath}";

        if (_interactive)
        {
            Console.Write($"\r{line}");
            int pad = Console.WindowWidth - 1 - line.Length;
            if (pad > 0) Console.Write(new string(' ', pad));
        }
        else
        {
            Console.WriteLine(line);
        }
    }

    public void Complete()
    {
        if (_interactive) Console.WriteLine();
    }
}