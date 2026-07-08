using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseUI;
    [SerializeField] private CameraFollower cameraFollower;
    [SerializeField] private GameObject mineralsUI;

    private bool isPaused = false;

    void Start()
    {
        ResolvePauseUI();

        if (pauseUI != null)
            pauseUI.SetActive(false);

        if (cameraFollower == null)
            cameraFollower = FindObjectOfType<CameraFollower>();

        if (mineralsUI == null)
        {
            InventoryUI[] inventoryUIs = FindObjectsOfType<InventoryUI>(true);
            if (inventoryUIs.Length > 0)
                mineralsUI = inventoryUIs[0].gameObject;
        }

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
    }

    public void Pause()
    {
        SetPauseUIActive(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPauseState(true);
        isPaused = true;
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

    private void SetupPauseButtons()
    {
        if (pauseUI == null)
            return;

        Button[] buttons = pauseUI.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            string buttonName = button.name;

            if (buttonName.Contains("Resume") || buttonName.Contains("Continue"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(Resume);
            }
            else if (buttonName.Contains("Menu") || buttonName.Contains("Restart") || buttonName.Contains("Main"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(MainMenu);
            }
            else if (buttonName.Contains("Quit"))
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

#pragma warning disable CS0618
        InventoryUI[] inventoryUIs = FindObjectsOfType<InventoryUI>(true);
#pragma warning restore CS0618
        foreach (InventoryUI inventoryUI in inventoryUIs)
        {
            if (inventoryUI != null)
                inventoryUI.gameObject.SetActive(!paused);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        Debug.Log("Voltando para o MainMenu...");
        SceneManager.LoadScene("MainMenu");
    }
}