using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class OnlineRoomManager : NetworkRoomManager
{
    private const string DefaultRoomScene = "Assets/lobbyonline.unity";
    private const string DefaultGameplayScene = "Assets/gameonline.unity";
    private const string DefaultOfflineScene = "Assets/MainMenu.unity";
    private const string DefaultAddress = "localhost";

    private Canvas lobbyCanvas;
    private TMP_Text statusText;
    private TMP_Text playersText;
    private TMP_InputField addressInput;
    private Button startGameButton;

    public override void Awake()
    {
        base.Awake();
        ConfigureDefaults();
    }

    public override void Start()
    {
        base.Start();
        ResolveLobbyInterface();
    }

    public override void Update()
    {
        base.Update();

        _lobbySearchTimer -= Time.unscaledDeltaTime;
        if (_lobbySearchTimer <= 0f)
        {
            _lobbySearchTimer = 1f;
            ResolveLobbyInterface();
        }

        if (statusText == null || playersText == null || startGameButton == null)
            return;

        string state = NetworkServer.active && NetworkClient.active
            ? "Host online"
            : NetworkServer.active
                ? "Server online"
                : NetworkClient.active
                    ? "Connected to server"
                    : "Offline";

        statusText.text = state + " | Address: " + networkAddress;
        playersText.text = "Players in lobby: " + roomSlots.Count;
        startGameButton.interactable = NetworkServer.active;
    }

    private float _lobbySearchTimer;

    /// <summary>Starts this instance as the host of the online lobby.</summary>
    public void CreateServer()
    {
        if (!NetworkServer.active && !NetworkClient.active)
        {
            StartHost();
        }
    }

    /// <summary>Connects this instance to the server address entered in the lobby.</summary>
    public void JoinServer()
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            return;
        }

        if (addressInput != null && !string.IsNullOrWhiteSpace(addressInput.text))
        {
            networkAddress = addressInput.text.Trim();
        }

        StartClient();
    }

    /// <summary>Moves every connected lobby player into the online gameplay scene.</summary>
    public void StartOnlineGame()
    {
        if (NetworkServer.active)
        {
            ServerChangeScene(GameplayScene);
        }
    }

    /// <summary>Stops the current connection and returns to the main menu.</summary>
    public void ReturnToMainMenu()
    {
        if (NetworkServer.active && NetworkClient.active)
        {
            StopHost();
        }
        else if (NetworkServer.active)
        {
            StopServer();
        }
        else if (NetworkClient.active)
        {
            StopClient();
        }
        else
        {
            SceneManager.LoadScene(DefaultOfflineScene);
        }
    }

    public override void OnRoomClientSceneChanged()
    {
        base.OnRoomClientSceneChanged();
        ResolveLobbyInterface();

        if (lobbyCanvas != null)
        {
            lobbyCanvas.gameObject.SetActive(SceneManager.GetActiveScene().name == "lobbyonline");
        }
    }

    private void ConfigureDefaults()
    {
        RoomScene = string.IsNullOrWhiteSpace(RoomScene) ? DefaultRoomScene : RoomScene;
        GameplayScene = string.IsNullOrWhiteSpace(GameplayScene) ? DefaultGameplayScene : GameplayScene;
        offlineScene = string.IsNullOrWhiteSpace(offlineScene) ? DefaultOfflineScene : offlineScene;
        onlineScene = RoomScene;
        networkAddress = string.IsNullOrWhiteSpace(networkAddress) ? DefaultAddress : networkAddress;
        maxConnections = Mathf.Max(maxConnections, 2);
        minPlayers = Mathf.Max(minPlayers, 1);
        showRoomGUI = false;
    }

    private void ResolveLobbyInterface()
    {
        GameObject canvasObject = GameObject.Find("OnlineLobbyCanvas");
        if (canvasObject == null)
        {
            return;
        }

        lobbyCanvas = canvasObject.GetComponent<Canvas>();
        statusText = GameObject.Find("OnlineLobbyCanvas/Panel/Status")?.GetComponent<TMP_Text>();
        playersText = GameObject.Find("OnlineLobbyCanvas/Panel/Players")?.GetComponent<TMP_Text>();
        addressInput = GameObject.Find("OnlineLobbyCanvas/Panel/ServerAddress")?.GetComponent<TMP_InputField>();
        startGameButton = GameObject.Find("OnlineLobbyCanvas/Panel/StartGame")?.GetComponent<Button>();
    }
}
