using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public Button newGameButton;
    public Button quitButton;

    private void Start()
    {
        // Registar os listeners dos botões
        if (newGameButton != null)
            newGameButton.onClick.AddListener(NewGame);
        else
            Debug.LogError("New Game Button não foi atribuído no Inspector!");

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        else
            Debug.LogError("Quit Button não foi atribuído no Inspector!");
    }

    public void NewGame()
    {
        Debug.Log("Carregando cena 'scene2'...");
        SceneManager.LoadScene("scene2");
    }

    public void QuitGame()
    {
        Debug.Log("Encerrando aplicação...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}