namespace DlMirrorSync;

using chia.dotnet;

public sealed class SyncService
{
    private readonly DataLayerProxy _dataLayer;
    private readonly ChiaService _chiaService;
    private readonly MirrorService _mirrorService;
    private readonly ILogger<SyncService> _logger;
    private readonly IConfiguration _configuration;

    public SyncService(DataLayerProxy dataLayer,
                        ChiaService chiaService,
                        MirrorService mirrorService,
                        ILogger<SyncService> logger,
                        IConfiguration configuration) =>
            (_dataLayer, _chiaService, _mirrorService, _logger, _configuration) = (dataLayer, chiaService, mirrorService, logger, configuration);

    public async Task SyncSubscriptions(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Syncing mirrors...");
        try
        {
            var reserveAmount = _configuration.GetValue<ulong>("DlMirrorSync:AddMirrorAmount", 300000001);
            var fee = await _chiaService.GetFee(reserveAmount, stoppingToken);

            _logger.LogInformation("Fetching subscriptions...");
            var subscriptions = await _dataLayer.Subscriptions(stoppingToken);
            var mirrorUris = await _mirrorService.GetMyMirrorUris(stoppingToken);
            _logger.LogInformation("Using mirror uris: {mirrorUris}", string.Join("\n", mirrorUris));

            var haveFunds = true;
            await foreach (var id in _mirrorService.FetchLatest(stoppingToken))
            {
                // subscribing and mirroring are split into two separate operations
                // as we might subscribe to a singleton that we don't want to mirror
                // or subscribe to a singleton but not be able to pay for the mirror etc
                if (!subscriptions.Contains(id))
                {
                    _logger.LogInformation("Subscribing to {id}", id);
                    await _dataLayer.Subscribe(id, Enumerable.Empty<string>(), stoppingToken);
                }

                if (_configuration.GetValue("DlMirrorSync:MirrorServer", true) && mirrorUris.Any() && haveFunds)
                {
                    // if we are out of funds to add mirrors, stop trying but continue subscribing
                    // next time around we will try again
                    haveFunds = await AddMirror(id, reserveAmount, mirrorUris, fee, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("There was a problem syncing subscriptions: {Message}", ex.InnerException?.Message ?? ex.Message);
        }
    }

    private async Task<bool> AddMirror(string id, ulong reserveAmount, IEnumerable<string> mirrorUris, ulong fee, CancellationToken stoppingToken)
    {
        var xchWallet = _chiaService.GetWallet(_configuration.GetValue<uint>("DlMirrorSync:XchWalletId", 1));
        var mirrors = await _dataLayer.GetMirrors(id, stoppingToken);
        // add any mirrors that aren't already ours
        if (!mirrors.Any(m => m.Ours))
        {
            var balance = await xchWallet.GetBalance(stoppingToken);
            if (reserveAmount + fee < balance.SpendableBalance)
            {
                _logger.LogInformation("Adding mirror {id}", id);
                await _dataLayer.AddMirror(id, reserveAmount, mirrorUris, fee, stoppingToken);
            }
            else if (balance.SpendableBalance == 0 && (reserveAmount + fee < balance.PendingChange || reserveAmount + fee < balance.ConfirmedWalletBalance))
            {
                // no more spendable funds but we have change incoming, pause and then see if it has arrived
                var waitingForChangeDelayMinutes = _configuration.GetValue("App:WaitingForChangeDelayMinutes", 2);
                _logger.LogWarning("Waiting {WaitingForChangeDelayMinutes} minutes for change", waitingForChangeDelayMinutes);
                await Task.Delay(TimeSpan.FromMinutes(waitingForChangeDelayMinutes), stoppingToken);
            }
            else
            {
                _logger.LogWarning("Insufficient funds to add mirror {id}. Balance={SpendableBalance}, Cost={reserveAmount}, Fee={fee}", id, balance.SpendableBalance, reserveAmount, fee);
                _logger.LogWarning("Pausing sync for now");
                return false; // out of money, stop mirror syncing
            }
        }

        return true;
    }
}
