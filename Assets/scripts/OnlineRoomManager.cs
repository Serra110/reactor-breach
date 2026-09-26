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

        // FIX (WebGL): isto corria em TODOS os frames e concatenava 3 strings por
        // frame, mesmo quando nada mudava. Isso gera recolha de lixo constante
        // (visivel como stutter no browser). Agora so escreve quando o valor muda.
        string state = NetworkServer.active && NetworkClient.active
            ? "Host online"
            : NetworkServer.active
                ? "Server online"
                : NetworkClient.active
                    ? "Connected to server"
                    : "Offline";

        string status = state + " | Address: " + networkAddress;
        if (status != _lastStatusText)
        {
            _lastStatusText = status;
            statusText.text = status;
        }

        int playerCount = roomSlots.Count;
        if (playerCount != _lastPlayerCount)
        {
            _lastPlayerCount = playerCount;
            playersText.text = "Players in lobby: " + playerCount;
        }

        bool canStart = NetworkServer.active;
        if (canStart != _lastCanStart)
        {
            _lastCanStart = canStart;
            startGameButton.interactable = canStart;
        }
    }

    private float _lobbySearchTimer;
    private string _lastStatusText;
    private int _lastPlayerCount = -1;
    private bool _lastCanStart;

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
        // FIX (WebGL): isto fazia 5 GameObject.Find por segundo durante o jogo todo.
        // Quando a interface ja esta resolvida nao ha nada a refazer, por isso sai logo.
        if (_lobbyInterfaceResolved && lobbyCanvas != null && statusText != null && playersText != null && startGameButton != null)
            return;

        GameObject canvasObject = GameObject.Find("OnlineLobbyCanvas");
        if (canvasObject == null)
        {
            _lobbyInterfaceResolved = false;
            return;
        }

        lobbyCanvas = canvasObject.GetComponent<Canvas>();
        statusText = GameObject.Find("OnlineLobbyCanvas/Panel/Status")?.GetComponent<TMP_Text>();
        playersText = GameObject.Find("OnlineLobbyCanvas/Panel/Players")?.GetComponent<TMP_Text>();
        addressInput = GameObject.Find("OnlineLobbyCanvas/Panel/ServerAddress")?.GetComponent<TMP_InputField>();
        startGameButton = GameObject.Find("OnlineLobbyCanvas/Panel/StartGame")?.GetComponent<Button>();

        _lobbyInterfaceResolved = lobbyCanvas != null && statusText != null && playersText != null && startGameButton != null;

        // Invalida o cache de texto para forcar a escrita do estado atual.
        _lastStatusText = null;
        _lastPlayerCount = -1;
    }

    private bool _lobbyInterfaceResolved;
}
