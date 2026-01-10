using Core.Entities.Tax;
using Core.Interfaces.Tax;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Tax
{
    /// <summary>
    /// Repository implementation for Advance Tax (Section 207) operations
    /// </summary>
    public class AdvanceTaxRepository : IAdvanceTaxRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AssessmentColumns = new[]
        {
            "id", "company_id", "financial_year", "assessment_year", "status",
            // YTD actuals
            "ytd_revenue", "ytd_expenses", "ytd_through_date",
            // Projected additional
            "projected_additional_revenue", "projected_additional_expenses",
            // Full year projections
            "projected_revenue", "projected_expenses", "projected_depreciation",
            "projected_other_income", "projected_profit_before_tax",
            // Book to Taxable Reconciliation
            "book_profit",
            "add_book_depreciation", "add_disallowed_40a3", "add_disallowed_40a7",
            "add_disallowed_43b", "add_other_disallowances", "total_additions",
            "less_it_depreciation", "less_deductions_80c", "less_deductions_80d",
            "less_other_deductions", "total_deductions",
            // Tax calculation
            "taxable_income", "tax_regime", "tax_rate", "surcharge_rate", "cess_rate",
            "base_tax", "surcharge", "cess", "total_tax_liability",
            "tds_receivable", "tcs_credit", "advance_tax_already_paid", "mat_credit", "net_tax_payable",
            "interest_234b", "interest_234c", "total_interest",
            "computation_details", "assumptions", "notes",
            // Revision tracking
            "revision_count", "last_revision_date", "last_revision_quarter",
            "created_by", "created_at", "updated_at"
        };

        private static readonly string[] RevisionColumns = new[]
        {
            "id", "assessment_id", "revision_number", "revision_quarter", "revision_date",
            "previous_projected_revenue", "previous_projected_expenses",
            "previous_taxable_income", "previous_total_tax_liability", "previous_net_tax_payable",
            "revised_projected_revenue", "revised_projected_expenses",
            "revised_taxable_income", "revised_total_tax_liability", "revised_net_tax_payable",
            "revenue_variance", "expense_variance", "taxable_income_variance",
            "tax_liability_variance", "net_payable_variance",
            "revision_reason", "notes", "revised_by", "created_at"
        };

        private static readonly string[] ScheduleColumns = new[]
        {
            "id", "assessment_id", "quarter", "due_date",
            "cumulative_percentage", "cumulative_tax_due", "tax_payable_this_quarter",
            "tax_paid_this_quarter", "cumulative_tax_paid",
            "shortfall_amount", "interest_234c", "payment_status",
            "created_at", "updated_at"
        };

        private static readonly string[] PaymentColumns = new[]
        {
            "id", "assessment_id", "schedule_id", "payment_date", "amount",
            "challan_number", "bsr_code", "cin",
            "bank_account_id", "journal_entry_id", "status", "notes",
            "created_by", "created_at", "updated_at"
        };

        private static readonly string[] ScenarioColumns = new[]
        {
            "id", "assessment_id", "scenario_name",
            "revenue_adjustment", "expense_adjustment", "capex_impact", "payroll_change", "other_adjustments",
            "adjusted_taxable_income", "adjusted_tax_liability", "variance_from_base",
            "assumptions", "notes",
            "created_by", "created_at", "updated_at"
        };

        public AdvanceTaxRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== Assessment CRUD ====================

        public async Task<AdvanceTaxAssessment?> GetAssessmentByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AdvanceTaxAssessment>(
                "SELECT * FROM advance_tax_assessments WHERE id = @id",
                new { id });
        }

        public async Task<AdvanceTaxAssessment?> GetAssessmentByCompanyAndFYAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AdvanceTaxAssessment>(
                "SELECT * FROM advance_tax_assessments WHERE company_id = @companyId AND financial_year = @financialYear",
                new { companyId, financialYear });
        }

        public async Task<IEnumerable<AdvanceTaxAssessment>> GetAssessmentsByCompanyAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AdvanceTaxAssessment>(
                "SELECT * FROM advance_tax_assessments WHERE company_id = @companyId ORDER BY financial_year DESC",
                new { companyId });
        }

        public async Task<(IEnumerable<AdvanceTaxAssessment> Items, int TotalCount)> GetAssessmentsPagedAsync(
            int pageNumber,
            int pageSize,
            Guid? companyId = null,
            string? status = null,
            string? financialYear = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var filters = new Dictionary<string, object?>();
            if (companyId.HasValue) filters["company_id"] = companyId.Value;
            if (!string.IsNullOrEmpty(status)) filters["status"] = status;
            if (!string.IsNullOrEmpty(financialYear)) filters["financial_year"] = financialYear;

            var builder = SqlQueryBuilder
                .From("advance_tax_assessments", AssessmentColumns)
                .ApplyFilters(filters)
                .OrderBy("financial_year", true)
                .Paginate(pageNumber, pageSize);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<AdvanceTaxAssessment>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<AdvanceTaxAssessment> CreateAssessmentAsync(AdvanceTaxAssessment assessment)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            assessment.Id = Guid.NewGuid();
            assessment.CreatedAt = DateTime.UtcNow;
            assessment.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO advance_tax_assessments (
                    id, company_id, financial_year, assessment_year, status,
                    ytd_revenue, ytd_expenses, ytd_through_date,
                    projected_additional_revenue, projected_additional_expenses,
                    projected_revenue, projected_expenses, projected_depreciation,
                    projected_other_income, projected_profit_before_tax,
                    book_profit,
                    add_book_depreciation, add_disallowed_40a3, add_disallowed_40a7,
                    add_disallowed_43b, add_other_disallowances, total_additions,
                    less_it_depreciation, less_deductions_80c, less_deductions_80d,
                    less_other_deductions, total_deductions,
                    taxable_income, tax_regime, tax_rate, surcharge_rate, cess_rate,
                    base_tax, surcharge, cess, total_tax_liability,
                    tds_receivable, tcs_credit, advance_tax_already_paid, mat_credit, net_tax_payable,
                    interest_234b, interest_234c, total_interest,
                    computation_details, assumptions, notes,
                    created_by, created_at, updated_at
                ) VALUES (
                    @Id, @CompanyId, @FinancialYear, @AssessmentYear, @Status,
                    @YtdRevenue, @YtdExpenses, @YtdThroughDate,
                    @ProjectedAdditionalRevenue, @ProjectedAdditionalExpenses,
                    @ProjectedRevenue, @ProjectedExpenses, @ProjectedDepreciation,
                    @ProjectedOtherIncome, @ProjectedProfitBeforeTax,
                    @BookProfit,
                    @AddBookDepreciation, @AddDisallowed40A3, @AddDisallowed40A7,
                    @AddDisallowed43B, @AddOtherDisallowances, @TotalAdditions,
                    @LessItDepreciation, @LessDeductions80C, @LessDeductions80D,
                    @LessOtherDeductions, @TotalDeductions,
                    @TaxableIncome, @TaxRegime, @TaxRate, @SurchargeRate, @CessRate,
                    @BaseTax, @Surcharge, @Cess, @TotalTaxLiability,
                    @TdsReceivable, @TcsCredit, @AdvanceTaxAlreadyPaid, @MatCredit, @NetTaxPayable,
                    @Interest234B, @Interest234C, @TotalInterest,
                    @ComputationDetails::jsonb, @Assumptions::jsonb, @Notes,
                    @CreatedBy, @CreatedAt, @UpdatedAt
                )";

            await connection.ExecuteAsync(sql, assessment);
            return assessment;
        }

        public async Task UpdateAssessmentAsync(AdvanceTaxAssessment assessment)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            assessment.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE advance_tax_assessments SET
                    status = @Status,
                    ytd_revenue = @YtdRevenue,
                    ytd_expenses = @YtdExpenses,
                    ytd_through_date = @YtdThroughDate,
                    projected_additional_revenue = @ProjectedAdditionalRevenue,
                    projected_additional_expenses = @ProjectedAdditionalExpenses,
                    projected_revenue = @ProjectedRevenue,
                    projected_expenses = @ProjectedExpenses,
                    projected_depreciation = @ProjectedDepreciation,
                    projected_other_income = @ProjectedOtherIncome,
                    projected_profit_before_tax = @ProjectedProfitBeforeTax,
                    book_profit = @BookProfit,
                    add_book_depreciation = @AddBookDepreciation,
                    add_disallowed_40a3 = @AddDisallowed40A3,
                    add_disallowed_40a7 = @AddDisallowed40A7,
                    add_disallowed_43b = @AddDisallowed43B,
                    add_other_disallowances = @AddOtherDisallowances,
                    total_additions = @TotalAdditions,
                    less_it_depreciation = @LessItDepreciation,
                    less_deductions_80c = @LessDeductions80C,
                    less_deductions_80d = @LessDeductions80D,
                    less_other_deductions = @LessOtherDeductions,
                    total_deductions = @TotalDeductions,
                    taxable_income = @TaxableIncome,
                    tax_regime = @TaxRegime,
                    tax_rate = @TaxRate,
                    surcharge_rate = @SurchargeRate,
                    cess_rate = @CessRate,
                    base_tax = @BaseTax,
                    surcharge = @Surcharge,
                    cess = @Cess,
                    total_tax_liability = @TotalTaxLiability,
                    tds_receivable = @TdsReceivable,
                    tcs_credit = @TcsCredit,
                    advance_tax_already_paid = @AdvanceTaxAlreadyPaid,
                    mat_credit = @MatCredit,
                    net_tax_payable = @NetTaxPayable,
                    interest_234b = @Interest234B,
                    interest_234c = @Interest234C,
                    total_interest = @TotalInterest,
                    computation_details = @ComputationDetails::jsonb,
                    assumptions = @Assumptions::jsonb,
                    notes = @Notes,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, assessment);
        }

        public async Task DeleteAssessmentAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM advance_tax_assessments WHERE id = @id",
                new { id });
        }

        // ==================== Schedule Operations ====================

        public async Task<IEnumerable<AdvanceTaxSchedule>> GetSchedulesByAssessmentAsync(Guid assessmentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AdvanceTaxSchedule>(
                "SELECT * FROM advance_tax_schedules WHERE assessment_id = @assessmentId ORDER BY quarter",
                new { assessmentId });
        }

        public async Task<AdvanceTaxSchedule?> GetScheduleByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AdvanceTaxSchedule>(
                "SELECT * FROM advance_tax_schedules WHERE id = @id",
                new { id });
        }

        public async Task<AdvanceTaxSchedule?> GetScheduleByQuarterAsync(Guid assessmentId, int quarter)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AdvanceTaxSchedule>(
                "SELECT * FROM advance_tax_schedules WHERE assessment_id = @assessmentId AND quarter = @quarter",
                new { assessmentId, quarter });
        }

        public async Task CreateSchedulesAsync(IEnumerable<AdvanceTaxSchedule> schedules)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                INSERT INTO advance_tax_schedules (
                    id, assessment_id, quarter, due_date,
                    cumulative_percentage, cumulative_tax_due, tax_payable_this_quarter,
                    tax_paid_this_quarter, cumulative_tax_paid,
                    shortfall_amount, interest_234c, payment_status,
                    created_at, updated_at
                ) VALUES (
                    @Id, @AssessmentId, @Quarter, @DueDate,
                    @CumulativePercentage, @CumulativeTaxDue, @TaxPayableThisQuarter,
                    @TaxPaidThisQuarter, @CumulativeTaxPaid,
                    @ShortfallAmount, @Interest234C, @PaymentStatus,
                    @CreatedAt, @UpdatedAt
                )";

            foreach (var schedule in schedules)
            {
                schedule.Id = Guid.NewGuid();
                schedule.CreatedAt = DateTime.UtcNow;
                schedule.UpdatedAt = DateTime.UtcNow;
            }

            await connection.ExecuteAsync(sql, schedules);
        }

        public async Task UpdateScheduleAsync(AdvanceTaxSchedule schedule)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            schedule.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE advance_tax_schedules SET
                    cumulative_tax_due = @CumulativeTaxDue,
                    tax_payable_this_quarter = @TaxPayableThisQuarter,
                    tax_paid_this_quarter = @TaxPaidThisQuarter,
                    cumulative_tax_paid = @CumulativeTaxPaid,
                    shortfall_amount = @ShortfallAmount,
                    interest_234c = @Interest234C,
                    payment_status = @PaymentStatus,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, schedule);
        }

        public async Task DeleteSchedulesByAssessmentAsync(Guid assessmentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM advance_tax_schedules WHERE assessment_id = @assessmentId",
                new { assessmentId });
        }

        // ==================== Payment Operations ====================

        public async Task<IEnumerable<AdvanceTaxPayment>> GetPaymentsByAssessmentAsync(Guid assessmentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AdvanceTaxPayment>(
                "SELECT * FROM advance_tax_payments WHERE assessment_id = @assessmentId ORDER BY payment_date DESC",
                new { assessmentId });
        }

        public async Task<AdvanceTaxPayment?> GetPaymentByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AdvanceTaxPayment>(
                "SELECT * FROM advance_tax_payments WHERE id = @id",
                new { id });
        }

        public async Task<AdvanceTaxPayment> CreatePaymentAsync(AdvanceTaxPayment payment)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            payment.Id = Guid.NewGuid();
            payment.CreatedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO advance_tax_payments (
                    id, assessment_id, schedule_id, payment_date, amount,
                    challan_number, bsr_code, cin,
                    bank_account_id, journal_entry_id, status, notes,
                    created_by, created_at, updated_at
                ) VALUES (
                    @Id, @AssessmentId, @ScheduleId, @PaymentDate, @Amount,
                    @ChallanNumber, @BsrCode, @Cin,
                    @BankAccountId, @JournalEntryId, @Status, @Notes,
                    @CreatedBy, @CreatedAt, @UpdatedAt
                )";

            await connection.ExecuteAsync(sql, payment);
            return payment;
        }

        public async Task UpdatePaymentAsync(AdvanceTaxPayment payment)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            payment.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE advance_tax_payments SET
                    schedule_id = @ScheduleId,
                    payment_date = @PaymentDate,
                    amount = @Amount,
                    challan_number = @ChallanNumber,
                    bsr_code = @BsrCode,
                    cin = @Cin,
                    bank_account_id = @BankAccountId,
                    journal_entry_id = @JournalEntryId,
                    status = @Status,
                    notes = @Notes,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, payment);
        }

        public async Task DeletePaymentAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM advance_tax_payments WHERE id = @id",
                new { id });
        }

        // ==================== Scenario Operations ====================

        public async Task<IEnumerable<AdvanceTaxScenario>> GetScenariosByAssessmentAsync(Guid assessmentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AdvanceTaxScenario>(
                "SELECT * FROM advance_tax_scenarios WHERE assessment_id = @assessmentId ORDER BY created_at DESC",
                new { assessmentId });
        }

        public async Task<AdvanceTaxScenario?> GetScenarioByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AdvanceTaxScenario>(
                "SELECT * FROM advance_tax_scenarios WHERE id = @id",
                new { id });
        }

        public async Task<AdvanceTaxScenario> CreateScenarioAsync(AdvanceTaxScenario scenario)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            scenario.Id = Guid.NewGuid();
            scenario.CreatedAt = DateTime.UtcNow;
            scenario.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO advance_tax_scenarios (
                    id, assessment_id, scenario_name,
                    revenue_adjustment, expense_adjustment, capex_impact, payroll_change, other_adjustments,
                    adjusted_taxable_income, adjusted_tax_liability, variance_from_base,
                    assumptions, notes,
                    created_by, created_at, updated_at
                ) VALUES (
                    @Id, @AssessmentId, @ScenarioName,
                    @RevenueAdjustment, @ExpenseAdjustment, @CapexImpact, @PayrollChange, @OtherAdjustments,
                    @AdjustedTaxableIncome, @AdjustedTaxLiability, @VarianceFromBase,
                    @Assumptions::jsonb, @Notes,
                    @CreatedBy, @CreatedAt, @UpdatedAt
                )";

            await connection.ExecuteAsync(sql, scenario);
            return scenario;
        }

        public async Task UpdateScenarioAsync(AdvanceTaxScenario scenario)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            scenario.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE advance_tax_scenarios SET
                    scenario_name = @ScenarioName,
                    revenue_adjustment = @RevenueAdjustment,
                    expense_adjustment = @ExpenseAdjustment,
                    capex_impact = @CapexImpact,
                    payroll_change = @PayrollChange,
                    other_adjustments = @OtherAdjustments,
                    adjusted_taxable_income = @AdjustedTaxableIncome,
                    adjusted_tax_liability = @AdjustedTaxLiability,
                    variance_from_base = @VarianceFromBase,
                    assumptions = @Assumptions::jsonb,
                    notes = @Notes,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, scenario);
        }

        public async Task DeleteScenarioAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM advance_tax_scenarios WHERE id = @id",
                new { id });
        }

        // ==================== Summary & Reports ====================

        public async Task<decimal> GetTotalAdvanceTaxPaidAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<decimal>(@"
                SELECT COALESCE(SUM(p.amount), 0)
                FROM advance_tax_payments p
                JOIN advance_tax_assessments a ON p.assessment_id = a.id
                WHERE a.company_id = @companyId AND a.financial_year = @financialYear AND p.status = 'completed'",
                new { companyId, financialYear });
        }

        public async Task<IEnumerable<AdvanceTaxAssessment>> GetAssessmentsWithPendingPaymentsAsync(Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT DISTINCT a.*
                FROM advance_tax_assessments a
                JOIN advance_tax_schedules s ON s.assessment_id = a.id
                WHERE s.payment_status IN ('pending', 'partial', 'overdue')
                AND s.due_date <= CURRENT_DATE
                AND a.status = 'active'";

            if (companyId.HasValue)
                sql += " AND a.company_id = @companyId";

            sql += " ORDER BY a.financial_year DESC";

            return await connection.QueryAsync<AdvanceTaxAssessment>(sql, new { companyId });
        }

        public async Task<IEnumerable<AdvanceTaxSchedule>> GetUpcomingPaymentDeadlinesAsync(
            DateOnly fromDate,
            DateOnly toDate,
            Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT s.*
                FROM advance_tax_schedules s
                JOIN advance_tax_assessments a ON s.assessment_id = a.id
                WHERE s.due_date >= @fromDate AND s.due_date <= @toDate
                AND s.payment_status IN ('pending', 'partial')
                AND a.status = 'active'";

            if (companyId.HasValue)
                sql += " AND a.company_id = @companyId";

            sql += " ORDER BY s.due_date";

            return await connection.QueryAsync<AdvanceTaxSchedule>(sql, new { fromDate, toDate, companyId });
        }

        // ==================== YTD Ledger Integration ====================

        public async Task<(decimal YtdIncome, decimal YtdExpenses)> GetYtdFinancialsFromLedgerAsync(
            Guid companyId,
            DateOnly fromDate,
            DateOnly toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Standard accounting convention:
            // - Income increases on credit side (credit_amount - debit_amount gives positive income)
            // - Expenses increase on debit side (debit_amount - credit_amount gives positive expense)
            const string sql = @"
                SELECT
                    COALESCE(SUM(CASE WHEN coa.account_type = 'income'
                        THEN jel.credit_amount - jel.debit_amount ELSE 0 END), 0) as YtdIncome,
                    COALESCE(SUM(CASE WHEN coa.account_type = 'expense'
                        THEN jel.debit_amount - jel.credit_amount ELSE 0 END), 0) as YtdExpenses
                FROM journal_entry_lines jel
                JOIN journal_entries je ON jel.journal_entry_id = je.id
                JOIN chart_of_accounts coa ON jel.account_id = coa.id
                WHERE je.company_id = @companyId
                    AND je.journal_date >= @fromDate
                    AND je.journal_date <= @toDate
                    AND je.status = 'posted'
                    AND coa.account_type IN ('income', 'expense')";

            var result = await connection.QueryFirstOrDefaultAsync<(decimal YtdIncome, decimal YtdExpenses)>(
                sql, new { companyId, fromDate, toDate });

            return result;
        }

        // ==================== TDS/TCS Integration ====================

        public async Task<decimal> GetTdsReceivableAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Sum all TDS receivable amounts for the company in the given FY
            // Only include entries that haven't been written off
            const string sql = @"
                SELECT COALESCE(SUM(tds_amount), 0)
                FROM tds_receivable
                WHERE company_id = @companyId
                    AND financial_year = @financialYear
                    AND status NOT IN ('written_off', 'cancelled')";

            return await connection.QuerySingleOrDefaultAsync<decimal>(sql, new { companyId, financialYear });
        }

        public async Task<decimal> GetTcsCreditAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Sum TCS amounts where transaction_type = 'paid' (TCS we paid = credit we can claim)
            // Only include completed/remitted transactions
            const string sql = @"
                SELECT COALESCE(SUM(tcs_amount), 0)
                FROM tcs_transactions
                WHERE company_id = @companyId
                    AND financial_year = @financialYear
                    AND transaction_type = 'paid'
                    AND status NOT IN ('cancelled')";

            return await connection.QuerySingleOrDefaultAsync<decimal>(sql, new { companyId, financialYear });
        }

        // ==================== Revision Operations ====================

        public async Task<IEnumerable<AdvanceTaxRevision>> GetRevisionsByAssessmentAsync(Guid assessmentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AdvanceTaxRevision>(
                "SELECT * FROM advance_tax_revisions WHERE assessment_id = @assessmentId ORDER BY revision_number DESC",
                new { assessmentId });
        }

        public async Task<AdvanceTaxRevision?> GetRevisionByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AdvanceTaxRevision>(
                "SELECT * FROM advance_tax_revisions WHERE id = @id",
                new { id });
        }

        public async Task<AdvanceTaxRevision?> GetLatestRevisionAsync(Guid assessmentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AdvanceTaxRevision>(
                "SELECT * FROM advance_tax_revisions WHERE assessment_id = @assessmentId ORDER BY revision_number DESC LIMIT 1",
                new { assessmentId });
        }

        public async Task<AdvanceTaxRevision> CreateRevisionAsync(AdvanceTaxRevision revision)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            revision.Id = Guid.NewGuid();
            revision.CreatedAt = DateTime.UtcNow;

            // Get next revision number
            var currentCount = await GetRevisionCountAsync(revision.AssessmentId);
            revision.RevisionNumber = currentCount + 1;

            const string sql = @"
                INSERT INTO advance_tax_revisions (
                    id, assessment_id, revision_number, revision_quarter, revision_date,
                    previous_projected_revenue, previous_projected_expenses,
                    previous_taxable_income, previous_total_tax_liability, previous_net_tax_payable,
                    revised_projected_revenue, revised_projected_expenses,
                    revised_taxable_income, revised_total_tax_liability, revised_net_tax_payable,
                    revision_reason, notes, revised_by, created_at
                ) VALUES (
                    @Id, @AssessmentId, @RevisionNumber, @RevisionQuarter, @RevisionDate,
                    @PreviousProjectedRevenue, @PreviousProjectedExpenses,
                    @PreviousTaxableIncome, @PreviousTotalTaxLiability, @PreviousNetTaxPayable,
                    @RevisedProjectedRevenue, @RevisedProjectedExpenses,
                    @RevisedTaxableIncome, @RevisedTotalTaxLiability, @RevisedNetTaxPayable,
                    @RevisionReason, @Notes, @RevisedBy, @CreatedAt
                )
                RETURNING *";

            return await connection.QuerySingleAsync<AdvanceTaxRevision>(sql, revision);
        }

        public async Task<int> GetRevisionCountAsync(Guid assessmentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<int>(
                "SELECT COUNT(*) FROM advance_tax_revisions WHERE assessment_id = @assessmentId",
                new { assessmentId });
        }
    }
}
