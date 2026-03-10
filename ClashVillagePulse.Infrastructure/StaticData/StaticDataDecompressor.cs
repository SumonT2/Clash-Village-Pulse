using System.IO;
using SharpCompress.Compressors.LZMA;
using ZstdNet;

namespace ClashVillagePulse.Infrastructure.StaticData;

public class StaticDataDecompressor
{
    private const uint ZstdMagic = 0xFD2FB528;

    public byte[] DecompressIfNeeded(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new InvalidOperationException("Static data payload is empty.");

        // 1) Already plain CSV/text
        if (LooksLikePlainCsv(data))
            return data;

        // 2) Signed payload: strip header then continue
        if (HasSigHeader(data))
        {
            if (data.Length <= 68)
                throw new InvalidOperationException("Sig payload is too short.");

            var stripped = data[68..];

            if (LooksLikePlainCsv(stripped))
                return stripped;

            return DecompressCompressedPayload(stripped);
        }

        // 3) SCLZ/LZHAM
        if (HasSclzHeader(data))
        {
            throw new NotSupportedException(
                "SCLZ/LZHAM payload detected. This requires a dedicated LZHAM implementation which is not wired yet.");
        }

        // 4) ZSTD / LZMA / others
        return DecompressCompressedPayload(data);
    }

    private byte[] DecompressCompressedPayload(byte[] data)
    {
        if (LooksLikePlainCsv(data))
            return data;

        if (IsZstd(data))
            return DecompressZstd(data);

        // Fallback to LZMA, matching the Python extractor behavior
        return DecompressLzmaFallback(data);
    }

    private static bool HasSigHeader(byte[] data)
    {
        return data.Length >= 4 &&
               data[0] == (byte)'S' &&
               data[1] == (byte)'i' &&
               data[2] == (byte)'g' &&
               data[3] == (byte)':';
    }

    private static bool HasSclzHeader(byte[] data)
    {
        return data.Length >= 4 &&
               data[0] == (byte)'S' &&
               data[1] == (byte)'C' &&
               data[2] == (byte)'L' &&
               data[3] == (byte)'Z';
    }

    private static bool IsZstd(byte[] data)
    {
        if (data.Length < 4)
            return false;

        uint magic = BitConverter.ToUInt32(data, 0);
        return magic == ZstdMagic;
    }

    private static bool LooksLikePlainCsv(byte[] data)
    {
        if (data.Length < 6)
            return false;

        // Common CSV starts seen in Clash files:
        // Name,...
        // "Name",...
        // name,...
        // "name",...
        var prefix = System.Text.Encoding.UTF8.GetString(data, 0, Math.Min(32, data.Length));

        if (prefix.StartsWith("Name", StringComparison.OrdinalIgnoreCase) ||
            prefix.StartsWith("\"Name\"", StringComparison.OrdinalIgnoreCase) ||
            prefix.StartsWith("name", StringComparison.OrdinalIgnoreCase) ||
            prefix.StartsWith("\"name\"", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Also accept printable comma-separated first line
        int newlineIndex = Array.IndexOf(data, (byte)'\n');
        if (newlineIndex > 0)
        {
            var firstLine = System.Text.Encoding.UTF8.GetString(data, 0, Math.Min(newlineIndex, 128));
            if (firstLine.Contains(',') && firstLine.All(ch => ch == '\r' || ch == '\n' || ch == '\t' || !char.IsControl(ch)))
                return true;
        }

        return false;
    }

    private static byte[] DecompressZstd(byte[] data)
    {
        using var decompressor = new Decompressor();
        return decompressor.Unwrap(data);
    }

    private static byte[] DecompressLzmaFallback(byte[] data)
    {
        if (data.Length < 9)
            throw new InvalidOperationException("Payload too short for LZMA fallback.");

        // Matches the Python logic:
        // data = data[0:9] + (b"\x00" * 4) + data[9:]
        var patched = new byte[data.Length + 4];
        Buffer.BlockCopy(data, 0, patched, 0, 9);
        Buffer.BlockCopy(data, 9, patched, 13, data.Length - 9);

        using var input = new MemoryStream(patched);

        // First 5 bytes are LZMA decoder properties
        byte[] properties = new byte[5];
        int propsRead = input.Read(properties, 0, 5);
        if (propsRead != 5)
            throw new InvalidOperationException("Invalid LZMA payload: missing decoder properties.");

        // Next 8 bytes are uncompressed size
        byte[] sizeBytes = new byte[8];
        int sizeRead = input.Read(sizeBytes, 0, 8);
        if (sizeRead != 8)
            throw new InvalidOperationException("Invalid LZMA payload: missing uncompressed size.");

        long uncompressedSize = BitConverter.ToInt64(sizeBytes, 0);

        var decoder = new Decoder();
        decoder.SetDecoderProperties(properties);

        using var output = new MemoryStream();

        decoder.Code(
            input,
            output,
            input.Length - input.Position,
            uncompressedSize,
            null);

        var result = output.ToArray();

        if (result.Length == 0)
            throw new InvalidOperationException("LZMA decompression produced no output.");

        return result;
    }
}