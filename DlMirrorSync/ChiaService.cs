using chia.dotnet;

namespace DlMirrorSync;

public sealed class ChiaService
{
    private readonly FullNodeProxy _fullNode;
    private readonly WalletProxy _wallet;
    private readonly ILogger<ChiaService> _logger;
    private readonly IConfiguration _configuration;

    public ChiaService(WalletProxy wallet, FullNodeProxy fullNode, ILogger<ChiaService> logger, IConfiguration configuration) =>
            (_wallet, _fullNode, _logger, _configuration) = (wallet, fullNode, logger, configuration);

    public async Task<ulong> GetFee(ulong cost, CancellationToken stoppingToken)
    {
        try
        {
            using var _ = new ScopedLogEntry(_logger, "Getting fee estimate");
            int[] targetTimes = [_configuration.GetValue<int>("DlMirrorSync:FeeEstimateTargetTimeMinutes", 5) * 60];
            var fee = await _fullNode.GetFeeEstimate(cost, targetTimes, stoppingToken);
            return fee.estimates.First();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not connect to full node. Using fee amount from config: {Message}", ex.InnerException?.Message ?? ex.Message);
            return _configuration.GetValue<ulong>("DlMirrorSync:DefaultFee", 500000);
        }
    }

    public Wallet GetWallet(uint walletId) => new(walletId, _wallet);
}
