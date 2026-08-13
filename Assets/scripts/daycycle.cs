using UnityEngine;


public class DayNightCycle : MonoBehaviour
{             
     
    [Header("Referências")]
    public Light sunLight;    
    public Light moonLight;
    public Material skyboxMaterial;

    [Header("Tempo")]
    [Tooltip("Hora atual do dia, 0-24. Podes arrastar isto em Play mode para testar.")]
    [Range(0f, 24f)] public float currentHour = 8f;

    [Tooltip("Quantos MINUTOS reais demora um dia completo (24h) a passar.")]
    [Min(0.01f)] public float dayLengthInMinutes = 20f;

    [Tooltip("Se false, o tempo não avança (útil para pausa/menus).")]
    public bool timeIsRunning = true;

    
    [Header("Debug (calculado automaticamente)")]
    [SerializeField] private float hoursPerSecond;

    [Header("Cores da luz (Sol) ao longo do dia")]
    public Gradient sunColor;
    public AnimationCurve sunIntensity = AnimationCurve.EaseInOut(0, 0, 1, 0);

    [Header("Cores da luz (Lua) ao longo do dia")]
    public Color moonColor = new Color(0.70f, 0.75f, 1.00f);
    public AnimationCurve moonIntensity = AnimationCurve.Constant(0, 1, 0.45f);
    public float moonMinimumIntensity = 0.05f;

    [Header("Skybox Procedural")]
    public Gradient skyTintColor;
    public Gradient groundColor;
    public AnimationCurve atmosphereThickness = AnimationCurve.Constant(0, 1, 1f);
    public AnimationCurve exposure = AnimationCurve.EaseInOut(0, 0.3f, 1, 1.3f);

    [Header("Disco visível no céu (só 1 de cada vez é possível)")]
    [Tooltip("Tamanho do disco do sol no céu (0.01 - 0.1 aprox)")]
    public float sunDiscSize = 0.04f;
    [Tooltip("Tamanho do disco da lua no céu — normalmente mais pequeno")]
    public float moonDiscSize = 0.02f;

    private void OnValidate()
    {
        
        RecalculateSpeed();
    }

    private void Awake()
    {
        RecalculateSpeed();

        // Sol e lua têm de ser Directional para iluminarem o mundo inteiro.
        // Se estiverem como Spot (configuração errada na cena), a luz só cobre
        // uma área pequena e a cena parece noite eterna.
        if (sunLight != null)
            sunLight.type = LightType.Directional;
        if (moonLight != null)
            moonLight.type = LightType.Directional;
    }

    private void RecalculateSpeed()
    {
        if (dayLengthInMinutes <= 0f) dayLengthInMinutes = 0.01f;
        hoursPerSecond = 24f / (dayLengthInMinutes * 60f);
    }

    private void Update()
    {
        if (timeIsRunning)
        {
            currentHour += Time.deltaTime * hoursPerSecond;
            if (currentHour >= 24f) currentHour -= 24f;
        }

        UpdateSun();
        UpdateMoon();
        UpdateSkybox();
    }

    private bool IsNightTime()
    {
        return currentHour < 5f || currentHour > 19f;
    }

    private void UpdateSun()
    {
        if (sunLight == null) return;

        float sunAngle = (currentHour / 24f) * 360f - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        float t = currentHour / 24f;
        float sunStrength = sunIntensity.Evaluate(t);
        sunLight.color = sunColor.Evaluate(t);

        bool isNight = IsNightTime();
        sunLight.enabled = !isNight;
        sunLight.intensity = isNight ? 0f : sunStrength;
    }

    private void UpdateMoon()
    {
        if (moonLight == null) return;

        float sunAngle = (currentHour / 24f) * 360f - 90f;
        float moonAngle = sunAngle + 180f;
        moonLight.transform.rotation = Quaternion.Euler(moonAngle, 170f, 0f);

        float t = currentHour / 24f;
        moonLight.color = moonColor;

        float sunStrength = sunIntensity.Evaluate(t);
        float moonBase = Mathf.Max(moonIntensity.Evaluate(t), moonMinimumIntensity);
        float moonStrength = moonBase * Mathf.Clamp01(1f - sunStrength);

        moonLight.enabled = true;
        moonLight.intensity = moonStrength;
    }

    private void UpdateSkybox()
    {
        if (skyboxMaterial == null) return;

        float t = currentHour / 24f;

        skyboxMaterial.SetColor("_SkyTint", skyTintColor.Evaluate(t));
        skyboxMaterial.SetColor("_GroundColor", groundColor.Evaluate(t));
        skyboxMaterial.SetFloat("_AtmosphereThickness", atmosphereThickness.Evaluate(t));
        skyboxMaterial.SetFloat("_Exposure", exposure.Evaluate(t));

        
        bool isNight = IsNightTime();

        if (isNight && moonLight != null)
        {
            RenderSettings.sun = moonLight;
            skyboxMaterial.SetFloat("_SunSize", moonDiscSize);
        }
        else if (sunLight != null)
        {
            RenderSettings.sun = sunLight;
            skyboxMaterial.SetFloat("_SunSize", sunDiscSize);
        }
    }

  
    private void Reset()
    {
       
        sunColor = new Gradient();
        sunColor.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.10f, 0.13f, 0.22f), 0.0f),  // midnight
                new GradientColorKey(new Color(1.00f, 0.71f, 0.45f), 0.25f), 
                new GradientColorKey(new Color(1.00f, 0.96f, 0.88f), 0.5f),  
                new GradientColorKey(new Color(1.00f, 0.55f, 0.35f), 0.75f), 
                new GradientColorKey(new Color(0.10f, 0.13f, 0.22f), 1.0f)   // midnight
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
        );

      
        sunIntensity = new AnimationCurve(
            new Keyframe(0.0f, 0f),
            new Keyframe(0.25f, 0.8f),
            new Keyframe(0.5f, 1.4f),
            new Keyframe(0.75f, 0.8f),
            new Keyframe(1.0f, 0f)
        );

     
        skyTintColor = new Gradient();
        skyTintColor.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.05f, 0.07f, 0.13f), 0.0f),
                new GradientColorKey(new Color(1.00f, 0.71f, 0.45f), 0.25f),
                new GradientColorKey(new Color(0.49f, 0.65f, 0.85f), 0.5f),
                new GradientColorKey(new Color(1.00f, 0.55f, 0.35f), 0.75f),
                new GradientColorKey(new Color(0.05f, 0.07f, 0.13f), 1.0f)
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
        );

       
        groundColor = new Gradient();
        groundColor.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.05f, 0.05f, 0.05f), 0.0f),
                new GradientColorKey(new Color(0.10f, 0.10f, 0.10f), 1.0f)
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
        );

        atmosphereThickness = AnimationCurve.Constant(0, 1, 1f);

        exposure = new AnimationCurve(
            new Keyframe(0.0f, 0.3f),
            new Keyframe(0.25f, 1.0f),
            new Keyframe(0.5f, 1.3f),
            new Keyframe(0.75f, 1.0f),
            new Keyframe(1.0f, 0.3f)
        );

        RecalculateSpeed();
    }
}