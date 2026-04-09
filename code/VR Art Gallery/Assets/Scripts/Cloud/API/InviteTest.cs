using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRGallery.Cloud;
using System.Linq;

/// <summary>
/// Specialized test for inviting other artists to a gallery.
/// Updates the 'gallery_access' array of an invited artist profile.
/// </summary>
public class InviteTest : MonoBehaviour
{
    [Header("Invitation Parameters")]
    [SerializeField] private int targetGalleryId;
    [SerializeField] private int ownerArtistId;
    [SerializeField] private int inviteeArtistId;

    private SupabaseGalleryRepository _galleryRepo;
    private SupabaseArtistRepository _artistRepo;

    public async void RunInviteTest()
    {
        Debug.Log($"[InviteTest] Starting invitation: Gallery {targetGalleryId} (Owner {ownerArtistId}) -> Invitee {inviteeArtistId}");

        try
        {
            _galleryRepo ??= await SupabaseGalleryRepository.CreateAsync();
            _artistRepo ??= await SupabaseArtistRepository.CreateAsync();

            // 1. Verify Gallery Ownership
            var gallery = await _galleryRepo.GetGalleryAsync(targetGalleryId);
            if (gallery == null)
            {
                Debug.LogError($"[InviteTest] Gallery {targetGalleryId} not found.");
                return;
            }

            if (gallery.owner_id != ownerArtistId)
            {
                Debug.LogError($"[InviteTest] Gallery {targetGalleryId} does not belong to owner {ownerArtistId} (Actual Owner: {gallery.owner_id}).");
                return;
            }

            // 2. Fetch Invitee Profile
            // We need to fetch all to find by numeric user_id or use GetArtistProfileAsync if it supports numeric
            // SupabaseArtistRepository.GetArtistProfileAsync(string userId) uses auth_user_id (string).
            // But ArtistProfile has user_id (int). 
            // Let's use GetAllArtistsAsync and find the numeric ID.
            var allArtists = await _artistRepo.GetAllArtistsAsync();
            var invitee = allArtists.Find(a => a.user_id == inviteeArtistId);

            if (invitee == null)
            {
                Debug.LogError($"[InviteTest] Invitee artist ID {inviteeArtistId} not found.");
                return;
            }

            // 3. Update Gallery Access
            var accessList = new List<string>();
            if (invitee.gallery_access != null)
                accessList.AddRange(invitee.gallery_access);

            string galleryIdStr = targetGalleryId.ToString();
            if (accessList.Contains(galleryIdStr))
            {
                Debug.LogWarning($"[InviteTest] Artist {invitee.username} (ID: {inviteeArtistId}) already has access to gallery {targetGalleryId}.");
            }
            else
            {
                accessList.Add(galleryIdStr);
                invitee.gallery_access = accessList.ToArray();

                bool success = await _artistRepo.UpdateArtistProfileAsync(invitee);
                if (success)
                {
                    Debug.Log($"[InviteTest] SUCCESS! Artist {invitee.username} now has access to gallery {targetGalleryId}.");
                }
                else
                {
                    Debug.LogError($"[InviteTest] Failed to update artist profile for {invitee.username}.");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InviteTest] Invitation FAILED: {ex.Message}");
        }
    }
}
