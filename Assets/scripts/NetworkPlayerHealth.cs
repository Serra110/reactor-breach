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

    public override void OnStartLocalPlayer()
    {
        localHealthBar = FindFirstObjectByType<HealthBar>();
        if (localHealthBar != null)
        {
            localHealthBar.SetMaxHealth(maxHealth);
            localHealthBar.SetHealth(currentHealth);
        }
    }

    public override void OnStartServer()
    {
        startPosition = transform.position;
        if (respawnPoint == null)
        {
            GameObject respawnObject = GameObject.Find("RespawnPoint");
            if (respawnObject != null)
                respawnPoint = respawnObject.transform;
        }
        currentHealth = maxHealth;
        dead = false;
    }

    [ServerCallback]
    private void Update()
    {
        if (!dead && transform.position.y < killY)
            Respawn();
    }

    [Server]
    public void TakeDamage(int amount)
    {
        if (dead || amount <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth == 0)
            Respawn();
    }

    [Server]
    public void Respawn()
    {
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
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            if (localHealthBar == null)
                localHealthBar = FindFirstObjectByType<HealthBar>();
            if (localHealthBar != null)
                localHealthBar.SetHealth(newValue);
        }

        PlayerHealth legacyHealth = GetComponent<PlayerHealth>();
        if (legacyHealth != null)
            legacyHealth.currentHealth = newValue;
    }
}
