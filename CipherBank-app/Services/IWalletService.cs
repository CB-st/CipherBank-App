using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// Service for managing cryptocurrency wallets.
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// Gets all wallets belonging to the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of user wallets.</returns>
    Task<List<Wallet>> GetWalletsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific wallet by its ID.
    /// </summary>
    /// <param name="id">The wallet ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The wallet if found.</returns>
    Task<Wallet> GetWalletAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current balance of a wallet.
    /// </summary>
    /// <param name="id">The wallet ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The wallet balance.</returns>
    Task<decimal> GetWalletBalanceAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new wallet for a specific cryptocurrency.
    /// </summary>
    /// <param name="cryptoSymbol">The cryptocurrency symbol (e.g., BTC, ETH).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created wallet.</returns>
    Task<Wallet> CreateWalletAsync(string cryptoSymbol, CancellationToken cancellationToken = default);
}
