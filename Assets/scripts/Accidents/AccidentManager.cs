using System.Collections;
using UnityEngine;

// MVP do Sistema de Acidentes: de vez em quando acontece qualquer coisa na instalação.
// - 60%: só uma mensagem caótica
// - 30%: mensagem + casca de banana perto do jogador
// - 10%: duas cascas de banana
//
// OPCIONAL/INACTIVO POR DEFEITO: não se cria sozinho. Só funciona se adicionares este
// componente a um objeto na cena (e deixares o checkbox enabled ligado). O loop respeita
// o estado `enabled` — desligar o componente desliga os acidentes sem o destruir.
public class AccidentManager : MonoBehaviour
{
    public float minInterval = 30f;
    public float maxInterval = 90f;
    public float messageDuration = 6f;

    public static AccidentManager Instance { get; private set; }

    private static readonly string[] Messages =
    {
        "Pedro overloaded Generator #2",
        "The intern pressed a suspicious button",
        "Someone forgot uranium safety procedures",
        "O estagiário está a 'ajudar'.",
        "Quota amanhã. Produção a 80%? Nice try.",
        "Pelo menos não foi o reator. Ainda.",
        "Extintor deslocado. Provavelmente o estagiário.",
        "Líquido misterioso encontrado na máquina de café.",
        "Alarme de fumo... falso. Provavelmente.",
        "Alguém deixou o reator em modo turbo. Outra vez.",
    };

    private string _currentMessage;
    private float _messageTimer;
    private static Material _peelMaterial;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        StartCoroutine(AccidentLoop());
    }

    private IEnumerator AccidentLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            if (!enabled) continue;
            TriggerAccident();
        }
    }

    private void TriggerAccident()
    {
        int roll = Random.Range(0, 100);
        if (roll < 60)
        {
            ShowMessage(Messages[Random.Range(0, Messages.Length)]);
        }
        else if (roll < 90)
        {
            ShowMessage("Cuidado! Casca de banana no chão!");
            SpawnBananaPeel();
        }
        else
        {
            SpawnBananaPeel();
            SpawnBananaPeel();
        }
    }

    public void ShowMessage(string msg)
    {
        _currentMessage = msg;
        _messageTimer = messageDuration;
    }

    private void SpawnBananaPeel()
    {
        var player = FindFirstObjectByType<PlayerMovement>();
        Vector3 basePos = player != null ? player.transform.position : Vector3.zero;

        Vector3 pos = basePos + new Vector3(Random.Range(-4f, 4f), 5f, Random.Range(-4f, 4f));
        if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 50f))
            pos = hit.point + Vector3.up * 0.05f;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "BananaPeel";
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        go.transform.localScale = new Vector3(0.6f, 0.04f, 0.4f);

        var rend = go.GetComponent<MeshRenderer>();
        rend.sharedMaterial = GetPeelMaterial();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var col = go.GetComponent<Collider>();
        col.isTrigger = true;

        go.AddComponent<BananaPeel>();
    }

    private static Material GetPeelMaterial()
    {
        if (_peelMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            _peelMaterial = new Material(shader);
            _peelMaterial.color = new Color(1f, 0.8f, 0f);
        }
        return _peelMaterial;
    }

    private void Update()
    {
        if (_messageTimer > 0f)
            _messageTimer -= Time.unscaledDeltaTime;
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(_currentMessage) || _messageTimer <= 0f)
            return;

        float alpha = Mathf.Clamp01(_messageTimer / 1.5f);

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        style.normal.textColor = new Color(1f, 0.9f, 0.35f, alpha);

        GUI.Label(new Rect(20, 20, Screen.width - 40, 100), _currentMessage, style);
    }
}
