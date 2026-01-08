using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
    /// <summary>
    /// Factory for creating appropriate Tally parser based on file type
    /// </summary>
    public class TallyParserFactory : ITallyParserFactory
    {
        private readonly IEnumerable<ITallyParserService> _parsers;
        private readonly ILogger<TallyParserFactory> _logger;

        public TallyParserFactory(
            IEnumerable<ITallyParserService> parsers,
            ILogger<TallyParserFactory> logger)
        {
            _parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ITallyParserService? GetParser(string fileName, string? contentType = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                _logger.LogWarning("Cannot get parser: fileName is empty");
                return null;
            }

            foreach (var parser in _parsers)
            {
                if (parser.CanParse(fileName, contentType))
                {
                    _logger.LogDebug("Selected {Parser} for file {FileName}", parser.GetType().Name, fileName);
                    return parser;
                }
            }

            _logger.LogWarning("No parser found for file {FileName} with content type {ContentType}", fileName, contentType);
            return null;
        }

        public async Task<Result<TallyParsedDataDto>> ParseAsync(Stream fileStream, string fileName, string? contentType = null)
        {
            var parser = GetParser(fileName, contentType);
            if (parser == null)
            {
                return Error.Validation($"Unsupported file format. Expected .xml or .json file, got: {Path.GetExtension(fileName)}");
            }

            return await parser.ParseAsync(fileStream, fileName);
        }
    }
}
