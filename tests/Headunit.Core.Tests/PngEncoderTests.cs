using System.Buffers.Binary;
using System.IO.Compression;
using Headunit.Core.Graphics;
using Xunit;

namespace Headunit.Core.Tests;

public class PngEncoderTests
{
    [Fact]
    public void Writes_a_well_formed_png()
    {
        var surface = new Surface(3, 2);
        surface.Pixels[0] = 0x80FF0000; // half-transparent red
        var png = PngEncoder.Encode(surface);

        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, png[..8]);

        var chunks = ReadChunks(png);
        Assert.Equal(["IHDR", "IDAT", "IEND"], chunks.Select(c => c.Type));

        var ihdr = chunks[0].Data;
        Assert.Equal(3, BinaryPrimitives.ReadInt32BigEndian(ihdr));
        Assert.Equal(2, BinaryPrimitives.ReadInt32BigEndian(ihdr.AsSpan(4)));
        Assert.Equal(8, ihdr[8]);
        Assert.Equal(6, ihdr[9]);

        using var z = new ZLibStream(new MemoryStream(chunks[1].Data), CompressionMode.Decompress);
        using var raw = new MemoryStream();
        z.CopyTo(raw);
        var bytes = raw.ToArray();

        Assert.Equal(2 * (1 + (3 * 4)), bytes.Length);
        Assert.Equal(new byte[] { 0, 0xFF, 0, 0, 0x80 }, bytes[..5]); // filter byte, then RGBA
    }

    [Fact]
    public void Chunk_crc_matches_the_reference_value()
    {
        // Every PNG ends with this exact IEND chunk.
        var crc = PngEncoder.Crc32(0xFFFFFFFFu, "IEND"u8) ^ 0xFFFFFFFFu;
        Assert.Equal(0xAE426082u, crc);
    }

    private static List<(string Type, byte[] Data)> ReadChunks(byte[] png)
    {
        var chunks = new List<(string, byte[])>();
        var at = 8;
        while (at < png.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(at));
            var type = System.Text.Encoding.ASCII.GetString(png, at + 4, 4);
            var data = png.AsSpan(at + 8, length).ToArray();

            var expected = PngEncoder.Crc32(PngEncoder.Crc32(0xFFFFFFFFu, png.AsSpan(at + 4, 4)), data) ^ 0xFFFFFFFFu;
            Assert.Equal(expected, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at + 8 + length)));

            chunks.Add((type, data));
            at += 12 + length;
        }

        return chunks;
    }
}
