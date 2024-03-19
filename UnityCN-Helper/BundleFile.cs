// This file is based on: https://github.com/RazTools/Studio/tree/main/AssetStudio/BundleFile.cs

using System.Text;
using System.Text.RegularExpressions;
using System.Buffers;
using K4os.Compression.LZ4;
namespace AssetStudio
{
    [Flags]
    public enum ArchiveFlags
    {
        CompressionTypeMask = 0x3f,
        BlocksAndDirectoryInfoCombined = 0x40,
        BlocksInfoAtTheEnd = 0x80,
        OldWebPluginCompatibility = 0x100,
        BlockInfoNeedPaddingAtStart = 0x200,
        UnityCNEncryption = 0x400
    }

    [Flags]
    public enum StorageBlockFlags
    {
        CompressionTypeMask = 0x3f,
        Streamed = 0x40,
    }

    public enum CompressionType
    {
        None,
        Lzma,
        Lz4,
        Lz4HC,
        Lzham,
        Lz4Mr0k,
        Lz4Inv = 5,
        Zstd = 5
    }
    
    public enum CryptoType
    {
        None,
        UnityCN
    }
    
    public sealed class Header
    {
        public string signature;
        public uint version;
        public string unityVersion;
        public string unityRevision;
        public long size;
        public uint compressedBlocksInfoSize;
        public uint uncompressedBlocksInfoSize;
        public ArchiveFlags flags;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"signature: {signature} | ");
            sb.Append($"version: {version} | ");
            sb.Append($"unityVersion: {unityVersion} | ");
            sb.Append($"unityRevision: {unityRevision} | ");
            sb.Append($"size: 0x{size:X8} | ");
            sb.Append($"compressedBlocksInfoSize: 0x{compressedBlocksInfoSize:X8} | ");
            sb.Append($"uncompressedBlocksInfoSize: 0x{uncompressedBlocksInfoSize:X8} | ");
            sb.Append($"flags: 0x{(int)flags:X8}");
            return sb.ToString();
        }
        
        // UnityFS only
        public void WriteToStream(FileStream writer)
        {
            writer.Write(Encoding.UTF8.GetBytes(signature));
            writer.Write(new byte[] { 0x00 });
            // write uint32
            writer.WriteInt32BigEndian((int)version);
            writer.Write(Encoding.UTF8.GetBytes(unityVersion));
            writer.Write(new byte[] { 0x00 });
            writer.Write(Encoding.UTF8.GetBytes(unityRevision));
            writer.Write(new byte[] { 0x00 });
            // wirte Int64
            writer.WriteInt64BigEndian(size);
            // write uint32
            writer.WriteInt32BigEndian((int)compressedBlocksInfoSize);
            writer.WriteInt32BigEndian((int)uncompressedBlocksInfoSize);
            writer.WriteInt32BigEndian((int)flags);
        }
    }
    
    public sealed class StorageBlock
    {
        public uint compressedSize;
        public uint uncompressedSize;
        public StorageBlockFlags flags;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"compressedSize: 0x{compressedSize:X8} | ");
            sb.Append($"uncompressedSize: 0x{uncompressedSize:X8} | ");
            sb.Append($"flags: 0x{(int)flags:X8}");
            return sb.ToString();
        }
        
        public void WriteToStream(MemoryStream writer)
        {
            // wirte uint32
            writer.WriteInt32BigEndian((int)uncompressedSize);
            writer.WriteInt32BigEndian((int)compressedSize);
            // write uint16
            writer.WriteUInt16BigEndian((ushort)flags);
        }
    }

    public sealed class Node
    {
        public long offset;
        public long size;
        public uint flags;
        public string path;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"offset: 0x{offset:X8} | ");
            sb.Append($"size: 0x{size:X8} | ");
            sb.Append($"flags: {flags} | ");
            sb.Append($"path: {path}");
            return sb.ToString();
        }
        
        public void WriteToStream(MemoryStream writer)
        {
            writer.WriteInt64BigEndian(offset);
            writer.WriteInt64BigEndian(size);
            writer.WriteInt32BigEndian((int)flags);
            writer.Write(Encoding.UTF8.GetBytes(path));
            writer.Write(new byte[] { 0x00 });
        }
    }

    public sealed class BundleFile
    {
        public static string UnityCNGameName = "";
        public static string UnityCNKey = "";
        
        private UnityCN UnityCN;

        public Header m_Header;
        private List<Node> m_DirectoryInfo;
        private List<StorageBlock> m_BlocksInfo;

        public List<StreamFile> fileList;
        
        private bool HasUncompressedDataHash = true;
        private bool HasBlockInfoNeedPaddingAtStart = true;
        
        private FileStream backupFileStream;
        private long unityCNaddress = 0;
        private long unityCNsize = 0;
        private long paddingPosition = 0;
        private CryptoType cryptoType;
        private List<byte[]> blocksBytesList;
        private byte[] UncompressedDataHash;
        private MemoryStream unityCNData;

        public BundleFile(FileReader reader, string targetFile, string unitycnFile,CryptoType cryptoType = CryptoType.UnityCN)
        {
            this.cryptoType = cryptoType;
            m_Header = ReadBundleHeader(reader);
            ReadHeader(reader);
            
            unityCNaddress = reader.BaseStream.Position;
            
            if (cryptoType == CryptoType.UnityCN)
            {
                ReadUnityCN(reader);
                if (!string.IsNullOrEmpty(unitycnFile))
                {
                    unityCNsize = reader.BaseStream.Position - unityCNaddress;
                    reader.BaseStream.Position = unityCNaddress;
                    using FileStream backup = new FileStream(unitycnFile, FileMode.Create, FileAccess.Write);
                    byte[] buffer = new byte[unityCNsize];
                    reader.BaseStream.Read(buffer, 0, buffer.Length);
                    backup.Write(buffer, 0, buffer.Length);
                    backup.Close();
                }
            }
            else
            {
                unityCNData = new MemoryStream();
                Console.WriteLine("Init UnityCN");
                using FileStream fs = new FileStream(unitycnFile, FileMode.Open, FileAccess.Read);
                var header = new byte[5];
                fs.Read(header, 0, 5);
                fs.Position = 0;
                if (Encoding.UTF8.GetString(header) == "Unity")
                {
                    FileReader tmpReader = new FileReader(unitycnFile, fs);
                    ReadBundleHeader(tmpReader);
                    tmpReader.ReadInt64();
                    tmpReader.ReadUInt32();
                    tmpReader.ReadUInt32();
                    tmpReader.ReadUInt32();
                    var begin = tmpReader.Position;
                    ReadUnityCN(tmpReader);
                    var offset = tmpReader.Position - begin;
                    tmpReader.Position = begin;
                    byte[] buffer = new byte[offset];
                    tmpReader.Read(buffer, 0, buffer.Length);
                    unityCNData.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    ReadUnityCN(new EndianBinaryReader(fs));
                    fs.Position = 0;
                    fs.CopyTo(unityCNData);
                }
            }
            
            Console.WriteLine("ReadBlocksInfoAndDirectory");
            ReadBlocksInfoAndDirectory(reader);
            blocksBytesList = new List<byte[]>();
            using (var blocksStream = CreateBlocksStream(reader.FullPath))
            {
                ReadBlocks(reader, blocksStream);
                ReadFiles(blocksStream, reader.FullPath);
            }
            
            FileStream outStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write);
            
            MemoryStream tmp = new MemoryStream();
            if (HasUncompressedDataHash)
            {
                tmp.Write(UncompressedDataHash);
            }
            tmp.WriteInt32BigEndian(m_BlocksInfo.Count);
            foreach (StorageBlock block in m_BlocksInfo)
            {
                
                if (cryptoType == CryptoType.None)
                {
                    
                    if (((int)block.flags & 0x100) == 0)
                    {
                        block.flags += 0x100;
                    }
                }
                else if (cryptoType == CryptoType.UnityCN)
                {
                    if (((int)block.flags & 0x100) != 0)
                    {
                        block.flags -= 0x100;
                    }
                }
                block.WriteToStream(tmp);
                Console.WriteLine($"Block Info: {block}");
            }
            tmp.WriteInt32BigEndian(m_DirectoryInfo.Count);
            foreach (Node node in m_DirectoryInfo)
            {
                node.WriteToStream(tmp);
                Console.WriteLine($"Directory Info: {node}");
            }
            // lz4hc compress
            byte[] inputBytes = tmp.ToArray();
            var newUncompressedSize = inputBytes.Length;
            byte[] newBlocksInfoBytes = new byte[LZ4Codec.MaximumOutputSize(newUncompressedSize)];
            var newCompressedBlocksInfoSize = LZ4Codec.Encode(inputBytes, newBlocksInfoBytes, LZ4Level.L09_HC);
            Array.Resize(ref newBlocksInfoBytes, newCompressedBlocksInfoSize);
            m_Header.compressedBlocksInfoSize = (uint)newCompressedBlocksInfoSize;
            m_Header.uncompressedBlocksInfoSize = (uint)newUncompressedSize;
            
            
            if (cryptoType == CryptoType.None)
            {
                if ((m_Header.flags & ArchiveFlags.BlockInfoNeedPaddingAtStart) == 0)
                {
                    m_Header.flags += (int)ArchiveFlags.BlockInfoNeedPaddingAtStart;
                }
            }
            else
            {
                if ((m_Header.flags & ArchiveFlags.BlockInfoNeedPaddingAtStart) != 0)
                {
                    m_Header.flags -= (int)ArchiveFlags.BlockInfoNeedPaddingAtStart;
                }
            }
            
            if ((m_Header.flags & ArchiveFlags.BlocksInfoAtTheEnd) != 0)
            {
                m_Header.flags -= (int)ArchiveFlags.BlocksInfoAtTheEnd;
            }
            
            m_Header.WriteToStream(outStream);
            
            if (cryptoType == CryptoType.None && !string.IsNullOrEmpty(unitycnFile))
            {
                unityCNData.Position = 0;
                unityCNData.CopyTo(outStream);
            }
            
            if (m_Header.version >= 7)
            {
                outStream.AlignStream(16);
            }
            // BlocksAndDirectoryInfoCombined
            if ((m_Header.flags & ArchiveFlags.BlocksInfoAtTheEnd) == 0)
            {
                Console.WriteLine($"BlocksAndDirectoryInfoCombined");
                outStream.Write(newBlocksInfoBytes);
            }
            
            if (HasBlockInfoNeedPaddingAtStart && (m_Header.flags & ArchiveFlags.BlockInfoNeedPaddingAtStart) != 0)
            {
                outStream.AlignStream(16);
            }
            foreach (var compressedBytes in blocksBytesList)
            {
                outStream.Write(compressedBytes);
            }
            outStream.Close();
        }

        private Header ReadBundleHeader(FileReader reader)
        {
            Header header = new Header();
            header.signature = reader.ReadStringToNull(20);
            Console.WriteLine($"Parsed signature {header.signature}");
            if (header.signature != "UnityFS")
            {
                throw new Exception("Unsupported signature,UnityFS wanted!");
            }
            header.version = reader.ReadUInt32();
            header.unityVersion = reader.ReadStringToNull();
            header.unityRevision = reader.ReadStringToNull();
            return header;
        }

        private Stream CreateBlocksStream(string path)
        {
            Stream blocksStream;
            var uncompressedSizeSum = m_BlocksInfo.Sum(x => x.uncompressedSize);
            Console.WriteLine($"Total size of decompressed blocks: {uncompressedSizeSum}");
            if (uncompressedSizeSum >= int.MaxValue)
            {
                blocksStream = new FileStream(path + ".temp", FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
            }
            else
            {
                blocksStream = new MemoryStream((int)uncompressedSizeSum);
            }
            return blocksStream;
        }
        
        public void ReadFiles(Stream blocksStream, string path)
        {
            Console.WriteLine($"Writing files from blocks stream...");

            fileList = new List<StreamFile>();
            for (int i = 0; i < m_DirectoryInfo.Count; i++)
            {
                var node = m_DirectoryInfo[i];
                var file = new StreamFile();
                fileList.Add(file);
                file.path = node.path;
                file.fileName = Path.GetFileName(node.path);
                if (node.size >= int.MaxValue)
                {
                    var extractPath = path + "_unpacked" + Path.DirectorySeparatorChar;
                    Directory.CreateDirectory(extractPath);
                    file.stream = new FileStream(extractPath + file.fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                else
                {
                    file.stream = new MemoryStream((int)node.size);
                }
                blocksStream.Position = node.offset;
                blocksStream.CopyTo(file.stream, node.size);
                file.stream.Position = 0;
            }
        }

        private void ReadHeader(FileReader reader)
        {
            m_Header.size = reader.ReadInt64();
            m_Header.compressedBlocksInfoSize = reader.ReadUInt32();
            m_Header.uncompressedBlocksInfoSize = reader.ReadUInt32();
            m_Header.flags = (ArchiveFlags)reader.ReadUInt32();
            Console.WriteLine($"Bundle header Info: {m_Header}");
        }

        private void ReadUnityCN(EndianBinaryReader reader)
        {
            ArchiveFlags mask;

            var version = ParseVersion();
            //Flag changed it in these versions
            if (version[0] < 2020 || //2020 and earlier
                (version[0] == 2020 && version[1] == 3 && version[2] <= 34) || //2020.3.34 and earlier
                (version[0] == 2021 && version[1] == 3 && version[2] <= 2) || //2021.3.2 and earlier
                (version[0] == 2022 && version[1] == 3 && version[2] <= 1)) //2022.3.1 and earlier
            {
                mask = ArchiveFlags.BlockInfoNeedPaddingAtStart;
                HasBlockInfoNeedPaddingAtStart = false;
            }
            else
            {
                mask = ArchiveFlags.UnityCNEncryption;
                HasBlockInfoNeedPaddingAtStart = true;
            }

            Console.WriteLine($"Mask set to {mask}");
            Console.WriteLine($"Header flags: {m_Header.flags}");

            if (cryptoType == CryptoType.UnityCN)
            {
                if ((m_Header.flags & mask) != 0)
                {
                    Console.WriteLine($"Encryption flag exist, file is encrypted, attempting to decrypt");
                    UnityCN.SetKey(new UnityCN.Entry(UnityCNGameName, UnityCNKey));
                    UnityCN = new UnityCN(reader);
                }
                else
                {
                    throw new Exception("File is not encrypted, but cryptoType is UnityCN");
                }
            }
            else
            {
                UnityCN.SetKey(new UnityCN.Entry(UnityCNGameName, UnityCNKey));
                UnityCN = new UnityCN(reader);
            }
        }

        private void ReadBlocksInfoAndDirectory(FileReader reader)
        {
            byte[] blocksInfoBytes;
            if (m_Header.version >= 7)
            {
                reader.AlignStream(16);
            }
            
            if ((m_Header.flags & ArchiveFlags.BlocksInfoAtTheEnd) != 0) //kArchiveBlocksInfoAtTheEnd
            {
                Console.WriteLine($"BlocksInfoAtTheEnd");
                var position = reader.Position;
                reader.Position = reader.BaseStream.Length - m_Header.compressedBlocksInfoSize;
                blocksInfoBytes = reader.ReadBytes((int)m_Header.compressedBlocksInfoSize);
                reader.Position = position;
            }
            else //0x40 BlocksAndDirectoryInfoCombined
            {
                Console.WriteLine($"try to read BlocksAndDirectoryInfoCombined: {(int)m_Header.compressedBlocksInfoSize}");
                blocksInfoBytes = reader.ReadBytes((int)m_Header.compressedBlocksInfoSize);
                Console.WriteLine($"BlocksAndDirectoryInfoCombined");
            }
            MemoryStream blocksInfoUncompresseddStream;
            var blocksInfoBytesSpan = blocksInfoBytes.AsSpan(0, (int)m_Header.compressedBlocksInfoSize);
            var uncompressedSize = m_Header.uncompressedBlocksInfoSize;
            var compressionType = (CompressionType)(m_Header.flags & ArchiveFlags.CompressionTypeMask);
            Console.WriteLine($"BlockInfo compression type: {compressionType}");

            switch (compressionType) //kArchiveCompressionTypeMask
            {
                case CompressionType.Lz4: //LZ4
                case CompressionType.Lz4HC: //LZ4HC
                    {
                        var uncompressedBytes = ArrayPool<byte>.Shared.Rent((int)uncompressedSize);
                        Console.WriteLine($"Allocated {uncompressedSize} bytes for decompression");
                        try
                        {
                            var uncompressedBytesSpan = uncompressedBytes.AsSpan(0, (int)uncompressedSize);
                            var numWrite = LZ4.Decompress(blocksInfoBytesSpan, uncompressedBytesSpan);
                            Console.WriteLine($"Lz4 decompression write {numWrite} bytes");
                            if (numWrite != uncompressedSize)
                            {
                                throw new IOException($"Lz4 decompression error, write {numWrite} bytes but expected {uncompressedSize} bytes");
                            }
                            blocksInfoUncompresseddStream = new MemoryStream(uncompressedBytesSpan.ToArray());
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(uncompressedBytes, true);
                        }
                        break;
                    }
                default:
                    throw new IOException($"Unsupported compression type {compressionType}");
            }
            using (var blocksInfoReader = new EndianBinaryReader(blocksInfoUncompresseddStream))
            {
                if (HasUncompressedDataHash)
                {
                    UncompressedDataHash = blocksInfoReader.ReadBytes(16);
                }
                var blocksInfoCount = blocksInfoReader.ReadInt32();
                m_BlocksInfo = new List<StorageBlock>();
                Console.WriteLine($"Blocks count: {blocksInfoCount}");
                for (int i = 0; i < blocksInfoCount; i++)
                {
                    m_BlocksInfo.Add(new StorageBlock
                    {
                        uncompressedSize = blocksInfoReader.ReadUInt32(),
                        compressedSize = blocksInfoReader.ReadUInt32(),
                        flags = (StorageBlockFlags)blocksInfoReader.ReadUInt16()
                    });
                }

                var nodesCount = blocksInfoReader.ReadInt32();
                m_DirectoryInfo = new List<Node>();
                Console.WriteLine($"Directory count: {nodesCount}");
                for (int i = 0; i < nodesCount; i++)
                {
                    m_DirectoryInfo.Add(new Node
                    {
                        offset = blocksInfoReader.ReadInt64(),
                        size = blocksInfoReader.ReadInt64(),
                        flags = blocksInfoReader.ReadUInt32(),
                        path = blocksInfoReader.ReadStringToNull(),
                    });
                }
            }
            paddingPosition = reader.Position;
            Console.WriteLine($"reader position: {reader.Position}");
            if (HasBlockInfoNeedPaddingAtStart && (m_Header.flags & ArchiveFlags.BlockInfoNeedPaddingAtStart) != 0)
            {
                Console.WriteLine($"BlockInfoNeedPaddingAtStart");
                Console.WriteLine($"reader position: {reader.Position}");
                reader.AlignStream(16);
            }
            else
            {
                while (reader.ReadByte() == 0)
                {
                }
                reader.Position -= 1;
            }
        }

        private void ReadBlocks(FileReader reader, Stream blocksStream)
        {
            Console.WriteLine($"Writing block to blocks stream...");
            for (int i = 0; i < m_BlocksInfo.Count; i++)
            {
                var blockInfo = m_BlocksInfo[i];
                var compressionType = (CompressionType)(blockInfo.flags & StorageBlockFlags.CompressionTypeMask);
                switch (compressionType) //kStorageBlockCompressionTypeMask
                {
                    case CompressionType.Lz4: //LZ4
                    case CompressionType.Lz4HC: //LZ4HC
                        {
                            var compressedSize = (int)blockInfo.compressedSize;
                            var uncompressedSize = (int)blockInfo.uncompressedSize;
                            var compressedBytes = new byte[compressedSize];
                            var uncompressedBytes = ArrayPool<byte>.Shared.Rent(uncompressedSize);

                            try
                            {
                                var compressedBytesSpan = compressedBytes.AsSpan(0, compressedSize);
                                var uncompressedBytesSpan = uncompressedBytes.AsSpan(0, uncompressedSize);

                                reader.Read(compressedBytesSpan);
                                if (cryptoType == CryptoType.UnityCN && ((int)blockInfo.flags & 0x100) != 0)
                                {
                                    UnityCN.DecryptBlock(compressedBytes, compressedSize, i);
                                    blocksBytesList.Add(compressedBytes);
                                }else if (cryptoType == CryptoType.None)
                                {
                                    UnityCN.EncryptBlock(compressedBytes, compressedSize, i); 
                                    blocksBytesList.Add(compressedBytes);
                                }
                                blocksStream.Write(uncompressedBytesSpan);
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(uncompressedBytes, true);
                            }
                            break;
                        }
                    default:
                        throw new IOException($"Unsupported compression type {compressionType}");
                }
            }
            blocksStream.Position = 0;
        }

        public int[] ParseVersion()
        {
            var versionSplit = Regex.Replace(m_Header.unityRevision, @"\D", ".").Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            return versionSplit.Select(int.Parse).ToArray();
        }
    }
}
