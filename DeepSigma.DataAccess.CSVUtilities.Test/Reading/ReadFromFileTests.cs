using DeepSigma.DataAccess.CsvUtilities.Configuration;
using DeepSigma.DataAccess.CsvUtilities.Reading;
using DeepSigma.DataAccess.CsvUtilities.Test.Fixtures;

namespace DeepSigma.DataAccess.CsvUtilities.Test.Reading;

public sealed class ReadFromFileTests
{
    private static string DataPath(string fileName) =>
        Path.Combine("TestData", fileName);

    [Fact]
    public void ReadFromFile_WithHeaderFile_ReturnsMappedRecords()
    {
        var records = CsvReader.ReadFromFile<CustomerCsvRow>(
            DataPath("customers_with_header.csv"));

        Assert.Equal(3, records.Count);
        Assert.Equal(1, records[0].Id);
        Assert.Equal("Alice", records[0].FirstName);
        Assert.Equal(new DateOnly(1990, 5, 15), records[0].BirthDate);
        Assert.Null(records[2].BirthDate);
    }

    [Fact]
    public void ReadFromFile_UsesGeneratedMapping()
    {
        var records = CsvReader.ReadFromFile<CustomerCsvRow>(
            DataPath("customers_with_header.csv"));

        Assert.Equal(3, records.Count);
    }

    [Fact]
    public void ReadFromFile_MissingFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            CsvReader.ReadFromFile<CustomerCsvRow>("TestData/does_not_exist.csv"));
    }

    [Fact]
    public void ReadFromFile_WithSemicolonDelimiter_ParsesCorrectly()
    {
        var config = CsvDefaults.CreateDefault(delimiter: ";");
        var records = CsvReader.ReadFromFile<CustomerCsvRow>(
            DataPath("customers_semicolon.csv"), config);

        Assert.Equal(2, records.Count);
        Assert.Equal("Alice", records[0].FirstName);
    }

    [Fact]
    public async Task ReadFromFileAsync_WithHeaderFile_ReturnsMappedRecords()
    {
        var records = await CsvReader.ReadFromFileAsync<CustomerCsvRow>(
            DataPath("customers_with_header.csv"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, records.Count);
        Assert.Equal("Bob", records[1].FirstName);
    }

    [Fact]
    public async Task ReadFromFileAsync_CancellationToken_ThrowsWhenCancelled()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Pre-cancelled token — intentionally not using TestContext token here.
#pragma warning disable xUnit1051
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await CsvReader.ReadFromFileAsync<CustomerCsvRow>(
                DataPath("customers_with_header.csv"),
                cancellationToken: cts.Token));
#pragma warning restore xUnit1051
    }

    [Fact]
    public async Task StreamFromFile_YieldsAllRecords()
    {
        var records = new List<CustomerCsvRow>();
        await foreach (var record in CsvReader.StreamFromFile<CustomerCsvRow>(
            DataPath("customers_with_header.csv"),
            cancellationToken: TestContext.Current.CancellationToken))
        {
            records.Add(record);
        }

        Assert.Equal(3, records.Count);
    }

    [Fact]
    public void ReadFromFileSafe_WithBadRows_CollectsErrorsAndContinues()
    {
        var result = CsvReader.ReadFromFileSafe<CustomerCsvRow>(
            DataPath("customers_with_bad_rows.csv"));

        Assert.Equal(2, result.Records.Count);
        Assert.Single(result.Errors);
        Assert.NotNull(result.Errors[0].RawRecord);
    }

    [Fact]
    public void ReadFromFile_WithNoHeaderFile_UsesIndexAttributes()
    {
        var config = CsvDefaults.CreateDefault(hasHeader: false);
        var records = CsvReader.ReadFromFile<CustomerCsvNoHeaderRow>(
            DataPath("customers_no_header.csv"), config);

        Assert.Equal(3, records.Count);
        Assert.Equal(1, records[0].Id);
        Assert.Equal("Alice", records[0].FirstName);
    }
}
