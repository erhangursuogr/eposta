using System.Collections.Concurrent;

namespace DeuEposta.Services;

public interface ITokenBlacklistService
{
    void BlacklistToken(string tokenId, DateTime expiration);
    bool IsTokenBlacklisted(string tokenId);
    void CleanupExpiredTokens();
}

public class TokenBlacklistService : ITokenBlacklistService, IDisposable
{
    private readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = new();
    private readonly ILogger<TokenBlacklistService> _logger;
    private readonly Timer _cleanupTimer;

    public TokenBlacklistService(ILogger<TokenBlacklistService> logger)
    {
        _logger = logger;
        
        // Her 15 dakikada bir expired token'ları temizle
        _cleanupTimer = new Timer(CleanupCallback, null, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
    }

    public void BlacklistToken(string tokenId, DateTime expiration)
    {
        if (string.IsNullOrEmpty(tokenId))
            return;

        _blacklistedTokens.TryAdd(tokenId, expiration);
        _logger.LogInformation("Token {TokenId} blacklisted until {Expiration}", tokenId, expiration);
    }

    public bool IsTokenBlacklisted(string tokenId)
    {
        if (string.IsNullOrEmpty(tokenId))
            return false;

        // Thread-safe expiration check without race condition
        while (_blacklistedTokens.TryGetValue(tokenId, out var expiration))
        {
            // Token'ın süresi dolmuşsa blacklist'ten çıkar
            if (DateTime.UtcNow > expiration)
            {
                // Race condition'dan kaçınmak için TryRemove'dan sonra tekrar kontrol et
                if (!_blacklistedTokens.TryRemove(tokenId, out _))
                {
                    // Başka bir thread kaldırmış olabilir, tekrar kontrol et
                    continue;
                }
                return false;
            }
            // Token hala aktif ve blacklist'te
            return true;
        }

        // Token blacklist'te değil
        return false;
    }

    public void CleanupExpiredTokens()
    {
        var now = DateTime.UtcNow;
        var expiredTokens = _blacklistedTokens
            .Where(kvp => now > kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var tokenId in expiredTokens)
        {
            _blacklistedTokens.TryRemove(tokenId, out _);
        }

        if (expiredTokens.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired blacklisted tokens", expiredTokens.Count);
        }
    }

    private void CleanupCallback(object? state)
    {
        CleanupExpiredTokens();
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}