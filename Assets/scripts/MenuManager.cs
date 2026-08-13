using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public Button newGameButton;
    public Button quitButton;

    private void Start()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(NewGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    public void NewGame()
    {
        SceneManager.LoadScene("scene2");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}