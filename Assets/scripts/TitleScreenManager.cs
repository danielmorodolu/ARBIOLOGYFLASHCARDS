using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene(1); // Load the next scene (AR scene at index 1)
    }
}