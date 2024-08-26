# UnityCN-Helper

A tool helps decrypt/encrypt assetbundle files for UnityCN.

## Command Line Arguments

- -i, --infile     Required. Input file to be processed.

- -o, --outfile    Required. Output processed file.

- -e, --encrypt    (Default: false) Encrypt the asset file.

- -d, --decrypt    (Default: false) Decrypt the asset file.

- -k, --key        Required. UnityCN key for decryption.

- -f, --folder     (Default: false) Operate on a folder instead of a file.

- --help           Display this help screen.

- --version        Display version information.

### Example

```bash
./UnityCN-Helper -i test.bundle -o test.bundle.de -k 5265736F6E616E63655265626F726E52 -d
./UnityCN-Helper -i test.bundle.de -o test.bundle -k 5265736F6E616E63655265626F726E52 -e
```

for operating on a folder:

```bash
./UnityCN-Helper -i test -o output -k 5265736F6E616E63655265626F726E52 -d -f
./UnityCN-Helper -i output -o output_en -k 5265736F6E616E63655265626F726E52 -e -f
```


## You need to know

In fact, after modifying the asset file, encrypting is not necessary. 

For games I encountered, the game can load unencrypted asset files as well as encrypted asset files.

## Special Thanks

- [Perfare](https://github.com/Perfare) for creating and maintaining and every contributor of [AssetStudio](https://github.com/Perfare/AssetStudio)

- [Razmoth](https://github.com/Razmoth) for figuring out and sharing Unity CN's AssetBundle decryption ([src](https://github.com/RazTools/Studio)).