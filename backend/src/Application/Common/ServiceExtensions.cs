using Core.Common;
using System;

namespace Application.Common
{
    /// <summary>
    /// Extension methods for service-related operations
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Validates that a GUID is not empty/default
        /// </summary>
        public static Result ValidateGuid(Guid id, string parameterName = "ID")
        {
            if (id == default(Guid))
                return Error.Validation($"{parameterName} cannot be default value");

            return Result.Success();
        }

        /// <summary>
        /// Validates pagination parameters
        /// </summary>
        public static Result ValidatePagination(int pageNumber, int pageSize, int maxPageSize = 100)
        {
            if (pageNumber < 1)
                return Error.Validation("Page number must be greater than 0");

            if (pageSize < 1 || pageSize > maxPageSize)
                return Error.Validation($"Page size must be between 1 and {maxPageSize}");

            return Result.Success();
        }
    }
}




