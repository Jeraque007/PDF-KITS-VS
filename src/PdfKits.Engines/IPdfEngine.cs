namespace PdfKits.Engines;

/// <summary>A pluggable HTML → PDF renderer.</summary>
public interface IPdfEngine
{
    /// <summary>Short machine name (e.g. "ironpdf").</summary>
    string Name { get; }

    /// <summary>Renders a complete HTML document to PDF bytes.</summary>
    byte[] Render(string html, PdfRenderOptions options);
}

/// <summary>Wraps engine-specific failures with actionable messages.</summary>
public sealed class PdfEngineException : Exception
{
    public PdfEngineException(string message, Exception? inner = null)
        : base(message, inner) { }
}
