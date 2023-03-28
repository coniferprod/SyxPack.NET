using System.Text;

namespace SyxPack;

[Flags]
public enum IncludeOptions
{
    None = 0,
    Offset = 1,
    PrintableCharacters = 1 << 1,
    MiddleGap = 1 << 2
}

public class HexDumpConfiguration
{
    public int BytesPerLine;
    public bool Uppercase = true;
    public IncludeOptions Included;
}

public class HexDump
{
    public List<byte> Data { get; set; }

    public HexDumpConfiguration Configuration { get; set; }

    public HexDump() : this(new List<byte>()) { }

    public HexDump(List<byte> data)
    {
        this.Data = data;
        this.Configuration = new HexDumpConfiguration
        {
            BytesPerLine = 16,
            Uppercase = true,
            Included = IncludeOptions.Offset | IncludeOptions.PrintableCharacters
        };
    }

    public HexDump(byte[] data) : this(data.ToList()) { }

    private const int BytesPerLine = 16;

    private string DumpLine(List<byte> data, int offset)
    {
        var byteFormat = "{0:x02}";
        var offsetFormat = "{0:x08}";
        if (this.Configuration.Uppercase)
        {
            byteFormat = "{0:X02}";
            offsetFormat = "{0:X08}";
        }

        var sb = new StringBuilder();

        if ((this.Configuration.Included & IncludeOptions.Offset) != 0)
        {
            sb.Append(string.Format(offsetFormat, offset));
            sb.Append(": ");
        }

        var printableCharacters = new StringBuilder();
        var index = 0;
        var midpoint = this.Configuration.BytesPerLine / 2;
        foreach (var b in data)
        {
            sb.Append(string.Format(byteFormat, b));
            sb.Append(" ");

            if (index + 1 == midpoint)
            {
                if ((this.Configuration.Included & IncludeOptions.MiddleGap) != 0)
                {
                    sb.Append(" ");
                }
            }

            printableCharacters.Append(
                char.IsControl((char)b) ? '.' : (char)b
            );

            index += 1;
        }

        // Insert spaces for each unused byte slot in the chunk
        var bytesLeft = this.Configuration.BytesPerLine - data.Count;
        while (bytesLeft >= 0)
        {
            sb.Append("   ");   // this is for the byte, to replace "XX "
            printableCharacters.Append(" ");  // handle characters too, even if we don't use them
            bytesLeft -= 1;
        }

        if ((this.Configuration.Included & IncludeOptions.PrintableCharacters) != 0)
        {
            sb.Append(" ");
            sb.Append(printableCharacters);
        }

        return sb.ToString();
    }

    private List<string> Dump()
    {
        List<byte[]> chunks;
        if (this.Configuration.BytesPerLine != 0)
        {
            chunks = this.Data.Chunk(this.Configuration.BytesPerLine).ToList();
        }
        else
        {
            chunks = new List<byte[]> { this.Data.ToArray() };
        }

        List<string> lines = new();

        int offset = 0;
        var line = new StringBuilder();
        foreach (var chunk in chunks)
        {
            lines.Add(DumpLine(chunk.ToList(), offset));
            offset += chunk.Length;
        }

        return lines;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var line in Dump())
        {
            sb.AppendLine(line);
        }

        return sb.ToString();
    }
}
