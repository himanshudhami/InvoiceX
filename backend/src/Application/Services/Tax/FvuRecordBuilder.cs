using System.Text;
using Application.DTOs.Tax.Fvu;
using Core.Interfaces.Tax;

namespace Application.Services.Tax
{
    /// <summary>
    /// Builds individual FVU record strings for TDS return files.
    /// Each method builds a specific record type (FH, BH, CD, DD) as per NSDL specification.
    ///
    /// Single Responsibility: This class is only responsible for formatting data into FVU record strings.
    /// </summary>
    public class FvuRecordBuilder : IFvuRecordBuilder
    {
        private const char Delimiter = FvuConstants.FieldDelimiter;

        /// <summary>
        /// Build File Header record (FH)
        /// </summary>
        public string BuildFileHeader(
            int lineNumber,
            string tan,
            string fileType,
            string uploadType,
            int totalBatches)
        {
            var sb = new StringBuilder();

            // Field 1: Line number
            sb.Append(lineNumber);
            sb.Append(Delimiter);

            // Field 2: Record type
            sb.Append(FvuConstants.RecordTypes.FileHeader);
            sb.Append(Delimiter);

            // Field 3: File type (NS1 for 26Q/24Q/27Q, TS1 for 27EQ)
            sb.Append(fileType);
            sb.Append(Delimiter);

            // Field 4: Upload type (R=Regular, C=Correction)
            sb.Append(uploadType);
            sb.Append(Delimiter);

            // Field 5: File creation date (ddmmyyyy)
            sb.Append(FormatDate(DateTime.Now));
            sb.Append(Delimiter);

            // Field 6: File sequence number
            sb.Append(1);
            sb.Append(Delimiter);

            // Field 7: Uploader type (D=Deductor)
            sb.Append(FvuConstants.UploaderTypes.Deductor);
            sb.Append(Delimiter);

            // Field 8: TAN of deductor
            sb.Append(tan.ToUpperInvariant());
            sb.Append(Delimiter);

            // Field 9: Total number of batches
            sb.Append(totalBatches);
            sb.Append(Delimiter);

            // Field 10: Return Preparation Utility name
            sb.Append(FvuConstants.UtilityName);
            sb.Append(Delimiter);

            // Fields 11-18: Hash fields (not applicable, leave empty)
            for (int i = 0; i < 8; i++)
            {
                sb.Append(Delimiter);
            }

            return sb.ToString().TrimEnd(Delimiter);
        }

        /// <summary>
        /// Build Batch Header record (BH) for Form 26Q
        /// </summary>
        public string BuildBatchHeader26Q(
            int lineNumber,
            int batchNumber,
            int challanCount,
            DeductorDetails deductor,
            ResponsiblePersonDetails responsiblePerson,
            string financialYear,
            string quarter,
            decimal totalDepositAmount,
            string? previousToken = null)
        {
            var sb = new StringBuilder();

            // Field 1: Line number
            sb.Append(lineNumber);
            sb.Append(Delimiter);

            // Field 2: Record type
            sb.Append(FvuConstants.RecordTypes.BatchHeader);
            sb.Append(Delimiter);

            // Field 3: Batch number
            sb.Append(batchNumber);
            sb.Append(Delimiter);

            // Field 4: Count of challans
            sb.Append(challanCount);
            sb.Append(Delimiter);

            // Field 5: Form number
            sb.Append("26Q");
            sb.Append(Delimiter);

            // Fields 6-8: Not applicable
            sb.Append(Delimiter);
            sb.Append(Delimiter);
            sb.Append(Delimiter);

            // Field 9: Token of previous regular statement
            sb.Append(previousToken ?? string.Empty);
            sb.Append(Delimiter);

            // Fields 10-14: Not applicable
            for (int i = 0; i < 5; i++)
                sb.Append(Delimiter);

            // Field 15: PAN of deductor
            sb.Append(deductor.Pan.ToUpperInvariant());
            sb.Append(Delimiter);

            // Field 16: Assessment Year (YYYYMM)
            sb.Append(FvuConstants.GetAssessmentYear(financialYear));
            sb.Append(Delimiter);

            // Field 17: Financial Year (YYYYMM)
            sb.Append(FvuConstants.GetFinancialYearCode(financialYear));
            sb.Append(Delimiter);

            // Field 18: Period/Quarter
            sb.Append(quarter);
            sb.Append(Delimiter);

            // Field 19: Name of deductor
            sb.Append(SanitizeText(deductor.Name, 75));
            sb.Append(Delimiter);

            // Field 20: Branch/Division
            sb.Append(SanitizeText(deductor.BranchName, 75));
            sb.Append(Delimiter);

            // Fields 21-25: Deductor address (5 lines)
            sb.Append(SanitizeText(deductor.FlatNo, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(deductor.BuildingName, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(deductor.RoadName, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(deductor.Area, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(deductor.City, 25));
            sb.Append(Delimiter);

            // Field 26: State code
            sb.Append(FvuConstants.GetStateCode(deductor.State));
            sb.Append(Delimiter);

            // Field 27: Pincode
            sb.Append(deductor.Pincode);
            sb.Append(Delimiter);

            // Field 28: Email
            sb.Append(deductor.Email);
            sb.Append(Delimiter);

            // Field 29: STD code
            sb.Append(Delimiter);

            // Field 30: Telephone
            sb.Append(Delimiter);

            // Field 31: Change of address since last return
            sb.Append("N");
            sb.Append(Delimiter);

            // Field 32: Deductor type
            sb.Append(GetDeductorTypeCode(deductor.DeductorType));
            sb.Append(Delimiter);

            // Field 33: Name of responsible person
            sb.Append(SanitizeText(responsiblePerson.Name, 75));
            sb.Append(Delimiter);

            // Field 34: Designation
            sb.Append(SanitizeText(responsiblePerson.Designation, 40));
            sb.Append(Delimiter);

            // Fields 35-39: Responsible person address
            sb.Append(SanitizeText(responsiblePerson.FlatNo, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(responsiblePerson.BuildingName, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(responsiblePerson.RoadName, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(responsiblePerson.Area, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(responsiblePerson.City, 25));
            sb.Append(Delimiter);

            // Field 40: Responsible person state code
            sb.Append(FvuConstants.GetStateCode(responsiblePerson.State));
            sb.Append(Delimiter);

            // Field 41: Responsible person pincode
            sb.Append(responsiblePerson.Pincode);
            sb.Append(Delimiter);

            // Field 42: Responsible person email
            sb.Append(responsiblePerson.Email);
            sb.Append(Delimiter);

            // Field 43: Mobile number
            sb.Append(ExtractPhoneNumber(responsiblePerson.Phone));
            sb.Append(Delimiter);

            // Field 44: STD code
            sb.Append(Delimiter);

            // Field 45: Telephone
            sb.Append(Delimiter);

            // Field 46: Change of address
            sb.Append("N");
            sb.Append(Delimiter);

            // Field 47: Batch total deposit amount
            sb.Append(FormatAmount(totalDepositAmount));
            sb.Append(Delimiter);

            // Fields 48-50: Not applicable
            for (int i = 0; i < 3; i++)
                sb.Append(Delimiter);

            // Field 51: AO approval
            sb.Append("N");
            sb.Append(Delimiter);

            // Field 52: Regular statement filed earlier
            sb.Append(string.IsNullOrEmpty(previousToken) ? "N" : "Y");
            sb.Append(Delimiter);

            // Field 53: Last deductor type (N/A)
            sb.Append(Delimiter);

            // Fields 54-57: Govt specific fields (N/A for company)
            for (int i = 0; i < 4; i++)
                sb.Append(Delimiter);

            // Field 58: Ministry name other
            sb.Append(Delimiter);

            // Field 59: TAN registration number
            sb.Append(Delimiter);

            // Fields 60-61: PAO/DDO registration
            sb.Append(Delimiter);
            sb.Append(Delimiter);

            // Fields 62-67: Alternate contact details
            for (int i = 0; i < 6; i++)
                sb.Append(Delimiter);

            // Field 68: Account office ID
            sb.Append(Delimiter);

            // Field 69: Record hash (N/A)
            // Don't append delimiter at end

            return sb.ToString().TrimEnd(Delimiter);
        }

        /// <summary>
        /// Build Challan Detail record (CD)
        /// </summary>
        public string BuildChallanDetail(
            int lineNumber,
            int batchNumber,
            int challanRecordNumber,
            TdsChallanDetail challan,
            int deducteeCount,
            decimal totalTdsFromDeductees)
        {
            var sb = new StringBuilder();

            bool isNilChallan = challan.TotalAmount == 0;

            // Field 1: Line number
            sb.Append(lineNumber);
            sb.Append(Delimiter);

            // Field 2: Record type
            sb.Append(FvuConstants.RecordTypes.ChallanDetail);
            sb.Append(Delimiter);

            // Field 3: Batch number
            sb.Append(batchNumber);
            sb.Append(Delimiter);

            // Field 4: Challan detail record number
            sb.Append(challanRecordNumber);
            sb.Append(Delimiter);

            // Field 5: Count of deductee records
            sb.Append(deducteeCount);
            sb.Append(Delimiter);

            // Field 6: NIL challan indicator
            sb.Append(isNilChallan ? "Y" : "N");
            sb.Append(Delimiter);

            // Fields 7-10: Not applicable
            for (int i = 0; i < 4; i++)
                sb.Append(Delimiter);

            // Field 11: Last bank challan number (N/A)
            sb.Append(Delimiter);

            // Field 12: Bank challan number
            sb.Append(isNilChallan ? string.Empty : challan.ChallanNumber);
            sb.Append(Delimiter);

            // Field 13: Last transfer voucher (N/A)
            sb.Append(Delimiter);

            // Field 14: DDO serial number (for govt)
            sb.Append(Delimiter);

            // Field 15: BSR code / Form 24G receipt
            sb.Append(isNilChallan ? string.Empty : challan.BsrCode);
            sb.Append(Delimiter);

            // Fields 16-17: Verification (N/A)
            sb.Append(Delimiter);
            sb.Append(Delimiter);

            // Field 18: Date of challan/transfer voucher
            sb.Append(FormatDate(challan.DepositDate));
            sb.Append(Delimiter);

            // Fields 19-20: Filler
            sb.Append(Delimiter);
            sb.Append(Delimiter);

            // Field 21: Section (only up to FY 2012-13, leave empty for later)
            sb.Append(Delimiter);

            // Field 22: Income tax amount (TDS)
            sb.Append(FormatAmount(challan.TdsAmount));
            sb.Append(Delimiter);

            // Field 23: Surcharge amount
            sb.Append(FormatAmount(challan.Surcharge));
            sb.Append(Delimiter);

            // Field 24: Education cess amount
            sb.Append(FormatAmount(challan.Cess));
            sb.Append(Delimiter);

            // Field 25: Interest amount
            sb.Append(FormatAmount(challan.Interest));
            sb.Append(Delimiter);

            // Field 26: Other amount
            sb.Append("0");
            sb.Append(Delimiter);

            // Field 27: Total deposit amount
            sb.Append(FormatAmount(challan.TotalAmount));
            sb.Append(Delimiter);

            // Field 28: Last total deposit (N/A)
            sb.Append(Delimiter);

            // Field 29: Total tax as per deductee annexure
            sb.Append(FormatAmount(totalTdsFromDeductees));
            sb.Append(Delimiter);

            // Fields 30-35: Sum of deductee taxes (populated by caller or left as totals)
            sb.Append(FormatAmount(totalTdsFromDeductees)); // Income tax
            sb.Append(Delimiter);
            sb.Append("0"); // Surcharge
            sb.Append(Delimiter);
            sb.Append("0"); // Cess
            sb.Append(Delimiter);
            sb.Append(FormatAmount(totalTdsFromDeductees)); // Total TDS
            sb.Append(Delimiter);
            sb.Append("0"); // Interest
            sb.Append(Delimiter);
            sb.Append("0"); // Other
            sb.Append(Delimiter);

            // Field 36: Cheque/DD number (N/A for current FY)
            sb.Append(Delimiter);

            // Field 37: Book entry indicator
            sb.Append(challan.BookEntryFlag == "Y" ? "Y" : "N");
            sb.Append(Delimiter);

            // Field 38: Remarks
            sb.Append(SanitizeText(challan.Remarks, 50));
            sb.Append(Delimiter);

            // Field 39: Fee section 234E
            sb.Append(FormatAmount(challan.LateFee));
            sb.Append(Delimiter);

            // Field 40: Minor head of challan
            sb.Append(challan.MinorHead ?? FvuConstants.MinorHeadCodes.TdsPayableByTaxpayer);
            sb.Append(Delimiter);

            // Field 41: Record hash (N/A)

            return sb.ToString().TrimEnd(Delimiter);
        }

        /// <summary>
        /// Build Deductee Detail record (DD) for Form 26Q
        /// </summary>
        public string BuildDeducteeDetail26Q(
            int lineNumber,
            int batchNumber,
            int challanRecordNumber,
            int deducteeRecordNumber,
            Form26QDeducteeRecord deductee)
        {
            var sb = new StringBuilder();

            // Field 1: Line number
            sb.Append(lineNumber);
            sb.Append(Delimiter);

            // Field 2: Record type
            sb.Append(FvuConstants.RecordTypes.DeducteeDetail);
            sb.Append(Delimiter);

            // Field 3: Batch number
            sb.Append(batchNumber);
            sb.Append(Delimiter);

            // Field 4: Challan detail record number
            sb.Append(challanRecordNumber);
            sb.Append(Delimiter);

            // Field 5: Deductee detail record number
            sb.Append(deducteeRecordNumber);
            sb.Append(Delimiter);

            // Field 6: Mode
            sb.Append("O");
            sb.Append(Delimiter);

            // Field 7: Employee serial number (N/A for 26Q)
            sb.Append(Delimiter);

            // Field 8: Deductee code (01=Company, 02=Other)
            sb.Append(FvuConstants.GetDeducteeCode(deductee.DeducteePan));
            sb.Append(Delimiter);

            // Field 9: Last employee PAN (N/A)
            sb.Append(Delimiter);

            // Field 10: Deductee PAN
            sb.Append(GetValidPan(deductee.DeducteePan));
            sb.Append(Delimiter);

            // Field 11: Last deductee reference (N/A)
            sb.Append(Delimiter);

            // Field 12: Deductee reference number
            sb.Append(deductee.SerialNumber);
            sb.Append(Delimiter);

            // Field 13: Name of deductee
            sb.Append(SanitizeText(deductee.DeducteeName, 75));
            sb.Append(Delimiter);

            // Field 14: Income tax deducted
            sb.Append(FormatAmountWithDecimals(deductee.TdsAmount));
            sb.Append(Delimiter);

            // Field 15: Surcharge deducted
            sb.Append("0.00");
            sb.Append(Delimiter);

            // Field 16: Education cess deducted
            sb.Append("0.00");
            sb.Append(Delimiter);

            // Field 17: Total income tax deducted
            sb.Append(FormatAmountWithDecimals(deductee.TdsAmount));
            sb.Append(Delimiter);

            // Field 18: Last total TDS (N/A)
            sb.Append(Delimiter);

            // Field 19: Total value of purchase (N/A)
            sb.Append(Delimiter);

            // Field 20: Total tax deposited
            sb.Append(FormatAmountWithDecimals(deductee.TdsAmount));
            sb.Append(Delimiter);

            // Field 21: Last total tax deposited (N/A)
            sb.Append(Delimiter);

            // Field 22: Amount paid/credited
            sb.Append(FormatAmountWithDecimals(deductee.GrossAmount));
            sb.Append(Delimiter);

            // Field 23: Date of payment/credit
            sb.Append(FormatDate(deductee.PaymentDate));
            sb.Append(Delimiter);

            // Field 24: Date of tax deduction
            sb.Append(deductee.TdsAmount > 0 ? FormatDate(deductee.PaymentDate) : string.Empty);
            sb.Append(Delimiter);

            // Field 25: Date of deposit (N/A)
            sb.Append(Delimiter);

            // Field 26: Rate of tax deducted
            sb.Append(FormatRate(deductee.TdsRate));
            sb.Append(Delimiter);

            // Field 27: Grossing up indicator (N/A)
            sb.Append(Delimiter);

            // Field 28: Book entry indicator (N/A for FY 2013-14+)
            sb.Append(Delimiter);

            // Field 29: Date of TDS certificate (N/A)
            sb.Append(Delimiter);

            // Field 30: Remarks 1 (Reason for lower/nil deduction)
            sb.Append(deductee.ReasonForLowerDeduction ?? string.Empty);
            sb.Append(Delimiter);

            // Fields 31-32: Remarks 2, 3 (N/A)
            sb.Append(Delimiter);
            sb.Append(Delimiter);

            // Field 33: Section code
            sb.Append(FvuConstants.GetFvuSectionCode(deductee.TdsSection));
            sb.Append(Delimiter);

            // Field 34: AO certificate number (mandatory if Remarks1 = A)
            sb.Append(deductee.CertificateNumber ?? string.Empty);
            sb.Append(Delimiter);

            // Fields 35-38: Filler
            for (int i = 0; i < 4; i++)
                sb.Append(Delimiter);

            // Field 39: Record hash (N/A)

            return sb.ToString().TrimEnd(Delimiter);
        }

        /// <summary>
        /// Build Batch Header record (BH) for Form 24Q
        /// </summary>
        public string BuildBatchHeader24Q(
            int lineNumber,
            int batchNumber,
            int challanCount,
            DeductorDetails deductor,
            ResponsiblePersonDetails responsiblePerson,
            string financialYear,
            string quarter,
            decimal totalDepositAmount,
            string? previousToken = null)
        {
            // Similar to 26Q with form number = "24Q"
            var sb = new StringBuilder();

            // Field 1: Line number
            sb.Append(lineNumber);
            sb.Append(Delimiter);

            // Field 2: Record type
            sb.Append(FvuConstants.RecordTypes.BatchHeader);
            sb.Append(Delimiter);

            // Field 3: Batch number
            sb.Append(batchNumber);
            sb.Append(Delimiter);

            // Field 4: Count of challans
            sb.Append(challanCount);
            sb.Append(Delimiter);

            // Field 5: Form number
            sb.Append("24Q");
            sb.Append(Delimiter);

            // Continue with remaining fields same as 26Q
            // Fields 6-8: Not applicable
            sb.Append(Delimiter);
            sb.Append(Delimiter);
            sb.Append(Delimiter);

            // Field 9: Token of previous regular statement
            sb.Append(previousToken ?? string.Empty);
            sb.Append(Delimiter);

            // Fields 10-14: Not applicable
            for (int i = 0; i < 5; i++)
                sb.Append(Delimiter);

            // Field 15: PAN of deductor
            sb.Append(deductor.Pan.ToUpperInvariant());
            sb.Append(Delimiter);

            // Field 16: Assessment Year
            sb.Append(FvuConstants.GetAssessmentYear(financialYear));
            sb.Append(Delimiter);

            // Field 17: Financial Year
            sb.Append(FvuConstants.GetFinancialYearCode(financialYear));
            sb.Append(Delimiter);

            // Field 18: Period/Quarter
            sb.Append(quarter);
            sb.Append(Delimiter);

            // Field 19: Name of deductor
            sb.Append(SanitizeText(deductor.Name, 75));
            sb.Append(Delimiter);

            // Field 20: Branch/Division
            sb.Append(SanitizeText(deductor.BranchName, 75));
            sb.Append(Delimiter);

            // Fields 21-25: Deductor address
            sb.Append(SanitizeText(deductor.FlatNo, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(deductor.BuildingName, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(deductor.RoadName, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(deductor.Area, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(deductor.City, 25));
            sb.Append(Delimiter);

            // Field 26: State code
            sb.Append(FvuConstants.GetStateCode(deductor.State));
            sb.Append(Delimiter);

            // Field 27: Pincode
            sb.Append(deductor.Pincode);
            sb.Append(Delimiter);

            // Field 28: Email
            sb.Append(deductor.Email);
            sb.Append(Delimiter);

            // Fields 29-30: Phone
            sb.Append(Delimiter);
            sb.Append(Delimiter);

            // Field 31: Change of address
            sb.Append("N");
            sb.Append(Delimiter);

            // Field 32: Deductor type
            sb.Append(GetDeductorTypeCode(deductor.DeductorType));
            sb.Append(Delimiter);

            // Field 33: Responsible person name
            sb.Append(SanitizeText(responsiblePerson.Name, 75));
            sb.Append(Delimiter);

            // Field 34: Designation
            sb.Append(SanitizeText(responsiblePerson.Designation, 40));
            sb.Append(Delimiter);

            // Fields 35-39: Responsible person address
            sb.Append(SanitizeText(responsiblePerson.FlatNo, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(responsiblePerson.BuildingName, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(responsiblePerson.RoadName, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(responsiblePerson.Area, 25));
            sb.Append(Delimiter);
            sb.Append(SanitizeText(responsiblePerson.City, 25));
            sb.Append(Delimiter);

            // Field 40: State code
            sb.Append(FvuConstants.GetStateCode(responsiblePerson.State));
            sb.Append(Delimiter);

            // Field 41: Pincode
            sb.Append(responsiblePerson.Pincode);
            sb.Append(Delimiter);

            // Field 42: Email
            sb.Append(responsiblePerson.Email);
            sb.Append(Delimiter);

            // Field 43: Mobile
            sb.Append(ExtractPhoneNumber(responsiblePerson.Phone));
            sb.Append(Delimiter);

            // Fields 44-45: Phone
            sb.Append(Delimiter);
            sb.Append(Delimiter);

            // Field 46: Change of address
            sb.Append("N");
            sb.Append(Delimiter);

            // Field 47: Total deposit
            sb.Append(FormatAmount(totalDepositAmount));
            sb.Append(Delimiter);

            // Fields 48-69: Same as 26Q (remaining fields)
            for (int i = 0; i < 22; i++)
            {
                if (i == 2) // Field 51: AO approval
                    sb.Append("N");
                else if (i == 3) // Field 52: Previous statement
                    sb.Append(string.IsNullOrEmpty(previousToken) ? "N" : "Y");
                else
                    sb.Append(string.Empty);

                if (i < 21)
                    sb.Append(Delimiter);
            }

            return sb.ToString().TrimEnd(Delimiter);
        }

        /// <summary>
        /// Build Deductee Detail record (DD) for Form 24Q (Employee record)
        /// </summary>
        public string BuildDeducteeDetail24Q(
            int lineNumber,
            int batchNumber,
            int challanRecordNumber,
            int deducteeRecordNumber,
            Form24QEmployeeRecord employee)
        {
            var sb = new StringBuilder();

            // Field 1: Line number
            sb.Append(lineNumber);
            sb.Append(Delimiter);

            // Field 2: Record type
            sb.Append(FvuConstants.RecordTypes.DeducteeDetail);
            sb.Append(Delimiter);

            // Field 3: Batch number
            sb.Append(batchNumber);
            sb.Append(Delimiter);

            // Field 4: Challan detail record number
            sb.Append(challanRecordNumber);
            sb.Append(Delimiter);

            // Field 5: Deductee detail record number
            sb.Append(deducteeRecordNumber);
            sb.Append(Delimiter);

            // Field 6: Mode
            sb.Append("O");
            sb.Append(Delimiter);

            // Field 7: Employee serial number
            sb.Append(employee.SerialNumber);
            sb.Append(Delimiter);

            // Field 8: Deductee code (02 for employees)
            sb.Append(FvuConstants.DeducteeCodes.Other);
            sb.Append(Delimiter);

            // Field 9: Last employee PAN (N/A)
            sb.Append(Delimiter);

            // Field 10: Employee PAN
            sb.Append(GetValidPan(employee.EmployeePan));
            sb.Append(Delimiter);

            // Field 11: Last deductee reference (N/A)
            sb.Append(Delimiter);

            // Field 12: Deductee reference number
            sb.Append(employee.EmployeeCode);
            sb.Append(Delimiter);

            // Field 13: Name of employee
            sb.Append(SanitizeText(employee.EmployeeName, 75));
            sb.Append(Delimiter);

            // Field 14: Income tax deducted
            sb.Append(FormatAmountWithDecimals(employee.TdsDeducted));
            sb.Append(Delimiter);

            // Field 15: Surcharge
            sb.Append("0.00");
            sb.Append(Delimiter);

            // Field 16: Cess
            sb.Append("0.00");
            sb.Append(Delimiter);

            // Field 17: Total TDS
            sb.Append(FormatAmountWithDecimals(employee.TdsDeducted));
            sb.Append(Delimiter);

            // Field 18: Last total TDS (N/A)
            sb.Append(Delimiter);

            // Field 19: Total value (N/A)
            sb.Append(Delimiter);

            // Field 20: Total tax deposited
            sb.Append(FormatAmountWithDecimals(employee.TdsDeposited));
            sb.Append(Delimiter);

            // Field 21: Last total deposited (N/A)
            sb.Append(Delimiter);

            // Field 22: Gross salary
            sb.Append(FormatAmountWithDecimals(employee.GrossSalary));
            sb.Append(Delimiter);

            // Field 23: Date of payment (last day of last month in quarter)
            var lastMonth = employee.MonthlyDetails.LastOrDefault();
            if (lastMonth != null)
            {
                var lastDayOfMonth = new DateOnly(lastMonth.Year, lastMonth.Month, 1).AddMonths(1).AddDays(-1);
                sb.Append(FormatDate(lastDayOfMonth));
            }
            sb.Append(Delimiter);

            // Field 24: Date of deduction
            if (employee.TdsDeducted > 0 && lastMonth != null)
            {
                var lastDayOfMonth = new DateOnly(lastMonth.Year, lastMonth.Month, 1).AddMonths(1).AddDays(-1);
                sb.Append(FormatDate(lastDayOfMonth));
            }
            sb.Append(Delimiter);

            // Field 25: Date of deposit (N/A)
            sb.Append(Delimiter);

            // Field 26: Rate (not applicable for 24Q, leave empty)
            sb.Append(Delimiter);

            // Fields 27-29: N/A
            sb.Append(Delimiter);
            sb.Append(Delimiter);
            sb.Append(Delimiter);

            // Field 30: Remarks 1 (N/A for salary)
            sb.Append(Delimiter);

            // Fields 31-32: N/A
            sb.Append(Delimiter);
            sb.Append(Delimiter);

            // Field 33: Section code (92 for salary)
            sb.Append("92");
            sb.Append(Delimiter);

            // Fields 34-37: N/A
            for (int i = 0; i < 4; i++)
                sb.Append(Delimiter);

            // Field 388A: Other TDS/TCS Credit (CBDT Feb 2025)
            // Total of TDS from other sources + TCS credit claimed by employee
            sb.Append(FormatAmountWithDecimals(employee.OtherTdsTcsCredit));
            sb.Append(Delimiter);

            // Field 39: Record hash (N/A)

            return sb.ToString().TrimEnd(Delimiter);
        }

        #region Helper Methods

        private static string FormatDate(DateTime date)
        {
            return date.ToString("ddMMyyyy");
        }

        private static string FormatDate(DateOnly date)
        {
            return date.ToString("ddMMyyyy");
        }

        private static string FormatAmount(decimal amount)
        {
            // FVU requires integer amounts (no decimals)
            return Math.Round(amount, 0).ToString("0");
        }

        private static string FormatAmountWithDecimals(decimal amount)
        {
            // Some fields allow 2 decimal places
            return amount.ToString("0.00");
        }

        private static string FormatRate(decimal rate)
        {
            // Rate with 4 decimal places
            return rate.ToString("0.0000");
        }

        private static string SanitizeText(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Remove special characters that could break the file format
            var sanitized = text
                .Replace("^", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("\t", " ")
                .Trim();

            if (sanitized.Length > maxLength)
                sanitized = sanitized.Substring(0, maxLength);

            return sanitized;
        }

        private static string GetValidPan(string? pan)
        {
            if (string.IsNullOrEmpty(pan))
                return FvuConstants.PanStatus.NotAvailable;

            var cleanPan = pan.Trim().ToUpperInvariant();

            if (cleanPan == FvuConstants.PanStatus.NotAvailable ||
                cleanPan == FvuConstants.PanStatus.Applied ||
                cleanPan == FvuConstants.PanStatus.Invalid)
                return cleanPan;

            if (FvuConstants.IsValidPan(cleanPan))
                return cleanPan;

            return FvuConstants.PanStatus.Invalid;
        }

        private static string ExtractPhoneNumber(string? phone)
        {
            if (string.IsNullOrEmpty(phone))
                return string.Empty;

            // Extract only digits, take last 10 for mobile
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.Length >= 10)
                return digits.Substring(digits.Length - 10);

            return digits;
        }

        private static string GetDeductorTypeCode(string? deductorType)
        {
            if (string.IsNullOrEmpty(deductorType))
                return FvuConstants.DeductorTypes.Company; // Default

            return deductorType.ToUpperInvariant() switch
            {
                "COMPANY" or "PRIVATE LIMITED" or "LLP" => FvuConstants.DeductorTypes.Company,
                "FIRM" or "PARTNERSHIP" => FvuConstants.DeductorTypes.Firm,
                "INDIVIDUAL" or "PROPRIETOR" => FvuConstants.DeductorTypes.Individual,
                "HUF" => FvuConstants.DeductorTypes.Huf,
                "TRUST" => FvuConstants.DeductorTypes.Trust,
                "GOVERNMENT" or "CENTRAL GOVERNMENT" => FvuConstants.DeductorTypes.CentralGovernment,
                "STATE GOVERNMENT" => FvuConstants.DeductorTypes.StateGovernment,
                _ => FvuConstants.DeductorTypes.Company
            };
        }

        #endregion
    }

    /// <summary>
    /// Interface for FVU record builder (for DI and testing)
    /// </summary>
    public interface IFvuRecordBuilder
    {
        string BuildFileHeader(int lineNumber, string tan, string fileType, string uploadType, int totalBatches);

        string BuildBatchHeader26Q(int lineNumber, int batchNumber, int challanCount,
            DeductorDetails deductor, ResponsiblePersonDetails responsiblePerson,
            string financialYear, string quarter, decimal totalDepositAmount, string? previousToken = null);

        string BuildBatchHeader24Q(int lineNumber, int batchNumber, int challanCount,
            DeductorDetails deductor, ResponsiblePersonDetails responsiblePerson,
            string financialYear, string quarter, decimal totalDepositAmount, string? previousToken = null);

        string BuildChallanDetail(int lineNumber, int batchNumber, int challanRecordNumber,
            TdsChallanDetail challan, int deducteeCount, decimal totalTdsFromDeductees);

        string BuildDeducteeDetail26Q(int lineNumber, int batchNumber, int challanRecordNumber,
            int deducteeRecordNumber, Form26QDeducteeRecord deductee);

        string BuildDeducteeDetail24Q(int lineNumber, int batchNumber, int challanRecordNumber,
            int deducteeRecordNumber, Form24QEmployeeRecord employee);
    }
}
