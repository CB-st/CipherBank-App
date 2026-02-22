using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Models;

namespace CipherBank_app.Services;

public interface IAuthService
{
    Task<AuthToken> LoginAsync(string user, string password, CancellationToken cancellationToken = default);
    Task<AuthToken> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AuthToken?> GetStoredTokenAsync();
    Task<bool> IsTokenExpiredAsync();
    Task LogoutAsync();
}