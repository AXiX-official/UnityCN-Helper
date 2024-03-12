using CommandLine;
using AssetStudio;

namespace Resonance_decryptor
{
    public class Options
    {
        [Option('i', "infile", Required = true, HelpText = "Input file to be processed.")]
        public string InFile { get; set; }

        [Option('o', "outfile", Required = true, HelpText = "Output processed file.")]
        public string OutFile { get; set; }
        
        [Option('u', "unitycn", Required = true, HelpText = "backup unitycn info.")]
        public string UnitycnFile { get; set; }

        [Option('e', "encrypt", Default = false, HelpText = "Encrypt the lua file.")]
        public bool Encrypt { get; set; }
        
        [Option('d', "decrypt", Default = false, HelpText = "Decrypt the lua file.")]
        public bool Decrypt { get; set; }
        
        [Option('n', "name", Default = "", HelpText = "Game Name.")]
        public string Name { get; set; }
        
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
                    BundleFile.UnityCNKey = o.Key;
                    BundleFile.UnityCNGameName = o.Name;
                    if (o.Encrypt)
                    {
                        BundleFile bundleFile = new BundleFile(new FileReader(o.InFile), o.OutFile, o.UnitycnFile, CryptoType.None);
                    }
                    else if (o.Decrypt)
                    {
                        BundleFile bundleFile = new BundleFile(new FileReader(o.InFile), o.OutFile, o.UnitycnFile, CryptoType.UnityCN);
                    }
                });
        }
    }
}