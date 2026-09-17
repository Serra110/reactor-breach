using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseUI;
    [SerializeField] private CameraFollower cameraFollower;
    [SerializeField] private GameObject mineralsUI;

    private Canvas pauseCanvas;
    private int originalSortingOrder;

    private bool isPaused = false;

    void Start()
    {
        EnsureEventSystem();
        ResolvePauseUI();
        CachePauseCanvas();

        if (pauseUI != null)
            pauseUI.SetActive(false);

        if (cameraFollower == null)
            cameraFollower = FindFirstObjectByType<CameraFollower>();

        SetupPauseButtons();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        SetPauseUIActive(false);
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetPauseState(false);
        isPaused = false;
        RestoreCanvasOrder();
    }

    public void Pause()
    {
        SetPauseUIActive(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPauseState(true);
        isPaused = true;
        RaiseCanvasOrder();
    }

    private void ResolvePauseUI()
    {
        if (pauseUI != null)
            return;

        pauseUI = transform.Find("PauseUI")?.gameObject;

        if (pauseUI == null)
            pauseUI = transform.Find("PauseMenu")?.gameObject;

        if (pauseUI == null)
            pauseUI = GameObject.Find("PauseUI");

        if (pauseUI == null)
            pauseUI = GameObject.Find("PauseMenu");

        if (pauseUI == null)
            Debug.LogWarning("[PauseMenu] pauseUI não foi atribuída e nenhum objeto com o nome PauseUI/PauseMenu foi encontrado.");
    }

    private void CachePauseCanvas()
    {
        if (pauseUI == null)
            return;

        pauseCanvas = pauseUI.GetComponentInParent<Canvas>(true);
        if (pauseCanvas != null)
            originalSortingOrder = pauseCanvas.sortingOrder;
    }

    private void RaiseCanvasOrder()
    {
        if (pauseCanvas != null)
            pauseCanvas.sortingOrder = 1000;
    }

    private void RestoreCanvasOrder()
    {
        if (pauseCanvas != null)
            pauseCanvas.sortingOrder = originalSortingOrder;
    }

    private void SetupPauseButtons()
    {
        if (pauseUI == null)
            return;

        Button[] buttons = pauseUI.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            string buttonName = button.name.ToLowerInvariant();

            if (buttonName.Contains("resume") || buttonName.Contains("continue"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(Resume);
            }
            else if (buttonName.Contains("menu") || buttonName.Contains("restart") || buttonName.Contains("main"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(MainMenu);
            }
            else if (buttonName.Contains("quit"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(QuitGame);
            }
        }
    }

    private void SetPauseUIActive(bool active)
    {
        if (pauseUI != null)
            pauseUI.SetActive(active);
    }

    private void SetPauseState(bool paused)
    {
        if (cameraFollower != null)
            cameraFollower.enabled = !paused;

        if (mineralsUI != null)
            mineralsUI.SetActive(!paused);
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

    public void QuitGame()
    {
        Application.Quit();
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;

        if (NetworkManager.singleton != null &&
            (NetworkServer.active || NetworkClient.active))
        {
            if (NetworkServer.active && NetworkClient.active)
                NetworkManager.singleton.StopHost();
            else if (NetworkServer.active)
                NetworkManager.singleton.StopServer();
            else
                NetworkManager.singleton.StopClient();

            // Mirror loads its configured offline scene after stopping the connection.
            return;
        }

        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}