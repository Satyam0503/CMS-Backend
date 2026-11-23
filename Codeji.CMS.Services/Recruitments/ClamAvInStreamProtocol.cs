using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>Socket-independent framing and verdict parsing for ClamAV's INSTREAM protocol.</summary>
public static class ClamAvInStreamProtocol
{
    private const int ChunkSize = 64 * 1024;
    private const int MaxResponseBytes = 4096;
    private const int MaxPingResponseBytes = 64;

    public static async Task PingAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        await stream.WriteAsync("zPING\0"u8.ToArray(), cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async Task<bool> ReadPingResponseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var response = new List<byte>(8);
        var buffer = new byte[1];
        while (response.Count < MaxPingResponseBytes)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0 || buffer[0] == 0) break;
            if (buffer[0] != (byte)'\r' && buffer[0] != (byte)'\n') response.Add(buffer[0]);
        }

        return string.Equals(Encoding.ASCII.GetString(response.ToArray()), "PONG", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<string> WriteAsync(Stream content, Stream destination, CancellationToken cancellationToken = default)
    {
        await destination.WriteAsync("zINSTREAM\0"u8.ToArray(), cancellationToken);
        var chunk = new byte[ChunkSize];
        var header = new byte[sizeof(int)];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        int bytesRead;
        while ((bytesRead = await content.ReadAsync(chunk.AsMemory(), cancellationToken)) > 0)
        {
            hash.AppendData(chunk, 0, bytesRead);
            BinaryPrimitives.WriteInt32BigEndian(header, bytesRead);
            await destination.WriteAsync(header, cancellationToken);
            await destination.WriteAsync(chunk.AsMemory(0, bytesRead), cancellationToken);
        }

        BinaryPrimitives.WriteInt32BigEndian(header, 0);
        await destination.WriteAsync(header, cancellationToken);
        await destination.FlushAsync(cancellationToken);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    public static async Task<string> ComputeSha256Async(Stream content, CancellationToken cancellationToken = default)
    {
        var chunk = new byte[ChunkSize];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        int bytesRead;
        while ((bytesRead = await content.ReadAsync(chunk.AsMemory(), cancellationToken)) > 0)
        {
            hash.AppendData(chunk, 0, bytesRead);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    public static async Task<ResumeScanStatus> ReadVerdictAsync(Stream source, CancellationToken cancellationToken = default)
    {
        var response = new List<byte>(64);
        var buffer = new byte[1];
        while (response.Count < MaxResponseBytes)
        {
            var read = await source.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0) break;
            if (buffer[0] == (byte)'\n') break;
            if (buffer[0] != (byte)'\r') response.Add(buffer[0]);
        }

        var line = Encoding.UTF8.GetString(response.ToArray());
        if (line.EndsWith(" OK", StringComparison.OrdinalIgnoreCase)) return ResumeScanStatus.Clean;
        if (line.EndsWith(" FOUND", StringComparison.OrdinalIgnoreCase)) return ResumeScanStatus.Infected;
        return ResumeScanStatus.Unavailable;
    }
}
