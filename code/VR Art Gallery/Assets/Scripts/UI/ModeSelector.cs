using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSelector : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";

    public void SelectVR()
    {
        PlatformModeManager.CurrentMode = PlatformMode.VR;
        SceneManager.LoadScene(gameSceneName);
    }

    public void SelectDesktop()
    {
        PlatformModeManager.CurrentMode = PlatformMode.Desktop;
        SceneManager.LoadScene(gameSceneName);
    }
}
