using CommandLine;
using UnityAsset.NET.Enums;
using UnityAsset.NET.Files;

namespace UnityCN_Helper
{
    public class Options
    {
        [Option('i', "infile", Required = true, HelpText = "Input file to be processed.")]
        public string InFile { get; set; }

        [Option('o', "outfile", Required = true, HelpText = "Output processed file.")]
        public string OutFile { get; set; }

        [Option('e', "encrypt", Default = false, HelpText = "Encrypt the asset file.")]
        public bool Encrypt { get; set; }
        
        [Option('d', "decrypt", Default = false, HelpText = "Decrypt the asset file.")]
        public bool Decrypt { get; set; }
        
        [Option('k', "key", Required = true, HelpText = "UnityCN key for decryption.")]
        public string Key { get; set; }
        
        [Option('f', "folder", Default = false, HelpText = "Operate on a folder instead of a file.")]
        public bool Folder { get; set; }
    }

    internal static class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    if (o.Encrypt)
                    {
                        if (o.Folder)
                        {
                            if (!Directory.Exists(o.InFile))
                            {
                                Console.WriteLine("Folder not found.");
                                return;
                            }

                            foreach (string file in
                                     Directory.EnumerateFiles(o.InFile, "*", SearchOption.AllDirectories))
                            {
                                string outFolder = Path.GetDirectoryName(file).Replace(o.InFile, o.OutFile);
                                if (!Directory.Exists(outFolder))
                                {
                                    Directory.CreateDirectory(outFolder);
                                }

                                EncryptSingle(file, Path.Combine(outFolder, Path.GetFileName(file)), o.Key);
                            }
                        }
                        else
                        {
                            EncryptSingle(o.InFile, o.OutFile, o.Key);
                        }
                    }
                    else if (o.Decrypt)
                    {
                        if (o.Folder)
                        {
                            if (!Directory.Exists(o.InFile))
                            {
                                Console.WriteLine("Folder not found.");
                                return;
                            }

                            foreach (string file in
                                     Directory.EnumerateFiles(o.InFile, "*", SearchOption.AllDirectories))
                            {
                                string outFolder = Path.GetDirectoryName(file).Replace(o.InFile, o.OutFile);
                                if (!Directory.Exists(outFolder))
                                {
                                    Directory.CreateDirectory(outFolder);
                                }
                                DecryptSingle(file, Path.Combine(outFolder, Path.GetFileName(file)), o.Key);
                            }
                        }
                        else
                        {
                            DecryptSingle(o.InFile, o.OutFile, o.Key);
                        }
                    }
                });
        }

        static void DecryptSingle(string inFile, string outFile, string key)
        {
            BundleFile inBundleFile = new BundleFile(inFile, key: key);
            inBundleFile.Serialize(outFile);
        }

        static void EncryptSingle(string inFile, string outFile, string key)
        {
            BundleFile inBundleFile = new BundleFile(inFile, key: key);
            inBundleFile.Serialize(outFile, CompressionType.Lz4HC, CompressionType.Lz4HC, unityCnKey : key);
        }
    }
}