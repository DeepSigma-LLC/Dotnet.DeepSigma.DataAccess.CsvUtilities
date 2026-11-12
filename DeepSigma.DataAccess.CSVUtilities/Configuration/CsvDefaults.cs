using CsvHelper;
using System.Globalization;

namespace DeepSigma.DataAccess.CsvUtilities.Configuration;

/// <summary>
/// Factory for creating CsvHelper 34 reader and writer options with sensible defaults.
/// </summary>
public static class CsvDefaults
{
    /// <summary>
    /// Creates reader options with forgiving header matching and empty-row skipping.
    /// </summary>
    public static CsvReaderOptions CreateDefault(
        bool hasHeader = true,
        string delimiter = ",",
        CultureInfo? culture = null)
    {
        var separator = GetSeparator(delimiter);
        return CsvReaderOptions.Default with
        {
            HasHeader = hasHeader,
            Separator = separator,
            FormatProvider = culture ?? CultureInfo.InvariantCulture,
            NormalizeHeaderName = static header => header.Trim()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .ToLowerInvariant(),
            ShouldSkipRecord = static record =>
            {
                for (var index = 0; index < record.FieldCount; index++)
                {
                    if (!string.IsNullOrWhiteSpace(record[index].ToString()))
                        return false;
                }

                return true;
            }
        };
    }

    /// <summary>
    /// Creates writer options using the requested header, delimiter, and culture settings.
    /// </summary>
    public static CsvWriterOptions CreateWriterOptions(
        bool hasHeader = true,
        string delimiter = ",",
        CultureInfo? culture = null)
    {
        return CsvWriterOptions.Default with
        {
            HasHeader = hasHeader,
            Separator = GetSeparator(delimiter),
            FormatProvider = culture ?? CultureInfo.InvariantCulture
        };
    }

    private static char GetSeparator(string delimiter)
    {
        ArgumentNullException.ThrowIfNull(delimiter);
        if (delimiter.Length != 1)
            throw new ArgumentException("CsvHelper 34 supports a single-character separator.", nameof(delimiter));

        return delimiter[0];
    }
}
