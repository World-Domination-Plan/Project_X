using System.IO;
using Newtonsoft.Json;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using UnityEngine;

public class UnitySessionPersistence : IGotrueSessionPersistence<Session>
{
    private readonly string _path;

    public UnitySessionPersistence(string fileName = "sb_session.json")
    {
        _path = Path.Combine(Application.persistentDataPath, fileName);
    }

    public void SaveSession(Session session)
    {
        try
        {
            var json = JsonConvert.SerializeObject(session);
            File.WriteAllText(_path, json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UnitySessionPersistence] SaveSession failed: {e.Message}");
        }
    }

    public Session? LoadSession()
    {
        try
        {
            if (!File.Exists(_path)) return null;
            var json = File.ReadAllText(_path);
            return JsonConvert.DeserializeObject<Session>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UnitySessionPersistence] LoadSession failed: {e.Message}");
            return null;
        }
    }

    public void DestroySession()
    {
        try
        {
            if (File.Exists(_path)) File.Delete(_path);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UnitySessionPersistence] DestroySession failed: {e.Message}");
        }
    }
}
