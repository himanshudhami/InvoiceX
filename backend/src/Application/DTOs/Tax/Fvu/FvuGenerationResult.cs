namespace Application.DTOs.Tax.Fvu
{
    /// <summary>
    /// Result of FVU file generation containing the file stream and metadata.
    /// </summary>
    public class FvuGenerationResult
    {
        /// <summary>
        /// The generated file content as a stream
        /// </summary>
        public Stream FileStream { get; set; } = Stream.Null;

        /// <summary>
        /// Suggested filename for download (e.g., "26Q_TANXXXXXX_2024-25_Q1.txt")
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// MIME type for the file
        /// </summary>
        public string MimeType { get; } = "text/plain";

        /// <summary>
        /// Total number of records in the file (including headers)
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Number of deductee/employee records
        /// </summary>
        public int TotalDeductees { get; set; }

        /// <summary>
        /// Number of challan records
        /// </summary>
        public int TotalChallans { get; set; }

        /// <summary>
        /// Total TDS amount across all records
        /// </summary>
        public decimal TotalTdsAmount { get; set; }

        /// <summary>
        /// Total gross amount across all records
        /// </summary>
        public decimal TotalGrossAmount { get; set; }

        /// <summary>
        /// Form type (26Q, 24Q, 27Q, 27EQ)
        /// </summary>
        public string FormType { get; set; } = string.Empty;

        /// <summary>
        /// Financial year in format YYYY-YY
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Quarter (Q1, Q2, Q3, Q4)
        /// </summary>
        public string Quarter { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is a correction return
        /// </summary>
        public bool IsCorrection { get; set; }

        /// <summary>
        /// Timestamp when the file was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Result of FVU data validation before file generation.
    /// </summary>
    public class FvuValidationResult
    {
        /// <summary>
        /// Whether the data is valid for FVU generation
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Whether the file can be generated despite warnings
        /// </summary>
        public bool CanGenerate => IsValid || Errors.Count == 0;

        /// <summary>
        /// Form type being validated
        /// </summary>
        public string FormType { get; set; } = string.Empty;

        /// <summary>
        /// Financial year
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Quarter
        /// </summary>
        public string Quarter { get; set; } = string.Empty;

        /// <summary>
        /// Critical errors that prevent file generation
        /// </summary>
        public List<FvuValidationError> Errors { get; set; } = new();

        /// <summary>
        /// Warnings that don't prevent generation but may cause FVU rejection
        /// </summary>
        public List<FvuValidationWarning> Warnings { get; set; } = new();

        /// <summary>
        /// Summary statistics
        /// </summary>
        public FvuValidationSummary Summary { get; set; } = new();
    }

    /// <summary>
    /// Validation error that prevents FVU generation
    /// </summary>
    public class FvuValidationError
    {
        /// <summary>
        /// Error code (maps to NSDL FVU error codes where applicable)
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable error message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Field name that caused the error
        /// </summary>
        public string? Field { get; set; }

        /// <summary>
        /// Record identifier (e.g., deductee PAN or serial number)
        /// </summary>
        public string? RecordIdentifier { get; set; }

        /// <summary>
        /// Record type (FH, BH, CD, DD)
        /// </summary>
        public string? RecordType { get; set; }

        /// <summary>
        /// Suggested fix for the error
        /// </summary>
        public string? SuggestedFix { get; set; }

        public static FvuValidationError Create(string code, string message, string? field = null, string? recordId = null)
        {
            return new FvuValidationError
            {
                Code = code,
                Message = message,
                Field = field,
                RecordIdentifier = recordId
            };
        }
    }

    /// <summary>
    /// Validation warning that doesn't prevent generation
    /// </summary>
    public class FvuValidationWarning
    {
        /// <summary>
        /// Warning code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable warning message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Field name related to the warning
        /// </summary>
        public string? Field { get; set; }

        /// <summary>
        /// Record identifier
        /// </summary>
        public string? RecordIdentifier { get; set; }

        /// <summary>
        /// Impact of ignoring this warning
        /// </summary>
        public string? Impact { get; set; }

        public static FvuValidationWarning Create(string code, string message, string? field = null, string? recordId = null)
        {
            return new FvuValidationWarning
            {
                Code = code,
                Message = message,
                Field = field,
                RecordIdentifier = recordId
            };
        }
    }

    /// <summary>
    /// Summary of validation results
    /// </summary>
    public class FvuValidationSummary
    {
        /// <summary>
        /// Total deductee/employee records
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Records with valid data
        /// </summary>
        public int ValidRecords { get; set; }

        /// <summary>
        /// Records with errors
        /// </summary>
        public int InvalidRecords { get; set; }

        /// <summary>
        /// Records with warnings only
        /// </summary>
        public int RecordsWithWarnings { get; set; }

        /// <summary>
        /// Total TDS amount
        /// </summary>
        public decimal TotalTdsAmount { get; set; }

        /// <summary>
        /// Total gross amount
        /// </summary>
        public decimal TotalGrossAmount { get; set; }

        /// <summary>
        /// Number of unique deductees
        /// </summary>
        public int UniqueDeductees { get; set; }

        /// <summary>
        /// Number of challans
        /// </summary>
        public int ChallanCount { get; set; }

        /// <summary>
        /// Whether deductor TAN is present and valid
        /// </summary>
        public bool HasValidTan { get; set; }

        /// <summary>
        /// Count of records with invalid/missing PAN
        /// </summary>
        public int InvalidPanCount { get; set; }

        /// <summary>
        /// Whether challan reconciliation is complete
        /// </summary>
        public bool ChallansReconciled { get; set; }
    }
}
