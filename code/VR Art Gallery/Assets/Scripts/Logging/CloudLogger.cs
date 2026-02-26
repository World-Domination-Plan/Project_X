using UnityEngine;
using Unity.Services.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CloudLogger : MonoBehaviour
{
    // Inject in tests
    public ICloudCodeClient CloudCodeClient { get; set; }

    // Let tests avoid UnityServices.InitializeAsync + global log hook
    public bool AutoInitializeOnStart = true;

    async void Start()
    {
        if (!AutoInitializeOnStart) return;

        CloudCodeClient ??= new UnityCloudCodeClient();

        await UnityServices.InitializeAsync();
        Application.logMessageReceived += HandleLog;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private async void HandleLog(string logString, string stackTrace, LogType type)
    {
        await SendLogToCloudCode(logString, type.ToString());
    }

    public async Task SendLogToCloudCode(string message, string logType)
    {
        try
        {
            var args = new Dictionary<string, object>
            {
                { "message", message },
                { "type", logType }
            };

            CloudCodeClient ??= new UnityCloudCodeClient();
            await CloudCodeClient.CallEndpointAsync("gamelogging", args);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to send log to Cloud Code: {e.Message}");
        }
    }
}

