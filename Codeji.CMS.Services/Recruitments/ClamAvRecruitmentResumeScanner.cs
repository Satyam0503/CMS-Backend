using System.Net.Sockets;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>Streams uploaded documents to a private ClamAV daemon using its INSTREAM protocol.</summary>
public sealed class ClamAvRecruitmentResumeScanner(string? host, int port = 3310, TimeSpan? timeout = null, bool strictMode = true) : IRecruitmentResumeScanner
{
    private readonly TimeSpan _timeout = timeout ?? TimeSpan.FromSeconds(15);
    private static readonly TimeSpan HealthProbeTimeout = TimeSpan.FromSeconds(2);
    private readonly string? _host = host?.Trim();
    private readonly int _port = port;
    private readonly bool _strictMode = strictMode;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_host) && _port is >= 1 and <= 65535;
    public bool StrictMode => _strictMode;

    public static ClamAvRecruitmentResumeScanner FromEnvironment()
    {
        var host = Environment.GetEnvironmentVariable("ATS_CLAMAV_HOST");
        var port = int.TryParse(Environment.GetEnvironmentVariable("ATS_CLAMAV_PORT"), out var configuredPort) ? configuredPort : 3310;
        var strictVar = Environment.GetEnvironmentVariable("ATS_CLAMAV_STRICT_MODE");
        var isDev = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

        bool strictMode = !string.IsNullOrWhiteSpace(strictVar)
            ? string.Equals(strictVar, "true", StringComparison.OrdinalIgnoreCase)
            : !isDev;

        return new(host, port, strictMode: strictMode);
    }

    public async Task<bool?> ProbeAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return false;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(HealthProbeTimeout);
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host!, _port, timeoutCts.Token);
            await using var network = client.GetStream();
            await ClamAvInStreamProtocol.PingAsync(network, timeoutCts.Token);
            return await ClamAvInStreamProtocol.ReadPingResponseAsync(network, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception) when (exception is IOException or SocketException or ArgumentException)
        {
            return false;
        }
    }

    public async Task<ResumeScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
            return new ResumeScanResult(ResumeScanStatus.Unavailable);

        if (!IsConfigured)
        {
            if (!_strictMode)
            {
                var sha256 = await ClamAvInStreamProtocol.ComputeSha256Async(content, cancellationToken);
                return new ResumeScanResult(ResumeScanStatus.Clean, sha256);
            }
            return new ResumeScanResult(ResumeScanStatus.Unavailable);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host!, _port, timeoutCts.Token);
            await using var network = client.GetStream();
            string sha256 = await ClamAvInStreamProtocol.WriteAsync(content, network, timeoutCts.Token);
            var verdict = await ClamAvInStreamProtocol.ReadVerdictAsync(network, timeoutCts.Token);
            return new ResumeScanResult(verdict, sha256);
        }
        // A caller cancellation is control flow and must remain observable to the request
        // pipeline. Only this scanner's own timeout should be translated into an unavailable
        // verdict so uploads can fail closed with the normal retryable response.
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (!_strictMode)
            {
                if (content.CanSeek) content.Position = 0;
                var sha256 = await ClamAvInStreamProtocol.ComputeSha256Async(content, cancellationToken);
                return new ResumeScanResult(ResumeScanStatus.Clean, sha256);
            }
            return new ResumeScanResult(ResumeScanStatus.Unavailable);
        }
        catch (Exception exception) when (exception is IOException or SocketException or ArgumentException)
        {
            if (!_strictMode)
            {
                if (content.CanSeek) content.Position = 0;
                var sha256 = await ClamAvInStreamProtocol.ComputeSha256Async(content, cancellationToken);
                return new ResumeScanResult(ResumeScanStatus.Clean, sha256);
            }
            return new ResumeScanResult(ResumeScanStatus.Unavailable);
        }
    }

}
