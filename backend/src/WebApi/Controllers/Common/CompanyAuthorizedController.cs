using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Text.Json;

namespace WebApi.Controllers.Common
{
    /// <summary>
    /// Base controller that provides company and employee context from JWT claims.
    /// All controllers handling company-specific data should inherit from this.
    ///
    /// Authorization logic:
    /// - Super Admin: Can access data from ALL companies (system-wide access).
    /// - Company Admin/HR: Can access data from ASSIGNED companies only.
    /// - Regular users: Can only access data from their own company (from JWT claims).
    /// </summary>
    [Authorize]
    public abstract class CompanyAuthorizedController : ControllerBase
    {
        /// <summary>
        /// Gets the company ID from the current user's JWT claims.
        /// Returns null if the claim is not present or invalid.
        /// </summary>
        protected Guid? CurrentCompanyId
        {
            get
            {
                var claim = User.FindFirst("company_id");
                if (claim != null && Guid.TryParse(claim.Value, out var companyId))
                    return companyId;
                return null;
            }
        }

        /// <summary>
        /// Gets the employee ID from the current user's JWT claims.
        /// Returns null if the claim is not present or invalid.
        /// </summary>
        protected Guid? CurrentEmployeeId
        {
            get
            {
                var claim = User.FindFirst("employee_id");
                if (claim != null && Guid.TryParse(claim.Value, out var employeeId))
                    return employeeId;
                return null;
            }
        }

        /// <summary>
        /// Gets the user ID from the current user's JWT claims.
        /// Returns null if the claim is not present or invalid.
        /// </summary>
        protected Guid? CurrentUserId
        {
            get
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
                if (claim != null && Guid.TryParse(claim.Value, out var userId))
                    return userId;
                return null;
            }
        }

        /// <summary>
        /// Gets the role from the current user's JWT claims.
        /// </summary>
        protected string? CurrentRole
        {
            get
            {
                return User.FindFirst(ClaimTypes.Role)?.Value;
            }
        }

        /// <summary>
        /// Checks if the current user is a Super Admin with access to ALL companies.
        /// Super Admin has system-wide access.
        /// </summary>
        protected bool IsSuperAdmin
        {
            get
            {
                return User.FindFirst("is_super_admin")?.Value == "true" ||
                       User.FindFirst("access_scope")?.Value == "all_companies";
            }
        }

        /// <summary>
        /// Checks if the current user has access to assigned companies (Company Admin/HR).
        /// </summary>
        protected bool HasAssignedCompanyAccess
        {
            get
            {
                var scope = User.FindFirst("access_scope")?.Value;
                return scope == "all_companies" || scope == "assigned_companies";
            }
        }

        /// <summary>
        /// Gets the list of company IDs the user has access to (from JWT).
        /// Returns empty list for regular users.
        /// </summary>
        protected List<Guid> AssignedCompanyIds
        {
            get
            {
                var claim = User.FindFirst("company_ids");
                if (claim != null)
                {
                    try
                    {
                        var ids = JsonSerializer.Deserialize<List<string>>(claim.Value);
                        return ids?.Select(Guid.Parse).ToList() ?? new List<Guid>();
                    }
                    catch
                    {
                        return new List<Guid>();
                    }
                }
                return new List<Guid>();
            }
        }

        /// <summary>
        /// Checks if the current user has Admin or HR role.
        /// Admin/HR users can access data across assigned companies.
        /// </summary>
        protected bool IsAdminOrHR
        {
            get
            {
                // Check role, access_scope, or is_super_admin for maximum compatibility
                var role = CurrentRole;
                return role == "Admin" || role == "HR" || IsSuperAdmin || HasAssignedCompanyAccess;
            }
        }

        /// <summary>
        /// Gets the effective company ID for filtering data.
        /// - For Super Admin: Returns query parameter if provided, otherwise their active company_id from JWT.
        /// - For Company Admin/HR: Returns query parameter if it's in their assigned companies, otherwise their active company_id from JWT.
        /// - For regular users: Returns the company ID from JWT claims (mandatory).
        /// </summary>
        /// <param name="queryCompanyId">Optional company ID from query parameter</param>
        /// <returns>The company ID to use for filtering, or null if no valid company found</returns>
        protected Guid? GetEffectiveCompanyId(Guid? queryCompanyId = null)
        {
            // Super Admin: Can use any query parameter or their default company
            if (IsSuperAdmin)
            {
                return queryCompanyId ?? CurrentCompanyId;
            }

            // Company Admin/HR: Can only use query parameter if it's in their assigned companies
            if (HasAssignedCompanyAccess)
            {
                if (queryCompanyId.HasValue)
                {
                    // Validate that the requested company is in their assigned companies
                    if (CanAccessCompany(queryCompanyId.Value))
                    {
                        return queryCompanyId.Value;
                    }
                    // Fall back to their default company if query param is not accessible
                    return CurrentCompanyId;
                }
                return CurrentCompanyId;
            }

            // Regular users must use their JWT company ID
            return CurrentCompanyId;
        }

        /// <summary>
        /// Gets the effective company ID and validates access.
        /// Returns null with error message if the query company is not accessible.
        /// </summary>
        /// <param name="queryCompanyId">Optional company ID from query parameter</param>
        /// <returns>Tuple with the company ID to use (or null) and an error message (if access denied)</returns>
        protected (Guid? CompanyId, string? ErrorMessage) GetEffectiveCompanyIdWithValidation(Guid? queryCompanyId = null)
        {
            // Super Admin: Can use any company
            if (IsSuperAdmin)
            {
                return (queryCompanyId ?? CurrentCompanyId, null);
            }

            // Company Admin/HR with query parameter: validate access
            if (queryCompanyId.HasValue)
            {
                if (!CanAccessCompany(queryCompanyId.Value))
                {
                    return (null, "Access denied. You do not have permission to access this company's data.");
                }
                return (queryCompanyId.Value, null);
            }

            // Use default company from JWT
            return (CurrentCompanyId, null);
        }

        /// <summary>
        /// Checks if the current user requires company isolation.
        /// Admin/HR users do not require company isolation.
        /// </summary>
        protected bool RequiresCompanyIsolation => !IsAdminOrHR;

        /// <summary>
        /// Validates company access for a resource.
        /// - Super Admin: Always allowed (all companies)
        /// - Company Admin/HR: Allowed if resource belongs to one of their assigned companies
        /// - Regular users: Only allowed if resource belongs to their company
        /// </summary>
        /// <param name="resourceCompanyId">The company ID of the resource</param>
        /// <returns>True if access is allowed, false otherwise</returns>
        protected bool HasCompanyAccess(Guid? resourceCompanyId)
        {
            // Super Admin has access to everything
            if (IsSuperAdmin)
                return true;

            // Allow if resource has no company (legacy data)
            if (!resourceCompanyId.HasValue)
                return true;

            // Company Admin/HR: Check if resource's company is in their assigned companies
            if (HasAssignedCompanyAccess)
            {
                var assignedIds = AssignedCompanyIds;
                if (assignedIds.Any())
                {
                    return assignedIds.Contains(resourceCompanyId.Value);
                }
            }

            // Regular users: Must match their single company
            if (CurrentCompanyId == null)
                return false;

            return resourceCompanyId.Value == CurrentCompanyId.Value;
        }

        /// <summary>
        /// Validates if the user can access data for a specific company.
        /// Use this when checking if a query parameter company is accessible.
        /// </summary>
        /// <param name="companyId">The company ID to check access for</param>
        /// <returns>True if user can access this company's data</returns>
        protected bool CanAccessCompany(Guid companyId)
        {
            // Super Admin can access any company
            if (IsSuperAdmin)
                return true;

            // Company Admin/HR: Check against assigned companies
            if (HasAssignedCompanyAccess)
            {
                var assignedIds = AssignedCompanyIds;
                if (assignedIds.Any())
                {
                    return assignedIds.Contains(companyId);
                }
            }

            // Regular users: Must match their single company
            return CurrentCompanyId.HasValue && CurrentCompanyId.Value == companyId;
        }

        /// <summary>
        /// Returns a 403 Forbidden response if company ID is not found in token.
        /// Only applies to non-Admin/HR users.
        /// </summary>
        protected IActionResult CompanyIdNotFoundResponse()
        {
            return StatusCode(403, new { error = "Company ID not found in token" });
        }

        /// <summary>
        /// Returns a 403 Forbidden response for access to resources from a different company.
        /// </summary>
        protected IActionResult AccessDeniedDifferentCompanyResponse(string resourceType = "Resource")
        {
            return StatusCode(403, new { error = $"Access denied. {resourceType} belongs to a different company." });
        }

        /// <summary>
        /// Returns a 403 Forbidden response for attempts to create/modify resources for a different company.
        /// </summary>
        protected IActionResult CannotModifyDifferentCompanyResponse(string action = "modify resource for")
        {
            return StatusCode(403, new { error = $"Cannot {action} a different company" });
        }
    }
}
