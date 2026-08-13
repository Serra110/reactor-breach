using UnityEngine;

// Casca de banana: quando o jogador a pisa, escorrega numa direção aleatória e a
// casca desaparece. Criada em runtime pelo AccidentManager (sem necessidade de prefab).
public class BananaPeel : MonoBehaviour
{
    public float slipForce = 12f;
    public float lifetime = 60f;

    private bool _triggered;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        var pm = other.GetComponentInParent<PlayerMovement>();
        if (pm == null) return;

        _triggered = true;

        Vector3 slipDir = transform.forward;
        if (Vector3.Dot(slipDir, pm.transform.forward) < 0f)
            slipDir = -slipDir;

        Vector3 force = new Vector3(slipDir.x, 0f, slipDir.z).normalized * slipForce;
        pm.AddSlip(force);

        AccidentManager.Instance?.ShowMessage("Escorregaste numa casca de banana!");

        Destroy(gameObject);
    }
}
