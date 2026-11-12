using DeepSigma.DataAccess.CsvUtilities.Reading;

namespace DeepSigma.DataAccess.CsvUtilities.Test.Reading;

public sealed class CsvRawReaderTests
{
    [Theory]
    [InlineData(",")]
    [InlineData("||")]
    [InlineData("<->")]
    public void ReadsVariableWidthRowsAndPreservesEmptyFields(string delimiter)
    {
        string text = $"name{delimiter}value{delimiter}\r\nAlice{delimiter}{delimiter}3\r\nBob\r\n";

        var rows = CsvRawReader.ReadFromString(text, delimiter, TestContext.Current.CancellationToken).ToArray();

        Assert.Equal(3, rows.Length);
        Assert.Equal(new[] { "name", "value", "" }, rows[0].Fields);
        Assert.Equal(new[] { "Alice", "", "3" }, rows[1].Fields);
        Assert.Equal(new[] { "Bob" }, rows[2].Fields);
        Assert.All(rows, row => Assert.False(row.IsMalformed));
    }

    [Fact]
    public void ReadsQuotedDelimitersEscapedQuotesAndMultilineFields()
    {
        const string text = "id||description\r\n1||\"one||two\r\n\"\"quoted\"\"\"\r\n2||last";

        var rows = CsvRawReader.ReadFromString(text, "||", TestContext.Current.CancellationToken).ToArray();

        Assert.Equal(3, rows.Length);
        Assert.Equal(new[] { "1", "one||two\r\n\"quoted\"" }, rows[1].Fields);
        Assert.Equal(2, rows[1].LineNumber);
        Assert.Equal(4, rows[2].LineNumber);
        Assert.Equal(new[] { "2", "last" }, rows[2].Fields);
        Assert.All(rows, row => Assert.False(row.IsMalformed));
    }

    [Fact]
    public void RecoversMalformedQuotesAndReportsAffectedRows()
    {
        const string text = "a,b\n1,ab\"c\n2,\"ok\"tail\n3,\"unterminated";

        var rows = CsvRawReader.ReadFromString(text, cancellationToken: TestContext.Current.CancellationToken).ToArray();

        Assert.Equal(4, rows.Length);
        Assert.False(rows[0].IsMalformed);
        Assert.Equal(new[] { "1", "ab\"c" }, rows[1].Fields);
        Assert.True(rows[1].IsMalformed);
        Assert.Equal(new[] { "2", "oktail" }, rows[2].Fields);
        Assert.True(rows[2].IsMalformed);
        Assert.Equal(new[] { "3", "unterminated" }, rows[3].Fields);
        Assert.True(rows[3].IsMalformed);
        Assert.Equal(4, rows[3].LineNumber);
    }

    [Fact]
    public void SkipsEmptyLinesButRetainsWhitespaceAndTrailingEmptyFields()
    {
        var rows = CsvRawReader.ReadFromString("\r\n \nvalue||\n\n", "||", TestContext.Current.CancellationToken).ToArray();

        Assert.Equal(2, rows.Length);
        Assert.Equal(new[] { " " }, rows[0].Fields);
        Assert.Equal(2, rows[0].LineNumber);
        Assert.Equal(new[] { "value", "" }, rows[1].Fields);
        Assert.Equal(3, rows[1].LineNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n")]
    [InlineData("\"x")]
    public void RejectsInvalidDelimiters(string delimiter)
    {
        Assert.ThrowsAny<ArgumentException>(() => CsvRawReader.ReadFromString("a,b", delimiter, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void EmptyInputReturnsNoRows()
    {
        Assert.Empty(CsvRawReader.ReadFromString(string.Empty, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void CancellationIsObservedDuringEnumeration()
    {
        using var source = new CancellationTokenSource();
        var rows = CsvRawReader.ReadFromString("a,b\nc,d", cancellationToken: source.Token);
        using var enumerator = rows.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void CanStopBeforeParsingLaterRows()
    {
        var row = Assert.Single(CsvRawReader.ReadFromString("first\n\"malformed", cancellationToken: TestContext.Current.CancellationToken).Take(1));

        Assert.Equal(new[] { "first" }, row.Fields);
        Assert.False(row.IsMalformed);
    }
}
