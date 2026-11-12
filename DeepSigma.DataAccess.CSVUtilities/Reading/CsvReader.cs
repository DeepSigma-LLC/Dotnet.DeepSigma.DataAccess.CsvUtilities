using CsvHelper;
using DeepSigma.DataAccess.CsvUtilities.Configuration;
using DeepSigma.DataAccess.CsvUtilities.Results;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace DeepSigma.DataAccess.CsvUtilities.Reading;

/// <summary>
/// Static helper for reading CSV data from files, streams, strings, and <see cref="TextReader"/> instances.
/// </summary>
/// <remarks>
/// All overloads accept optional <see cref="CsvReaderOptions"/>; when omitted the defaults from
/// <see cref="CsvDefaults.CreateDefault"/> are used.  Async overloads accept a
/// <see cref="CancellationToken"/> and streaming overloads return <see cref="IAsyncEnumerable{T}"/>
/// so callers can process large files row-by-row without buffering everything into memory.
/// </remarks>
public static class CsvReader
{
    // -------------------------------------------------------------------------
    // From string
    // -------------------------------------------------------------------------

    /// <summary>
    /// Parses a CSV string and returns all records as a list.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="csvText">The raw CSV content.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <returns>A list of <typeparamref name="T"/> instances.</returns>
    public static List<T> ReadFromString<T>(
        string csvText,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null)
    {
        using var reader = new StringReader(csvText);
        return ReadFromReader<T>(reader, config, culture);
    }

    /// <summary>
    /// Parses a CSV string, collecting successfully mapped records and per-row errors
    /// instead of throwing on the first failure.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="csvText">The raw CSV content.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <returns>A <see cref="CsvImportResult{T}"/> containing records and any errors.</returns>
    public static CsvImportResult<T> ReadFromStringSafe<T>(
        string csvText,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null)
    {
        using var reader = new StringReader(csvText);
        return ReadFromReaderSafe<T>(reader, config, culture);
    }

    // -------------------------------------------------------------------------
    // From TextReader
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads all records from an existing <see cref="TextReader"/>.
    /// </summary>
    /// <remarks>The caller retains ownership of <paramref name="textReader"/> and is responsible for disposal.</remarks>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="textReader">The reader to consume.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <returns>A list of <typeparamref name="T"/> instances.</returns>
    public static List<T> ReadFromReader<T>(
        TextReader textReader,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null)
    {
        using var csv = CreateCsvReader(textReader, config, culture);
        return ReadRecords<T>(csv);
    }

    /// <summary>
    /// Reads all records from an existing <see cref="TextReader"/>, capturing per-row errors.
    /// </summary>
    /// <remarks>The caller retains ownership of <paramref name="textReader"/> and is responsible for disposal.</remarks>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="textReader">The reader to consume.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <returns>A <see cref="CsvImportResult{T}"/> containing records and any errors.</returns>
    public static CsvImportResult<T> ReadFromReaderSafe<T>(
        TextReader textReader,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null)
    {
        using var csv = CreateCsvReader(textReader, config, culture);
        return ReadSafe<T>(csv);
    }

    // -------------------------------------------------------------------------
    // From stream
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads all records from a <see cref="Stream"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="leaveOpen">
    /// When <see langword="true"/> the underlying <paramref name="stream"/> is left open after reading;
    /// when <see langword="false"/> (the default) it is disposed.
    /// </param>
    /// <returns>A list of <typeparamref name="T"/> instances.</returns>
    public static List<T> ReadFromStream<T>(
        Stream stream,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null,
        bool leaveOpen = false)
    {
        using var reader = new StreamReader(stream, leaveOpen: leaveOpen);
        return ReadFromReader<T>(reader, config, culture);
    }

    /// <summary>
    /// Reads all records from a <see cref="Stream"/>, capturing per-row errors.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="leaveOpen">
    /// When <see langword="true"/> the underlying <paramref name="stream"/> is left open after reading.
    /// </param>
    /// <returns>A <see cref="CsvImportResult{T}"/> containing records and any errors.</returns>
    public static CsvImportResult<T> ReadFromStreamSafe<T>(
        Stream stream,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null,
        bool leaveOpen = false)
    {
        using var reader = new StreamReader(stream, leaveOpen: leaveOpen);
        return ReadFromReaderSafe<T>(reader, config, culture);
    }

    /// <summary>
    /// Reads all records from a <see cref="Stream"/> asynchronously.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="leaveOpen">
    /// When <see langword="true"/> the underlying <paramref name="stream"/> is left open after reading.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of <typeparamref name="T"/> instances.</returns>
    public static async Task<List<T>> ReadFromStreamAsync<T>(
        Stream stream,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: leaveOpen);
        using var csv = CreateCsvReader(reader, config, culture);
        return await CollectAsync<T>(csv, cancellationToken);
    }

    /// <summary>
    /// Streams records from a <see cref="Stream"/> one at a time via <see cref="IAsyncEnumerable{T}"/>,
    /// suitable for large files where buffering all rows is undesirable.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="leaveOpen">
    /// When <see langword="true"/> the underlying <paramref name="stream"/> is left open after reading.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An async sequence of <typeparamref name="T"/> instances.</returns>
    public static async IAsyncEnumerable<T> StreamFromStream<T>(
        Stream stream,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null,
        bool leaveOpen = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: leaveOpen);
        using var csv = CreateCsvReader(reader, config, culture);

        await foreach (var record in ReadRecordsAsync<T>(csv, cancellationToken))
        {
            yield return record;
        }
    }

    // -------------------------------------------------------------------------
    // From file
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads all records from a CSV file at <paramref name="path"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="path">Absolute or relative path to the CSV file.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <returns>A list of <typeparamref name="T"/> instances.</returns>
    public static List<T> ReadFromFile<T>(
        string path,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null)
    {
        using var reader = new StreamReader(path);
        return ReadFromReader<T>(reader, config, culture);
    }

    /// <summary>
    /// Reads all records from a CSV file, capturing per-row errors instead of throwing.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="path">Absolute or relative path to the CSV file.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <returns>A <see cref="CsvImportResult{T}"/> containing records and any errors.</returns>
    public static CsvImportResult<T> ReadFromFileSafe<T>(
        string path,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null)
    {
        using var reader = new StreamReader(path);
        return ReadFromReaderSafe<T>(reader, config, culture);
    }

    /// <summary>
    /// Reads all records from a CSV file asynchronously.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="path">Absolute or relative path to the CSV file.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of <typeparamref name="T"/> instances.</returns>
    public static async Task<List<T>> ReadFromFileAsync<T>(
        string path,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        using var csv = CreateCsvReader(reader, config, culture);
        return await CollectAsync<T>(csv, cancellationToken);
    }

    /// <summary>
    /// Streams records from a file one at a time via <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="path">Absolute or relative path to the CSV file.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An async sequence of <typeparamref name="T"/> instances.</returns>
    public static async IAsyncEnumerable<T> StreamFromFile<T>(
        string path,
        CsvReaderOptions? config = null,
        CultureInfo? culture = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        using var csv = CreateCsvReader(reader, config, culture);

        await foreach (var record in ReadRecordsAsync<T>(csv, cancellationToken))
        {
            yield return record;
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a <see cref="CsvHelper.CsvReader"/> over <paramref name="textReader"/>, applying
    /// the supplied <paramref name="config"/> or falling back to <see cref="CsvDefaults.CreateDefault"/>.
    /// </summary>
    private static CsvHelper.CsvReader CreateCsvReader(
        TextReader textReader,
        CsvReaderOptions? config,
        CultureInfo? culture)
    {
        config ??= CsvDefaults.CreateDefault(culture: culture);
        return CsvHelper.CsvReader.Create(textReader, config, leaveOpen: true);
    }

    /// <summary>
    /// Asynchronously consumes <paramref name="csv"/> into a list of <typeparamref name="T"/>.
    /// </summary>
    private static List<T> ReadRecords<T>(CsvHelper.CsvReader csv)
    {
        var records = new List<T>();
        while (csv.Read())
        {
            records.Add(csv.GetRecord<T>());
        }

        return records;
    }

    private static async Task<List<T>> CollectAsync<T>(
        CsvHelper.CsvReader csv,
        CancellationToken cancellationToken)
    {
        var records = new List<T>();
        await foreach (var record in ReadRecordsAsync<T>(csv, cancellationToken))
        {
            records.Add(record);
        }

        return records;
    }

    private static async IAsyncEnumerable<T> ReadRecordsAsync<T>(
        CsvHelper.CsvReader csv,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await csv.ReadAsync(cancellationToken))
        {
            yield return csv.GetRecord<T>();
        }
    }

    /// <summary>
    /// Iterates an already-configured <see cref="CsvHelper.CsvReader"/>, collecting records and
    /// capturing per-row errors into a <see cref="CsvImportResult{T}"/>.
    /// </summary>
    private static CsvImportResult<T> ReadSafe<T>(CsvHelper.CsvReader csv)
    {
        var result = new CsvImportResult<T>();

        while (csv.Read())
        {
            try
            {
                var record = csv.GetRecord<T>();
                if (record is not null)
                    result.Records.Add(record);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new CsvImportError(
                    Row: checked((int)(csv.RowNumber + (csv.Options.HasHeader ? 1 : 0))),
                    Message: ex.Message,
                    RawRecord: Encoding.UTF8.GetString(csv.RawRecord)));
            }
        }

        return result;
    }
}
