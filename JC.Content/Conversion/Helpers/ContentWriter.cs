using System.Text;

namespace JC.Content.Conversion.Helpers;

/// <summary>
/// Builds line-based output from a document walk, tracking block spacing, nesting prefixes and
/// collapsed whitespace.
/// </summary>
/// <remarks>
/// Newlines are emitted lazily, when the next line actually opens. That is what stops a document
/// ending in a run of blank lines, and what lets a prefix pushed after a list marker reach only the
/// continuation lines below it.
/// </remarks>
/// <param name="escape">
/// Whether text is escaped as Markdown on the way in. Off for plain-text output, where a backslash
/// would be literal.
/// </param>
internal sealed class ContentWriter(bool escape = true)
{
    private readonly StringBuilder _builder = new();
    private readonly List<string> _prefixes = [];

    private bool _lineOpen;
    private bool _lineHasContent;
    private bool _blankPending;
    private bool _spacePending;
    private bool _suppressNextBlock;
    private int _listDepth;

    /// <summary>Indents every line opened from here until the matching pop.</summary>
    public void PushPrefix(string prefix) => _prefixes.Add(prefix);

    public void PopPrefix()
    {
        if(_prefixes.Count > 0)
            _prefixes.RemoveAt(_prefixes.Count - 1);
    }

    /// <summary>Marks the start of a block, separating it from whatever came before.</summary>
    public void StartBlock()
    {
        if (_suppressNextBlock)
        {
            _suppressNextBlock = false;
            return;
        }

        EndLine();

        //Inside a list the items are their own separation, and a blank line between them would
        //make the list loose
        if(_builder.Length > 0 && _listDepth == 0)
            _blankPending = true;
    }

    /// <summary>Keeps the next block on the current line — for the first child of a list item.</summary>
    public void SuppressNextBlock() => _suppressNextBlock = true;

    public void EnterList() => _listDepth++;

    public void ExitList() => _listDepth--;

    /// <summary>Writes syntax, which is never escaped.</summary>
    public void Write(string? value)
    {
        if(string.IsNullOrEmpty(value))
            return;

        EnsureLine();
        FlushSpace();

        _builder.Append(value);
        _lineHasContent = true;
    }

    /// <summary>Writes a whole line, blank ones included.</summary>
    public void WriteLine(string? value)
    {
        EnsureLine();
        _builder.Append(value);
        _lineHasContent = true;
        EndLine();
    }

    /// <summary>Writes content, escaped where the writer was built to escape.</summary>
    public void WriteText(string? value)
    {
        if(string.IsNullOrEmpty(value))
            return;

        EnsureLine();
        FlushSpace();

        _builder.Append(escape ? MarkdownEscaper.Escape(value, !_lineHasContent) : value);
        _lineHasContent = true;
    }

    /// <summary>
    /// Requests a single space, however much whitespace it stood for. Dropped at the start of a
    /// line, where it would read as indentation rather than as a gap between words.
    /// </summary>
    public void WriteSpace() => _spacePending = true;

    /// <summary>A break within a block — two trailing spaces, which is the portable spelling.</summary>
    public void HardBreak()
    {
        if(!_lineHasContent)
            return;

        if(escape)
            _builder.Append("  ");

        EndLine();
    }

    public void EndLine()
    {
        _lineOpen = false;
        _lineHasContent = false;
        _spacePending = false;
    }

    public string Build() => _builder.ToString().TrimEnd();

    private void EnsureLine()
    {
        if(_lineOpen)
            return;

        if (_builder.Length > 0)
        {
            _builder.Append('\n');

            //A blank line inside a blockquote still carries its marker, so the quote is not broken
            //in two. A list indent trims away to nothing, which is what it should be
            if(_blankPending)
                _builder.Append(Prefix().TrimEnd()).Append('\n');
        }

        _builder.Append(Prefix());

        _blankPending = false;
        _lineOpen = true;
        _lineHasContent = false;
    }

    private void FlushSpace()
    {
        if(!_spacePending)
            return;

        _spacePending = false;

        if(_lineHasContent)
            _builder.Append(' ');
    }

    private string Prefix() => _prefixes.Count == 0 ? string.Empty : string.Concat(_prefixes);
}
