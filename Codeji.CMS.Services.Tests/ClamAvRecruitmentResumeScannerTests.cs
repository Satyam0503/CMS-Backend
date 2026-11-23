using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Codeji.CMS.Services.Recruitments;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class ClamAvRecruitmentResumeScannerTests
{
    [Fact]
    public async Task Ping_probe_uses_nul_terminated_command_framing()
    {
        await using var request = new MemoryStream();

        await ClamAvInStreamProtocol.PingAsync(request);

        Assert.Equal("zPING\0", Encoding.ASCII.GetString(request.ToArray()));
    }

    [Theory]
    [InlineData("PONG\0", true)]
    [InlineData("PONG\r\n", true)]
    [InlineData("BUSY\0", false)]
    [InlineData("", false)]
    public async Task Ping_probe_accepts_only_a_pong_response(string response, bool expected)
    {
        await using var reply = new MemoryStream(Encoding.ASCII.GetBytes(response));

        Assert.Equal(expected, await ClamAvInStreamProtocol.ReadPingResponseAsync(reply));
    }

    [Fact]
    public async Task InStream_protocol_writes_command_chunk_lengths_payload_and_terminator()
    {
        var payload = Enumerable.Range(0, 70_000).Select(x => (byte)(x % 251)).ToArray();
        await using var source = new MemoryStream(payload);
        await using var request = new MemoryStream();

        string sha256 = await ClamAvInStreamProtocol.WriteAsync(source, request);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(payload)), sha256);

        var bytes = request.ToArray();
        Assert.Equal("zINSTREAM\0", Encoding.ASCII.GetString(bytes, 0, 10));
        var offset = 10;
        var firstChunkLength = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
        Assert.Equal(64 * 1024, firstChunkLength);
        offset += 4;
        Assert.Equal(payload.AsSpan(0, firstChunkLength).ToArray(), bytes.AsSpan(offset, firstChunkLength).ToArray());
        offset += firstChunkLength;
        var secondChunkLength = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
        Assert.Equal(payload.Length - firstChunkLength, secondChunkLength);
        offset += 4;
        Assert.Equal(payload.AsSpan(firstChunkLength).ToArray(), bytes.AsSpan(offset, secondChunkLength).ToArray());
        offset += secondChunkLength;
        Assert.Equal(0, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4)));
        Assert.Equal(bytes.Length, offset + 4);
    }

    [Theory]
    [InlineData("stream: OK\r\n", ResumeScanStatus.Clean)]
    [InlineData("stream: Eicar-Test-Signature FOUND\n", ResumeScanStatus.Infected)]
    [InlineData("unexpected response\n", ResumeScanStatus.Unavailable)]
    public async Task Verdict_parser_maps_clean_infected_and_invalid_responses(string response, ResumeScanStatus expected)
    {
        await using var source = new MemoryStream(Encoding.UTF8.GetBytes(response));
        var result = await ClamAvInStreamProtocol.ReadVerdictAsync(source);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Missing_scanner_configuration_fails_closed()
    {
        var scanner = new ClamAvRecruitmentResumeScanner(null);
        var result = await scanner.ScanAsync(new MemoryStream(Encoding.ASCII.GetBytes("%PDF")));
        Assert.Equal(ResumeScanStatus.Unavailable, result.Status);
        Assert.False(await scanner.ProbeAsync());
    }

    [Fact]
    public async Task Missing_scanner_configuration_in_relaxed_mode_computes_sha256_and_passes_clean()
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.7 Test Document Content");
        var expectedSha256 = Convert.ToHexString(SHA256.HashData(bytes));
        var scanner = new ClamAvRecruitmentResumeScanner(null, strictMode: false);

        var result = await scanner.ScanAsync(new MemoryStream(bytes));

        Assert.Equal(ResumeScanStatus.Clean, result.Status);
        Assert.Equal(expectedSha256, result.Sha256);
    }

    [Fact]
    public async Task Unreachable_daemon_in_relaxed_mode_falls_back_to_clean_with_sha256()
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.7 Offline Daemon Document");
        var expectedSha256 = Convert.ToHexString(SHA256.HashData(bytes));
        // Port 59999 is unbound on localhost
        var scanner = new ClamAvRecruitmentResumeScanner("127.0.0.1", 59999, timeout: TimeSpan.FromMilliseconds(200), strictMode: false);

        var result = await scanner.ScanAsync(new MemoryStream(bytes));

        Assert.Equal(ResumeScanStatus.Clean, result.Status);
        Assert.Equal(expectedSha256, result.Sha256);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_misreported_as_scanner_outage()
    {
        var scanner = new ClamAvRecruitmentResumeScanner("127.0.0.1", 3310);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scanner.ScanAsync(new MemoryStream(Encoding.ASCII.GetBytes("%PDF")), cancellation.Token));
    }

    [Theory]
    [InlineData(ResumeScanStatus.Clean, "ResumeScanPassed")]
    [InlineData(ResumeScanStatus.Infected, "ResumeScanRejected")]
    [InlineData(ResumeScanStatus.Unavailable, "ResumeScanUnavailable")]
    public void Scan_audit_records_only_candidate_and_attempt_references(ResumeScanStatus status, string expectedEventType)
    {
        var log = RecruitmentResumeScanAudit.Create("company-1", "applicant-1", "job-1", "actor-1", "attempt-1", status);

        Assert.Equal("company-1", log.CompanyId);
        Assert.Equal("applicant-1", log.ApplicantId);
        Assert.Equal(expectedEventType, log.EventType);
        Assert.Equal("resume-scan:attempt-1", log.IdempotencyKey);
        Assert.Empty(log.ApplicantName);
        Assert.DoesNotContain("@", log.Description);
        Assert.DoesNotContain(".pdf", log.Description);
    }
}
