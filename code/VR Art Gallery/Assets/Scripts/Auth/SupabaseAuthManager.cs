using System;
using System.Collections;
using System.Text;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine.Networking;

public class SupabaseAuthManager : MonoBehaviour
{
    // Replace these with your Supabase credentials
    private string supabaseUrl = "https://jdorkglqkatydqxcgshu.supabase.co";
    private string supabaseKey = "sb_publishable__CS5YpEcdfuUKljKCVjfQw_dPKw-gW1";
    private string tableName = "artists"; // Replace with your table name

    async void Start()
    {
        // Step 1: Initialize Unity Services
        await InitializeUnityServices();
        
        // Step 2: Sign in anonymously
        await SignInAnonymously();
        
        // Step 3: Send data to Supabase
        StartCoroutine(SendToSupabase());
    }

    private async System.Threading.Tasks.Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Services initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Unity Services initialization failed: {e.Message}");
        }
    }

    private async System.Threading.Tasks.Task SignInAnonymously()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in anonymously!");
            Debug.Log($"Player ID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Authentication failed: {ex.Message}");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Request failed: {ex.Message}");
        }
    }

    private IEnumerator SendToSupabase()
    {
        // Get the Unity Player ID
        string playerId = AuthenticationService.Instance.PlayerId;
        
        // Create your JSON data - modify these fields based on your Supabase table structure
        string jsonData = $"{{\"user_id\":\"{playerId}\",\"created_at\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\", \"managed_gallary\": \"[]\"}}";
        
        // Convert JSON to bytes
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        
        // Create the web request
        string endpoint = $"{supabaseUrl}/rest/v1/{tableName}";
        UnityWebRequest request = new UnityWebRequest(endpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        // Set required headers
        request.SetRequestHeader("apikey", supabaseKey);
        request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Prefer", "return=representation");
        
        // Send the request
        yield return request.SendWebRequest();
        
        // Check for errors
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Successfully sent data to Supabase!");
            Debug.Log($"Response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"Error sending to Supabase: {request.error}");
            Debug.LogError($"Response Code: {request.responseCode}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
        }
        
        request.Dispose();
    }
}
