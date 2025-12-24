namespace Application.DTOs.Tax.Fvu
{
    /// <summary>
    /// Constants for FVU (File Validation Utility) file generation.
    /// Contains all static mappings required for NSDL-compliant TDS return files.
    /// </summary>
    public static class FvuConstants
    {
        /// <summary>
        /// Field delimiter used in FVU text files
        /// </summary>
        public const char FieldDelimiter = '^';

        /// <summary>
        /// Name of the utility generating the file
        /// </summary>
        public const string UtilityName = "InvoiceApp";

        /// <summary>
        /// File type codes for different TDS/TCS forms
        /// </summary>
        public static class FileTypes
        {
            public const string NonSalary = "NS1";  // Form 24Q, 26Q, 27Q
            public const string Tcs = "TS1";         // Form 27EQ
        }

        /// <summary>
        /// Upload type codes
        /// </summary>
        public static class UploadTypes
        {
            public const string Regular = "R";
            public const string Correction = "C";
        }

        /// <summary>
        /// Uploader type codes
        /// </summary>
        public static class UploaderTypes
        {
            public const string Deductor = "D";
            public const string TinFc = "T";
        }

        /// <summary>
        /// Record type identifiers
        /// </summary>
        public static class RecordTypes
        {
            public const string FileHeader = "FH";
            public const string BatchHeader = "BH";
            public const string ChallanDetail = "CD";
            public const string DeducteeDetail = "DD";
        }

        /// <summary>
        /// Deductee code types
        /// </summary>
        public static class DeducteeCodes
        {
            public const string Company = "01";
            public const string Other = "02";
        }

        /// <summary>
        /// Deductor type codes as per Income Tax Department
        /// </summary>
        public static class DeductorTypes
        {
            public const string CentralGovernment = "A";
            public const string StateGovernment = "S";
            public const string StatutoryBodyCentral = "D";
            public const string StatutoryBodyState = "E";
            public const string AutonomousBodyCentral = "G";
            public const string AutonomousBodyState = "H";
            public const string LocalAuthorityCentral = "L";
            public const string LocalAuthorityState = "N";
            public const string Company = "K";
            public const string Firm = "F";
            public const string Aop = "A";
            public const string Boi = "B";
            public const string Trust = "T";
            public const string Individual = "P";
            public const string Huf = "H";
            public const string Others = "O";
        }

        /// <summary>
        /// Reason codes for lower/nil deduction
        /// </summary>
        public static class LowerDeductionReasons
        {
            public const string NilCertificate = "A";    // Certificate u/s 197 for nil deduction
            public const string LowerCertificate = "B";  // Certificate u/s 197 for lower deduction
        }

        /// <summary>
        /// PAN status values for invalid/unavailable PAN
        /// </summary>
        public static class PanStatus
        {
            public const string NotAvailable = "PANNOTAVBL";
            public const string Applied = "PANAPPLIED";
            public const string Invalid = "PANINVALID";
        }

        /// <summary>
        /// Minor head codes for challans
        /// </summary>
        public static class MinorHeadCodes
        {
            public const string TdsPayableByTaxpayer = "200";
            public const string TdsRegularAssessment = "400";
        }

        /// <summary>
        /// Maps TDS section codes from internal format to FVU format
        /// </summary>
        public static readonly Dictionary<string, string> SectionCodeMapping = new()
        {
            { "192", "92" },     // Salary
            { "193", "93" },     // Interest on Securities
            { "194", "94" },     // Dividend
            { "194A", "4A" },    // Interest other than securities
            { "194B", "4B" },    // Lottery/Gambling
            { "194BB", "BB" },   // Horse race winnings
            { "194C", "4C" },    // Contractor payments
            { "194D", "4D" },    // Insurance commission
            { "194DA", "DA" },   // Life insurance maturity
            { "194E", "4E" },    // Non-resident sportsman
            { "194EE", "EE" },   // NSS withdrawal
            { "194F", "4F" },    // Repurchase of MF units
            { "194G", "4G" },    // Lottery commission
            { "194H", "4H" },    // Commission/Brokerage
            { "194I", "4I" },    // Rent (general)
            { "194IA", "IA" },   // Immovable property
            { "194IB", "IB" },   // Rent by individual
            { "194IC", "IC" },   // JDA payments
            { "194J", "4J" },    // Professional/Technical fees
            { "194K", "4K" },    // MF units
            { "194LA", "LA" },   // Compensation for land
            { "194LB", "LB" },   // Infrastructure debt fund
            { "194LBA", "BA" },  // Business trust
            { "194LBB", "2B" },  // Investment fund
            { "194LBC", "BC" },  // Securitization trust
            { "194LC", "LC" },   // Foreign currency bonds
            { "194LD", "LD" },   // Interest on rupee bonds
            { "194M", "4M" },    // Payment by individual/HUF
            { "194N", "4N" },    // Cash withdrawal
            { "194O", "4O" },    // E-commerce
            { "194P", "4P" },    // Senior citizen TDS
            { "194Q", "4Q" },    // Purchase of goods
            { "194R", "4R" },    // Perquisites
            { "194S", "4S" },    // Crypto/VDA
            { "195", "95" },     // Non-resident payments
            { "196A", "6A" },    // Foreign company dividends
            { "196B", "6B" },    // GDR units
            { "196C", "6C" },    // Foreign currency bonds
            { "196D", "6D" },    // FII income
            { "206AB", "AB" },   // Higher rate (non-filers)
        };

        /// <summary>
        /// Maps state names to Income Tax Department state codes
        /// </summary>
        public static readonly Dictionary<string, string> StateCodeMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Jammu and Kashmir", "01" },
            { "Jammu & Kashmir", "01" },
            { "J&K", "01" },
            { "Himachal Pradesh", "02" },
            { "HP", "02" },
            { "Punjab", "03" },
            { "Chandigarh", "04" },
            { "Uttarakhand", "05" },
            { "Uttaranchal", "05" },
            { "Haryana", "06" },
            { "Delhi", "07" },
            { "New Delhi", "07" },
            { "NCT of Delhi", "07" },
            { "Rajasthan", "08" },
            { "Uttar Pradesh", "09" },
            { "UP", "09" },
            { "Bihar", "10" },
            { "Sikkim", "11" },
            { "Arunachal Pradesh", "12" },
            { "Nagaland", "13" },
            { "Manipur", "14" },
            { "Mizoram", "15" },
            { "Tripura", "16" },
            { "Meghalaya", "17" },
            { "Assam", "18" },
            { "West Bengal", "19" },
            { "WB", "19" },
            { "Jharkhand", "20" },
            { "Odisha", "21" },
            { "Orissa", "21" },
            { "Chhattisgarh", "22" },
            { "Chattisgarh", "22" },
            { "Madhya Pradesh", "23" },
            { "MP", "23" },
            { "Gujarat", "24" },
            { "Dadra and Nagar Haveli and Daman and Diu", "26" },
            { "Dadra and Nagar Haveli", "26" },
            { "Daman and Diu", "26" },
            { "D&N Haveli", "26" },
            { "Maharashtra", "27" },
            { "MH", "27" },
            { "Andhra Pradesh", "28" },
            { "AP", "28" },
            { "Karnataka", "29" },
            { "KA", "29" },
            { "Goa", "30" },
            { "Lakshadweep", "31" },
            { "Kerala", "32" },
            { "KL", "32" },
            { "Tamil Nadu", "33" },
            { "TN", "33" },
            { "Puducherry", "34" },
            { "Pondicherry", "34" },
            { "Andaman and Nicobar Islands", "35" },
            { "Andaman and Nicobar", "35" },
            { "A&N Islands", "35" },
            { "Telangana", "36" },
            { "TS", "36" },
            { "Ladakh", "37" },
            { "Other Country", "97" },
            { "Foreign", "97" },
        };

        /// <summary>
        /// Quarter month ranges for Indian financial year (April to March)
        /// </summary>
        public static readonly Dictionary<string, (int StartMonth, int EndMonth)> QuarterMonths = new()
        {
            { "Q1", (4, 6) },   // Apr-Jun
            { "Q2", (7, 9) },   // Jul-Sep
            { "Q3", (10, 12) }, // Oct-Dec
            { "Q4", (1, 3) },   // Jan-Mar
        };

        /// <summary>
        /// Gets the FVU section code for a given internal section code
        /// </summary>
        public static string GetFvuSectionCode(string internalCode)
        {
            if (string.IsNullOrEmpty(internalCode))
                return string.Empty;

            var cleanCode = internalCode.Trim().ToUpperInvariant();
            return SectionCodeMapping.TryGetValue(cleanCode, out var fvuCode) ? fvuCode : cleanCode;
        }

        /// <summary>
        /// Gets the state code for a given state name
        /// </summary>
        public static string GetStateCode(string? stateName)
        {
            if (string.IsNullOrEmpty(stateName))
                return "99"; // Default/Unknown

            var cleanName = stateName.Trim();
            return StateCodeMapping.TryGetValue(cleanName, out var code) ? code : "99";
        }

        /// <summary>
        /// Determines deductee code based on PAN structure
        /// 4th character: C=Company, P=Person, H=HUF, F=Firm, etc.
        /// </summary>
        public static string GetDeducteeCode(string? pan)
        {
            if (string.IsNullOrEmpty(pan) || pan.Length < 4)
                return DeducteeCodes.Other;

            var entityType = pan[3];
            return entityType == 'C' ? DeducteeCodes.Company : DeducteeCodes.Other;
        }

        /// <summary>
        /// Validates PAN format
        /// </summary>
        public static bool IsValidPan(string? pan)
        {
            if (string.IsNullOrEmpty(pan))
                return false;

            if (pan == PanStatus.NotAvailable || pan == PanStatus.Applied || pan == PanStatus.Invalid)
                return true;

            // PAN format: 5 letters + 4 digits + 1 letter
            if (pan.Length != 10)
                return false;

            for (int i = 0; i < 5; i++)
                if (!char.IsLetter(pan[i]))
                    return false;

            for (int i = 5; i < 9; i++)
                if (!char.IsDigit(pan[i]))
                    return false;

            return char.IsLetter(pan[9]);
        }

        /// <summary>
        /// Validates TAN format
        /// </summary>
        public static bool IsValidTan(string? tan)
        {
            if (string.IsNullOrEmpty(tan) || tan.Length != 10)
                return false;

            // TAN format: 4 letters + 5 digits + 1 letter
            for (int i = 0; i < 4; i++)
                if (!char.IsLetter(tan[i]))
                    return false;

            for (int i = 4; i < 9; i++)
                if (!char.IsDigit(tan[i]))
                    return false;

            return char.IsLetter(tan[9]);
        }

        /// <summary>
        /// Gets assessment year from financial year
        /// Financial year 2024-25 -> Assessment year 202526
        /// </summary>
        public static string GetAssessmentYear(string financialYear)
        {
            if (string.IsNullOrEmpty(financialYear) || !financialYear.Contains('-'))
                return string.Empty;

            var parts = financialYear.Split('-');
            if (parts.Length != 2)
                return string.Empty;

            // FY 2024-25 -> AY 2025-26 -> 202526
            if (int.TryParse(parts[0], out var startYear))
            {
                var ayStart = startYear + 1;
                var ayEnd = (ayStart + 1) % 100;
                return $"{ayStart}{ayEnd:D2}";
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets financial year in FVU format (YYYYMM where MM is start month)
        /// Financial year 2024-25 -> 202404
        /// </summary>
        public static string GetFinancialYearCode(string financialYear)
        {
            if (string.IsNullOrEmpty(financialYear) || !financialYear.Contains('-'))
                return string.Empty;

            var parts = financialYear.Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var startYear))
                return string.Empty;

            return $"{startYear}04"; // April is month 04
        }
    }
}
