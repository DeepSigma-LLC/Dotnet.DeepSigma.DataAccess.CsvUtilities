# DeepSigma.DataAccess.CSVUtilities

A lightweight, opinionated .NET 11 library that wraps [CsvHelper](https://joshclose.github.io/CsvHelper/) to provide a clean, consistent API for reading and writing CSV data from files, streams, strings, and `TextReader`/`TextWriter` instances — including full async support, `IAsyncEnumerable<T>` streaming, per-row safe error collection, and culture-aware type conversion.

---

## Table of Contents

- [Why This Library?](#why-this-library)
- [Features](#features)
- [Installation](#installation)
- [Requirements](#requirements)
- [Project Structure](#project-structure)
- [Quick Start](#quick-start)
- [Reading CSV Data](#reading-csv-data)
  - [From a String](#from-a-string)
  - [From a File](#from-a-file)
  - [From a Stream](#from-a-stream)
  - [From a TextReader](#from-a-textreader)
  - [Async Reading](#async-reading)
  - [Streaming with IAsyncEnumerable](#streaming-with-iasyncenumerable)
  - [Safe Reading (Per-Row Error Collection)](#safe-reading-per-row-error-collection)
- [Writing CSV Data](#writing-csv-data)
  - [To a String](#to-a-string)
  - [To a File](#to-a-file)
  - [To a Stream](#to-a-stream)
  - [To a TextWriter](#to-a-textwriter)
  - [Async Writing](#async-writing)
  - [Async-Enumerable Writing](#async-enumerable-writing)
  - [Appending to a File](#appending-to-a-file)
- [String Extension Methods](#string-extension-methods)
- [Configuration](#configuration)
  - [CsvDefaults](#csvdefaults)
  - [Header Matching](#header-matching)
  - [Delimiter](#delimiter)
  - [Culture and Localization](#culture-and-localization)
  - [Headerless Files](#headerless-files)
- [Attribute-Based Record Mapping](#attribute-based-record-mapping)
- [Advanced Topics](#advanced-topics)
  - [Cancellation Tokens](#cancellation-tokens)
  - [The leaveOpen Flag](#the-leaveopen-flag)
  - [Safe Mode vs Throwing Mode](#safe-mode-vs-throwing-mode)
- [API Reference Summary](#api-reference-summary)
- [License](#license)

---

## Why This Library?

CsvHelper is an excellent, full-featured library, but using it directly involves boilerplate: creating reader/writer options, wiring up streams, handling cancellation, and managing disposal. This library removes that setup for the common cases while staying thin enough that you can still pass CsvHelper 34 options when you need custom behavior.

**Guiding principles:**

- One-liners for the 90% case
- Every method works identically regardless of the input source (string / file / stream / reader)
- Consistent opt-in patterns for generated record attributes, culture, cancellation, and `leaveOpen`
- If you need full CsvHelper control, pass a `CsvReaderOptions` or `CsvWriterOptions` instance

---

## Features

- Read from **string**, **file path**, **`Stream`**, or **`TextReader`**
- Write to **string**, **file path**, **`Stream`**, or **`TextWriter`**
- **Sync and async** variants for every operation
- **`IAsyncEnumerable<T>` streaming** for reading and writing large files without buffering all records
- **Safe reading** — collect per-row errors into `CsvImportResult<T>` instead of throwing
- **Untyped raw rows** from a string, with multi-character delimiters and malformed-row reporting
- **Append** to existing files without duplicating the header row
- **Forgiving header matching** — strips spaces, underscores, and dashes; case-insensitive by default
- **Opt-in `CultureInfo`** for locale-sensitive type conversion (dates, decimals, etc.)
- **`leaveOpen` flag** on all stream-accepting methods
- **`CancellationToken` support** on all async methods
- **Source-generated record mappings** via CsvHelper 34's `[CsvRecord]` and `[CsvColumn]` attributes
- **XML documentation** on every public member for full IntelliSense coverage

---

## Installation

```bash
dotnet add package DeepSigma.DataAccess.CSVUtilities
```

Or add the `PackageReference` directly to your `.csproj`:

```xml
<PackageReference Include="DeepSigma.DataAccess.CSVUtilities" Version="2.1.0" />
```

---

## Requirements

| Component | Version |
|---|---|
| .NET | 11.0 |
| CsvHelper | 34.0.0 |

---

## Project Structure

```
DeepSigma.DataAccess.CSVUtilities/
├── Configuration/
│   └── CsvDefaults.cs          # Default CsvHelper options factories
├── Reading/
│   ├── CsvReader.cs            # Typed record operations
│   └── CsvRawReader.cs         # Untyped rows from strings
├── Writing/
│   └── CsvWriter.cs            # All write operations
├── Results/
│   ├── CsvImportResult.cs      # Safe-read result bag
│   └── CsvImportError.cs       # Per-row error record
└── Extensions/
    └── StringExtensions.cs     # FromCsv<T> / ToCsv extension methods
```

---

## Raw rows without a model

`CsvRawReader.ReadFromString` lazily returns every nonempty record, including the first
record; callers decide whether it is a header. It supports nonempty delimiters of one
or more characters, quoted fields, escaped quotes, and embedded line breaks. Delimiters
cannot contain quotes or line breaks. Each `CsvRawRow` includes the fields, the one-based
starting physical line, and an `IsMalformed` flag. Malformed quoting is recovered: stray
quotes in unquoted fields are retained, text after a closing quote is appended, and an
unclosed quoted field consumes the rest of the input. Check `IsMalformed` before relying
on recovered data. Empty physical lines are skipped; whitespace-only lines are retained.
The input is already a string, but parsing and row allocation are lazy, so enumeration can
stop early. A cancellation token can be supplied for long inputs.

```csharp
using DeepSigma.DataAccess.CsvUtilities.Reading;

foreach (CsvRawRow row in CsvRawReader.ReadFromString("name||value\nAlice||42", "||"))
{
    Console.WriteLine($"Line {row.LineNumber}: {string.Join(", ", row.Fields)}");
    if (row.IsMalformed)
        Console.WriteLine("Malformed quoting was recovered.");
}
```

This API is separate from the typed `CsvReader` methods, which use CsvHelper 34 and
continue to accept only single-character delimiters.

---

## Quick Start

Define a record model for CsvHelper 34's source generator:

```csharp
using CsvHelper;

[CsvRecord]
public partial class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateOnly ReleaseDate { get; set; }
}
```

**Read a file:**

```csharp
using DeepSigma.DataAccess.CSVUtilities.Reading;

List<Product> products = CsvReader.ReadFromFile<Product>("products.csv");
```

**Write a file:**

```csharp
using DeepSigma.DataAccess.CSVUtilities.Writing;

CsvWriter.WriteToFile("products.csv", products);
```

**Parse a CSV string:**

```csharp
string csv = "Id,Name,Price\n1,Widget,9.99\n2,Gadget,24.50\n";
List<Product> products = CsvReader.ReadFromString<Product>(csv);
```

**Round-trip with extension methods:**

```csharp
using DeepSigma.DataAccess.CSVUtilities.Extensions;

string csv = products.ToCsv();
List<Product> restored = csv.FromCsv<Product>();
```

---

## Reading CSV Data

All read methods share the same optional parameters:

| Parameter | Type | Default | Description |
|---|---|---|---|
| `config` | `CsvReaderOptions?` | `null` | CsvHelper 34 reader options; defaults to `CsvDefaults.CreateDefault()` when omitted |
| `culture` | `CultureInfo?` | `null` | Culture for type conversion; applied when `config` is `null`; defaults to `InvariantCulture` |
| `leaveOpen` | `bool` | `false` | (stream overloads only) Whether to keep the stream open after the read |
| `cancellationToken` | `CancellationToken` | `default` | (async overloads only) Token to cancel the operation |

---

### From a String

```csharp
string csvText = """
    Id,Name,Price
    1,Widget,9.99
    2,Gadget,24.50
    """;

// The Product type is annotated with [CsvRecord]; use [CsvColumn] for aliases.
List<Product> products = CsvReader.ReadFromString<Product>(csvText);

// Safe mode — collect errors instead of throwing
CsvImportResult<Product> result = CsvReader.ReadFromStringSafe<Product>(csvText);
```

---

### From a File

```csharp
// Product uses CsvHelper 34's source-generated record attributes.
List<Product> products = CsvReader.ReadFromFile<Product>("products.csv");

// Safe mode
CsvImportResult<Product> result = CsvReader.ReadFromFileSafe<Product>("products.csv");
```

---

### From a Stream

```csharp
using var stream = File.OpenRead("products.csv");

// Read and dispose stream when done (default)
List<Product> products = CsvReader.ReadFromStream<Product>(stream);

// Keep stream open after reading
List<Product> products = CsvReader.ReadFromStream<Product>(stream, leaveOpen: true);

// Safe mode
CsvImportResult<Product> result = CsvReader.ReadFromStreamSafe<Product>(stream);
```

The `leaveOpen: true` flag is essential when the stream is owned by a caller that needs to continue using it (e.g., reading multiple sections from one HTTP response body).

---

### From a TextReader

Useful when a `TextReader` is already provided by a framework or middleware and you don't want to wrap it yourself.

```csharp
// The caller retains ownership — the reader is NOT disposed.
using var reader = new StringReader(csvText);
List<Product> products = CsvReader.ReadFromReader<Product>(reader);

// Safe mode
CsvImportResult<Product> result = CsvReader.ReadFromReaderSafe<Product>(reader);
```

---

### Async Reading

```csharp
// From file; mapping comes from the Product record attributes
List<Product> products = await CsvReader.ReadFromFileAsync<Product>(
    "products.csv",
    cancellationToken: cancellationToken);

// From stream
await using var stream = File.OpenRead("products.csv");
List<Product> products = await CsvReader.ReadFromStreamAsync<Product>(
    stream,
    leaveOpen: false,
    cancellationToken: cancellationToken);
```

---

### Streaming with IAsyncEnumerable

For large files it is wasteful to load everything into a `List<T>` before processing. The `StreamFromFile` and `StreamFromStream` overloads return `IAsyncEnumerable<T>`, letting you process records one at a time:

```csharp
await foreach (var product in CsvReader.StreamFromFile<Product>(
    "large-catalog.csv",
    cancellationToken: cancellationToken))
{
    await database.InsertAsync(product);
}
```

From a stream:

```csharp
await foreach (var product in CsvReader.StreamFromStream<Product>(
    responseStream,
    leaveOpen: true,
    cancellationToken: cancellationToken))
{
    Console.WriteLine(product.Name);
}
```

Because `IAsyncEnumerable<T>` is lazy, the CSV file is read only as fast as the consumer pulls from it. This is ideal for pipelines, batch inserts, and memory-sensitive scenarios.

---

### Safe Reading (Per-Row Error Collection)

By default, any row that fails to parse throws an exception and aborts the operation. The `*Safe` variants instead capture each row-level error and continue, returning a `CsvImportResult<T>`:

```csharp
CsvImportResult<Product> result = CsvReader.ReadFromFileSafe<Product>(
    "products.csv");

if (result.IsSuccess)
{
    Console.WriteLine($"Loaded {result.Records.Count} products.");
}
else
{
    Console.WriteLine($"Loaded {result.Records.Count} products with {result.Errors.Count} error(s):");

    foreach (CsvImportError error in result.Errors)
    {
        Console.WriteLine($"  Row {error.Row}: {error.Message}");
        Console.WriteLine($"  Raw:  {error.RawRecord}");
    }
}
```

`CsvImportResult<T>` properties:

| Property | Type | Description |
|---|---|---|
| `Records` | `List<T>` | Successfully parsed records |
| `Errors` | `List<CsvImportError>` | Per-row failures |
| `IsSuccess` | `bool` | `true` when `Errors` is empty |

`CsvImportError` properties:

| Property | Type | Description |
|---|---|---|
| `Row` | `int?` | 1-based row number in the source file |
| `Message` | `string` | Exception message from CsvHelper |
| `RawRecord` | `string?` | The raw CSV text of the failing row |

Safe variants are available for every source type:

```csharp
CsvReader.ReadFromStringSafe<T>()
CsvReader.ReadFromFileSafe<T>()
CsvReader.ReadFromStreamSafe<T>()
CsvReader.ReadFromReaderSafe<T>()
```

---

## Writing CSV Data

All write methods share the same optional parameters:

| Parameter | Type | Default | Description |
|---|---|---|---|
| `config` | `CsvWriterOptions?` | `null` | CsvHelper 34 writer options; defaults to `CsvDefaults.CreateWriterOptions()` when omitted |
| `culture` | `CultureInfo?` | `null` | Culture for type conversion; applied when `config` is `null` |
| `leaveOpen` | `bool` | `false` | (stream overloads only) Whether to keep the stream open after writing |
| `cancellationToken` | `CancellationToken` | `default` | (async overloads only) Token to cancel the operation |

---

### To a String

```csharp
string csv = CsvWriter.WriteToString(products);
// Output:
// Id,Name,Price,ReleaseDate
// 1,Widget,9.99,2024-03-01
// 2,Gadget,24.50,2023-11-15
```

---

### To a File

Creates or overwrites the file:

```csharp
CsvWriter.WriteToFile("products.csv", products);
```

---

### To a Stream

```csharp
using var stream = File.Create("products.csv");

// Dispose stream when done (default)
CsvWriter.WriteToStream(stream, products);

// Keep stream open after writing
CsvWriter.WriteToStream(stream, products, leaveOpen: true);
```

---

### To a TextWriter

```csharp
// Caller retains ownership; writer is NOT disposed.
using var writer = new StreamWriter(Response.Body, leaveOpen: true);
CsvWriter.WriteToWriter(writer, products);
```

---

### Async Writing

```csharp
// To file
await CsvWriter.WriteToFileAsync("products.csv", products,
    cancellationToken: cancellationToken);

// To stream
await using var stream = File.Create("products.csv");
await CsvWriter.WriteToStreamAsync(stream, products,
    leaveOpen: false,
    cancellationToken: cancellationToken);
```

---

### Async-Enumerable Writing

The async writer methods also accept `IAsyncEnumerable<T>`. Records are requested one at a time, so an async producer can be written without first buffering everything into a list. The cancellation token is passed to the source enumerator as well as the writer.

```csharp
await CsvWriter.WriteToFileAsync(
    "processed.csv",
    CsvReader.StreamFromFile<Product>("input.csv", cancellationToken: cancellationToken),
    cancellationToken: cancellationToken);

await CsvWriter.AppendToFileAsync(
    "processed.csv",
    GetNewProductsAsync(cancellationToken),
    cancellationToken: cancellationToken);
```

Async-enumerable overloads are available for `WriteToStringAsync`, `WriteToWriterAsync`, `WriteToStreamAsync`, `WriteToFileAsync`, and `AppendToFileAsync`.

---

### Appending to a File

`AppendToFile` and `AppendToFileAsync` add records to an existing file **without writing the header row again**. If the file does not exist, or is empty, the header is written normally.

```csharp
// First call — creates file with header + first batch
CsvWriter.AppendToFile("log.csv", firstBatch);

// Second call — appends data rows only, no duplicate header
CsvWriter.AppendToFile("log.csv", secondBatch);

// Async variant
await CsvWriter.AppendToFileAsync("log.csv", newRecords,
    cancellationToken: cancellationToken);
```

This is useful for log-style or incremental export scenarios where you write records over time without re-reading the file.

---

## String Extension Methods

`StringExtensions` provides two convenience methods for quick inline use:

```csharp
using DeepSigma.DataAccess.CSVUtilities.Extensions;

// Parse a CSV string into a list
List<Product> products = csvText.FromCsv<Product>();

// Serialize a list to a CSV string
string csv = products.ToCsv();

// Round-trip
string roundTripped = products.ToCsv().FromCsv<Product>().ToCsv();
```

`FromCsv` accepts `CsvReaderOptions`; `ToCsv` accepts `CsvWriterOptions`. Both accept an optional culture override.

---

## Configuration

### CsvDefaults

`CsvDefaults.CreateDefault()` returns CsvHelper 34 `CsvReaderOptions`; use `CreateWriterOptions()` for output:

```csharp
using DeepSigma.DataAccess.CSVUtilities.Configuration;

CsvReaderOptions readerOptions = CsvDefaults.CreateDefault();
CsvReaderOptions semicolonReader = CsvDefaults.CreateDefault(delimiter: ";");
CsvReaderOptions headerlessReader = CsvDefaults.CreateDefault(hasHeader: false);
CsvReaderOptions localizedReader = CsvDefaults.CreateDefault(culture: new CultureInfo("de-DE"));

CsvWriterOptions writerOptions = CsvDefaults.CreateWriterOptions(delimiter: "|");
```

Reader defaults normalize headers by trimming, removing spaces, underscores, and dashes, and lowercasing; they also skip rows containing only empty or whitespace fields. Missing fields are read as empty values and malformed records use CsvHelper 34's recovery behavior. The default format provider is `InvariantCulture`.

Pass any `CsvReaderOptions` or `CsvWriterOptions` value to the corresponding methods to customize behavior. The option types are separate in CsvHelper 34, so reading and writing use distinct options.

---

### Header Matching

The default configuration normalizes both CSV headers and .NET property names before comparing them. The normalization:

1. Trims leading/trailing whitespace
2. Removes spaces, underscores (`_`), and dashes (`-`)
3. Converts to lowercase

This means the following headers all map to the `FirstName` property:

| CSV Header | Normalized | Matches property |
|---|---|---|
| `first_name` | `firstname` | `FirstName` → `firstname` ✓ |
| `First Name` | `firstname` | `FirstName` → `firstname` ✓ |
| `FIRST-NAME` | `firstname` | `FirstName` → `firstname` ✓ |
| `firstname` | `firstname` | `FirstName` → `firstname` ✓ |

> **Note:** Compound headers like `customer_id` normalize to `customerid`, which does **not** match a property named `Id` (normalizes to `id`). Specify aliases such as `[CsvColumn("customer_id", "id")]` on the record member.

---

### Delimiter

```csharp
// Tab-separated
var config = CsvDefaults.CreateDefault(delimiter: "\t");

// Pipe-separated
var config = CsvDefaults.CreateDefault(delimiter: "|");

// Semicolon-separated
var config = CsvDefaults.CreateDefault(delimiter: ";");

List<Product> products = CsvReader.ReadFromFile<Product>("products.tsv", config);
```

---

### Culture and Localization

By default, `InvariantCulture` is used, which ensures decimal points (`.`) and ISO date formats are handled consistently regardless of the host machine's regional settings. This is the safest default for files exchanged between systems.

To parse files that use locale-specific formatting (e.g. German comma-separated decimals or French date formats):

```csharp
// German: decimals use comma, thousands use period
var config = CsvDefaults.CreateDefault(culture: new CultureInfo("de-DE"));

List<Order> orders = CsvReader.ReadFromFile<Order>("orders_de.csv", config);
```

Alternatively, pass the `culture` parameter directly without constructing a config:

```csharp
List<Order> orders = CsvReader.ReadFromFile<Order>(
    "orders_de.csv",
    culture: new CultureInfo("de-DE"));
```

> **Note:** The `culture` parameter is a shorthand that only applies when no `config` is provided. If you pass both, the `culture` parameter is ignored.

---

### Attribute-Based Record Mapping

CsvHelper 34 generates mapper code at compile time. Record models must be `partial` and marked `[CsvRecord]`; use `[CsvColumn]` for aliases or index-based fields.

```csharp
using CsvHelper;

[CsvRecord]
public sealed partial class Product
{
    [CsvColumn("product_id", "id", "sku")]
    public int Id { get; set; }

    [CsvColumn("product_name", "name", "title")]
    public string Name { get; set; } = string.Empty;

    [CsvColumn("unit_price", "price", "cost")]
    public decimal Price { get; set; }

    [CsvColumn("release_date", "date", Optional = true)]
    public DateOnly? ReleaseDate { get; set; }
}
```

Read using the ordinary generic overloads; the mapping is generated from the model attributes:

```csharp
List<Product> products = CsvReader.ReadFromFile<Product>("products.csv");
List<Product> fromText = CsvReader.ReadFromString<Product>(csvText);
```

For files without a header, set `[CsvColumn(Index = ...)]` on each member and pass `CsvDefaults.CreateDefault(hasHeader: false)`. A missing optional column leaves the member's default value.

---

## Advanced Topics

### Cancellation Tokens

Every async method accepts an optional `CancellationToken`:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

List<Product> products = await CsvReader.ReadFromFileAsync<Product>(
    "large-file.csv",
    cancellationToken: cts.Token);

await foreach (var product in CsvReader.StreamFromFile<Product>(
    "large-file.csv",
    cancellationToken: cts.Token))
{
    // cancelled if cts is cancelled during enumeration
}

await CsvWriter.WriteToFileAsync("output.csv", products,
    cancellationToken: cts.Token);
```

In ASP.NET Core, pass `HttpContext.RequestAborted` so operations automatically stop if the client disconnects:

```csharp
[HttpGet("export")]
public async Task<IActionResult> Export(CancellationToken cancellationToken)
{
    var products = await CsvReader.ReadFromFileAsync<Product>(
        "products.csv",
        cancellationToken: cancellationToken);

    return Ok(products);
}
```

> **Note:** CsvHelper wraps `OperationCanceledException` in its own `WriterException` during write operations. If you need to detect cancellation specifically, check `exception.InnerException is OperationCanceledException`.

---

### The leaveOpen Flag

Stream-accepting overloads default to `leaveOpen: false`, which disposes the stream after the operation. This is the safe default for streams you open yourself.

Set `leaveOpen: true` when:

- The stream was provided by a caller that owns it (e.g., ASP.NET Core request body)
- You need to read multiple sections from one stream
- You are in a `using` block that should control stream lifetime

```csharp
// Caller owns and manages the stream
public List<Product> ParseProductsFromBody(Stream body)
{
    return CsvReader.ReadFromStream<Product>(body, leaveOpen: true);
}

// Verify stream is still usable after the call
Assert.True(body.CanRead);
```

---

### Safe Mode vs Throwing Mode

| Mode | Method naming | Behaviour on bad row |
|---|---|---|
| **Throwing** (default) | `ReadFromFile`, `ReadFromString`, etc. | Throws immediately; remaining rows are not processed |
| **Safe** | `ReadFromFileSafe`, `ReadFromStringSafe`, etc. | Captures the error in `CsvImportResult<T>.Errors`; continues to the next row |

Use **throwing mode** when your data should always be clean and a single bad row indicates a programming or integration error.

Use **safe mode** when you are importing user-uploaded or third-party data where partial success is acceptable and you want to report which rows failed.

```csharp
// Throwing — stops at the first bad row
List<Product> products = CsvReader.ReadFromFile<Product>("import.csv");

// Safe — processes all rows, collects errors
CsvImportResult<Product> result = CsvReader.ReadFromFileSafe<Product>("import.csv");

int imported = result.Records.Count;
int rejected = result.Errors.Count;

foreach (var error in result.Errors)
{
    logger.LogWarning("Row {Row} failed: {Message} | Raw: {Raw}",
        error.Row, error.Message, error.RawRecord);
}
```

---

## API Reference Summary

### `CsvReader` — static class

| Method | Sync/Async | Returns | Notes |
|---|---|---|---|
| `ReadFromString<T>` | Sync | `List<T>` | |
| `ReadFromStringSafe<T>` | Sync | `CsvImportResult<T>` | |
| `ReadFromReader<T>` | Sync | `List<T>` | Caller owns `TextReader` |
| `ReadFromReaderSafe<T>` | Sync | `CsvImportResult<T>` | |
| `ReadFromStream<T>` | Sync | `List<T>` | `leaveOpen` |
| `ReadFromStreamSafe<T>` | Sync | `CsvImportResult<T>` | `leaveOpen` |
| `ReadFromStreamAsync<T>` | Async | `Task<List<T>>` | `leaveOpen`, `CancellationToken` |
| `StreamFromStream<T>` | Async streaming | `IAsyncEnumerable<T>` | `leaveOpen`, `CancellationToken` |
| `ReadFromFile<T>` | Sync | `List<T>` | |
| `ReadFromFileSafe<T>` | Sync | `CsvImportResult<T>` | |
| `ReadFromFileAsync<T>` | Async | `Task<List<T>>` | `CancellationToken` |
| `StreamFromFile<T>` | Async streaming | `IAsyncEnumerable<T>` | `CancellationToken` |

### `CsvWriter` — static class

| Method | Sync/Async | Notes |
|---|---|---|
| `WriteToString<T>` | Sync | Returns `string` |
| `WriteToStringAsync<T>` | Async | Accepts `IAsyncEnumerable<T>`, returns `Task<string>` |
| `WriteToWriter<T>` | Sync | Caller owns `TextWriter` |
| `WriteToWriterAsync<T>` | Async | Accepts `IAsyncEnumerable<T>`; caller owns `TextWriter` |
| `WriteToStream<T>` | Sync | `leaveOpen` |
| `WriteToStreamAsync<T>` | Async | Accepts `IEnumerable<T>` or `IAsyncEnumerable<T>`; `leaveOpen`, `CancellationToken` |
| `WriteToFile<T>` | Sync | Creates or overwrites |
| `WriteToFileAsync<T>` | Async | Accepts `IEnumerable<T>` or `IAsyncEnumerable<T>`; `CancellationToken` |
| `AppendToFile<T>` | Sync | No duplicate header |
| `AppendToFileAsync<T>` | Async | Accepts `IEnumerable<T>` or `IAsyncEnumerable<T>`; no duplicate header, `CancellationToken` |

### `CsvDefaults` — static class

| Method | Description |
|---|---|
| `CreateDefault(hasHeader, delimiter, culture)` | Returns `CsvReaderOptions` with forgiving defaults |
| `CreateWriterOptions(hasHeader, delimiter, culture)` | Returns configured `CsvWriterOptions` |

### `StringExtensions`

| Method | Description |
|---|---|
| `string.FromCsv<T>()` | Parses a CSV string into `List<T>` |
| `IEnumerable<T>.ToCsv()` | Serializes a sequence into a CSV string |

### `CsvImportResult<T>`

| Member | Type | Description |
|---|---|---|
| `Records` | `List<T>` | Successfully parsed records |
| `Errors` | `List<CsvImportError>` | Per-row failures |
| `IsSuccess` | `bool` | `true` when no errors |

### `CsvImportError`

| Member | Type | Description |
|---|---|---|
| `Row` | `int?` | 1-based row number |
| `Message` | `string` | CsvHelper exception message |
| `RawRecord` | `string?` | Raw CSV text of the failing row |

---

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

---

*Built on top of [CsvHelper](https://github.com/JoshClose/CsvHelper) by Josh Close
