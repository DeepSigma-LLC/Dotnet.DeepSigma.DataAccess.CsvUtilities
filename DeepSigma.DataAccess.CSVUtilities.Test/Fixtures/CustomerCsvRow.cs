using CsvHelper;

namespace DeepSigma.DataAccess.CsvUtilities.Test.Fixtures;

/// <summary>Test model representing a customer record.</summary>
[CsvRecord]
public sealed partial class CustomerCsvRow
{
    [CsvColumn("customer_id", "id")]
    public int Id { get; set; }

    [CsvColumn("first_name", "firstname", "first")]
    public string FirstName { get; set; } = string.Empty;

    [CsvColumn("last_name", "lastname", "last")]
    public string LastName { get; set; } = string.Empty;

    [CsvColumn("email_address", "email")]
    public string Email { get; set; } = string.Empty;

    [CsvColumn("account_balance", "balance")]
    public decimal Balance { get; set; }

    [CsvColumn("birth_date", "dob", Optional = true)]
    public DateOnly? BirthDate { get; set; }
}
