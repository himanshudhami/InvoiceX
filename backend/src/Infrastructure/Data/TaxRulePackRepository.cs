using System.Text.Json;
using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;

namespace Infrastructure.Data;

public class TaxRulePackRepository : ITaxRulePackRepository
{
    private readonly string _connectionString;

    public TaxRulePackRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    #region Basic CRUD

    public async Task<TaxRulePack?> GetByIdAsync(Guid id)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT id, pack_code, pack_name, financial_year, version,
                   source_notification, description, status,
                   income_tax_slabs, standard_deductions, rebate_thresholds,
                   cess_rates, surcharge_rates, tds_rates, pf_esi_rates,
                   professional_tax_config, gst_rates,
                   created_at, created_by, updated_at, updated_by,
                   activated_at, activated_by
            FROM tax_rule_packs
            WHERE id = @Id";

        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return result != null ? MapToEntity(result) : null;
    }

    public async Task<IEnumerable<TaxRulePack>> GetAllAsync()
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT id, pack_code, pack_name, financial_year, version,
                   source_notification, description, status,
                   income_tax_slabs, standard_deductions, rebate_thresholds,
                   cess_rates, surcharge_rates, tds_rates, pf_esi_rates,
                   professional_tax_config, gst_rates,
                   created_at, created_by, updated_at, updated_by,
                   activated_at, activated_by
            FROM tax_rule_packs
            ORDER BY financial_year DESC, version DESC";

        var results = await conn.QueryAsync<dynamic>(sql);
        return results.Select(r => (TaxRulePack)MapToEntity(r)).ToList();
    }

    public async Task<TaxRulePack> CreateAsync(TaxRulePack rulePack)
    {
        using var conn = CreateConnection();
        const string sql = @"
            INSERT INTO tax_rule_packs (
                id, pack_code, pack_name, financial_year, version,
                source_notification, description, status,
                income_tax_slabs, standard_deductions, rebate_thresholds,
                cess_rates, surcharge_rates, tds_rates, pf_esi_rates,
                professional_tax_config, gst_rates,
                created_at, created_by
            ) VALUES (
                @Id, @PackCode, @PackName, @FinancialYear, @Version,
                @SourceNotification, @Description, @Status,
                @IncomeTaxSlabs::jsonb, @StandardDeductions::jsonb, @RebateThresholds::jsonb,
                @CessRates::jsonb, @SurchargeRates::jsonb, @TdsRates::jsonb, @PfEsiRates::jsonb,
                @ProfessionalTaxConfig::jsonb, @GstRates::jsonb,
                @CreatedAt, @CreatedBy
            )
            RETURNING id";

        rulePack.Id = rulePack.Id == Guid.Empty ? Guid.NewGuid() : rulePack.Id;
        rulePack.CreatedAt = DateTime.UtcNow;

        await conn.ExecuteAsync(sql, new
        {
            rulePack.Id,
            rulePack.PackCode,
            rulePack.PackName,
            rulePack.FinancialYear,
            rulePack.Version,
            rulePack.SourceNotification,
            rulePack.Description,
            rulePack.Status,
            IncomeTaxSlabs = rulePack.IncomeTaxSlabs?.RootElement.GetRawText(),
            StandardDeductions = rulePack.StandardDeductions?.RootElement.GetRawText(),
            RebateThresholds = rulePack.RebateThresholds?.RootElement.GetRawText(),
            CessRates = rulePack.CessRates?.RootElement.GetRawText(),
            SurchargeRates = rulePack.SurchargeRates?.RootElement.GetRawText(),
            TdsRates = rulePack.TdsRates?.RootElement.GetRawText(),
            PfEsiRates = rulePack.PfEsiRates?.RootElement.GetRawText(),
            ProfessionalTaxConfig = rulePack.ProfessionalTaxConfig?.RootElement.GetRawText(),
            GstRates = rulePack.GstRates?.RootElement.GetRawText(),
            rulePack.CreatedAt,
            rulePack.CreatedBy
        });

        return rulePack;
    }

    public async Task<TaxRulePack> UpdateAsync(TaxRulePack rulePack)
    {
        using var conn = CreateConnection();
        const string sql = @"
            UPDATE tax_rule_packs SET
                pack_code = @PackCode,
                pack_name = @PackName,
                source_notification = @SourceNotification,
                description = @Description,
                status = @Status,
                income_tax_slabs = @IncomeTaxSlabs::jsonb,
                standard_deductions = @StandardDeductions::jsonb,
                rebate_thresholds = @RebateThresholds::jsonb,
                cess_rates = @CessRates::jsonb,
                surcharge_rates = @SurchargeRates::jsonb,
                tds_rates = @TdsRates::jsonb,
                pf_esi_rates = @PfEsiRates::jsonb,
                professional_tax_config = @ProfessionalTaxConfig::jsonb,
                gst_rates = @GstRates::jsonb,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy
            WHERE id = @Id";

        rulePack.UpdatedAt = DateTime.UtcNow;

        await conn.ExecuteAsync(sql, new
        {
            rulePack.Id,
            rulePack.PackCode,
            rulePack.PackName,
            rulePack.SourceNotification,
            rulePack.Description,
            rulePack.Status,
            IncomeTaxSlabs = rulePack.IncomeTaxSlabs?.RootElement.GetRawText(),
            StandardDeductions = rulePack.StandardDeductions?.RootElement.GetRawText(),
            RebateThresholds = rulePack.RebateThresholds?.RootElement.GetRawText(),
            CessRates = rulePack.CessRates?.RootElement.GetRawText(),
            SurchargeRates = rulePack.SurchargeRates?.RootElement.GetRawText(),
            TdsRates = rulePack.TdsRates?.RootElement.GetRawText(),
            PfEsiRates = rulePack.PfEsiRates?.RootElement.GetRawText(),
            ProfessionalTaxConfig = rulePack.ProfessionalTaxConfig?.RootElement.GetRawText(),
            GstRates = rulePack.GstRates?.RootElement.GetRawText(),
            rulePack.UpdatedAt,
            rulePack.UpdatedBy
        });

        return rulePack;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = CreateConnection();
        const string sql = "DELETE FROM tax_rule_packs WHERE id = @Id AND status = 'draft'";
        var affected = await conn.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    #endregion

    #region Specialized Queries

    public async Task<TaxRulePack?> GetActivePackForFyAsync(string financialYear)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT id, pack_code, pack_name, financial_year, version,
                   source_notification, description, status,
                   income_tax_slabs, standard_deductions, rebate_thresholds,
                   cess_rates, surcharge_rates, tds_rates, pf_esi_rates,
                   professional_tax_config, gst_rates,
                   created_at, created_by, updated_at, updated_by,
                   activated_at, activated_by
            FROM tax_rule_packs
            WHERE financial_year = @FinancialYear AND status = 'active'
            ORDER BY version DESC
            LIMIT 1";

        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { FinancialYear = financialYear });
        return result != null ? MapToEntity(result) : null;
    }

    public async Task<IEnumerable<TaxRulePack>> GetByFinancialYearAsync(string financialYear)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT id, pack_code, pack_name, financial_year, version,
                   source_notification, description, status,
                   income_tax_slabs, standard_deductions, rebate_thresholds,
                   cess_rates, surcharge_rates, tds_rates, pf_esi_rates,
                   professional_tax_config, gst_rates,
                   created_at, created_by, updated_at, updated_by,
                   activated_at, activated_by
            FROM tax_rule_packs
            WHERE financial_year = @FinancialYear
            ORDER BY version DESC";

        var results = await conn.QueryAsync<dynamic>(sql, new { FinancialYear = financialYear });
        return results.Select(r => (TaxRulePack)MapToEntity(r)).ToList();
    }

    public async Task<IEnumerable<TaxRulePack>> GetByStatusAsync(string status)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT id, pack_code, pack_name, financial_year, version,
                   source_notification, description, status,
                   created_at, created_by, updated_at, updated_by,
                   activated_at, activated_by
            FROM tax_rule_packs
            WHERE status = @Status
            ORDER BY financial_year DESC, version DESC";

        var results = await conn.QueryAsync<dynamic>(sql, new { Status = status });
        return results.Select(r => (TaxRulePack)MapToEntity(r)).ToList();
    }

    public async Task<TaxRulePack?> GetLatestVersionAsync(string financialYear)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT id, pack_code, pack_name, financial_year, version,
                   source_notification, description, status,
                   income_tax_slabs, standard_deductions, rebate_thresholds,
                   cess_rates, surcharge_rates, tds_rates, pf_esi_rates,
                   professional_tax_config, gst_rates,
                   created_at, created_by, updated_at, updated_by,
                   activated_at, activated_by
            FROM tax_rule_packs
            WHERE financial_year = @FinancialYear
            ORDER BY version DESC
            LIMIT 1";

        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { FinancialYear = financialYear });
        return result != null ? MapToEntity(result) : null;
    }

    #endregion

    #region TDS Section Rates

    public async Task<IEnumerable<TdsSectionRate>> GetTdsSectionRatesAsync(Guid rulePackId)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT id, rule_pack_id, section_code, section_name,
                   rate_individual, rate_company, rate_no_pan,
                   threshold_amount, threshold_type, payee_types,
                   is_active, notes, effective_from, effective_to, created_at
            FROM tds_section_rates
            WHERE rule_pack_id = @RulePackId
            ORDER BY section_code";

        var results = await conn.QueryAsync<dynamic>(sql, new { RulePackId = rulePackId });
        return results.Select(r => (TdsSectionRate)MapToTdsSectionRate(r)).ToList();
    }

    public async Task<TdsSectionRate?> GetTdsSectionRateAsync(Guid rulePackId, string sectionCode)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT id, rule_pack_id, section_code, section_name,
                   rate_individual, rate_company, rate_no_pan,
                   threshold_amount, threshold_type, payee_types,
                   is_active, notes, effective_from, effective_to, created_at
            FROM tds_section_rates
            WHERE rule_pack_id = @RulePackId AND section_code = @SectionCode
            LIMIT 1";

        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { RulePackId = rulePackId, SectionCode = sectionCode });
        return result != null ? MapToTdsSectionRate(result) : null;
    }

    public async Task<TdsSectionRate> CreateTdsSectionRateAsync(TdsSectionRate rate)
    {
        using var conn = CreateConnection();
        const string sql = @"
            INSERT INTO tds_section_rates (
                id, rule_pack_id, section_code, section_name,
                rate_individual, rate_company, rate_no_pan,
                threshold_amount, threshold_type, payee_types,
                is_active, notes, effective_from, effective_to
            ) VALUES (
                @Id, @RulePackId, @SectionCode, @SectionName,
                @RateIndividual, @RateCompany, @RateNoPan,
                @ThresholdAmount, @ThresholdType, @PayeeTypes,
                @IsActive, @Notes, @EffectiveFrom, @EffectiveTo
            )";

        rate.Id = rate.Id == Guid.Empty ? Guid.NewGuid() : rate.Id;

        await conn.ExecuteAsync(sql, new
        {
            rate.Id,
            rate.RulePackId,
            rate.SectionCode,
            rate.SectionName,
            rate.RateIndividual,
            rate.RateCompany,
            rate.RateNoPan,
            rate.ThresholdAmount,
            rate.ThresholdType,
            rate.PayeeTypes,
            rate.IsActive,
            rate.Notes,
            rate.EffectiveFrom,
            rate.EffectiveTo
        });

        return rate;
    }

    public async Task<TdsSectionRate> UpdateTdsSectionRateAsync(TdsSectionRate rate)
    {
        using var conn = CreateConnection();
        const string sql = @"
            UPDATE tds_section_rates SET
                section_name = @SectionName,
                rate_individual = @RateIndividual,
                rate_company = @RateCompany,
                rate_no_pan = @RateNoPan,
                threshold_amount = @ThresholdAmount,
                threshold_type = @ThresholdType,
                payee_types = @PayeeTypes,
                is_active = @IsActive,
                notes = @Notes,
                effective_from = @EffectiveFrom,
                effective_to = @EffectiveTo
            WHERE id = @Id";

        await conn.ExecuteAsync(sql, rate);
        return rate;
    }

    #endregion

    #region Usage Logging

    public async Task<RulePackUsageLog> LogUsageAsync(RulePackUsageLog log)
    {
        using var conn = CreateConnection();
        const string sql = @"
            INSERT INTO rule_pack_usage_log (
                id, rule_pack_id, company_id, computation_type, computation_id,
                computation_date, rules_snapshot, input_amount, computed_tax,
                effective_rate, computed_at, computed_by
            ) VALUES (
                @Id, @RulePackId, @CompanyId, @ComputationType, @ComputationId,
                @ComputationDate, @RulesSnapshot::jsonb, @InputAmount, @ComputedTax,
                @EffectiveRate, @ComputedAt, @ComputedBy
            )";

        log.Id = log.Id == Guid.Empty ? Guid.NewGuid() : log.Id;
        log.ComputedAt = DateTime.UtcNow;

        await conn.ExecuteAsync(sql, new
        {
            log.Id,
            log.RulePackId,
            log.CompanyId,
            log.ComputationType,
            log.ComputationId,
            log.ComputationDate,
            RulesSnapshot = log.RulesSnapshot?.RootElement.GetRawText(),
            log.InputAmount,
            log.ComputedTax,
            log.EffectiveRate,
            log.ComputedAt,
            log.ComputedBy
        });

        return log;
    }

    public async Task<IEnumerable<RulePackUsageLog>> GetUsageLogsAsync(Guid rulePackId, int limit = 100)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT id, rule_pack_id, company_id, computation_type, computation_id,
                   computation_date, rules_snapshot, input_amount, computed_tax,
                   effective_rate, computed_at, computed_by
            FROM rule_pack_usage_log
            WHERE rule_pack_id = @RulePackId
            ORDER BY computed_at DESC
            LIMIT @Limit";

        var results = await conn.QueryAsync<dynamic>(sql, new { RulePackId = rulePackId, Limit = limit });
        return results.Select(r => (RulePackUsageLog)MapToUsageLog(r)).ToList();
    }

    public async Task<IEnumerable<RulePackUsageLog>> GetUsageLogsByCompanyAsync(Guid companyId, int limit = 100)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT id, rule_pack_id, company_id, computation_type, computation_id,
                   computation_date, rules_snapshot, input_amount, computed_tax,
                   effective_rate, computed_at, computed_by
            FROM rule_pack_usage_log
            WHERE company_id = @CompanyId
            ORDER BY computed_at DESC
            LIMIT @Limit";

        var results = await conn.QueryAsync<dynamic>(sql, new { CompanyId = companyId, Limit = limit });
        return results.Select(r => (RulePackUsageLog)MapToUsageLog(r)).ToList();
    }

    #endregion

    #region Activation

    public async Task<bool> ActivatePackAsync(Guid id, string activatedBy)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // Get the pack to activate
            var pack = await GetByIdAsync(id);
            if (pack == null) return false;

            // Supersede any currently active pack for the same FY
            const string supersedeSql = @"
                UPDATE tax_rule_packs
                SET status = 'superseded', updated_at = @Now
                WHERE financial_year = @FinancialYear AND status = 'active'";

            await conn.ExecuteAsync(supersedeSql, new
            {
                Now = DateTime.UtcNow,
                pack.FinancialYear
            }, transaction);

            // Activate the new pack
            const string activateSql = @"
                UPDATE tax_rule_packs
                SET status = 'active', activated_at = @Now, activated_by = @ActivatedBy, updated_at = @Now
                WHERE id = @Id";

            await conn.ExecuteAsync(activateSql, new
            {
                Id = id,
                Now = DateTime.UtcNow,
                ActivatedBy = activatedBy
            }, transaction);

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> SupersedePackAsync(Guid id)
    {
        using var conn = CreateConnection();
        const string sql = @"
            UPDATE tax_rule_packs
            SET status = 'superseded', updated_at = @Now
            WHERE id = @Id AND status = 'active'";

        var affected = await conn.ExecuteAsync(sql, new { Id = id, Now = DateTime.UtcNow });
        return affected > 0;
    }

    #endregion

    #region Mapping Helpers

    private static TaxRulePack MapToEntity(dynamic row)
    {
        return new TaxRulePack
        {
            Id = row.id,
            PackCode = row.pack_code,
            PackName = row.pack_name,
            FinancialYear = row.financial_year,
            Version = row.version,
            SourceNotification = row.source_notification,
            Description = row.description,
            Status = row.status,
            IncomeTaxSlabs = ParseJsonDocument(row.income_tax_slabs),
            StandardDeductions = ParseJsonDocument(row.standard_deductions),
            RebateThresholds = ParseJsonDocument(row.rebate_thresholds),
            CessRates = ParseJsonDocument(row.cess_rates),
            SurchargeRates = ParseJsonDocument(row.surcharge_rates),
            TdsRates = ParseJsonDocument(row.tds_rates),
            PfEsiRates = ParseJsonDocument(row.pf_esi_rates),
            ProfessionalTaxConfig = ParseJsonDocument(row.professional_tax_config),
            GstRates = ParseJsonDocument(row.gst_rates),
            CreatedAt = row.created_at,
            CreatedBy = row.created_by,
            UpdatedAt = row.updated_at,
            UpdatedBy = row.updated_by,
            ActivatedAt = row.activated_at,
            ActivatedBy = row.activated_by
        };
    }

    private static TdsSectionRate MapToTdsSectionRate(dynamic row)
    {
        return new TdsSectionRate
        {
            Id = row.id,
            RulePackId = row.rule_pack_id,
            SectionCode = row.section_code,
            SectionName = row.section_name,
            RateIndividual = row.rate_individual,
            RateCompany = row.rate_company,
            RateNoPan = row.rate_no_pan,
            ThresholdAmount = row.threshold_amount,
            ThresholdType = row.threshold_type ?? "per_transaction",
            PayeeTypes = row.payee_types as string[],
            IsActive = row.is_active,
            Notes = row.notes,
            EffectiveFrom = row.effective_from,
            EffectiveTo = row.effective_to,
            CreatedAt = row.created_at
        };
    }

    private static RulePackUsageLog MapToUsageLog(dynamic row)
    {
        return new RulePackUsageLog
        {
            Id = row.id,
            RulePackId = row.rule_pack_id,
            CompanyId = row.company_id,
            ComputationType = row.computation_type,
            ComputationId = row.computation_id,
            ComputationDate = row.computation_date,
            RulesSnapshot = ParseJsonDocument(row.rules_snapshot),
            InputAmount = row.input_amount,
            ComputedTax = row.computed_tax,
            EffectiveRate = row.effective_rate,
            ComputedAt = row.computed_at,
            ComputedBy = row.computed_by
        };
    }

    private static JsonDocument? ParseJsonDocument(object? value)
    {
        if (value == null || value == DBNull.Value) return null;
        var jsonString = value.ToString();
        if (string.IsNullOrEmpty(jsonString)) return null;
        return JsonDocument.Parse(jsonString);
    }

    #endregion
}
