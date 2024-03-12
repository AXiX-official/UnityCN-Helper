# UnityCN-Helper

A tool helps decrypt/encrypt assetbundle files for UnityCN.

## Command Line Arguments

- -i, --infile     Required. Input file to be processed.

- -o, --outfile    Required. Output processed file.

- -u, --unitycn    Required. backup unitycn info.

- -e, --encrypt    (Default: false) Encrypt the lua file.

- -d, --decrypt    (Default: false) Decrypt the lua file.

- -n, --name       (Default: ) Game Name.

- -k, --key        Required. UnityCN key for decryption.

- --help           Display this help screen.

- --version        Display version information.

### Example

decrypt `test.asset` to `test.asset.de` with `test.cn` and `5265736F6E616E63655265626F726E52` key.
```bash
./UnityCN-Helper -i test.asset -o test.asset.de -u test.cn -k 5265736F6E616E63655265626F726E52 -d
```

after modify `test.asset.de`, encrypt it to `test.asset` with `test.cn` and `5265736F6E616E63655265626F726E52` key.
```bash
./UnityCN-Helper -i test.asset.de -o test.asset -u test.cn -k 5265736F6E616E63655265626F726E52 -e
```

## Supported File Types

Only support UnityFS now.

Only tested on Unity 2019.4.40f1c1.

If this tool doesn't work for you, please open an issue with the file you are trying to decrypt/encrypt.

## Special Thanks

- [Perfare](https://github.com/Perfare) for creating and maintaining and every contributor of [AssetStudio](https://github.com/Perfare/AssetStudio)

- [Razmoth](https://github.com/Razmoth) for figuring out and sharing Unity CN's AssetBundle decryption ([src](https://github.com/RazTools/Studio)).