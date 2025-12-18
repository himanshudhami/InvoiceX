using Microsoft.AspNetCore.Mvc;
using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Application.DTOs.Payroll;
using Application.Services.Payroll;
using AutoMapper;
using System.Text.Json;

namespace WebApi.Controllers.Payroll;

/// <summary>
/// API endpoints for managing calculation rules
/// </summary>
[ApiController]
[Route("api/payroll/[controller]")]
[Produces("application/json")]
public class CalculationRulesController : ControllerBase
{
    private readonly ICalculationRuleRepository _ruleRepository;
    private readonly IFormulaVariableRepository _variableRepository;
    private readonly ICalculationRuleTemplateRepository _templateRepository;
    private readonly CalculationRuleEngine _ruleEngine;
    private readonly IMapper _mapper;

    public CalculationRulesController(
        ICalculationRuleRepository ruleRepository,
        IFormulaVariableRepository variableRepository,
        ICalculationRuleTemplateRepository templateRepository,
        CalculationRuleEngine ruleEngine,
        IMapper mapper)
    {
        _ruleRepository = ruleRepository;
        _variableRepository = variableRepository;
        _templateRepository = templateRepository;
        _ruleEngine = ruleEngine;
        _mapper = mapper;
    }

    /// <summary>
    /// Get all calculation rules for a company
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CalculationRuleDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? companyId)
    {
        IEnumerable<CalculationRule> rules;

        if (companyId.HasValue)
        {
            rules = await _ruleRepository.GetByCompanyIdAsync(companyId.Value);
        }
        else
        {
            rules = await _ruleRepository.GetAllAsync();
        }

        var dtos = _mapper.Map<IEnumerable<CalculationRuleDto>>(rules);
        return Ok(dtos);
    }

    /// <summary>
    /// Get paginated calculation rules
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        [FromQuery] Guid? companyId = null,
        [FromQuery] string? componentType = null,
        [FromQuery] string? componentCode = null)
    {
        var filters = new Dictionary<string, object>();
        if (companyId.HasValue)
            filters["company_id"] = companyId.Value;
        if (!string.IsNullOrEmpty(componentType))
            filters["component_type"] = componentType;
        if (!string.IsNullOrEmpty(componentCode))
            filters["component_code"] = componentCode;

        var (items, totalCount) = await _ruleRepository.GetPagedAsync(
            pageNumber, pageSize, searchTerm, sortBy, sortDescending,
            filters.Any() ? filters : null);

        var dtos = _mapper.Map<IEnumerable<CalculationRuleDto>>(items);

        return Ok(new
        {
            items = dtos,
            totalCount,
            pageNumber,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Get a calculation rule by ID with its conditions
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CalculationRuleDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var rule = await _ruleRepository.GetByIdWithConditionsAsync(id);
        if (rule == null)
            return NotFound($"Rule with ID {id} not found");

        var dto = _mapper.Map<CalculationRuleDto>(rule);
        return Ok(dto);
    }

    /// <summary>
    /// Create a new calculation rule
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CalculationRuleDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateCalculationRuleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validate formula if it's a formula rule
        if (dto.RuleType == "formula" && !string.IsNullOrEmpty(dto.FormulaConfig))
        {
            try
            {
                var config = JsonDocument.Parse(dto.FormulaConfig).RootElement;
                if (config.TryGetProperty("expression", out var exprElement))
                {
                    var validation = _ruleEngine.ValidateFormula(exprElement.GetString() ?? "");
                    if (!validation.IsValid)
                        return BadRequest($"Invalid formula: {validation.ErrorMessage}");
                }
            }
            catch (JsonException ex)
            {
                return BadRequest($"Invalid formula config JSON: {ex.Message}");
            }
        }

        var rule = _mapper.Map<CalculationRule>(dto);
        rule.FormulaConfig = dto.FormulaConfig ?? "{}";
        rule.EffectiveFrom = dto.EffectiveFrom ?? DateTime.UtcNow.Date;

        var created = await _ruleRepository.AddAsync(rule);

        // Add conditions
        if (dto.Conditions?.Any() == true)
        {
            foreach (var condDto in dto.Conditions)
            {
                var condition = new CalculationRuleCondition
                {
                    RuleId = created.Id,
                    ConditionGroup = condDto.ConditionGroup,
                    Field = condDto.Field,
                    Operator = condDto.Operator,
                    Value = condDto.Value ?? "{}"
                };
                await _ruleRepository.AddConditionAsync(condition);
            }
        }

        var result = await _ruleRepository.GetByIdWithConditionsAsync(created.Id);
        var resultDto = _mapper.Map<CalculationRuleDto>(result);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, resultDto);
    }

    /// <summary>
    /// Update a calculation rule
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CalculationRuleDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCalculationRuleDto dto)
    {
        var existing = await _ruleRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound($"Rule with ID {id} not found");

        if (existing.IsSystem)
            return BadRequest("System rules cannot be modified");

        // Validate formula if updating to formula rule
        if (dto.RuleType == "formula" && !string.IsNullOrEmpty(dto.FormulaConfig))
        {
            try
            {
                var config = JsonDocument.Parse(dto.FormulaConfig).RootElement;
                if (config.TryGetProperty("expression", out var exprElement))
                {
                    var validation = _ruleEngine.ValidateFormula(exprElement.GetString() ?? "");
                    if (!validation.IsValid)
                        return BadRequest($"Invalid formula: {validation.ErrorMessage}");
                }
            }
            catch (JsonException ex)
            {
                return BadRequest($"Invalid formula config JSON: {ex.Message}");
            }
        }

        // Update fields
        if (dto.Name != null) existing.Name = dto.Name;
        if (dto.Description != null) existing.Description = dto.Description;
        if (dto.ComponentType != null) existing.ComponentType = dto.ComponentType;
        if (dto.ComponentCode != null) existing.ComponentCode = dto.ComponentCode;
        if (dto.ComponentName != null) existing.ComponentName = dto.ComponentName;
        if (dto.RuleType != null) existing.RuleType = dto.RuleType;
        if (dto.FormulaConfig != null) existing.FormulaConfig = dto.FormulaConfig;
        if (dto.Priority.HasValue) existing.Priority = dto.Priority.Value;
        if (dto.EffectiveFrom.HasValue) existing.EffectiveFrom = dto.EffectiveFrom.Value;
        existing.EffectiveTo = dto.EffectiveTo;
        if (dto.IsActive.HasValue) existing.IsActive = dto.IsActive.Value;
        if (dto.IsTaxable.HasValue) existing.IsTaxable = dto.IsTaxable.Value;
        if (dto.AffectsPfWage.HasValue) existing.AffectsPfWage = dto.AffectsPfWage.Value;
        if (dto.AffectsEsiWage.HasValue) existing.AffectsEsiWage = dto.AffectsEsiWage.Value;

        await _ruleRepository.UpdateAsync(existing);

        // Update conditions if provided
        if (dto.Conditions != null)
        {
            await _ruleRepository.DeleteConditionsByRuleIdAsync(id);
            foreach (var condDto in dto.Conditions)
            {
                var condition = new CalculationRuleCondition
                {
                    RuleId = id,
                    ConditionGroup = condDto.ConditionGroup,
                    Field = condDto.Field,
                    Operator = condDto.Operator,
                    Value = condDto.Value ?? "{}"
                };
                await _ruleRepository.AddConditionAsync(condition);
            }
        }

        var result = await _ruleRepository.GetByIdWithConditionsAsync(id);
        var resultDto = _mapper.Map<CalculationRuleDto>(result);

        return Ok(resultDto);
    }

    /// <summary>
    /// Delete a calculation rule
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _ruleRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound($"Rule with ID {id} not found");

        if (existing.IsSystem)
            return BadRequest("System rules cannot be deleted");

        await _ruleRepository.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Validate a formula expression
    /// </summary>
    [HttpPost("validate-formula")]
    [ProducesResponseType(typeof(FormulaValidationResultDto), 200)]
    public async Task<IActionResult> ValidateFormula([FromBody] ValidateFormulaDto dto)
    {
        var sampleValues = dto.SampleValues ?? new Dictionary<string, decimal>();

        // Add default sample values for common variables
        var variables = await _variableRepository.GetActiveAsync();
        foreach (var v in variables)
        {
            if (!sampleValues.ContainsKey(v.Code))
            {
                sampleValues[v.Code] = 10000m; // Default sample value
            }
        }

        var result = _ruleEngine.ValidateFormula(dto.Expression, sampleValues);

        return Ok(new FormulaValidationResultDto
        {
            IsValid = result.IsValid,
            ErrorMessage = result.ErrorMessage,
            SampleResult = result.SampleResult,
            UsedVariables = result.UsedVariables,
            UnknownVariables = FormulaEvaluator.ExtractVariables(dto.Expression)
                .Where(v => !variables.Any(fv => fv.Code.Equals(v, StringComparison.OrdinalIgnoreCase)))
                .ToList()
        });
    }

    /// <summary>
    /// Preview a rule calculation
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(RuleCalculationPreviewDto), 200)]
    public async Task<IActionResult> PreviewCalculation([FromBody] PreviewRuleCalculationDto dto)
    {
        var variables = dto.CustomValues ?? new Dictionary<string, decimal>();

        // If employee ID provided, load their actual values
        // (would need to inject salary structure repository here)

        // Add default values for required variables if not provided
        var allVariables = await _variableRepository.GetActiveAsync();
        foreach (var v in allVariables)
        {
            if (!variables.ContainsKey(v.Code))
            {
                variables[v.Code] = v.Code switch
                {
                    "basic" => 50000m,
                    "hra" => 20000m,
                    "da" => 5000m,
                    "monthly_gross" => 80000m,
                    "pf_wage" => 55000m,
                    "pf_ceiling" => 15000m,
                    "pf_employee_rate" => 12m,
                    "working_days" => 30m,
                    "payable_days" => 30m,
                    _ => 0m
                };
            }
        }

        CalculationRule rule;
        if (dto.RuleId.HasValue)
        {
            var existingRule = await _ruleRepository.GetByIdWithConditionsAsync(dto.RuleId.Value);
            if (existingRule == null)
                return NotFound($"Rule with ID {dto.RuleId} not found");
            rule = existingRule;
        }
        else if (dto.Rule != null)
        {
            rule = _mapper.Map<CalculationRule>(dto.Rule);
            rule.FormulaConfig = dto.Rule.FormulaConfig ?? "{}";
        }
        else
        {
            return BadRequest("Either RuleId or Rule must be provided");
        }

        try
        {
            var result = _ruleEngine.PreviewRule(rule, variables);
            return Ok(new RuleCalculationPreviewDto
            {
                Success = true,
                Result = result.Value,
                InputValues = variables,
                Steps = result.Steps.Select(s => new CalculationStepDto
                {
                    Description = s.Description,
                    Expression = s.Expression,
                    Value = s.Value
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return Ok(new RuleCalculationPreviewDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                InputValues = variables
            });
        }
    }

    /// <summary>
    /// Get all formula variables
    /// </summary>
    [HttpGet("variables")]
    [ProducesResponseType(typeof(IEnumerable<FormulaVariableDto>), 200)]
    public async Task<IActionResult> GetVariables()
    {
        var variables = await _variableRepository.GetActiveAsync();
        var dtos = _mapper.Map<IEnumerable<FormulaVariableDto>>(variables);
        return Ok(dtos);
    }

    /// <summary>
    /// Get all rule templates
    /// </summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IEnumerable<CalculationRuleTemplateDto>), 200)]
    public async Task<IActionResult> GetTemplates([FromQuery] string? category = null)
    {
        IEnumerable<CalculationRuleTemplate> templates;

        if (!string.IsNullOrEmpty(category))
        {
            templates = await _templateRepository.GetByCategoryAsync(category);
        }
        else
        {
            templates = await _templateRepository.GetAllAsync();
        }

        var dtos = _mapper.Map<IEnumerable<CalculationRuleTemplateDto>>(templates);
        return Ok(dtos);
    }

    /// <summary>
    /// Create a rule from a template
    /// </summary>
    [HttpPost("from-template/{templateId}")]
    [ProducesResponseType(typeof(CalculationRuleDto), 201)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateFromTemplate(Guid templateId, [FromQuery] Guid companyId, [FromQuery] string? name = null)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null)
            return NotFound($"Template with ID {templateId} not found");

        var rule = new CalculationRule
        {
            CompanyId = companyId,
            Name = name ?? template.Name,
            Description = template.Description,
            ComponentType = template.ComponentType,
            ComponentCode = template.ComponentCode,
            RuleType = template.RuleType,
            FormulaConfig = template.FormulaConfig,
            Priority = 100,
            EffectiveFrom = DateTime.UtcNow.Date,
            IsActive = true,
            IsSystem = false
        };

        var created = await _ruleRepository.AddAsync(rule);

        var result = await _ruleRepository.GetByIdWithConditionsAsync(created.Id);
        var resultDto = _mapper.Map<CalculationRuleDto>(result);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, resultDto);
    }
}
