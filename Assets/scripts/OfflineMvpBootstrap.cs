using TMPro;

using Mirror;
using ReactorBreach.InventorySystem;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine;

using UnityEngine.UI;

/// <summary>
/// Builds the focused offline MVP facility around the existing reactor gameplay.
/// </summary>
public sealed class OfflineMvpBootstrap : MonoBehaviour
{
    private const string FacilityRootName = "MVP Facility";
    private const string OfflineSceneName = "scene2";

    private ReactorController reactor;

    private GameObject offlinePlayer;
    private TMP_Text statusText;
    private TMP_Text objectiveText;
    private float statusRefreshTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneBootstrap()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += TryBootstrapScene;
        TryBootstrapScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private static void TryBootstrapScene(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name != OfflineSceneName)
            return;

        if (FindFirstObjectByType<OfflineMvpBootstrap>() != null)
            return;

        new GameObject("Offline MVP Bootstrap").AddComponent<OfflineMvpBootstrap>();
    }

    private void Awake()
    {
        BuildFacility();
        ConfigureOfflinePlayer();
        ConfigureExistingGameplayObjects();
        ConfigureAiAndNavigation();
        CreateHud();
    }

    private void Update()
    {
        EnsureOfflinePlayerActive();

        if (Input.GetKeyDown(KeyCode.R) && reactor != null)
            reactor.StartReactor();

        if (Input.GetKeyDown(KeyCode.T) && reactor != null)
            reactor.Scram();

        statusRefreshTimer -= Time.unscaledDeltaTime;
        if (statusRefreshTimer <= 0f)
        {
            statusRefreshTimer = 0.15f;
            RefreshHud();
        }
    }

    private void BuildFacility()
    {
        if (GameObject.Find(FacilityRootName) != null)
            return;

        HideLegacyScenery();

        GameObject root = new GameObject(FacilityRootName);
        Material floorMaterial = CreateMaterial("Facility Floor", new Color(0.075f, 0.095f, 0.12f), 0.65f, 0.1f);
        Material wallMaterial = CreateMaterial("Facility Wall", new Color(0.16f, 0.19f, 0.23f), 0.85f, 0.05f);
        Material trimMaterial = CreateMaterial("Facility Trim", new Color(0.04f, 0.45f, 0.5f), 0.55f, 1.6f);
        Material warningMaterial = CreateMaterial("Warning Trim", new Color(0.65f, 0.2f, 0.04f), 0.5f, 0.8f);

        CreateBox("Main Floor", root.transform, new Vector3(0f, -0.15f, 0f), new Vector3(60f, 0.3f, 36f), floorMaterial);
        CreateBox("Entrance Floor", root.transform, new Vector3(0f, -0.12f, -14f), new Vector3(12f, 0.24f, 8f), trimMaterial);
        CreateBox("Reactor Floor", root.transform, new Vector3(20f, -0.1f, 7f), new Vector3(18f, 0.2f, 18f), warningMaterial);
        CreateBox("Control Floor", root.transform, new Vector3(0f, -0.1f, 10f), new Vector3(22f, 0.2f, 10f), trimMaterial);
        CreateBox("Workshop Floor", root.transform, new Vector3(-19f, -0.1f, 5f), new Vector3(16f, 0.2f, 16f), wallMaterial);

        CreateBox("North Wall", root.transform, new Vector3(0f, 2.2f, 18f), new Vector3(60f, 4.4f, 0.5f), wallMaterial);
        CreateBox("South Wall Left", root.transform, new Vector3(-24f, 2.2f, -18f), new Vector3(12f, 4.4f, 0.5f), wallMaterial);
        CreateBox("South Wall Right", root.transform, new Vector3(24f, 2.2f, -18f), new Vector3(12f, 4.4f, 0.5f), wallMaterial);
        CreateBox("West Wall", root.transform, new Vector3(-30f, 2.2f, 0f), new Vector3(0.5f, 4.4f, 36f), wallMaterial);
        CreateBox("East Wall", root.transform, new Vector3(30f, 2.2f, 0f), new Vector3(0.5f, 4.4f, 36f), wallMaterial);

        CreateBox("Reactor Divider Left", root.transform, new Vector3(11f, 2.2f, 14f), new Vector3(0.5f, 4.4f, 8f), wallMaterial);
        CreateBox("Reactor Divider Right", root.transform, new Vector3(11f, 2.2f, 0f), new Vector3(0.5f, 4.4f, 16f), wallMaterial);
        CreateBox("Workshop Divider Upper", root.transform, new Vector3(-11f, 2.2f, 12f), new Vector3(0.5f, 4.4f, 12f), wallMaterial);
        CreateBox("Workshop Divider Lower", root.transform, new Vector3(-11f, 2.2f, -9f), new Vector3(0.5f, 4.4f, 18f), wallMaterial);
        CreateBox("Control Divider Left", root.transform, new Vector3(-7.5f, 2.2f, 5f), new Vector3(5f, 4.4f, 0.5f), wallMaterial);
        CreateBox("Control Divider Right", root.transform, new Vector3(7.5f, 2.2f, 5f), new Vector3(5f, 4.4f, 0.5f), wallMaterial);

        CreateTrimLine(root.transform, new Vector3(-28f, 0.03f, -17.4f), new Vector3(56f, 0.06f, 0.18f), trimMaterial);
        CreateTrimLine(root.transform, new Vector3(11.4f, 0.03f, 7f), new Vector3(0.18f, 0.06f, 18f), warningMaterial);
        CreateTrimLine(root.transform, new Vector3(-29.4f, 0.03f, 5f), new Vector3(0.18f, 0.06f, 25f), trimMaterial);

        for (int x = -25; x <= 25; x += 10)
        {
            CreateBox("Ceiling Beam", root.transform, new Vector3(x, 4.35f, 0f), new Vector3(0.32f, 0.32f, 35f), wallMaterial);
        }

        CreateLight(root.transform, new Vector3(0f, 3.4f, -10f), new Color(0.35f, 0.8f, 1f), 5f, 18f);
        CreateLight(root.transform, new Vector3(-19f, 3.4f, 5f), new Color(0.2f, 0.75f, 0.9f), 4f, 16f);
        CreateLight(root.transform, new Vector3(20f, 3.4f, 7f), new Color(1f, 0.25f, 0.08f), 5f, 18f);
        CreateLight(root.transform, new Vector3(0f, 3.4f, 11f), new Color(0.3f, 1f, 0.55f), 4f, 16f);

        CreateLabel(root.transform, "ENTRANCE // REACTOR BREACH", new Vector3(0f, 3.1f, -17.55f), 0.55f, Color.white);
        CreateLabel(root.transform, "CONTROL ROOM", new Vector3(0f, 2.8f, 14.5f), 0.45f, new Color(0.4f, 1f, 0.65f));
        CreateLabel(root.transform, "REACTOR CHAMBER", new Vector3(20f, 2.8f, 16f), 0.45f, new Color(1f, 0.35f, 0.15f));
        CreateLabel(root.transform, "WORKSHOP", new Vector3(-19f, 2.8f, 12f), 0.45f, new Color(0.3f, 0.8f, 1f));
    }

    private void HideLegacyScenery()
    {
        string[] names =
        {
            "Plane", "Plane (1)", "Plane (2)", "Wall", "Wall (1)",
            "Cube", "Cube (1)", "Cube (2)", "Cube (3)", "Cube (4)",
            "PolyShape", "PolyShape (1)", "PolyShape (2)", "PolyShape (3)",
            "PolyShape (4)", "PolyShape (5)", "CutGemsVar1", "Buildobjects",
            "gameobjects"
        };

        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (GameObject rootObject in activeScene.GetRootGameObjects())
        {
            bool matches = rootObject.name.StartsWith("pb_Mesh");
            if (!matches)
            {
                foreach (string name in names)
                {
                    if (rootObject.name == name)
                    {
                        matches = true;
                        break;
                    }
                }
            }

            if (matches && !HasGameplayComponents(rootObject))
                rootObject.SetActive(false);
        }
    }

    private static bool HasGameplayComponents(GameObject target)
    {
        return target.GetComponentInChildren<ReactorController>(true) != null
            || target.GetComponentInChildren<Storage>(true) != null
            || target.GetComponentInChildren<SmelterController>(true) != null
            || target.GetComponentInChildren<Conveyor>(true) != null
            || target.GetComponentInChildren<Inventory>(true) != null
            || target.GetComponentInChildren<Port>(true) != null
            || target.GetComponentInChildren<NetworkIdentity>(true) != null;
    }

    private void ConfigureOfflinePlayer()
    {
        offlinePlayer = GameObject.Find("OfflinePlayer");
        if (offlinePlayer == null)
        {
            PlayerMovement movementFallback = FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            if (movementFallback != null)
                offlinePlayer = movementFallback.gameObject;
        }

        if (offlinePlayer == null)
            return;

        offlinePlayer.SetActive(true);
        DisableNetworkComponents(offlinePlayer);

        CharacterController controller = offlinePlayer.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        offlinePlayer.transform.SetPositionAndRotation(new Vector3(0f, 1.05f, -14f), Quaternion.identity);

        if (controller != null)
            controller.enabled = true;

        PlayerMovement movement = offlinePlayer.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = true;

        PlayerInteraction interaction = offlinePlayer.GetComponent<PlayerInteraction>();
        if (interaction != null)
            interaction.enabled = true;

        Camera playerCamera = offlinePlayer.GetComponentInChildren<Camera>(true);
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            playerCamera.tag = "MainCamera";
        }

        CameraFollower follower = offlinePlayer.GetComponentInChildren<CameraFollower>(true);
        if (follower != null)
            follower.enabled = true;

        GameObject respawn = GameObject.Find("RespawnPoint");
        if (respawn != null)
            respawn.transform.position = new Vector3(0f, 1.05f, -14f);
    }

    private void EnsureOfflinePlayerActive()
    {
        if (offlinePlayer == null)
            return;
        if (!offlinePlayer.activeSelf)
        {
            offlinePlayer.SetActive(true);
            ConfigureOfflinePlayer();
        }
    }

    private static void DisableNetworkComponents(GameObject player)
    {
        NetworkIdentity identity = player.GetComponent<NetworkIdentity>();
        if (identity != null)
            identity.enabled = false;

        NetworkBehaviour[] networkBehaviours = player.GetComponents<NetworkBehaviour>();
        foreach (NetworkBehaviour networkBehaviour in networkBehaviours)
            networkBehaviour.enabled = false;
    }

    private void ConfigureExistingGameplayObjects()
    {
        reactor = GameObject.Find("Nuclear Reactor")?.GetComponent<ReactorController>();
        SetPosition("Nuclear Reactor", new Vector3(20f, 0f, 8f), Quaternion.Euler(0f, 90f, 0f));
        SetPosition("ButtonDashboard 1", new Vector3(15.5f, 1f, 8f), Quaternion.Euler(0f, 90f, 0f));
        SetPosition("LeverDashboard", new Vector3(15.5f, 1f, 5f), Quaternion.Euler(0f, 90f, 0f));
        SetPosition("smelter", new Vector3(-20f, 0f, 5f), Quaternion.identity);
        SetPosition("Fuel Tank", new Vector3(-20f, 0f, 10f), Quaternion.identity);
        SetPosition("eletric", new Vector3(0f, 0f, 11f), Quaternion.identity);
    }

    private void ConfigureAiAndNavigation()
    {
        NavMeshSurface surface = FindFirstObjectByType<NavMeshSurface>(FindObjectsInactive.Include);
        if (surface != null)
        {
            surface.gameObject.SetActive(true);
            surface.transform.position = Vector3.zero;
            surface.center = new Vector3(0f, 2f, 0f);
            surface.size = new Vector3(58f, 6f, 34f);
            surface.BuildNavMesh();
        }

        NPC npc = FindFirstObjectByType<NPC>(FindObjectsInactive.Include);
        if (npc == null)
            return;

        npc.gameObject.SetActive(true);

        foreach (Camera npcCamera in npc.GetComponentsInChildren<Camera>(true))
            npcCamera.enabled = false;
        foreach (AudioListener npcListener in npc.GetComponentsInChildren<AudioListener>(true))
            npcListener.enabled = false;

        npc.transform.position = new Vector3(25f, 1f, -10f);
        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
            agent.Warp(new Vector3(25f, 1f, -10f));
    }

    private void CreateHud()
    {
        GameObject hud = new GameObject("MVP Status HUD");
        Canvas canvas = hud.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        CanvasScaler scaler = hud.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        hud.AddComponent<GraphicRaycaster>().enabled = false;

        GameObject panel = new GameObject("Status Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(hud.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-24f, -24f);
        panelRect.sizeDelta = new Vector2(370f, 128f);
        panel.GetComponent<Image>().color = new Color(0.025f, 0.04f, 0.06f, 0.92f);
        panel.GetComponent<Image>().raycastTarget = false;

        objectiveText = CreateHudText(panel.transform, "Objective", new Vector2(18f, -14f), new Vector2(334f, 30f), 20f, Color.white);
        statusText = CreateHudText(panel.transform, "Status", new Vector2(18f, -50f), new Vector2(334f, 60f), 16f, new Color(0.55f, 0.9f, 1f));
        objectiveText.text = "MVP // RESTORE REACTOR POWER";
        RefreshHud();
    }

    private void RefreshHud()
    {
        if (statusText == null)
            return;

        if (reactor == null)
        {
            statusText.text = "Reach the reactor chamber\nWASD move  •  Mouse look  •  E interact";
            return;
        }

        statusText.text = string.Format(
            "REACTOR: {0}\nPOWER {1:0}%  COOLANT {2:0}%  FUEL {3}/{4}\nR start  •  T SCRAM  •  Esc pause",
            ReactorStatus.GetName(reactor.State),
            reactor.Power * 100f,
            reactor.CoolantLevel * 100f,
            reactor.Fuel,
            reactor.maxFuel);
        statusText.color = reactor.AlarmLevel > 0 ? new Color(1f, 0.45f, 0.25f) : new Color(0.55f, 0.9f, 1f);
    }

    private static TMP_Text CreateHudText(Transform parent, string objectName, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateBox(string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(parent, false);
        box.transform.SetPositionAndRotation(position, Quaternion.identity);
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private static void CreateTrimLine(Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        CreateBox("Floor Marking", parent, position, scale, material);
    }

    private static void CreateLight(Transform parent, Vector3 position, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject("Facility Light");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
    }

    private static TMP_Text CreateLabel(Transform parent, string text, Vector3 position, float size, Color color)
    {
        GameObject labelObject = new GameObject(text, typeof(TextMeshPro));
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = position;
        labelObject.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }

    private static Material CreateMaterial(string name, Color color, float metallic, float emissionStrength)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader) { name = name };
        material.color = color;
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.72f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emissionStrength);
        }
        return material;
    }

    private static void SetPosition(string objectName, Vector3 position, Quaternion rotation)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
            target.transform.SetPositionAndRotation(position, rotation);
    }
}
