using DeepSigma.DataAccess.CsvUtilities.Configuration;
using DeepSigma.DataAccess.CsvUtilities.Reading;
using DeepSigma.DataAccess.CsvUtilities.Test.Fixtures;
using System.Globalization;

namespace DeepSigma.DataAccess.CsvUtilities.Test.Reading;

public sealed class CsvDefaultsTests
{
    [Fact]
    public void CreateDefault_DefaultsToInvariantCulture()
    {
        var config = CsvDefaults.CreateDefault();

        Assert.Equal(CultureInfo.InvariantCulture, config.FormatProvider);
    }

    [Fact]
    public void CreateDefault_CustomCulture_UsesThatCulture()
    {
        var fr = new CultureInfo("fr-FR");
        var config = CsvDefaults.CreateDefault(culture: fr);

        Assert.Equal(fr, config.FormatProvider);
    }

    [Fact]
    public void CreateDefault_HasHeaderTrue_ByDefault()
    {
        var config = CsvDefaults.CreateDefault();

        Assert.True(config.HasHeader);
    }

    [Fact]
    public void CreateDefault_HasHeaderFalse_Propagates()
    {
        var config = CsvDefaults.CreateDefault(hasHeader: false);

        Assert.False(config.HasHeader);
    }

    [Fact]
    public void CreateDefault_CustomDelimiter_Propagates()
    {
        var config = CsvDefaults.CreateDefault(delimiter: ";");

        Assert.Equal(';', config.Separator);
    }

    /// <summary>
    /// Verifies that NormalizeHeaderName handles spaces, underscores, dashes, and mixed casing.
    /// Tested through real input rather than constructing internal callback arguments.
    /// </summary>
    [Theory]
    [InlineData("customer_id,first_name,last_name,email,balance")]   // snake_case
    [InlineData("Customer ID,First Name,Last Name,Email,Balance")]   // spaces + Title Case
    [InlineData("CUSTOMER-ID,FIRST-NAME,LAST-NAME,EMAIL,BALANCE")]   // dashes + upper
    public void NormalizeHeaderName_VariousHeaderFormats_MapsFirstNameCorrectly(string headerRow)
    {
        var csv = $"{headerRow}\n1,Alice,Smith,alice@example.com,10.00\n";

        // "balance" normalises to "balance"; "first_name"/"First Name"/"FIRST-NAME" all → "firstname"
        // CustomerCsvRow.FirstName property name lowercased → "firstname" — should match.
        var records = CsvReader.ReadFromString<CustomerCsvRow>(csv);

        Assert.Single(records);
        Assert.Equal("Alice", records[0].FirstName);
        Assert.Equal("Smith", records[0].LastName);
        Assert.Equal(10.00m, records[0].Balance);
    }

    [Fact]
    public void ShouldSkipRecord_EmptyRows_AreSkipped()
    {
        const string csv =
            "Id,FirstName,LastName,Email,Balance\n" +
            "1,Alice,Smith,alice@example.com,10.00\n" +
            ", , , , \n" +
            "2,Bob,Jones,bob@example.com,20.00\n";

        var records = CsvReader.ReadFromString<CustomerCsvRow>(csv);

        Assert.Equal(2, records.Count);
    }
}
