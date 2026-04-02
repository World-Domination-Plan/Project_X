using System.Collections;
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Spawn Point")]
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Player Rigs")]
    [SerializeField] private Transform xrRig;
    [SerializeField] private Transform desktopPlayer;

    IEnumerator Start()
    {
        // Let the scene finish initializing first
        yield return null;
        yield return null;

        if (playerSpawnPoint == null)
        {
            Debug.LogError("[PlayerSpawnManager] playerSpawnPoint is not assigned.");
            yield break;
        }

        if (PlatformModeManager.CurrentMode == PlatformMode.VR)
        {
            if (xrRig == null)
            {
                Debug.LogError("[PlayerSpawnManager] xrRig is not assigned.");
                yield break;
            }

            xrRig.position = playerSpawnPoint.position;
            xrRig.rotation = Quaternion.Euler(0f, playerSpawnPoint.eulerAngles.y, 0f);

            Debug.Log("[PlayerSpawnManager] Spawned VR rig at PlayerSpawnPoint.");
        }
        else
        {
            if (desktopPlayer == null)
            {
                Debug.LogError("[PlayerSpawnManager] desktopPlayer is not assigned.");
                yield break;
            }

            desktopPlayer.position = playerSpawnPoint.position;
            desktopPlayer.rotation = Quaternion.Euler(0f, playerSpawnPoint.eulerAngles.y, 0f);

            Debug.Log("[PlayerSpawnManager] Spawned desktop player at PlayerSpawnPoint.");
        }
    }
}