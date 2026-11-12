using CsvHelper;

namespace DeepSigma.DataAccess.CsvUtilities.Test.Fixtures;

[CsvRecord]
public sealed partial class CustomerCsvNoHeaderRow
{
    [CsvColumn(Index = 0)]
    public int Id { get; set; }

    [CsvColumn(Index = 1)]
    public string FirstName { get; set; } = string.Empty;

    [CsvColumn(Index = 2)]
    public string LastName { get; set; } = string.Empty;

    [CsvColumn(Index = 3)]
    public string Email { get; set; } = string.Empty;

    [CsvColumn(Index = 4)]
    public decimal Balance { get; set; }

    [CsvColumn(Index = 5, Optional = true)]
    public DateOnly? BirthDate { get; set; }
}
