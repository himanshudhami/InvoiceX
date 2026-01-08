using Application.DTOs.Migration;
using Core.Common;

namespace Application.Interfaces.Migration
{
    /// <summary>
    /// Interface for parsing Tally export files
    /// </summary>
    public interface ITallyParserService
    {
        /// <summary>
        /// Parse a Tally export file (XML or JSON)
        /// </summary>
        Task<Result<TallyParsedDataDto>> ParseAsync(Stream fileStream, string fileName);

        /// <summary>
        /// Check if this parser can handle the given file
        /// </summary>
        bool CanParse(string fileName, string? contentType = null);

        /// <summary>
        /// Get the format this parser handles
        /// </summary>
        string Format { get; }
    }

    /// <summary>
    /// Factory for creating appropriate parser based on file type
    /// </summary>
    public interface ITallyParserFactory
    {
        /// <summary>
        /// Get the appropriate parser for the given file
        /// </summary>
        ITallyParserService? GetParser(string fileName, string? contentType = null);

        /// <summary>
        /// Parse a file using the appropriate parser
        /// </summary>
        Task<Result<TallyParsedDataDto>> ParseAsync(Stream fileStream, string fileName, string? contentType = null);
    }
}
