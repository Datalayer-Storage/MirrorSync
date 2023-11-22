namespace DlMirrorSync;

public sealed class DnsService
{
    private readonly ILogger<DnsService> _logger;
    private readonly IConfiguration _configuration;

    public DnsService(ILogger<DnsService> logger, IConfiguration configuration) =>
            (_logger, _configuration) = (logger, configuration);

    public async Task<string?> GetHostUri(CancellationToken stoppingToken)
    {
        // config file takes precedence
        var host = _configuration["DlMirrorSync:MirrorHostUri"];
        if (!string.IsNullOrEmpty(host))
        {
            return host;
        }

        var ip = await GetPublicIPAdress(stoppingToken);
        return $"http://{ip}:8575";
    }

    private async Task<string> GetPublicIPAdress(CancellationToken stoppingToken)
    {
        try
        {
            using var httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            return await httpClient.GetStringAsync("https://api.ipify.org", stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get public ip address: {Message}", ex.Message);
            return string.Empty;
        }
    }
}
