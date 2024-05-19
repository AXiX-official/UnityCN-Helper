# UnityCN-Helper

A tool helps decrypt/encrypt assetbundle files for UnityCN.

## Command Line Arguments

- -i, --infile     Required. Input file to be processed.

- -o, --outfile    Required. Output processed file.

- -u, --unitycn    (Default: ) Backup unitycn info file.You can use original encrypted asset file instead when
  encrypting.

- -e, --encrypt    (Default: false) Encrypt the asset file.

- -d, --decrypt    (Default: false) Decrypt the asset file.

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

If you don't want save UnityCN info file, you can use original encrypted asset file instead.
```bash
./UnityCN-Helper -i test.asset -o test.asset.de -k 5265736F6E616E63655265626F726E52 -d
./UnityCN-Helper -i test.asset.de -o test.asset -u test.asset -k 5265736F6E616E63655265626F726E52 -e
```

## You need to know

In fact, after modifying the asset file, encrypting is not necessary. 

For games I encountered, the game can load unencrypted asset files as well as encrypted asset files.

## Special Thanks

- [Perfare](https://github.com/Perfare) for creating and maintaining and every contributor of [AssetStudio](https://github.com/Perfare/AssetStudio)

- [Razmoth](https://github.com/Razmoth) for figuring out and sharing Unity CN's AssetBundle decryption ([src](https://github.com/RazTools/Studio)).