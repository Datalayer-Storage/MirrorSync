namespace DlMirrorSync;

using System.Net.Http;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public sealed class MirrorService
{
    private readonly DnsService _dnsService;
    private readonly ILogger<MirrorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    public MirrorService(
        DnsService dnsService,
        ILogger<MirrorService> logger,
        IConfiguration configuration) =>
        (_dnsService, _logger, _configuration) = (dnsService, logger, configuration);

    public async Task<IEnumerable<string>> GetMyMirrorUris(CancellationToken cancellationToken)
    {
        var uri = await _dnsService.GetHostUri(cancellationToken);
        if (string.IsNullOrEmpty(uri))
        {
            return Enumerable.Empty<string>();
        }
        return [uri];
    }

    public async IAsyncEnumerable<string> FetchLatest([EnumeratorCancellation] CancellationToken stoppingToken)
    {
        var uri = _configuration["DlMirrorSync:MirrorServiceUri"] ?? throw new InvalidOperationException("Missing MirrorServiceUri");

        using var _ = new ScopedLogEntry(_logger, $"Fetching latest mirrors from {uri}");
        using var httpClient = new HttpClient();
        var currentPage = 1;
        var totalPages = 0; // we won't know actual total pages until we get the first page

        do
        {
            var page = await GetPage(httpClient, uri, currentPage, stoppingToken);
            totalPages = page.TotalPages;

            foreach (var singleton in page.Mirrors)
            {
                yield return singleton.SingletonId;
            }

            currentPage++;
        } while (currentPage <= totalPages && !stoppingToken.IsCancellationRequested);
    }

    private async Task<PageRecord> GetPage(HttpClient httpClient, string uri, int currentPage, CancellationToken stoppingToken)
    {
        try
        {
            using var _ = new ScopedLogEntry(_logger, $"Fetching page {currentPage} from {uri}");
            using var response = await httpClient.GetAsync($"{uri}?page={currentPage}", stoppingToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(stoppingToken);
            return JsonConvert.DeserializeObject<PageRecord>(responseBody, _settings) ?? throw new InvalidOperationException("Failed to fetch mirrors");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("There was a problem fetching the singleton list: {Message}", ex.InnerException?.Message ?? ex.Message);
            // this is not fatal to the process, so return an empty page
            return new PageRecord();
        }
    }
}

public record PageRecord
{
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int TotalPages { get; init; }
    public IEnumerable<SingletonRef> Mirrors { get; init; } = new List<SingletonRef>();
}

public record SingletonRef
{
    public string SingletonId { get; init; } = string.Empty;
}
