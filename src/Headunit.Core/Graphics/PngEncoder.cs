using System.Buffers.Binary;
using System.IO.Compression;

namespace Headunit.Core.Graphics;

/// <summary>
/// A minimal, dependency-free PNG writer (8-bit RGBA, no filtering). Used to
/// export the atlas for the macOS port and to render theme previews in CI —
/// never on the playback path.
/// </summary>
public static class PngEncoder
{
    private static readonly byte[] Signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static byte[] Encode(Surface surface)
    {
        using var output = new MemoryStream();
        output.Write(Signature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, surface.Width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], surface.Height);
        header[8] = 8;  // bit depth
        header[9] = 6;  // colour type: RGBA
        header[10] = 0; // compression
        header[11] = 0; // filter method
        header[12] = 0; // no interlace
        WriteChunk(output, "IHDR", header);

        WriteChunk(output, "IDAT", Compress(surface));
        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    public static void Write(Surface surface, string path) => File.WriteAllBytes(path, Encode(surface));

    private static byte[] Compress(Surface surface)
    {
        var raw = new byte[surface.Height * (1 + (surface.Width * 4))];
        var o = 0;
        for (var y = 0; y < surface.Height; y++)
        {
            raw[o++] = 0; // filter type: None
            for (var x = 0; x < surface.Width; x++)
            {
                var p = surface.Pixels[(y * surface.Width) + x];
                raw[o++] = (byte)(p >> 16);
                raw[o++] = (byte)(p >> 8);
                raw[o++] = (byte)p;
                raw[o++] = (byte)(p >> 24);
            }
        }

        using var compressed = new MemoryStream();
        using (var z = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            z.Write(raw);
        }

        return compressed.ToArray();
    }

    private static void WriteChunk(Stream output, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);

        Span<byte> typeBytes = stackalloc byte[4];
        for (var i = 0; i < 4; i++)
        {
            typeBytes[i] = (byte)type[i];
        }

        output.Write(typeBytes);
        output.Write(data);

        var crc = Crc32(Crc32(0xFFFFFFFFu, typeBytes), data) ^ 0xFFFFFFFFu;
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        output.Write(crcBytes);
    }

    internal static uint Crc32(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}
