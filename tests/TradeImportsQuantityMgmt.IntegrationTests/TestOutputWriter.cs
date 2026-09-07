using System.Text;

namespace TradeImportsQuantityMgmt.IntegrationTests;

/// <summary>
/// Forwards Console output (used by Serilog's console sink and the ASP.NET Core
/// console logger) line-by-line to an xunit <see cref="ITestOutputHelper"/>, so that
/// logs written by the host under test show up in the test runner's output pane.
/// </summary>
internal sealed class TestOutputWriter(ITestOutputHelper output) : TextWriter
{
    private readonly StringBuilder _buffer = new();

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        if (value == '\n')
        {
            Flush();
        }
        else if (value != '\r')
        {
            _buffer.Append(value);
        }
    }

    public override void Write(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        foreach (var c in value)
            Write(c);
    }

    public override void Flush()
    {
        if (_buffer.Length == 0)
            return;

        try
        {
            output.WriteLine(_buffer.ToString());
        }
        catch (InvalidOperationException)
        {
            // The test has already finished (e.g. logging happening on a background
            // thread after the test completed) - there's nowhere left to write to.
        }

        _buffer.Clear();
    }
}
