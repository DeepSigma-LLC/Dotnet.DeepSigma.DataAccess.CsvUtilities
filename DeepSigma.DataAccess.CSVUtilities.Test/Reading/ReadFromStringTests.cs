using DeepSigma.DataAccess.CsvUtilities.Configuration;
using DeepSigma.DataAccess.CsvUtilities.Reading;
using DeepSigma.DataAccess.CsvUtilities.Test.Fixtures;

namespace DeepSigma.DataAccess.CsvUtilities.Test.Reading;

public sealed class ReadFromStringTests
{
    private const string ValidCsv =
        "customer_id,first_name,last_name,email,balance,birth_date\n" +
        "1,Alice,Smith,alice@example.com,1234.56,1990-05-15\n" +
        "2,Bob,Jones,bob@example.com,0.00,1985-11-30\n";

    [Fact]
    public void ReadFromString_WithValidCsv_ReturnsMappedRecords()
    {
        var records = CsvReader.ReadFromString<CustomerCsvRow>(ValidCsv);

        Assert.Equal(2, records.Count);
        Assert.Equal(1, records[0].Id);
        Assert.Equal("Alice", records[0].FirstName);
        Assert.Equal("Smith", records[0].LastName);
        Assert.Equal("alice@example.com", records[0].Email);
        Assert.Equal(1234.56m, records[0].Balance);
        Assert.Equal(new DateOnly(1990, 5, 15), records[0].BirthDate);
    }

    [Fact]
    public void ReadFromString_WithEmptyString_ReturnsEmptyList()
    {
        var records = CsvReader.ReadFromString<CustomerCsvRow>(string.Empty);

        Assert.Empty(records);
    }

    [Fact]
    public void ReadFromString_WithWhitespaceOnlyRows_SkipsBlankLines()
    {
        const string csv =
            "customer_id,first_name,last_name,email,balance\n" +
            "1,Alice,Smith,alice@example.com,10.00\n" +
            "   ,   ,   ,   ,   \n" +
            "2,Bob,Jones,bob@example.com,20.00\n";

        var records = CsvReader.ReadFromString<CustomerCsvRow>(csv);

        Assert.Equal(2, records.Count);
    }

    [Fact]
    public void ReadFromString_WithCustomDelimiter_ParsesCorrectly()
    {
        const string csv =
            "customer_id;first_name;last_name;email;balance\n" +
            "1;Alice;Smith;alice@example.com;99.99\n";

        var config = CsvDefaults.CreateDefault(delimiter: ";");
        var records = CsvReader.ReadFromString<CustomerCsvRow>(csv, config);

        Assert.Single(records);
        Assert.Equal(1, records[0].Id);
        Assert.Equal(99.99m, records[0].Balance);
    }

    [Fact]
    public void ReadFromString_HeaderMatchingIsCaseInsensitiveAndIgnoresSeparators()
    {
        // Header normalization is applied before the generated mapper resolves columns.
        const string csv =
            "Customer ID,First Name,Last Name,Email Address,Balance\n" +
            "1,Alice,Smith,alice@example.com,50.00\n";

        // CustomerCsvRow property names: Id, FirstName, LastName, Email, Balance
        // Header normalization strips spaces: "customerid","firstname","lastname","emailaddress","balance"
        // Attribute aliases normalize to the matching names.
        // "customerid" won't match "id" — so Id stays default 0, but the rest should map.
        var records = CsvReader.ReadFromString<CustomerCsvRow>(csv);

        Assert.Single(records);
        Assert.Equal("Alice", records[0].FirstName);
        Assert.Equal("Smith", records[0].LastName);
        Assert.Equal(50.00m, records[0].Balance);
    }

    [Fact]
    public void ReadFromStringSafe_WithBadRow_CollectsErrorAndContinues()
    {
        const string csv =
            "customer_id,first_name,last_name,email,balance\n" +
            "1,Alice,Smith,alice@example.com,1234.56\n" +
            "2,Bob,Jones,bob@example.com,NOT_A_NUMBER\n" +
            "3,Carol,White,carol@example.com,9.99\n";

        var result = CsvReader.ReadFromStringSafe<CustomerCsvRow>(csv);

        Assert.Equal(2, result.Records.Count);
        Assert.Single(result.Errors);
        Assert.False(result.IsSuccess);
        Assert.Equal(3, result.Errors[0].Row); // row 2 data = parser row 3 (1 header + 1 good + 1 bad)
    }

    [Fact]
    public void ReadFromStringSafe_WithAllValidRows_IsSuccess()
    {
        var result = CsvReader.ReadFromStringSafe<CustomerCsvRow>(ValidCsv);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Records.Count);
        Assert.Empty(result.Errors);
    }
}
