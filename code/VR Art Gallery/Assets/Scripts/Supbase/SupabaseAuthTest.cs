using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

public class SupabaseTest : MonoBehaviour
{
    // ============================================
    // CONFIGURATION
    // ============================================
    private string supabaseUrl = "https://jdorkglqkatydqxcgshu.supabase.co";
    private string supabaseKey = "sb_publishable__CS5YpEcdfuUKljKCVjfQw_dPKw-gW1";
    private string tableName = "artists";

    void Start()
    {
        Debug.Log("🚀 Starting Supabase Connection Test in Unity...");
        StartCoroutine(RunTests());
    }

    IEnumerator RunTests()
    {
        yield return StartCoroutine(TestReadData());
        yield return StartCoroutine(TestInsertData());
    }

    // ============================================
    // Test 1: Read data (GET)
    // ============================================
    IEnumerator TestReadData()
    {
        Debug.Log("--------------------------------------------------");
        Debug.Log("TEST 1: Reading data from Supabase...");
        
        string url = $"{supabaseUrl}/rest/v1/{tableName}?select=*";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            SetHeaders(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✓ Success! Data retrieved: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"✗ Read failed: {request.error}\nResponse: {request.downloadHandler.text}");
            }
        }
    }

    // ============================================
    // Test 2: Insert data (POST)
    // ============================================
    IEnumerator TestInsertData()
    {
        Debug.Log("--------------------------------------------------");
        Debug.Log("TEST 2: Inserting data to Supabase...");

        string url = $"{supabaseUrl}/rest/v1/{tableName}";
        
        // Generate unique ID just like the Python script
        string uniqueId = "unity_user_" + Guid.NewGuid().ToString().Substring(0, 6);
        
        // Create a simple JSON string (Manually or using a Serializer)
        // For production, use a library like Newtonsoft.Json or JsonUtility with a [Serializable] class
        string jsonData = "{" +
            $"\"user_id\": \"{uniqueId}\"," +
            $"\"username\": \"unity_guest_{uniqueId}\"," +
            "\"managed_gallary\": [\"1234\", \"5678\"]," +
            "\"created_at\": \"" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\"" +
        "}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            SetHeaders(request);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Prefer", "return=representation");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✓ Success! Inserted row: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"✗ Insert failed: {request.error}\nResponse: {request.downloadHandler.text}");
            }
        }
    }

    private void SetHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("apikey", supabaseKey);
        request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
    }
}