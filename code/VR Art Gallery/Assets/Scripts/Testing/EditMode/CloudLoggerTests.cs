using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

public class CloudLoggerTests
{
    private class FakeCloudCodeClient : ICloudCodeClient
    {
        public int CallCount;
        public string LastEndpoint;
        public Dictionary<string, object> LastArgs;

        public Task CallEndpointAsync(string endpoint, Dictionary<string, object> args)
        {
            CallCount++;
            LastEndpoint = endpoint;
            LastArgs = args;
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task SendLogToCloudCode_SendsExpectedEndpointAndArgs()
    {
        var go = new GameObject("CloudLoggerTest");
        var logger = go.AddComponent<CloudLogger>();
        logger.AutoInitializeOnStart = false; // avoid UnityServices init
        var fake = new FakeCloudCodeClient();
        logger.CloudCodeClient = fake;

        await logger.SendLogToCloudCode("hello", "Log");

        Assert.AreEqual(1, fake.CallCount);
        Assert.AreEqual("gamelogging", fake.LastEndpoint);
        Assert.AreEqual("hello", fake.LastArgs["message"]);
        Assert.AreEqual("Log", fake.LastArgs["type"]);

        Object.DestroyImmediate(go);
    }

    [Test]
    public async Task SendLogToCloudCode_WhenClientThrows_LogsError()
    {
        var go = new GameObject("CloudLoggerTest");
        var logger = go.AddComponent<CloudLogger>();
        logger.AutoInitializeOnStart = false;
        logger.CloudCodeClient = new ThrowingCloudCodeClient();

        LogAssert.Expect(LogType.Error, "Failed to send log to Cloud Code: boom");

        await logger.SendLogToCloudCode("x", "Error");

        Object.DestroyImmediate(go);
    }

    private class ThrowingCloudCodeClient : ICloudCodeClient
    {
        public Task CallEndpointAsync(string endpoint, Dictionary<string, object> args)
            => throw new System.Exception("boom");
    }
}

