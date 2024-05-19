using CommandLine;
using UnityAsset.NET.BundleFile;

namespace UnityCN_Helper
{
    public class Options
    {
        [Option('i', "infile", Required = true, HelpText = "Input file to be processed.")]
        public string InFile { get; set; }

        [Option('o', "outfile", Required = true, HelpText = "Output processed file.")]
        public string OutFile { get; set; }
        
        [Option('u', "unitycn", Default = "", HelpText = "Backup unitycn info file.You can use original encrypted asset file instead when encrypting.")]
        public string UnitycnFile { get; set; }

        [Option('e', "encrypt", Default = false, HelpText = "Encrypt the asset file.")]
        public bool Encrypt { get; set; }
        
        [Option('d', "decrypt", Default = false, HelpText = "Decrypt the asset file.")]
        public bool Decrypt { get; set; }
        
        [Option('k', "key", Required = true, HelpText = "UnityCN key for decryption.")]
        public string Key { get; set; }
    }
    
    internal static class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    using FileStream inStream = new FileStream(o.InFile, FileMode.Open, FileAccess.Read);
                    BundleFile inBundleFile = new BundleFile(inStream, key: o.Key);
                    if (o.Encrypt)
                    {
                        if (string.IsNullOrEmpty(o.UnitycnFile))
                        {
                            Console.WriteLine("UnityCN file is required for encryption.");
                            return;
                        }
                        BundleFile cnbundleFile = new BundleFile(new FileStream(o.UnitycnFile, FileMode.Open, FileAccess.Read), key: o.Key);
                        inBundleFile.UnityCNInfo = cnbundleFile.UnityCNInfo;
                        inBundleFile.Write(new FileStream(o.OutFile, FileMode.Create, FileAccess.Write), infoPacker: "lz4hc", dataPacker: "lz4hc", unityCN: true);
                    }
                    else if (o.Decrypt)
                    {
                        inBundleFile.Write(new FileStream(o.OutFile, FileMode.Create, FileAccess.Write), unityCN: false);
                    }
                });
        }
    }
}