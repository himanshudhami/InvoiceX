namespace Core.Entities
{
    /// <summary>
    /// Represents a refresh token for JWT authentication
    /// </summary>
    public class RefreshToken
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The user this token belongs to
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The token string (hashed for storage)
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// When the token expires
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Whether the token has been revoked
        /// </summary>
        public bool IsRevoked { get; set; } = false;

        /// <summary>
        /// When the token was revoked (if revoked)
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Reason for revocation
        /// </summary>
        public string? RevokedReason { get; set; }

        /// <summary>
        /// IP address that created this token
        /// </summary>
        public string? CreatedByIp { get; set; }

        /// <summary>
        /// User agent that created this token
        /// </summary>
        public string? CreatedByUserAgent { get; set; }

        /// <summary>
        /// The token that replaced this one (if rotated)
        /// </summary>
        public Guid? ReplacedByTokenId { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Check if token is expired
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        /// <summary>
        /// Check if token is active (not revoked and not expired)
        /// </summary>
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
