using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using VRGallery.Authentication;


public class BackendClient : MonoBehaviour
{
    private const string BACKEND_URL = "http://localhost:5091";

    [System.Serializable]
    private class BootstrapPayload
    {
        public string playerId;
    }

    public async Task<bool> RegisterUserIfNeeded()
    {
        var payload = new BootstrapPayload
        {
            playerId = AuthenticationManager.Instance.PlayerId,
        };

        var json = JsonUtility.ToJson(payload);

        using var req = new UnityWebRequest(
            $"{BACKEND_URL}/auth/register",
            "POST"
        );

        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Backend register failed: {req.error}");
            return false;
        }

        return true;
    }
}
