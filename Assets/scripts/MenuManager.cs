using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Controls the main menu routes for the offline MVP and online lobby.
/// </summary>
public sealed class MenuManager : MonoBehaviour
{
    private const string OfflineSceneName = "scene2";
    private const string LobbySceneName = "lobbyonline";

    public Button newGameButton;
    public Button quitButton;
    [SerializeField] private GameObject offlineWipPanel;
    [SerializeField] private TMP_Text offlineWipText;
    [SerializeField] private float offlineNoticeDuration = 1.5f;

    private Button onlineButton;
    private bool openingOnlineLobby;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeMenuManager()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
            return;
        if (FindFirstObjectByType<MenuManager>() == null)
            new GameObject("Runtime Menu Manager").AddComponent<MenuManager>();
    }

    private void Awake()
    {
        EnsureEventSystem();
        BindButtons();
    }
    private void OnDestroy()
    {
        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(StartOfflineGame);
        if (onlineButton != null)
            onlineButton.onClick.RemoveListener(OpenOnlineLobby);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }

    private void BindButtons()
    {
        if (newGameButton == null)
            newGameButton = FindButton("Canvas/newgame", "newgame");
        if (quitButton == null)
            quitButton = FindButton("Canvas/quit", "quit");

        onlineButton = FindButton("Canvas/Button", "Button");
        ResolveOfflineNotice();

        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveListener(StartOfflineGame);
            newGameButton.onClick.AddListener(StartOfflineGame);
        }

        if (onlineButton != null)
        {
            onlineButton.onClick.RemoveListener(OpenOnlineLobby);
            onlineButton.onClick.AddListener(OpenOnlineLobby);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        eventSystem.enabled = true;
        BaseInputModule inputModule = eventSystem.GetComponent<BaseInputModule>();
        if (inputModule != null)
            inputModule.enabled = true;
    }

    private static Button FindButton(string primaryPath, string fallbackName)
    {
        Button button = GameObject.Find(primaryPath)?.GetComponent<Button>();
        if (button != null)
            return button;

        return GameObject.Find(fallbackName)?.GetComponent<Button>();
    }

    /// <summary>Starts the local playable MVP without creating a Mirror connection.</summary>
    public void StartOfflineGame()
    {
        if (openingOnlineLobby)
            return;

        openingOnlineLobby = true;
        Time.timeScale = 1f;
        ShowOfflineUnavailableNotice();
        StartCoroutine(OpenOnlineLobbyAfterNotice());
    }

    private void ShowOfflineUnavailableNotice()
    {
        if (offlineWipPanel != null)
        {
            offlineWipPanel.SetActive(true);

            CanvasGroup canvasGroup = offlineWipPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        if (offlineWipText != null)
        {
            offlineWipText.text = "Offline mode is currently unavailable.\nWork in progress. Opening online mode...";
        }
    }

    private System.Collections.IEnumerator OpenOnlineLobbyAfterNotice()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(offlineNoticeDuration, 0.1f));
        OpenOnlineLobby();
    }

    private void ResolveOfflineNotice()
    {
        if (offlineWipPanel == null)
        {
            offlineWipPanel = FindObjectByName("OfflineWIP", "WIPPanel", "WIP", "WorkInProgress");
        }

        if (offlineWipText == null && offlineWipPanel != null)
            offlineWipText = offlineWipPanel.GetComponentInChildren<TMP_Text>(true);
    }

    private static GameObject FindObjectByName(params string[] names)
    {
        Transform[] objects = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform candidate in objects)
        {
            foreach (string name in names)
            {
                if (string.Equals(candidate.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return candidate.gameObject;
            }
        }

        return null;
    }

    private void StartOfflineScene()
    {
        if (NetworkServer.active && NetworkManager.singleton != null)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.active && NetworkManager.singleton != null)
            NetworkManager.singleton.StopClient();

        if (!Application.CanStreamedLevelBeLoaded(OfflineSceneName))
        {
            Debug.LogError("The offline MVP scene is not enabled in Build Settings: " + OfflineSceneName);
            return;
        }

        SceneManager.LoadScene(OfflineSceneName, LoadSceneMode.Single);
    }

    /// <summary>Opens the online lobby.</summary>
    public void OpenOnlineLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(LobbySceneName, LoadSceneMode.Single);
    }

    /// <summary>Stops the application in the editor or in a built player.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
