using Mirror;
using UnityEngine;

public sealed class NetworkPlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;
    public float killY = -20f;
    public Transform respawnPoint;

    private Vector3 startPosition;
    private bool dead;
    private HealthBar localHealthBar;

    private bool IsOffline => OfflineMvpBootstrap.IsOffline && !NetworkServer.active && !NetworkClient.active;

    private void Start()
    {
        if (NetworkServer.active)
            return;

        startPosition = transform.position;
        ResolveRespawnPoint();
        currentHealth = maxHealth;
        dead = false;
        RefreshLocalHealthBar();
    }

    public override void OnStartLocalPlayer()
    {
        RefreshLocalHealthBar();
    }

    public override void OnStartServer()
    {
        startPosition = transform.position;
        ResolveRespawnPoint();
        currentHealth = maxHealth;
        dead = false;
    }

    private void Update()
    {
        if (!NetworkServer.active && !IsOffline)
            return;

        if (!dead && transform.position.y < killY)
            Respawn();
    }

    public void TakeDamage(int amount)
    {
        if (!NetworkServer.active && !IsOffline)
            return;

        if (dead || amount <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        RefreshLocalHealthBar();
        if (currentHealth == 0)
            Respawn();
    }

    public void Respawn()
    {
        if (!NetworkServer.active && !IsOffline)
            return;

        dead = true;
        NetworkPlayerMovement movement = GetComponent<NetworkPlayerMovement>();
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        transform.position = respawnPoint != null ? respawnPoint.position : startPosition;

        if (controller != null)
            controller.enabled = true;
        if (movement != null)
            movement.ResetVelocity();

        currentHealth = maxHealth;
        dead = false;
        RefreshLocalHealthBar();
    }

    private void ResolveRespawnPoint()
    {
        if (respawnPoint == null)
        {
            GameObject respawnObject = GameObject.Find("RespawnPoint");
            if (respawnObject != null)
                respawnPoint = respawnObject.transform;
        }
    }

    private void RefreshLocalHealthBar()
    {
        if (localHealthBar == null)
            localHealthBar = FindFirstObjectByType<HealthBar>(FindObjectsInactive.Include);

        if (localHealthBar != null && localHealthBar.healthSlider != null)
        {
            localHealthBar.SetMaxHealth(maxHealth);
            localHealthBar.SetHealth(currentHealth);
        }
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer || IsOffline)
        {
            if (localHealthBar == null)
                localHealthBar = FindFirstObjectByType<HealthBar>(FindObjectsInactive.Include);
            if (localHealthBar != null && localHealthBar.healthSlider != null)
                localHealthBar.SetHealth(newValue);
        }

        PlayerHealth legacyHealth = GetComponent<PlayerHealth>();
        if (legacyHealth != null)
            legacyHealth.currentHealth = newValue;
    }
}
