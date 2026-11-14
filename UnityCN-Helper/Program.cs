using UnityCN_Helper;
using XenoAtom.CommandLine;
using CompressionType = UnityAsset.NET.CompressionType;

string? inFile = null;
string? outFile = null;
bool encrypt = false;
bool decrypt = false;
string? key = null;
bool folder = false;
CompressionType compression = CompressionType.None;

var app = new CommandApp("UnityCN-Helper")
{
    new CommandUsage(), // 自动生成 Usage 行
    { "i|infile=", "Input file to be processed.", v => inFile = v },
    { "o|outfile=", "Output processed file.", v => outFile = v },
    { "e|encrypt", "Encrypt the asset file.", v => encrypt = v is not null },
    { "d|decrypt", "Decrypt the asset file.", v => decrypt = v is not null },
    { "k|key=", "UnityCN key for decryption.", v => key = v },
    { "f|folder", "Operate on a folder instead of a file.", v => folder = v is not null },
    { "c|compression=", $"Compression type ({EnumWrapper<CompressionType>.Names}).", 
        (EnumWrapper<CompressionType> v) => compression = v },
    new HelpOption(),
    new VersionOption(),
    
    (ctx, _) =>
    {
        if (encrypt == decrypt)
        {
            ctx.Error.WriteLine("Specify exactly one of --encrypt or --decrypt.");
            return ValueTask.FromResult(1);
        }
        
        if (string.IsNullOrEmpty(inFile) || string.IsNullOrEmpty(outFile) || string.IsNullOrEmpty(key))
        {
            ctx.Error.WriteLine("Error: --infile, --outfile and --key are required.");
            return ValueTask.FromResult(1);
        }
        
        var progress = new ConsoleFileProgress();

        if (folder)
        {
            BundleProcessor.ProcessFolder(inFile, outFile, key!, compression,
                encrypt ? BundleProcessor.EncryptSingle : BundleProcessor.DecryptSingle,
                encrypt ? "Encrypting" : "Decrypting",
                progress);
        }
        else
        {
            bool ok = encrypt
                ? BundleProcessor.EncryptSingle(inFile, outFile, key!, compression)
                : BundleProcessor.DecryptSingle(inFile, outFile, key!, compression);

            if (!ok) Console.WriteLine($"{inFile} skipped (not a BundleFile).");
        }
        
        return ValueTask.FromResult(0);
    }
};

return await app.RunAsync(args);