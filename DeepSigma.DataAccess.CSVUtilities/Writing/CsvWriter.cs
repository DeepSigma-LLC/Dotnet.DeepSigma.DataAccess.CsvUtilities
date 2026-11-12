using CsvHelper;
using DeepSigma.DataAccess.CsvUtilities.Configuration;
using System.Globalization;
using System.Text;

namespace DeepSigma.DataAccess.CsvUtilities.Writing;

/// <summary>
/// Static helper for writing CSV data to files, streams, strings, and <see cref="TextWriter"/> instances.
/// </summary>
/// <remarks>
/// All overloads accept optional <see cref="CsvWriterOptions"/>; when omitted the defaults from
/// <see cref="CsvDefaults.CreateWriterOptions"/> are used.  Async overloads accept a
/// <see cref="CancellationToken"/>.  <see cref="AppendToFile{T}"/> omits the header row when the
/// target file already exists and is non-empty.
/// </remarks>
public static class CsvWriter
{
    // -------------------------------------------------------------------------
    // To string
    // -------------------------------------------------------------------------

    /// <summary>
    /// Serializes <paramref name="records"/> to a CSV string.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="records">The records to serialize.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <returns>A CSV-formatted string.</returns>
    public static string WriteToString<T>(
        IEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null)
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        using var csv = CreateCsvWriter(writer, config, culture);
        WriteRecords(csv, records);
        csv.Flush();
        return sb.ToString();
    }

    /// <summary>
    /// Asynchronously serializes an async sequence of records to a CSV string.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="records">The records to serialize.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="cancellationToken">Token to cancel enumeration and writing.</param>
    /// <returns>A task containing the CSV-formatted string.</returns>
    public static async Task<string> WriteToStringAsync<T>(
        IAsyncEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        await using var csv = CreateCsvWriter(writer, config, culture);
        await WriteRecordsAsync(csv, records, cancellationToken);
        await csv.FlushAsync(cancellationToken);
        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // To TextWriter
    // -------------------------------------------------------------------------

    /// <summary>
    /// Writes <paramref name="records"/> to an existing <see cref="TextWriter"/>.
    /// </summary>
    /// <remarks>The caller retains ownership of <paramref name="textWriter"/> and is responsible for disposal.</remarks>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="textWriter">The writer to output into.</param>
    /// <param name="records">The records to serialize.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    public static void WriteToWriter<T>(
        TextWriter textWriter,
        IEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null)
    {
        using var csv = CreateCsvWriter(textWriter, config, culture, leaveOpen: true);
        WriteRecords(csv, records);
    }

    /// <summary>
    /// Asynchronously writes an async sequence of records to an existing <see cref="TextWriter"/>.
    /// </summary>
    /// <remarks>The caller retains ownership of <paramref name="textWriter"/> and is responsible for disposal.</remarks>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="textWriter">The writer to output into.</param>
    /// <param name="records">The records to serialize.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="cancellationToken">Token to cancel enumeration and writing.</param>
    public static async Task WriteToWriterAsync<T>(
        TextWriter textWriter,
        IAsyncEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var csv = CreateCsvWriter(textWriter, config, culture, leaveOpen: true);
        await WriteRecordsAsync(csv, records, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // To stream
    // -------------------------------------------------------------------------

    /// <summary>
    /// Writes <paramref name="records"/> to a <see cref="Stream"/>.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="records">The records to serialize.</param>
    /// <param name="config">Optional configuration; defaults to <see cref="CsvDefaults.CreateDefault"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="leaveOpen">
    /// When <see langword="true"/> the <paramref name="stream"/> is left open after writing;
    /// when <see langword="false"/> (the default) it is disposed.
    /// </param>
    public static void WriteToStream<T>(
        Stream stream,
        IEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null,
        bool leaveOpen = false)
    {
        using var writer = new StreamWriter(stream, leaveOpen: leaveOpen);
        using var csv = CreateCsvWriter(writer, config, culture);
        WriteRecords(csv, records);
    }

    /// <summary>
    /// Writes <paramref name="records"/> to a <see cref="Stream"/> asynchronously.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="records">The records to serialize.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="leaveOpen">
    /// When <see langword="true"/> the <paramref name="stream"/> is left open after writing.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task WriteToStreamAsync<T>(
        Stream stream,
        IEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var writer = new StreamWriter(stream, leaveOpen: leaveOpen);
        await using var csv = CreateCsvWriter(writer, config, culture);
        await WriteRecordsAsync(csv, records, cancellationToken);
    }

    /// <summary>
    /// Asynchronously writes an async sequence of records to a <see cref="Stream"/>.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="records">The records to serialize.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="leaveOpen">Whether to leave the underlying stream open after writing.</param>
    /// <param name="cancellationToken">Token to cancel enumeration and writing.</param>
    public static async Task WriteToStreamAsync<T>(
        Stream stream,
        IAsyncEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var writer = new StreamWriter(stream, leaveOpen: leaveOpen);
        await using var csv = CreateCsvWriter(writer, config, culture);
        await WriteRecordsAsync(csv, records, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // To file
    // -------------------------------------------------------------------------

    /// <summary>
    /// Writes <paramref name="records"/> to a CSV file, creating or overwriting it.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="path">Absolute or relative path to the target file.</param>
    /// <param name="records">The records to serialize.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    public static void WriteToFile<T>(
        string path,
        IEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null)
    {
        using var writer = new StreamWriter(path, append: false);
        using var csv = CreateCsvWriter(writer, config, culture);
        WriteRecords(csv, records);
    }

    /// <summary>
    /// Writes <paramref name="records"/> to a CSV file asynchronously, creating or overwriting it.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="path">Absolute or relative path to the target file.</param>
    /// <param name="records">The records to serialize.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task WriteToFileAsync<T>(
        string path,
        IEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var writer = new StreamWriter(path, append: false);
        await using var csv = CreateCsvWriter(writer, config, culture);
        await WriteRecordsAsync(csv, records, cancellationToken);
    }

    /// <summary>
    /// Asynchronously writes an async sequence of records to a CSV file, creating or overwriting it.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="path">Absolute or relative path to the target file.</param>
    /// <param name="records">The records to serialize.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="cancellationToken">Token to cancel enumeration and writing.</param>
    public static async Task WriteToFileAsync<T>(
        string path,
        IAsyncEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var writer = new StreamWriter(path, append: false);
        await using var csv = CreateCsvWriter(writer, config, culture);
        await WriteRecordsAsync(csv, records, cancellationToken);
    }

    /// <summary>
    /// Appends <paramref name="records"/> to an existing CSV file, or creates it if it does not exist.
    /// The header row is written only when the file is new or empty.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="path">Absolute or relative path to the target file.</param>
    /// <param name="records">The records to append.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    public static void AppendToFile<T>(
        string path,
        IEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null)
    {
        var fileHasContent = File.Exists(path) && new FileInfo(path).Length > 0;
        if (fileHasContent)
        {
            config = (config ?? CsvDefaults.CreateWriterOptions(culture: culture)) with { HasHeader = false };
        }

        using var writer = new StreamWriter(path, append: true);
        using var csv = CreateCsvWriter(writer, config, culture);
        WriteRecords(csv, records);
    }

    /// <summary>
    /// Appends <paramref name="records"/> to an existing CSV file asynchronously, or creates it.
    /// The header row is written only when the file is new or empty.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="path">Absolute or relative path to the target file.</param>
    /// <param name="records">The records to append.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task AppendToFileAsync<T>(
        string path,
        IEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fileHasContent = File.Exists(path) && new FileInfo(path).Length > 0;
        if (fileHasContent)
        {
            config = (config ?? CsvDefaults.CreateWriterOptions(culture: culture)) with { HasHeader = false };
        }

        await using var writer = new StreamWriter(path, append: true);
        await using var csv = CreateCsvWriter(writer, config, culture);
        await WriteRecordsAsync(csv, records, cancellationToken);
    }

    /// <summary>
    /// Asynchronously appends an async sequence of records to a CSV file, or creates it if it does not exist.
    /// The header row is written only when the file is new or empty.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="path">Absolute or relative path to the target file.</param>
    /// <param name="records">The records to append.</param>
    /// <param name="config">Optional CsvHelper writer options; defaults to <see cref="CsvDefaults.CreateWriterOptions"/>.</param>
    /// <param name="culture">Optional culture override for type conversion.</param>
    /// <param name="cancellationToken">Token to cancel enumeration and writing.</param>
    public static async Task AppendToFileAsync<T>(
        string path,
        IAsyncEnumerable<T> records,
        CsvWriterOptions? config = null,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fileHasContent = File.Exists(path) && new FileInfo(path).Length > 0;
        if (fileHasContent)
        {
            config = (config ?? CsvDefaults.CreateWriterOptions(culture: culture)) with { HasHeader = false };
        }

        await using var writer = new StreamWriter(path, append: true);
        await using var csv = CreateCsvWriter(writer, config, culture);
        await WriteRecordsAsync(csv, records, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a <see cref="CsvHelper.CsvWriter"/> over <paramref name="textWriter"/>, applying
    /// the supplied <paramref name="config"/> or falling back to <see cref="CsvDefaults.CreateWriterOptions"/>.
    /// </summary>
    private static CsvHelper.CsvWriter CreateCsvWriter(
        TextWriter textWriter,
        CsvWriterOptions? config,
        CultureInfo? culture,
        bool leaveOpen = false)
    {
        config ??= CsvDefaults.CreateWriterOptions(culture: culture);
        return CsvHelper.CsvWriter.Create(textWriter, config, leaveOpen);
    }

    private static void WriteRecords<T>(CsvHelper.CsvWriter csv, IEnumerable<T> records)
    {
        if (csv.Options.HasHeader && csv.RecordCount == 0)
        {
            csv.WriteHeader<T>();
        }

        foreach (var record in records)
        {
            csv.WriteRecord(record);
        }
    }

    private static async Task WriteRecordsAsync<T>(
        CsvHelper.CsvWriter csv,
        IEnumerable<T> records,
        CancellationToken cancellationToken)
    {
        if (csv.Options.HasHeader && csv.RecordCount == 0)
        {
            csv.WriteHeader<T>();
        }

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await csv.WriteRecordAsync(record, cancellationToken);
        }
    }

    private static async Task WriteRecordsAsync<T>(
        CsvHelper.CsvWriter csv,
        IAsyncEnumerable<T> records,
        CancellationToken cancellationToken)
    {
        if (csv.Options.HasHeader && csv.RecordCount == 0)
        {
            csv.WriteHeader<T>();
        }

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            await csv.WriteRecordAsync(record, cancellationToken);
        }
    }
}
