using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.DTOs.Auth;
using Application.Interfaces;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly ICompaniesRepository _companiesRepository;
        private readonly IUserCompanyAssignmentRepository _userCompanyAssignmentRepository;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        // Lockout settings
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IEmployeesRepository employeesRepository,
            ICompaniesRepository companiesRepository,
            IUserCompanyAssignmentRepository userCompanyAssignmentRepository,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _employeesRepository = employeesRepository;
            _companiesRepository = companiesRepository;
            _userCompanyAssignmentRepository = userCompanyAssignmentRepository;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<Result<TokenResponseDto>> LoginAsync(LoginDto dto, string? ipAddress = null, string? userAgent = null)
        {
            _logger.LogInformation("Login attempt for email {Email}", dto.Email);

            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for email {Email}", dto.Email);
                return Error.Unauthorized("Invalid email or password");
            }

            // Check if account is locked
            if (user.LockoutEndAt.HasValue && user.LockoutEndAt > DateTime.UtcNow)
            {
                _logger.LogWarning("Login failed: Account locked for user {UserId}", user.Id);
                return Error.Unauthorized($"Account is locked. Try again after {user.LockoutEndAt:HH:mm}");
            }

            // Check if account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: Account inactive for user {UserId}", user.Id);
                return Error.Unauthorized("Account is deactivated. Contact administrator.");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
                await HandleFailedLoginAsync(user);
                return Error.Unauthorized("Invalid email or password");
            }

            // Clear failed login attempts on success
            await _userRepository.ResetFailedLoginAttemptsAsync(user.Id);
            await _userRepository.UpdateLastLoginAsync(user.Id);

            // Generate tokens
            var tokenResponse = await GenerateTokensAsync(user, ipAddress, userAgent);

            _logger.LogInformation("Login successful for user {UserId}", user.Id);
            return tokenResponse;
        }

        public async Task<Result<UserInfoDto>> RegisterAsync(RegisterUserDto dto, Guid createdBy)
        {
            _logger.LogInformation("Registering new user with email {Email}", dto.Email);

            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(dto.Email))
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists", dto.Email);
                return Error.Conflict("Email already registered");
            }

            // Validate role
            if (!UserRoles.All.Contains(dto.Role))
            {
                return Error.Validation($"Invalid role. Valid roles are: {string.Join(", ", UserRoles.All)}");
            }

            var user = new User
            {
                Email = dto.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                DisplayName = dto.DisplayName,
                Role = dto.Role,
                CompanyId = dto.CompanyId,
                EmployeeId = dto.EmployeeId,
                IsActive = true,
                CreatedBy = createdBy.ToString()
            };

            var createdUser = await _userRepository.CreateAsync(user);

            _logger.LogInformation("User registered successfully with ID {UserId}", createdUser.Id);

            return await MapToUserInfoAsync(createdUser);
        }

        public async Task<Result<TokenResponseDto>> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (token == null)
            {
                _logger.LogWarning("Refresh token not found");
                return Error.Unauthorized("Invalid refresh token");
            }

            if (!token.IsActive)
            {
                _logger.LogWarning("Refresh token is not active for token {TokenId}", token.Id);

                // If token was revoked, revoke all tokens for this user (potential token theft)
                if (token.IsRevoked)
                {
                    await _refreshTokenRepository.RevokeAllByUserIdAsync(token.UserId, "Potential token reuse detected");
                }

                return Error.Unauthorized("Invalid refresh token");
            }

            var user = await _userRepository.GetByIdAsync(token.UserId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("User not found or inactive for refresh token {TokenId}", token.Id);
                return Error.Unauthorized("Invalid refresh token");
            }

            // Rotate refresh token
            var newTokenResponse = await GenerateTokensAsync(user, ipAddress, userAgent);

            // Revoke old token
            await _refreshTokenRepository.RevokeAsync(token.Id, "Token rotated",
                (await _refreshTokenRepository.GetByTokenAsync(newTokenResponse.RefreshToken))?.Id);

            _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);
            return newTokenResponse;
        }

        public async Task<Result> RevokeTokenAsync(string refreshToken, string? reason = null)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (token == null)
            {
                return Error.NotFound("Refresh token not found");
            }

            if (!token.IsActive)
            {
                return Error.Validation("Token is already revoked or expired");
            }

            await _refreshTokenRepository.RevokeAsync(token.Id, reason ?? "User logout");

            _logger.LogInformation("Refresh token revoked for token {TokenId}", token.Id);
            return Result.Success();
        }

        public async Task<Result> RevokeAllTokensAsync(Guid userId, string? reason = null)
        {
            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, reason ?? "Logout from all devices");

            _logger.LogInformation("All refresh tokens revoked for user {UserId}", userId);
            return Result.Success();
        }

        public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Error.NotFound("User not found");
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                return Error.Validation("Current password is incorrect");
            }

            // Update password
            var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _userRepository.ChangePasswordAsync(userId, newHash);

            // Revoke all refresh tokens (force re-login on all devices)
            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, "Password changed");

            _logger.LogInformation("Password changed for user {UserId}", userId);
            return Result.Success();
        }

        public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto, Guid resetBy)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null)
            {
                return Error.NotFound("User not found");
            }

            var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _userRepository.ChangePasswordAsync(dto.UserId, newHash);

            // Revoke all refresh tokens
            await _refreshTokenRepository.RevokeAllByUserIdAsync(dto.UserId, $"Password reset by {resetBy}");

            // Unlock account if locked
            await _userRepository.UnlockUserAsync(dto.UserId);

            _logger.LogInformation("Password reset for user {UserId} by {ResetBy}", dto.UserId, resetBy);
            return Result.Success();
        }

        public async Task<Result<UserInfoDto>> GetUserByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Error.NotFound("User not found");
            }

            return await MapToUserInfoAsync(user);
        }

        public async Task<Result<(IEnumerable<UserInfoDto> Items, int TotalCount)>> GetUsersAsync(
            Guid companyId, int pageNumber, int pageSize, string? searchTerm = null, string? role = null)
        {
            var (users, totalCount) = await _userRepository.GetPagedAsync(companyId, pageNumber, pageSize, searchTerm, role);
            var userInfoTasks = users.Select(MapToUserInfoAsync);
            var userInfos = await Task.WhenAll(userInfoTasks);

            return (userInfos, totalCount);
        }

        public async Task<Result<UserInfoDto>> UpdateUserAsync(Guid userId, UpdateUserDto dto, Guid updatedBy)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Error.NotFound("User not found");
            }

            if (dto.DisplayName != null)
                user.DisplayName = dto.DisplayName;

            if (dto.Role != null)
            {
                if (!UserRoles.All.Contains(dto.Role))
                {
                    return Error.Validation($"Invalid role. Valid roles are: {string.Join(", ", UserRoles.All)}");
                }
                user.Role = dto.Role;
            }

            if (dto.EmployeeId.HasValue)
                user.EmployeeId = dto.EmployeeId;

            if (dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;

            user.UpdatedBy = updatedBy.ToString();

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {UserId} updated by {UpdatedBy}", userId, updatedBy);
            return await MapToUserInfoAsync(user);
        }

        public async Task<Result> DeactivateUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Error.NotFound("User not found");
            }

            await _userRepository.DeactivateAsync(userId);
            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, "Account deactivated");

            _logger.LogInformation("User {UserId} deactivated", userId);
            return Result.Success();
        }

        public async Task<Result> ActivateUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Error.NotFound("User not found");
            }

            await _userRepository.ActivateAsync(userId);

            _logger.LogInformation("User {UserId} activated", userId);
            return Result.Success();
        }

        public async Task<Result<User>> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return Error.Unauthorized("Invalid credentials");
            }

            if (!user.IsActive)
            {
                return Error.Unauthorized("Account is deactivated");
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return Error.Unauthorized("Invalid credentials");
            }

            return user;
        }

        private async Task HandleFailedLoginAsync(User user)
        {
            await _userRepository.IncrementFailedLoginAttemptsAsync(user.Id);

            // Lock account if max attempts exceeded
            if (user.FailedLoginAttempts + 1 >= MaxFailedAttempts)
            {
                var lockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                await _userRepository.LockUserAsync(user.Id, lockoutEnd);
                _logger.LogWarning("Account locked for user {UserId} due to too many failed attempts", user.Id);
            }
        }

        private async Task<TokenResponseDto> GenerateTokensAsync(User user, string? ipAddress, string? userAgent)
        {
            var accessToken = await GenerateAccessTokenAsync(user);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress, userAgent);
            var userInfo = await MapToUserInfoAsync(user);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                User = userInfo
            };
        }

        private async Task<string> GenerateAccessTokenAsync(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.Name, user.DisplayName),
                new(ClaimTypes.Role, user.Role),
            };

            // Super Admin: Access to ALL companies (system-wide admin)
            if (user.IsSuperAdmin)
            {
                _logger.LogInformation("User {UserId} is Super Admin - granting all_companies access", user.Id);

                // Fetch all companies and add them to the JWT
                var allCompanies = await _companiesRepository.GetAllAsync();
                var companyIds = allCompanies.Select(c => c.Id.ToString()).ToList();

                if (companyIds.Any())
                {
                    // Add company_ids as a JSON array claim
                    claims.Add(new Claim("company_ids", System.Text.Json.JsonSerializer.Serialize(companyIds)));

                    // Set active company_id to the first company (can be switched later)
                    claims.Add(new Claim("company_id", companyIds.First()));
                }

                claims.Add(new Claim("access_scope", "all_companies"));
                claims.Add(new Claim("is_super_admin", "true"));
            }
            // Company Admin/HR: Access to ASSIGNED companies only
            else if (user.Role == UserRoles.Admin || user.Role == UserRoles.HR)
            {
                _logger.LogInformation("User {UserId} is Company Admin/HR - checking assigned companies", user.Id);

                // Get assigned companies from the junction table
                var assignments = await _userCompanyAssignmentRepository.GetByUserIdAsync(user.Id);
                var assignmentList = assignments.ToList();

                if (assignmentList.Any())
                {
                    var companyIds = assignmentList.Select(a => a.CompanyId.ToString()).ToList();
                    claims.Add(new Claim("company_ids", System.Text.Json.JsonSerializer.Serialize(companyIds)));
                    claims.Add(new Claim("access_scope", "assigned_companies"));

                    // Set primary company or first as default
                    var primary = assignmentList.FirstOrDefault(a => a.IsPrimary) ?? assignmentList.First();
                    claims.Add(new Claim("company_id", primary.CompanyId.ToString()));

                    _logger.LogInformation("User {UserId} has access to {Count} companies", user.Id, companyIds.Count);
                }
                else
                {
                    // Fallback: No assignments, use user's default company
                    _logger.LogWarning("Company Admin/HR {UserId} has no company assignments, falling back to user.CompanyId", user.Id);
                    if (user.CompanyId != Guid.Empty)
                    {
                        claims.Add(new Claim("company_id", user.CompanyId.ToString()));
                        claims.Add(new Claim("company_ids", System.Text.Json.JsonSerializer.Serialize(new[] { user.CompanyId.ToString() })));
                        claims.Add(new Claim("access_scope", "assigned_companies"));
                    }
                }
            }
            else
            {
                // Regular users get their assigned company only
                if (user.CompanyId != Guid.Empty)
                {
                    claims.Add(new Claim("company_id", user.CompanyId.ToString()));
                }
            }

            if (user.EmployeeId.HasValue)
            {
                claims.Add(new Claim("employee_id", user.EmployeeId.Value.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string? ipAddress, string? userAgent)
        {
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = GenerateSecureToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedByIp = ipAddress,
                CreatedByUserAgent = userAgent
            };

            return await _refreshTokenRepository.CreateAsync(refreshToken);
        }

        private static string GenerateSecureToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private async Task<UserInfoDto> MapToUserInfoAsync(User user)
        {
            // Derive isManager from:
            // 1. User role is "Manager", OR
            // 2. Employee has direct reports in the hierarchy
            var isManager = user.Role == UserRoles.Manager;

            if (!isManager && user.EmployeeId.HasValue)
            {
                // Check if this employee has any direct reports
                var directReports = await _employeesRepository.GetByManagerIdAsync(user.EmployeeId.Value);
                isManager = directReports.Any();
            }

            return new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = user.Role,
                CompanyId = user.CompanyId,
                EmployeeId = user.EmployeeId,
                IsManager = isManager
            };
        }
    }
}
