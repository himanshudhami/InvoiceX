using Core.Common;
using FluentValidation;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Common
{
    /// <summary>
    /// Helper class for consistent validation handling
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates a DTO and returns a Result indicating success or validation errors
        /// </summary>
        /// <typeparam name="T">The DTO type</typeparam>
        /// <param name="validator">The FluentValidation validator instance</param>
        /// <param name="dto">The DTO to validate</param>
        /// <returns>A Result indicating validation success or failure with error messages</returns>
        public static async Task<Result> ValidateAsync<T>(IValidator<T> validator, T dto)
        {
            if (dto == null)
                return Error.Validation("DTO cannot be null");

            var validationResult = await validator.ValidateAsync(dto);
            
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Error.Validation(errors);
            }

            return Result.Success();
        }

        /// <summary>
        /// Validates a DTO synchronously and returns a Result indicating success or validation errors
        /// </summary>
        /// <typeparam name="T">The DTO type</typeparam>
        /// <param name="validator">The FluentValidation validator instance</param>
        /// <param name="dto">The DTO to validate</param>
        /// <returns>A Result indicating validation success or failure with error messages</returns>
        public static Result Validate<T>(IValidator<T> validator, T dto)
        {
            if (dto == null)
                return Error.Validation("DTO cannot be null");

            var validationResult = validator.Validate(dto);
            
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Error.Validation(errors);
            }

            return Result.Success();
        }
    }
}




