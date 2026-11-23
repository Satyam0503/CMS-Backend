using System.Security.Cryptography;
using System.Buffers.Binary;

namespace Codeji.CMS.Services.Recruitments;

public static class RecruitmentOfferSignatureRules
{
    private const string PngDataUrlPrefix = "data:image/png;base64,";
    private const int MaximumSignatureBytes = 256 * 1024;
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static bool IsValidPngDataUrl(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith(PngDataUrlPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var encoded = dataUrl[PngDataUrlPrefix.Length..];
        if (encoded.Length == 0 || encoded.Length > (MaximumSignatureBytes * 4 / 3) + 8)
            return false;

        try
        {
            var bytes = Convert.FromBase64String(encoded);
            return IsStructurallyValidPng(bytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsStructurallyValidPng(byte[] bytes)
    {
        if (bytes.Length is < 57 or > MaximumSignatureBytes ||
            !CryptographicOperations.FixedTimeEquals(bytes.AsSpan(0, 8), PngSignature))
            return false;

        var offset = 8;
        var hasHeader = false;
        var hasImageData = false;
        var hasEnd = false;
        while (offset <= bytes.Length - 12)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            if (length > int.MaxValue || length > bytes.Length - offset - 12)
                return false;
            var dataLength = (int)length;
            var type = bytes.AsSpan(offset + 4, 4);
            var data = bytes.AsSpan(offset + 8, dataLength);
            var storedCrc = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset + 8 + dataLength, 4));
            if (ComputeCrc32(bytes.AsSpan(offset + 4, 4 + dataLength)) != storedCrc)
                return false;

            if (!hasHeader)
            {
                if (!type.SequenceEqual("IHDR"u8) || dataLength != 13 ||
                    BinaryPrimitives.ReadUInt32BigEndian(data[..4]) is 0 or > 4096 ||
                    BinaryPrimitives.ReadUInt32BigEndian(data.Slice(4, 4)) is 0 or > 4096 ||
                    (ulong)BinaryPrimitives.ReadUInt32BigEndian(data[..4]) * BinaryPrimitives.ReadUInt32BigEndian(data.Slice(4, 4)) > 4_000_000 ||
                    !IsValidBitDepth(data[8], data[9]) ||
                    data[10] != 0 || data[11] != 0 || data[12] > 1)
                    return false;
                hasHeader = true;
            }
            else if (type.SequenceEqual("IDAT"u8))
            {
                hasImageData = true;
            }
            else if (type.SequenceEqual("IEND"u8))
            {
                if (dataLength != 0 || !hasImageData || offset + 12 != bytes.Length)
                    return false;
                hasEnd = true;
                break;
            }

            offset += 12 + dataLength;
        }

        return hasHeader && hasImageData && hasEnd;
    }

    private static bool IsValidBitDepth(byte bitDepth, byte colorType) => colorType switch
    {
        0 => bitDepth is 1 or 2 or 4 or 8 or 16,
        2 => bitDepth is 8 or 16,
        3 => bitDepth is 1 or 2 or 4 or 8,
        4 => bitDepth is 8 or 16,
        6 => bitDepth is 8 or 16,
        _ => false
    };

    private static uint ComputeCrc32(ReadOnlySpan<byte> bytes)
    {
        uint crc = 0xffffffff;
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320 : crc >> 1;
        }
        return ~crc;
    }
}
