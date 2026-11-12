using CsvHelper;
using DeepSigma.DataAccess.CsvUtilities.Configuration;
using System.Globalization;

namespace DeepSigma.DataAccess.CsvUtilities.Extensions;

/// <summary>
/// Convenience extension methods for reading CSV from strings and serializing objects back to CSV.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Parses a CSV-formatted string and returns all records as a list of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="csvText">The CSV content to parse.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <returns>A list of <typeparamref name="T"/> instances.</returns>
    public static List<T> FromCsv<T>(
        this string csvText,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null)
        where T : class
    {
        return Reading.CsvReader.ReadFromString<T>(csvText, config, culture);
    }

    /// <summary>
    /// Serializes a sequence of <typeparamref name="T"/> records to a CSV-formatted string.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="records">The records to serialize.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <returns>A CSV-formatted string.</returns>
    public static string ToCsv<T>(
        this IEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null)
    {
        return Writing.CsvWriter.WriteToString(records, config, culture);
    }
}
