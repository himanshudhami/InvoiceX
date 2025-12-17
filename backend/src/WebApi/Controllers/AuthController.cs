using System.Security.Claims;
using Application.DTOs.Auth;
using Application.Interfaces;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Authentication and user management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Authenticate user with email and password
        /// </summary>
        /// <param name="dto">Login credentials</param>
        /// <returns>Access token and refresh token</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(TokenResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var ipAddress = GetIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            var result = await _authService.LoginAsync(dto, ipAddress, userAgent);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Unauthorized => Unauthorized(new { message = result.Error.Message }),
                    ErrorType.Validation => BadRequest(new { message = result.Error.Message }),
                    _ => BadRequest(new { message = result.Error.Message })
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Register a new user (Admin only)
        /// </summary>
        /// <param name="dto">User registration details</param>
        /// <returns>Created user information</returns>
        [HttpPost("register")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(UserInfoDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _authService.RegisterAsync(dto, currentUserId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Conflict => Conflict(new { message = result.Error.Message }),
                    ErrorType.Validation => BadRequest(new { message = result.Error.Message }),
                    _ => BadRequest(new { message = result.Error.Message })
                };
            }

            return CreatedAtAction(nameof(GetUser), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="dto">Refresh token</param>
        /// <returns>New access token and refresh token</returns>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(TokenResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            var ipAddress = GetIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            var result = await _authService.RefreshTokenAsync(dto.RefreshToken, ipAddress, userAgent);

            if (result.IsFailure)
            {
                return Unauthorized(new { message = result.Error!.Message });
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Logout (revoke current refresh token)
        /// </summary>
        /// <param name="dto">Refresh token to revoke</param>
        [HttpPost("logout")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
        {
            var result = await _authService.RevokeTokenAsync(dto.RefreshToken, "User logout");

            if (result.IsFailure)
            {
                return BadRequest(new { message = result.Error!.Message });
            }

            return NoContent();
        }

        /// <summary>
        /// Logout from all devices (revoke all refresh tokens)
        /// </summary>
        [HttpPost("logout-all")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            await _authService.RevokeAllTokensAsync(userId, "Logout from all devices");
            return NoContent();
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserInfoDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _authService.GetUserByIdAsync(userId);

            if (result.IsFailure)
            {
                return NotFound(new { message = result.Error!.Message });
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Change current user's password
        /// </summary>
        /// <param name="dto">Password change details</param>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _authService.ChangePasswordAsync(userId, dto);

            if (result.IsFailure)
            {
                return BadRequest(new { message = result.Error!.Message });
            }

            return NoContent();
        }

        /// <summary>
        /// Get user by ID (Admin only)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User information</returns>
        [HttpGet("users/{id}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(UserInfoDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var result = await _authService.GetUserByIdAsync(id);

            if (result.IsFailure)
            {
                return NotFound(new { message = result.Error!.Message });
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all users for the current company (Admin only)
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <param name="searchTerm">Search term for email/name</param>
        /// <param name="role">Filter by role</param>
        /// <param name="companyId">Filter by company (optional, defaults to current user's company)</param>
        /// <returns>Paged list of users</returns>
        [HttpGet("users")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? role = null,
            [FromQuery] Guid? companyId = null)
        {
            // Use provided companyId or fall back to current user's company
            var effectiveCompanyId = companyId ?? GetCurrentCompanyId();
            if (effectiveCompanyId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _authService.GetUsersAsync(effectiveCompanyId, pageNumber, pageSize, searchTerm, role);

            if (result.IsFailure)
            {
                return BadRequest(new { message = result.Error!.Message });
            }

            var (items, totalCount) = result.Value;
            return Ok(new
            {
                data = items,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        /// <summary>
        /// Update user (Admin only)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="dto">Update details</param>
        /// <returns>Updated user information</returns>
        [HttpPut("users/{id}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(UserInfoDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _authService.UpdateUserAsync(id, dto, currentUserId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(new { message = result.Error.Message }),
                    ErrorType.Validation => BadRequest(new { message = result.Error.Message }),
                    _ => BadRequest(new { message = result.Error.Message })
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Reset user password (Admin only)
        /// </summary>
        /// <param name="dto">Reset password details</param>
        [HttpPost("users/reset-password")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _authService.ResetPasswordAsync(dto, currentUserId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(new { message = result.Error.Message }),
                    _ => BadRequest(new { message = result.Error.Message })
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Deactivate user account (Admin only)
        /// </summary>
        /// <param name="id">User ID</param>
        [HttpPost("users/{id}/deactivate")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeactivateUser(Guid id)
        {
            var result = await _authService.DeactivateUserAsync(id);

            if (result.IsFailure)
            {
                return NotFound(new { message = result.Error!.Message });
            }

            return NoContent();
        }

        /// <summary>
        /// Activate user account (Admin only)
        /// </summary>
        /// <param name="id">User ID</param>
        [HttpPost("users/{id}/activate")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ActivateUser(Guid id)
        {
            var result = await _authService.ActivateUserAsync(id);

            if (result.IsFailure)
            {
                return NotFound(new { message = result.Error!.Message });
            }

            return NoContent();
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        private Guid GetCurrentCompanyId()
        {
            var companyIdClaim = User.FindFirst("company_id")?.Value;
            return Guid.TryParse(companyIdClaim, out var companyId) ? companyId : Guid.Empty;
        }

        private string? GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                return Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            }
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        }
    }
}
