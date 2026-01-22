using System.Collections.Generic;
using System.Threading.Tasks;

public interface ICloudCodeClient
{
    Task CallEndpointAsync(string endpoint, Dictionary<string, object> args);
}

