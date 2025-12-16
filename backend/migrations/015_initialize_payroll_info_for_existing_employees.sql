-- Migration: Initialize payroll info for existing employees who have salary structures
-- This ensures all employees with salary structures have payroll info records
-- which are required for payroll processing

-- Insert payroll info for employees who have salary structures but no payroll info
INSERT INTO employee_payroll_info (
    id,
    employee_id,
    company_id,
    payroll_type,
    is_pf_applicable,
    is_esi_applicable,
    is_pt_applicable,
    tax_regime,
    is_active,
    created_at,
    updated_at
)
SELECT 
    gen_random_uuid() as id,
    ess.employee_id,
    ess.company_id,
    'employee' as payroll_type, -- Default to employee
    true as is_pf_applicable,   -- Default PF applicable
    false as is_esi_applicable, -- Default ESI not applicable
    true as is_pt_applicable,   -- Default PT applicable
    'new' as tax_regime,         -- Default to new tax regime
    true as is_active,
    NOW() as created_at,
    NOW() as updated_at
FROM employee_salary_structures ess
INNER JOIN employees e ON e.id = ess.employee_id
WHERE ess.is_active = true
  AND ess.effective_from <= CURRENT_DATE
  AND (ess.effective_to IS NULL OR ess.effective_to >= CURRENT_DATE)
  AND NOT EXISTS (
      SELECT 1 
      FROM employee_payroll_info epi 
      WHERE epi.employee_id = ess.employee_id
  )
ON CONFLICT (employee_id) DO NOTHING;

-- Add comment
COMMENT ON TABLE employee_payroll_info IS 'Payroll-specific information for employees. Auto-created when salary structure is created.';



