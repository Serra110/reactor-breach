using UnityEngine;
using UnityEngine.UI;
using ReactorBreach.InventorySystem;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Queda Fora do Mapa")]
    public float killY = -20f;

    [Header("Respawn")]
    public Transform respawnPoint;

    [Header("UI")]
    public HealthBar healthBar;

    private Vector3 startPosition;
    private bool dead;

    private void Start()
    {
        startPosition = transform.position;
        currentHealth = maxHealth;
        dead = false;

        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);
    }

    private void Update()
    {
        if (dead) return;

        if (transform.position.y < killY)
        {
            Die();
            return;
        }
    }

    public void TakeDamage(int amount)
    {
        if (dead || amount <= 0) return;

        currentHealth -= amount;
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (dead) return;
        dead = true;

        Respawn();
    }

    private void Respawn()
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        transform.position = respawnPoint != null ? respawnPoint.position : startPosition;

        if (cc != null)
            cc.enabled = true;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.ResetVelocity();

        if (Inventory.Instance != null)
            Inventory.Instance.Clear();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.Clear();

        currentHealth = maxHealth;
        dead = false;

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }
}
