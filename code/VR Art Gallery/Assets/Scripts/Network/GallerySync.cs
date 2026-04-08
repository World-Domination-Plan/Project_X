using Unity.Netcode;
using Unity.Collections;
using System.Diagnostics;
using UnityEngine;

public class GallerySync : NetworkBehaviour
{
    [Header("Gallaery Data")]
    [SerializeField] private GalleryManager galleryManager;
    [SerializeField] private GalleryProfileUI galleryProfileUI;
    // Only the server can write; all clients can read
    private NetworkVariable<int> galleryId = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Player B (host) sets the gallery ID when this object spawns
            galleryId.Value = galleryProfileUI.m_galleryIdToView; // your logic here
            UnityEngine.Debug.Log($"[GallerySync] Server set galleryId to {galleryId.Value}");
        }
        else
        {
            // Player A: value is already synced by this point, read it
            LoadGallery(galleryId.Value);
            UnityEngine.Debug.Log($"[GallerySync] Client read initial galleryId: {galleryId.Value}");
        }

        // Subscribe to future changes (in case host changes gallery later)
        galleryId.OnValueChanged += OnGalleryIdChanged;
    }

    private void OnGalleryIdChanged(int previous, int current)
    {
        if (!IsServer)
            LoadGallery(current);
    }

    private void LoadGallery(int id)
    {
        // Render your gallery using the id
    }
}