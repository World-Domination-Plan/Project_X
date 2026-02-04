using UnityEngine;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CloudLogger : MonoBehaviour
{
    async void Start()
    {
        // Initialize Unity Services
        await UnityServices.InitializeAsync();
        
        // Hook into Unity's log system
        Application.logMessageReceived += HandleLog;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private async void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Send log to Cloud Code
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

            await CloudCodeService.Instance.CallEndpointAsync(
                "gamelogging",
                args
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to send log to Cloud Code: {e.Message}");
        }
    }
}