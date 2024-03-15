// This file is copied from: https://github.com/RazTools/Studio/tree/main/AssetStudio/Extensions/StreamExtensions.cs
using System.IO;

namespace AssetStudio
{
    public static class StreamExtensions
    {
        private const int BufferSize = 81920;

        public static void CopyTo(this Stream source, Stream destination, long size)
        {
            var buffer = new byte[BufferSize];
            for (var left = size; left > 0; left -= BufferSize)
            {
                int toRead = BufferSize < left ? BufferSize : (int)left;
                int read = source.Read(buffer, 0, toRead);
                destination.Write(buffer, 0, read);
                if (read != toRead)
                {
                    return;
                }
            }
        }

        public static void AlignStream(this Stream stream, int alignment)
        {
            var pos = stream.Position;
            Console.WriteLine($"Aligning stream from {pos} to {pos + (alignment - pos % alignment) % alignment}");
            var mod = pos % alignment;
            if (mod != 0)
            {
                var rem = alignment - mod;
                for (int _ = 0; _ < rem; _++)
                {
                    if (!stream.CanWrite)
                    {
                        throw new IOException("End of stream");
                    }

                    stream.WriteByte(0);
                }
            }
        }
        
        public static void WriteInt32BigEndian(this Stream stream, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            stream.Write(bytes, 0, bytes.Length);
        }
        
        public static void WriteInt64BigEndian(this Stream stream, long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            stream.Write(bytes, 0, bytes.Length);
        }
        
        public static void WriteUInt16BigEndian(this Stream stream, ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
