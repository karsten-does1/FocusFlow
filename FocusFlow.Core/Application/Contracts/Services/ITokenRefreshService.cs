namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface ITokenRefreshService
    {
        Task<TokenRefreshResult> RefreshTokenAsync(Guid accountId, CancellationToken ct = default);
        Task<bool> IsTokenExpiredAsync(Guid accountId, CancellationToken ct = default);
        Task<bool> ShouldRefreshTokenAsync(Guid accountId, CancellationToken ct = default);
    }

    public sealed record TokenRefreshResult(
        bool Success,
        string? NewAccessToken = null,
        DateTime? NewExpiresAtUtc = null,
        string? ErrorMessage = null);
}

