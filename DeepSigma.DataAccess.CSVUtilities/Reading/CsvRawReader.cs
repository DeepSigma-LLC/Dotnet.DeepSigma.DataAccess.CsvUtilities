using System.Text;

namespace DeepSigma.DataAccess.CsvUtilities.Reading;

/// <summary>Reads untyped CSV records without requiring a predefined record model.</summary>
public static class CsvRawReader
{
    /// <summary>
    /// Lazily reads records from a string, including the first record without treating it as a header.
    /// Empty lines are skipped. Quoting errors are recovered and reported on the affected record.
    /// </summary>
    /// <param name="text">The CSV text.</param>
    /// <param name="delimiter">A nonempty delimiter that does not contain quotes or line breaks.</param>
    /// <param name="cancellationToken">A token to cancel parsing.</param>
    /// <returns>Records with their starting line numbers and malformed-data flags.</returns>
    public static IEnumerable<CsvRawRow> ReadFromString(
        string text,
        string delimiter = ",",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrEmpty(delimiter);
        if (delimiter.IndexOfAny(['"', '\r', '\n']) >= 0)
        {
            throw new ArgumentException("The delimiter cannot contain quotes or line breaks.", nameof(delimiter));
        }

        return ReadRows(text, delimiter, cancellationToken);
    }

    private static IEnumerable<CsvRawRow> ReadRows(string text, string delimiter, CancellationToken cancellationToken)
    {
        int position = 0;
        int line = 1;

        while (position < text.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (text[position] is '\r' or '\n')
            {
                ConsumeLineBreak(text, ref position, ref line);
                continue;
            }

            int startLine = line;
            var fields = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            bool afterQuote = false;
            bool fieldStarted = false;
            bool malformed = false;

            while (position < text.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                char current = text[position];

                if (inQuotes)
                {
                    if (current == '"')
                    {
                        if (position + 1 < text.Length && text[position + 1] == '"')
                        {
                            field.Append('"');
                            position += 2;
                        }
                        else
                        {
                            inQuotes = false;
                            afterQuote = true;
                            position++;
                        }
                    }
                    else if (current is '\r' or '\n')
                    {
                        int start = position;
                        ConsumeLineBreak(text, ref position, ref line);
                        field.Append(text, start, position - start);
                    }
                    else
                    {
                        field.Append(current);
                        position++;
                    }

                    continue;
                }

                if (text.AsSpan(position).StartsWith(delimiter.AsSpan(), StringComparison.Ordinal))
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    fieldStarted = false;
                    afterQuote = false;
                    position += delimiter.Length;
                    continue;
                }

                if (current is '\r' or '\n')
                {
                    ConsumeLineBreak(text, ref position, ref line);
                    break;
                }

                if (current == '"' && !fieldStarted)
                {
                    inQuotes = true;
                    fieldStarted = true;
                    position++;
                    continue;
                }

                if (current == '"' || afterQuote)
                {
                    malformed = true;
                }

                field.Append(current);
                fieldStarted = true;
                position++;
            }

            if (inQuotes)
            {
                malformed = true;
            }

            fields.Add(field.ToString());
            yield return new CsvRawRow(fields.ToArray(), startLine, malformed);
        }
    }

    private static void ConsumeLineBreak(string text, ref int position, ref int line)
    {
        if (text[position++] == '\r' && position < text.Length && text[position] == '\n')
        {
            position++;
        }

        line++;
    }
}

/// <summary>An untyped CSV record and its parsing status.</summary>
/// <param name="Fields">The record's fields in input order.</param>
/// <param name="LineNumber">The one-based physical line where the record starts.</param>
/// <param name="IsMalformed">Whether the record contained malformed quoting.</param>
public sealed record CsvRawRow(IReadOnlyList<string> Fields, int LineNumber, bool IsMalformed);
