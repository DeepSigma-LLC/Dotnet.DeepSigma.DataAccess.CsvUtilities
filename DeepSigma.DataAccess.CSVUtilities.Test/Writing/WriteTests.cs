using DeepSigma.DataAccess.CsvUtilities.Reading;
using DeepSigma.DataAccess.CsvUtilities.Test.Fixtures;
using DeepSigma.DataAccess.CsvUtilities.Writing;
using System.Runtime.CompilerServices;
using System.Text;

namespace DeepSigma.DataAccess.CsvUtilities.Test.Writing;

public sealed class WriteTests : IDisposable
{
    private readonly string _tempFile = Path.GetTempFileName();

    public void Dispose() => File.Delete(_tempFile);

    private static List<CustomerCsvRow> SampleRecords() =>
    [
        new() { Id = 1, FirstName = "Alice", LastName = "Smith", Email = "alice@example.com", Balance = 1234.56m, BirthDate = new DateOnly(1990, 5, 15) },
        new() { Id = 2, FirstName = "Bob", LastName = "Jones", Email = "bob@example.com", Balance = 0m }
    ];

    private static async IAsyncEnumerable<CustomerCsvRow> ToAsyncEnumerable(
        IEnumerable<CustomerCsvRow> records,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return record;
        }
    }

    // -------------------------------------------------------------------------
    // WriteToString
    // -------------------------------------------------------------------------

    [Fact]
    public void WriteToString_ProducesNonEmptyOutput()
    {
        var csv = CsvWriter.WriteToString(SampleRecords());

        Assert.False(string.IsNullOrWhiteSpace(csv));
        Assert.Contains("Alice", csv);
        Assert.Contains("Bob", csv);
    }

    [Fact]
    public void WriteToString_RoundTrip_ReturnsOriginalData()
    {
        var original = SampleRecords();
        var csv = CsvWriter.WriteToString(original);
        var roundTripped = CsvReader.ReadFromString<CustomerCsvRow>(csv);

        Assert.Equal(original.Count, roundTripped.Count);
        Assert.Equal(original[0].FirstName, roundTripped[0].FirstName);
        Assert.Equal(original[0].Balance, roundTripped[0].Balance);
    }

    [Fact]
    public async Task WriteToStringAsync_AsyncEnumerable_RoundTripsData()
    {
        var original = SampleRecords();
        var csv = await CsvWriter.WriteToStringAsync(
            ToAsyncEnumerable(original),
            cancellationToken: TestContext.Current.CancellationToken);
        var roundTripped = CsvReader.ReadFromString<CustomerCsvRow>(csv);

        Assert.Equal(original.Count, roundTripped.Count);
        Assert.Equal(original[0].FirstName, roundTripped[0].FirstName);
        Assert.Equal(original[1].Email, roundTripped[1].Email);
    }

    // -------------------------------------------------------------------------
    // WriteToFile
    // -------------------------------------------------------------------------

    [Fact]
    public void WriteToFile_CreatesFileWithContent()
    {
        CsvWriter.WriteToFile(_tempFile, SampleRecords());

        Assert.True(new FileInfo(_tempFile).Length > 0);
        var lines = File.ReadAllLines(_tempFile);
        Assert.True(lines.Length >= 3); // header + 2 data rows
    }

    [Fact]
    public void WriteToFile_RoundTrip_ReturnsOriginalData()
    {
        var original = SampleRecords();
        CsvWriter.WriteToFile(_tempFile, original);
        var roundTripped = CsvReader.ReadFromFile<CustomerCsvRow>(_tempFile);

        Assert.Equal(original.Count, roundTripped.Count);
        Assert.Equal(original[1].Email, roundTripped[1].Email);
    }

    [Fact]
    public async Task WriteToFileAsync_CreatesFileWithContent()
    {
        await CsvWriter.WriteToFileAsync(_tempFile, SampleRecords(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(new FileInfo(_tempFile).Length > 0);
    }

    [Fact]
    public async Task WriteToFileAsync_AsyncEnumerable_WritesAllRecords()
    {
        await CsvWriter.WriteToFileAsync(
            _tempFile,
            ToAsyncEnumerable(SampleRecords()),
            cancellationToken: TestContext.Current.CancellationToken);

        var records = CsvReader.ReadFromFile<CustomerCsvRow>(_tempFile);
        Assert.Equal(2, records.Count);
        Assert.Equal("Alice", records[0].FirstName);
        Assert.Equal("Bob", records[1].FirstName);
    }

    [Fact]
    public async Task WriteToFileAsync_CancellationToken_ThrowsWhenCancelled()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // CsvHelper wraps OperationCanceledException in WriterException — check either form.
        // Pre-cancelled token — intentionally not using TestContext token here.
#pragma warning disable xUnit1051
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await CsvWriter.WriteToFileAsync(_tempFile, SampleRecords(),
                cancellationToken: cts.Token));
#pragma warning restore xUnit1051

        Assert.True(
            ex is OperationCanceledException ||
            ex.InnerException is OperationCanceledException,
            $"Expected OperationCanceledException (possibly wrapped), but got: {ex.GetType().Name}");
    }

    // -------------------------------------------------------------------------
    // AppendToFile
    // -------------------------------------------------------------------------

    [Fact]
    public void AppendToFile_SecondCall_DoesNotDuplicateHeader()
    {
        var first = new List<CustomerCsvRow> { SampleRecords()[0] };
        var second = new List<CustomerCsvRow> { SampleRecords()[1] };

        CsvWriter.AppendToFile(_tempFile, first);
        CsvWriter.AppendToFile(_tempFile, second);

        var lines = File.ReadAllLines(_tempFile)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        // 1 header + 2 data rows = 3 lines
        Assert.Equal(3, lines.Count);
    }

    [Fact]
    public void AppendToFile_ThenRead_ContainsAllRecords()
    {
        CsvWriter.AppendToFile(_tempFile, SampleRecords().Take(1));
        CsvWriter.AppendToFile(_tempFile, SampleRecords().Skip(1));

        var records = CsvReader.ReadFromFile<CustomerCsvRow>(_tempFile);
        Assert.Equal(2, records.Count);
    }

    [Fact]
    public async Task AppendToFileAsync_SecondCall_DoesNotDuplicateHeader()
    {
        var first = new List<CustomerCsvRow> { SampleRecords()[0] };
        var second = new List<CustomerCsvRow> { SampleRecords()[1] };

        await CsvWriter.AppendToFileAsync(_tempFile, first,
            cancellationToken: TestContext.Current.CancellationToken);
        await CsvWriter.AppendToFileAsync(_tempFile, second,
            cancellationToken: TestContext.Current.CancellationToken);

        var lines = File.ReadAllLines(_tempFile)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        Assert.Equal(3, lines.Count);
    }

    [Fact]
    public async Task AppendToFileAsync_AsyncEnumerable_DoesNotDuplicateHeader()
    {
        var records = SampleRecords();
        await CsvWriter.AppendToFileAsync(
            _tempFile,
            ToAsyncEnumerable(records.Take(1)),
            cancellationToken: TestContext.Current.CancellationToken);
        await CsvWriter.AppendToFileAsync(
            _tempFile,
            ToAsyncEnumerable(records.Skip(1)),
            cancellationToken: TestContext.Current.CancellationToken);

        var imported = CsvReader.ReadFromFile<CustomerCsvRow>(_tempFile);
        Assert.Equal(2, imported.Count);
        Assert.Equal("Alice", imported[0].FirstName);
        Assert.Equal("Bob", imported[1].FirstName);
    }

    // -------------------------------------------------------------------------
    // WriteToStream
    // -------------------------------------------------------------------------

    [Fact]
    public void WriteToStream_LeaveOpenFalse_DisposesStream()
    {
        var stream = new MemoryStream();
        CsvWriter.WriteToStream(stream, SampleRecords(), leaveOpen: false);

        Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
    }

    [Fact]
    public void WriteToStream_LeaveOpenTrue_StreamRemainsOpen()
    {
        using var stream = new MemoryStream();
        CsvWriter.WriteToStream(stream, SampleRecords(), leaveOpen: true);

        Assert.True(stream.CanRead);
        stream.Position = 0;
        var text = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("Alice", text);
    }

    [Fact]
    public async Task WriteToStreamAsync_WritesContent()
    {
        using var stream = new MemoryStream();
        await CsvWriter.WriteToStreamAsync(stream, SampleRecords(), leaveOpen: true,
            cancellationToken: TestContext.Current.CancellationToken);

        stream.Position = 0;
        var text = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("Alice", text);
    }

    [Fact]
    public async Task WriteToStreamAsync_AsyncEnumerable_WritesAllRecordsAndLeavesStreamOpen()
    {
        using var stream = new MemoryStream();
        await CsvWriter.WriteToStreamAsync(
            stream,
            ToAsyncEnumerable(SampleRecords()),
            leaveOpen: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(stream.CanRead);
        stream.Position = 0;
        var imported = CsvReader.ReadFromStream<CustomerCsvRow>(stream, leaveOpen: true);
        Assert.Equal(2, imported.Count);
        Assert.Equal("Alice", imported[0].FirstName);
        Assert.Equal("Bob", imported[1].FirstName);
    }

    [Fact]
    public async Task WriteToWriterAsync_AsyncEnumerable_WritesContent()
    {
        using var writer = new StringWriter();
        await CsvWriter.WriteToWriterAsync(
            writer,
            ToAsyncEnumerable(SampleRecords()),
            cancellationToken: TestContext.Current.CancellationToken);

        var imported = CsvReader.ReadFromString<CustomerCsvRow>(writer.ToString());
        Assert.Equal(2, imported.Count);
        Assert.Equal("Alice", imported[0].FirstName);
        Assert.Equal("Bob", imported[1].FirstName);
    }
}
