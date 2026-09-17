using Mirror;
using UnityEngine;

public sealed class NetworkDiagnosticsHUD : MonoBehaviour
{
    public bool visibleByDefault = true;
    public KeyCode toggleKey = KeyCode.F3;
    public KeyCode freezeKey = KeyCode.F4;
    public KeyCode logSnapshotKey = KeyCode.F6;
    public bool autoHideInPlayMode = true;

    private float smoothedFps;
    private float frameTimeMs;
    private float sampleTimer;
    private int frameCount;
    private bool frozen;
    private GUIStyle panelStyle;
    private GUIStyle textStyle;
    private GUIStyle titleStyle;

    private void Awake()
    {
        if (autoHideInPlayMode)
            visibleByDefault = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            visibleByDefault = !visibleByDefault;
        if (Input.GetKeyDown(freezeKey))
            frozen = !frozen;
        if (Input.GetKeyDown(logSnapshotKey))
            LogSnapshot();

        if (frozen)
            return;

        float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        frameCount++;
        sampleTimer += Time.unscaledDeltaTime;
        if (sampleTimer >= 0.25f)
        {
            smoothedFps = frameCount / sampleTimer;
            frameTimeMs = deltaTime * 1000f;
            frameCount = 0;
            sampleTimer = 0f;
        }
    }

    private void OnGUI()
    {
        if (!visibleByDefault)
            return;

        EnsureStyles();
        NetworkPlayerMovement movement = GetLocalMovement();
        Mirror.NetworkTransformReliable networkTransform = GetLocalNetworkTransform();
        NetworkStatistics statistics = NetworkManager.singleton != null
            ? NetworkManager.singleton.GetComponent<NetworkStatistics>()
            : null;

        float rttMs = NetworkClient.active ? (float)(NetworkTime.rtt * 1000d) : -1f;
        float jitterMs = NetworkClient.active ? (float)(NetworkTime.rttVariance * 1000d) : -1f;
        string role = GetNetworkRole();
        string quality = NetworkClient.active ? NetworkClient.connectionQuality.ToString() : "Offline";

        GUILayout.BeginArea(new Rect(12f, 12f, 430f, 430f), panelStyle);
        GUILayout.Label("NETWORK MOVEMENT DIAGNOSTICS", titleStyle);
        GUILayout.Label($"F3 toggle | F4 freeze: {frozen} | F6 log snapshot", textStyle);
        GUILayout.Label($"Role: {role} | Client active: {NetworkClient.active} | Server active: {NetworkServer.active}", textStyle);
        GUILayout.Label($"FPS: {smoothedFps:0.0} | Frame: {frameTimeMs:0.00} ms", textStyle);
        GUILayout.Label($"RTT: {FormatMs(rttMs)} | Jitter: {FormatMs(jitterMs)} | Quality: {quality}", textStyle);
        GUILayout.Label($"Mirror send rate: {NetworkClient.sendRate} Hz | Server tick: {NetworkServer.tickRate} Hz", textStyle);

        if (statistics != null)
        {
            GUILayout.Label($"Packets in/out: {statistics.clientReceivedPacketsPerSecond}/{statistics.clientSentPacketsPerSecond} p/s", textStyle);
            GUILayout.Label($"Bytes in/out: {statistics.clientReceivedBytesPerSecond}/{statistics.clientSentBytesPerSecond} B/s", textStyle);
        }

        if (movement == null)
        {
            GUILayout.Label("Movement: local player not available", textStyle);
        }
        else
        {
            GUILayout.Label("--- MOVEMENT PATH ---", titleStyle);
            GUILayout.Label($"Owned: {movement.isOwned} | Seq sent/ack: {movement.LastMovementSequence}/{movement.LastAcknowledgedSequence}", textStyle);
            GUILayout.Label($"Input: {movement.CurrentInput} | Sprint: {movement.CurrentSprint}", textStyle);
            GUILayout.Label($"Input age: {FormatMs(movement.LastInputSendAgeMs)} | Ack age: {FormatMs(movement.LastMovementAckAgeMs)}", textStyle);
            GUILayout.Label($"Telemetry command RTT: {FormatMs(movement.LastMovementCommandRttMs)}", textStyle);
            GUILayout.Label($"Local speed: {movement.horizontalSpeed:0.00} | Position: {FormatVector(movement.transform.position)}", textStyle);
            GUILayout.Label($"Last server position: {FormatVector(movement.LastServerPosition)}", textStyle);
            GUILayout.Label($"Local/server position error: {movement.LastServerPositionError:0.000} m", textStyle);
        }

        if (networkTransform != null)
        {
            GUILayout.Label($"Transform authority: {networkTransform.syncDirection} | Snapshots: {networkTransform.clientSnapshots.Count}", textStyle);
            GUILayout.Label($"Transform mode: {(networkTransform.onlySyncOnChange ? "On change" : "Every tick")} | Interpolation: {networkTransform.interpolatePosition}", textStyle);
        }

        GUILayout.EndArea();
    }

    private NetworkPlayerMovement GetLocalMovement()
    {
        if (NetworkClient.localPlayer == null)
            return null;
        return NetworkClient.localPlayer.GetComponent<NetworkPlayerMovement>();
    }

    private Mirror.NetworkTransformReliable GetLocalNetworkTransform()
    {
        if (NetworkClient.localPlayer == null)
            return null;
        return NetworkClient.localPlayer.GetComponent<Mirror.NetworkTransformReliable>();
    }

    private string GetNetworkRole()
    {
        if (NetworkServer.active && NetworkClient.active)
            return "Host";
        if (NetworkServer.active)
            return "Server";
        if (NetworkClient.active)
            return "Client";
        return "Offline";
    }

    private string FormatMs(float milliseconds)
    {
        return milliseconds < 0f ? "--" : $"{milliseconds:0.0} ms";
    }

    private string FormatVector(Vector3 value)
    {
        return $"({value.x:0.00}, {value.y:0.00}, {value.z:0.00})";
    }

    private void LogSnapshot()
    {
        NetworkPlayerMovement movement = GetLocalMovement();
        Debug.Log(movement == null
            ? "[NetworkDiagnostics] No local player yet."
            : $"[NetworkDiagnostics] role={GetNetworkRole()} fps={smoothedFps:0.0} rttMs={(NetworkTime.rtt * 1000d):0.0} movementSeq={movement.LastMovementSequence} ack={movement.LastAcknowledgedSequence} commandRttMs={movement.LastMovementCommandRttMs:0.0} positionError={movement.LastServerPositionError:0.000} position={movement.transform.position}");
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 8, 8)
        };
        textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = Color.white }
        };
        titleStyle = new GUIStyle(textStyle)
        {
            fontStyle = FontStyle.Bold
        };
    }
}
