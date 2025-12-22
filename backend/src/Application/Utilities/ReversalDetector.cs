using System.Text.RegularExpressions;

namespace Application.Utilities
{
    /// <summary>
    /// Utility for detecting reversal transactions based on Indian bank patterns
    /// </summary>
    public static class ReversalDetector
    {
        // Common reversal patterns in Indian bank statements
        private static readonly (string Pattern, int Confidence, string Name)[] ReversalPatterns = new[]
        {
            // Direct reversal prefixes
            (@"^REV[-/\s]", 95, "REV-"),
            (@"^REVERSAL[-/\s]?", 95, "REVERSAL"),
            (@"^R[-/]", 80, "R-"),
            (@"^RV[-/]", 85, "RV-"),

            // NEFT/RTGS reversals
            (@"^NEFT[-\s]REV", 95, "NEFT-REV"),
            (@"^RTGS[-\s]REV", 95, "RTGS-REV"),
            (@"NEFT[-\s]RETURN", 90, "NEFT-RETURN"),
            (@"RTGS[-\s]RETURN", 90, "RTGS-RETURN"),

            // UPI reversals
            (@"^REV[-/]UPI", 95, "REV-UPI"),
            (@"UPI[-\s]REV", 90, "UPI-REV"),
            (@"UPI.*REVERSAL", 85, "UPI-REVERSAL"),

            // IMPS reversals
            (@"^REV[-/]IMPS", 95, "REV-IMPS"),
            (@"IMPS[-\s]REV", 90, "IMPS-REV"),

            // Cheque returns
            (@"CHQ[-\s]?RET", 90, "CHQ-RETURN"),
            (@"CHEQUE[-\s]RETURN", 90, "CHEQUE-RETURN"),
            (@"INWARD[-\s]RETURN", 85, "INWARD-RETURN"),

            // NACH/ECS bounces
            (@"NACH[-\s]RETURN", 90, "NACH-RETURN"),
            (@"ECS[-\s]RETURN", 90, "ECS-RETURN"),
            (@"NACH[-\s]BOUNCE", 90, "NACH-BOUNCE"),
            (@"ECS[-\s]BOUNCE", 90, "ECS-BOUNCE"),

            // Chargebacks
            (@"CHARGEBACK", 95, "CHARGEBACK"),
            (@"CHARGE[-\s]?BACK", 95, "CHARGEBACK"),
            (@"DISPUTE[-\s]CREDIT", 85, "DISPUTE-CREDIT"),

            // Auto-debit reversals
            (@"AUTO[-\s]?DEBIT[-\s]REV", 90, "AUTO-DEBIT-REV"),
            (@"SI[-\s]REV", 85, "SI-REV"), // Standing Instruction

            // General refund patterns
            (@"^REFUND", 80, "REFUND"),
            (@"^RFD[-/]", 85, "RFD-"),

            // Credit reversals (for failed credits)
            (@"CREDIT[-\s]REVERSAL", 90, "CREDIT-REVERSAL"),
            (@"CR[-\s]REV", 85, "CR-REV"),
        };

        // Pattern to extract reference number from reversal description
        private static readonly Regex ReferenceExtractor = new(
            @"(?:REV[-/\s]?)?" + // Optional REV prefix
            @"(?:UPI|NEFT|RTGS|IMPS)?" + // Optional payment method
            @"[-/\s]?" +
            @"([A-Za-z0-9]+)" + // Capture the reference
            @"(?:[-/][A-Za-z0-9]+)*", // Additional reference parts
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        /// <summary>
        /// Detect if a transaction description indicates a reversal
        /// </summary>
        public static (bool IsReversal, string? Pattern, int Confidence) DetectReversal(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return (false, null, 0);

            var normalizedDesc = description.Trim().ToUpperInvariant();

            foreach (var (pattern, confidence, name) in ReversalPatterns)
            {
                if (Regex.IsMatch(normalizedDesc, pattern, RegexOptions.IgnoreCase))
                {
                    return (true, name, confidence);
                }
            }

            return (false, null, 0);
        }

        /// <summary>
        /// Check if a transaction is likely a reversal based on multiple signals
        /// </summary>
        public static bool IsLikelyReversal(string? description, string transactionType, decimal amount)
        {
            // Reversals are typically credits (money coming back)
            if (!string.Equals(transactionType, "credit", StringComparison.OrdinalIgnoreCase))
                return false;

            var (isReversal, _, confidence) = DetectReversal(description);
            return isReversal && confidence >= 80;
        }

        /// <summary>
        /// Extract the original reference number from a reversal description
        /// For example: "REV-UPI/Apple India P/403106523911/Pay" -> "403106523911"
        /// </summary>
        public static string? ExtractOriginalReference(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return null;

            // Try to find a numeric reference that could be the original transaction ref
            var numericRefPattern = new Regex(@"\b(\d{10,})\b"); // 10+ digit reference
            var match = numericRefPattern.Match(description);

            if (match.Success)
                return match.Groups[1].Value;

            // Try alphanumeric pattern
            var alphaNumPattern = new Regex(@"\b([A-Z]{2,4}\d{8,})\b", RegexOptions.IgnoreCase);
            match = alphaNumPattern.Match(description);

            if (match.Success)
                return match.Groups[1].Value;

            return null;
        }

        /// <summary>
        /// Calculate similarity between original and reversal descriptions
        /// Used to match reversal with its original transaction
        /// </summary>
        public static int CalculateDescriptionSimilarity(string? original, string? reversal)
        {
            if (string.IsNullOrWhiteSpace(original) || string.IsNullOrWhiteSpace(reversal))
                return 0;

            // Normalize descriptions
            var origNorm = NormalizeDescription(original);
            var revNorm = NormalizeDescription(reversal);

            // Remove common reversal prefixes for comparison
            revNorm = Regex.Replace(revNorm, @"^(REV|REVERSAL|RV|R)[-/\s]?", "", RegexOptions.IgnoreCase);

            // Check for exact match after normalization
            if (origNorm.Equals(revNorm, StringComparison.OrdinalIgnoreCase))
                return 100;

            // Check if reversal contains the original description
            if (revNorm.Contains(origNorm, StringComparison.OrdinalIgnoreCase) ||
                origNorm.Contains(revNorm, StringComparison.OrdinalIgnoreCase))
                return 90;

            // Extract and compare key parts (payee name, reference numbers)
            var origParts = ExtractKeyParts(original);
            var revParts = ExtractKeyParts(reversal);

            int matchingParts = 0;
            int totalParts = Math.Max(origParts.Count, revParts.Count);

            foreach (var part in origParts)
            {
                if (revParts.Any(rp => rp.Contains(part, StringComparison.OrdinalIgnoreCase) ||
                                       part.Contains(rp, StringComparison.OrdinalIgnoreCase)))
                    matchingParts++;
            }

            if (totalParts == 0) return 0;
            return (int)(matchingParts * 80.0 / totalParts);
        }

        /// <summary>
        /// Calculate match score between a potential original and reversal transaction
        /// </summary>
        public static (int Score, string Reason) CalculateMatchScore(
            decimal originalAmount, DateOnly originalDate, string? originalDesc, string? originalRef,
            decimal reversalAmount, DateOnly reversalDate, string? reversalDesc, string? reversalRef)
        {
            int score = 0;
            var reasons = new List<string>();

            // Amount match (40% weight)
            if (originalAmount == reversalAmount)
            {
                score += 40;
                reasons.Add("Exact amount match");
            }
            else if (Math.Abs(originalAmount - reversalAmount) <= 1)
            {
                score += 35;
                reasons.Add("Amount match (rounding diff)");
            }

            // Date proximity (30% weight)
            int daysDiff = Math.Abs(reversalDate.DayNumber - originalDate.DayNumber);
            if (daysDiff == 0)
            {
                score += 30;
                reasons.Add("Same day");
            }
            else if (daysDiff <= 3)
            {
                score += 25;
                reasons.Add($"{daysDiff} day(s) apart");
            }
            else if (daysDiff <= 7)
            {
                score += 20;
                reasons.Add($"{daysDiff} days apart");
            }
            else if (daysDiff <= 30)
            {
                score += 10;
                reasons.Add($"{daysDiff} days apart");
            }
            else if (daysDiff <= 90)
            {
                score += 5;
                reasons.Add($"{daysDiff} days apart (chargeback)");
            }

            // Reference match (20% weight)
            if (!string.IsNullOrEmpty(originalRef) && !string.IsNullOrEmpty(reversalRef))
            {
                if (originalRef.Equals(reversalRef, StringComparison.OrdinalIgnoreCase))
                {
                    score += 20;
                    reasons.Add("Reference match");
                }
                else if (reversalRef.Contains(originalRef, StringComparison.OrdinalIgnoreCase) ||
                         originalRef.Contains(reversalRef, StringComparison.OrdinalIgnoreCase))
                {
                    score += 15;
                    reasons.Add("Partial reference match");
                }
            }
            else
            {
                // Try extracting reference from descriptions
                var origExtracted = ExtractOriginalReference(originalDesc);
                var revExtracted = ExtractOriginalReference(reversalDesc);

                if (!string.IsNullOrEmpty(origExtracted) && !string.IsNullOrEmpty(revExtracted))
                {
                    if (origExtracted.Equals(revExtracted, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 18;
                        reasons.Add("Extracted reference match");
                    }
                }
            }

            // Description similarity (10% weight)
            int descSimilarity = CalculateDescriptionSimilarity(originalDesc, reversalDesc);
            if (descSimilarity >= 80)
            {
                score += 10;
                reasons.Add("Description match");
            }
            else if (descSimilarity >= 50)
            {
                score += 5;
                reasons.Add("Partial description match");
            }

            return (Math.Min(score, 100), string.Join(", ", reasons));
        }

        private static string NormalizeDescription(string desc)
        {
            // Remove special characters, extra spaces
            return Regex.Replace(desc.Trim(), @"[^a-zA-Z0-9\s]", " ")
                       .Replace("  ", " ")
                       .Trim();
        }

        private static List<string> ExtractKeyParts(string? desc)
        {
            if (string.IsNullOrWhiteSpace(desc))
                return new List<string>();

            // Split by common delimiters and filter meaningful parts
            return desc.Split(new[] { '/', '-', ' ', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                      .Where(p => p.Length >= 3) // Ignore very short parts
                      .Where(p => !IsCommonWord(p))
                      .ToList();
        }

        private static bool IsCommonWord(string word)
        {
            var common = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "REV", "REVERSAL", "UPI", "NEFT", "RTGS", "IMPS", "INR", "PAY", "PAYMENT",
                "TRF", "TRANSFER", "THE", "AND", "FOR", "FROM", "TO", "PVT", "LTD", "PRIVATE", "LIMITED"
            };
            return common.Contains(word);
        }
    }
}
