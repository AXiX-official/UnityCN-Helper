// This file is based on: https://github.com/RazTools/Studio/tree/main/AssetStudio/Crypto/UnityCN.cs

using System.Text;
using System.Security.Cryptography;

namespace AssetStudio
{
    public class UnityCN
    {
        private const string Signature = "#$unity3dchina!@";

        private static ICryptoTransform Encryptor;

        public byte[] Index = new byte[0x10];
        public byte[] Sub = new byte[0x10];

        public UnityCN(EndianBinaryReader reader)
        {
            reader.ReadUInt32();

            var infoBytes = reader.ReadBytes(0x10);
            var infoKey = reader.ReadBytes(0x10);
            reader.Position += 1;

            var signatureBytes = reader.ReadBytes(0x10);
            var signatureKey = reader.ReadBytes(0x10);
            Console.WriteLine($"Signature is {Convert.ToHexString(signatureBytes)}");
            Console.WriteLine($"Signature Key is {Convert.ToHexString(signatureKey)}");
            reader.Position += 1;

            DecryptKey(signatureKey, signatureBytes);

            var str = Encoding.UTF8.GetString(signatureBytes);
            Console.WriteLine($"Decrypted signature is {str}");
            if (str != Signature)
            {
                throw new Exception($"Invalid Signature, Expected {Signature} but found {str} instead");
            }
            
            Console.WriteLine($"Info is {Convert.ToHexString(infoBytes)}");
            Console.WriteLine($"Info Key is {Convert.ToHexString(infoKey)}");
            DecryptKey(infoKey, infoBytes);

            infoBytes = infoBytes.ToUInt4Array();
            infoBytes.AsSpan(0, 0x10).CopyTo(Index);
            var subBytes = infoBytes.AsSpan(0x10, 0x10);
            for (var i = 0; i < subBytes.Length; i++)
            {
                var idx = (i % 4 * 4) + (i / 4);
                Sub[idx] = subBytes[i];
            }
            Console.WriteLine($"Index is {Convert.ToHexString(Index)}");
            Console.WriteLine($"Sub is {Convert.ToHexString(Sub)}");
        }

        public static bool SetKey(Entry entry)
        {
            Console.WriteLine($"Initializing decryptor with key {entry.Key}");
            try
            {
                using var aes = Aes.Create();
                aes.Mode = CipherMode.ECB;
                aes.Key = Convert.FromHexString(entry.Key);

                Encryptor = aes.CreateEncryptor();
                Console.WriteLine($"Decryptor initialized !!");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[UnityCN] Invalid key !!\n{e.Message}");
                return false;
            }
            return true;
        }

        public void DecryptBlock(Span<byte> bytes, int size, int index)
        {
            var offset = 0;
            while (offset < size)
            {
                offset += Decrypt(bytes.Slice(offset), index++, size - offset);
            }
        }
        
        public void EncryptBlock(Span<byte> bytes, int size, int index)
        {
            var offset = 0;
            while (offset < size)
            {
                offset += Encrypt(bytes.Slice(offset), index++, size - offset);
            }
        }

        private void DecryptKey(byte[] key, byte[] data)
        {
            if (Encryptor != null)
            {
                key = Encryptor.TransformFinalBlock(key, 0, key.Length);
                for (int i = 0; i < 0x10; i++)
                    data[i] ^= key[i];
            }
        }

        private int DecryptByte(Span<byte> bytes, ref int offset, ref int index)
        {
            var b = Sub[((index >> 2) & 3) + 4] + Sub[index & 3] + Sub[((index >> 4) & 3) + 8] + Sub[((byte)index >> 6) + 12];
            bytes[offset] = (byte)((Index[bytes[offset] & 0xF] - b) & 0xF | 0x10 * (Index[bytes[offset] >> 4] - b));
            b = bytes[offset];
            offset++;
            index++;
            return b;
        }
        
        private int EncryptByte(Span<byte> bytes, ref int offset, ref int index)
        {
            byte currentByte = bytes[offset];
            var low = currentByte & 0xF;
            var high = currentByte >> 4;
            
            var b = Sub[((index >> 2) & 3) + 4] + Sub[index & 3] + Sub[((index >> 4) & 3) + 8] + Sub[((byte)index >> 6) + 12];
            
            int i = 0;
            while (((Index[i] - b) & 0xF) != low && i < 0x10)
            {
                i++;
            }
            low = i;
            i = 0;
            while (((Index[i] - b) & 0xF) != high && i < 0x10)
            {
                i++;
            }
            high = i;

            bytes[offset] = (byte)(low | (high << 4));
            offset++;
            index++;
            return currentByte;
        }

        private int Decrypt(Span<byte> bytes, int index, int remaining)
        {
            var offset = 0;

            var curByte = DecryptByte(bytes, ref offset, ref index);
            var byteHigh = curByte >> 4;
            var byteLow = curByte & 0xF;

            if (byteHigh == 0xF)
            {
                int b;
                do
                {
                    b = DecryptByte(bytes, ref offset, ref index);
                    byteHigh += b;
                } while (b == 0xFF);
            }

            offset += byteHigh;

            if (offset < remaining)
            {
                DecryptByte(bytes, ref offset, ref index);
                DecryptByte(bytes, ref offset, ref index);
                if (byteLow == 0xF)
                {
                    int b;
                    do
                    {
                        b = DecryptByte(bytes, ref offset, ref index);
                    } while (b == 0xFF);
                }
            }

            return offset;
        }
        
        private int Encrypt(Span<byte> bytes, int index, int remaining)
        {
            var offset = 0;
            
            var curByte = EncryptByte(bytes, ref offset, ref index);
            var byteHigh = curByte >> 4;
            var byteLow = curByte & 0xF;

            if (byteHigh == 0xF)
            {
                int b;
                do
                {
                    b = EncryptByte(bytes, ref offset, ref index);
                    byteHigh += b;
                } while (b == 0xFF);
            }

            offset += byteHigh;

            if (offset < remaining)
            {
                EncryptByte(bytes, ref offset, ref index);
                EncryptByte(bytes, ref offset, ref index);
                if (byteLow == 0xF)
                {
                    int b;
                    do
                    {
                        b = EncryptByte(bytes, ref offset, ref index);
                    } while (b == 0xFF);
                }
            }

            return offset;
        }

        public class Entry
        {
            public string Name { get; private set; }
            public string Key { get; private set; }

            public Entry(string name, string key)
            {
                Name = name;
                Key = key;
            }

            public bool Validate()
            {
                var bytes = Convert.FromHexString(Key);
                if (bytes.Length != 0x10)
                {
                    Console.WriteLine($"[UnityCN] {this} has invalid key, size should be 16 bytes, skipping...");
                    return false;
                }

                return true;
            }

            public override string ToString() => $"{Name} ({Key})";
        }
    }
}