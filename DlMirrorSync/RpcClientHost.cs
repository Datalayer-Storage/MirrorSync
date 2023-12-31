using chia.dotnet;

namespace DlMirrorSync;

/// <summary>
/// Wrapper type to hold multiple RpcClient instances by name and dsiposae of them 
/// It owns the RpcClient lifetimes so don't dispose when retreived
/// </summary>
public sealed class RpcClientHost : IDisposable
{
    private readonly IDictionary<string, HttpRpcClient> _rpcClients = new Dictionary<string, HttpRpcClient>();

    public RpcClientHost(ChiaConfig config)
    {
        // create an rpc client for each specified endpoint
        foreach (var name in config.GetEndpointNames())
        {
            var endpoint = config.GetEndpoint(name);
            _rpcClients.Add(name, new HttpRpcClient(endpoint));
        }
    }

    public HttpRpcClient GetRpcClient(string name) => _rpcClients[name];

    public void Dispose()
    {
        foreach (var rpcClient in _rpcClients.Values)
        {
            rpcClient.Dispose();
        }
    }
}
