using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;

public class UnityCloudCodeClient : ICloudCodeClient
{
    public Task CallEndpointAsync(string endpoint, Dictionary<string, object> args)
        => CloudCodeService.Instance.CallEndpointAsync(endpoint, args);
}

