using Mirror;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Reactor BWR — 9 inputs + 10 outputs normalizados (0-1).
/// Point kinetics real (implicit Euler), termico 2 nos, decay heat 3 grupos,
/// feedback Doppler/moderador/vazio, S-curve rod worth.
///
/// INPUT  (1-9):  RodInsertion, CoolantFeed, Scram, TurbineValve,
///                ContainmentSpray, FeedwaterPump, ElectricalLoad,
///                ReactorStart, ReactorStop
/// OUTPUT (10-19): Temperature, CoolantLevel, Pressure, PowerOutput, Status,
///                 NeutronFlux, TurbineRPM, Radiation, EnergyTotal, FuelPercent
/// </summary>
public class ReactorController : EletricUnit
{
    // ===================================================================
    //  INSPECTOR
    // ===================================================================

    [Header("Bus")]
    public Node commandNode;
    public Node outputNode;

    [Header("Fuel")]
    public ItemSO fuelItem;
    public int fuelPerItem = 1;
    public int maxFuel = 30;
    public int startingFuel = 10;

    [Header("Neutron Kinetics")]
    [Tooltip("Fracao de neutrons atrasados. U-235 = 0.0065.")]
    public float beta = 0.0065f;
    [Tooltip("Constante de decaimento dos precursores (1/s). U-235 = 0.081.")]
    public float decayConstant = 0.081f;
    [Tooltip("Tempo de vida dos neutrons prontos (s). Termico: 1e-4 a 2e-4.")]
    public float promptNeutronLifetime = 0.0002f;

    [Header("Rod Dynamics")]
    public float rodInsertSpeed = 0.06f;
    public float rodWithdrawSpeed = 0.04f;
    public float scramSpeed = 0.3f;

    [Header("Reactivity")]
    [Tooltip("Reactividade total das barras (dk/k). 0.005 = 5000 pcm.")]
    public float totalRodWorth = 0.005f;
    [Tooltip("Feedback Doppler por unidade de temp combustivel (negativo).")]
    public float dopplerCoefficient = -0.003f;
    [Tooltip("Feedback moderador por unidade de temp coolant (negativo).")]
    public float moderatorCoefficient = -0.002f;
    [Tooltip("Boost de vazio quando coolant level baixo (dk/k).")]
    public float voidCoefficient = 0.003f;
    public float voidThreshold = 0.40f;

    [Header("Thermal - Fuel")]
    [Tooltip("Calor gerado por unidade de potencia normalizada (por s).")]
    public float heatGain = 0.25f;
    public float fuelCapacity = 1.0f;
    public float fuelCoolantUA = 0.40f;

    [Header("Thermal - Coolant")]
    public float coolantCapacity = 3.0f;
    public float coolantSinkUA = 0.12f;

    [Header("Coolant")]
    public float feedwaterFillPerSecond = 0.12f;
    public float evaporationPerSecond = 0.06f;
    public float sprayWaterLossPerSecond = 0.18f;
    public float sprayCoolingBonus = 0.15f;

    [Header("Pressure")]
    public float nominalPressure = 0.50f;
    public float pressureControlRate = 0.05f;
    public float reliefValvePressure = 0.95f;
    public float ventWaterLossPerSecond = 0.12f;

    [Header("Turbine")]
    public float turbineSteamDraw = 0.6f;
    public float turbineRampPerSecond = 0.5f;
    public float generatorEfficiency = 0.85f;
    public float loadBackpressurePenalty = 0.3f;

    [Header("Safety")]
    [UnityEngine.Range(0f, 1f)]
    public float autoScramTemperature = 0.92f;
    [UnityEngine.Range(0f, 1f)]
    public float autoScramPressure = 0.95f;
    [UnityEngine.Range(0f, 1f)]
    public float autoScramCoolantLevel = 0.05f;

    [Header("Meltdown")]
    public float meltdownGraceSeconds = 8f;
    public float meltdownThreshold = 0.98f;

    [Header("Radiation")]
    public float baseRadiation = 0.05f;
    public float radiationFromHeat = 0.6f;
    public float radiationFromDry = 0.8f;

    [Header("Electrical")]
    public float maxPowerMegawatts = 10f;

    [Header("Telemetry")]
    public float telemetryInterval = 0.05f;

    [Header("Events")]
    public UnityEvent onMeltdown;

    [Header("Visual")]
    public Renderer reactorRenderer;
    public Color coldColor = new Color(0.45f, 0.45f, 0.55f);
    public Color hotColor = Color.red;

    [System.Serializable]
    public struct DecayHeatGroup
    {
        public float fraction;
        public float decayConstant;
        [System.NonSerialized] public float value;
    }

    [Header("Decay Heat")]
    public DecayHeatGroup[] decayHeatGroups = new DecayHeatGroup[]
    {
        new DecayHeatGroup { fraction = 0.035f, decayConstant = 0.100f },
        new DecayHeatGroup { fraction = 0.020f, decayConstant = 0.005f },
        new DecayHeatGroup { fraction = 0.015f, decayConstant = 0.0003f }
    };

    // ===================================================================
    //  ESTADO INTERNO
    // ===================================================================

    private float n;
    private float kEff;
    private float precursorConcentration;
    private float fuelTemp;
    private float coolantTemp;
    private float steamPressure;
    private float steamProduction;
    private float turbineOutput;
    private float rodInsertion;
    private float targetRodInsertion;
    private float turbineRPM;
    private float electricalOutput;
    private float neutronFluxVal;
    private float radiationVal;
    private float fuelNormalized;
    private float meltdownTimer;
    private float telemetryTimer;
    private int telemetryIndex;
    private float energyAccumulator;
    private Material cachedRendererMaterial;
    private float[] ch = new float[ReactorChannelDef.ChannelCount + 1];
    private bool started;
    private bool scrammed;
    private int state = ReactorStatus.Offline;
    private int alarmLevel;

    // ===================================================================
    //  PROPRIEDADES PUBLICAS
    // ===================================================================

    public float Power           { get; private set; }
    public float CoreTemp        => coolantTemp;
    public float CoolantLevel    { get; private set; } = 0.8f;
    public float CoolantFlow     { get; private set; }
    public float SteamProduction => steamProduction;
    public float SteamPressure   => steamPressure;
    public float RodInsertion    => rodInsertion;
    public float TurbineOutput   => turbineOutput;
    public float TurbineRPM      => turbineRPM;
    public float ElectricalOutput => electricalOutput;
    public float NeutronFluxVal  => neutronFluxVal;
    public float RadiationVal    => radiationVal;
    public float Reactivity      { get; private set; }
    public float KEff            => kEff;
    public bool  Started         => started;
    public bool  Scrammed        => scrammed;
    public int   State           => state;
    public int   AlarmLevel      => alarmLevel;
    public int   Fuel            => Mathf.RoundToInt(fuelNormalized * maxFuel);
    public float EnergyMWh       => energyAccumulator;

    // ===================================================================
    //  DEBUG UI
    // ===================================================================

    [System.Serializable]
    public struct ChannelDebug
    {
        public string label;
        [UnityEngine.Range(0f, 1f)]
        public float value;
        public override string ToString() => $"{label}: {value * 100f:F1}%";
    }

    [Header("=== INPUTS ===")]
    public ChannelDebug debugRodInsertion    = new ChannelDebug { label = "Rod Insertion", value = 0f };
    public ChannelDebug debugCoolantFeed     = new ChannelDebug { label = "Coolant Feed", value = 0.5f };
    public ChannelDebug debugScram           = new ChannelDebug { label = "Scram", value = 0f };
    public ChannelDebug debugTurbineValve    = new ChannelDebug { label = "Turbine Valve", value = 0f };
    public ChannelDebug debugContainSpray    = new ChannelDebug { label = "Containment Spray", value = 0f };
    public ChannelDebug debugFeedwaterPump   = new ChannelDebug { label = "Feedwater Pump", value = 0.5f };
    public ChannelDebug debugElectricalLoad  = new ChannelDebug { label = "Electrical Load", value = 0.5f };

    [Header("=== OUTPUTS ===")]
    public ChannelDebug debugTemperature     = new ChannelDebug { label = "Temperature", value = 0f };
    public ChannelDebug debugCoolantLevel    = new ChannelDebug { label = "Coolant Level", value = 0.8f };
    public ChannelDebug debugPressure        = new ChannelDebug { label = "Pressure", value = 0f };
    public ChannelDebug debugPower           = new ChannelDebug { label = "Power Output", value = 0f };
    public ChannelDebug debugReactivity      = new ChannelDebug { label = "Reactivity", value = 0f };
    public ChannelDebug debugKEff            = new ChannelDebug { label = "K_eff", value = 1f };
    public ChannelDebug debugNeutronFlux     = new ChannelDebug { label = "Neutron Flux", value = 0f };
    public ChannelDebug debugTurbineRPM      = new ChannelDebug { label = "Turbine RPM", value = 0f };
    public ChannelDebug debugRadiation       = new ChannelDebug { label = "Radiation", value = 0f };
    public ChannelDebug debugFuel            = new ChannelDebug { label = "Fuel", value = 1f };
    public ChannelDebug debugEnergyMWh       = new ChannelDebug { label = "Energy (MWh)", value = 0f };
    public ChannelDebug debugSteamProd       = new ChannelDebug { label = "Steam Production", value = 0f };
    [SerializeField] private string debugState = "OFFLINE";

    private const string DefaultUnitName = "Nuclear Reactor";

    // ===================================================================
    //  S-CURVE ROD WORTH
    //  worth(x) = x - sin(2pi*x)/(2pi)
    // ===================================================================

    private static float RodWorth(float insertion)
    {
        float pi2 = 2f * Mathf.PI;
        return insertion - Mathf.Sin(pi2 * insertion) / pi2;
    }

    // ===================================================================
    //  CANAIS
    // ===================================================================

    public float GetChannel(int id)
    {
        if (id < 1 || id > ReactorChannelDef.ChannelCount) return 0f;
        return ch[id];
    }

    public void SetChannel(int id, float value)
    {
        if (id < 1 || id > ReactorChannelDef.ChannelCount) return;
        ch[id] = Mathf.Clamp01(value);
    }

    // ===================================================================
    //  LIFECYCLE
    // ===================================================================

    private void Awake()
    {
        unitName = DefaultUnitName;
        ConfigureNodes();
    }

    private void ConfigureNodes()
    {
        if (commandNode != null)
        {
            commandNode.name = "Reactor Command Input";
            commandNode.unitName = "Nuclear Reactor Input";
            commandNode.nodeLabel = "Nuclear Reactor Input";
            commandNode.signalType = NodeSignal.Command;
        }
        if (outputNode != null)
        {
            outputNode.name = "Reactor Telemetry Output";
            outputNode.unitName = "Nuclear Reactor Output";
            outputNode.nodeLabel = "Nuclear Reactor Output";
            outputNode.signalType = NodeSignal.Telemetry;
        }
    }

    private void OnEnable()
    {
        if (commandNode != null)
            commandNode.onValueChanged += OnCommand;
    }

    private void OnDisable()
    {
        if (commandNode != null)
            commandNode.onValueChanged -= OnCommand;
    }

    private void Start()
    {
        if (commandNode != null) commandNode.type = PortType.Input;
        if (outputNode != null) outputNode.type = PortType.Output;
        fuelNormalized = Mathf.Clamp01((float)startingFuel / Mathf.Max(1, maxFuel));
        state = ReactorStatus.Offline;
        if (reactorRenderer != null)
            cachedRendererMaterial = reactorRenderer.material;

        ch[ReactorChannelDef.RodInsertion] = 0f;
        ch[ReactorChannelDef.CoolantFeed] = 0.5f;
        ch[ReactorChannelDef.Scram] = 0f;
        ch[ReactorChannelDef.TurbineValve] = 0f;
        ch[ReactorChannelDef.ContainmentSpray] = 0f;
        ch[ReactorChannelDef.FeedwaterPump] = 0.5f;
        ch[ReactorChannelDef.ElectricalLoad] = 0.5f;
        ch[ReactorChannelDef.ReactorStart] = 0f;
        ch[ReactorChannelDef.ReactorStop] = 0f;
    }

    public override void OnEnteract()
    {
        Interact();
    }

    private void Update()
    {
        Tick(Time.deltaTime);
        WriteOutputChannels();
        UpdateTelemetry();
        UpdateVisual();
        UpdateGrid();
    }

    // ===================================================================
    //  SIMULACAO PRINCIPAL
    // ===================================================================

    private void Tick(float dt)
    {
        bool melted = state == ReactorStatus.Meltdown;

        // 0. Start / Stop
        if (ch[ReactorChannelDef.ReactorStart] > 0.5f)
        {
            StartReactor();
            ch[ReactorChannelDef.ReactorStart] = 0f;
        }
        if (ch[ReactorChannelDef.ReactorStop] > 0.5f)
        {
            StopReactor();
            ch[ReactorChannelDef.ReactorStop] = 0f;
        }

        // 1. SCRAM automatico
        if (!scrammed && !melted && started)
        {
            if (coolantTemp >= autoScramTemperature
             || steamPressure >= autoScramPressure
             || CoolantLevel <= autoScramCoolantLevel)
            {
                ScramInternal(true);
            }
        }

        bool scram = scrammed || ch[ReactorChannelDef.Scram] > 0.5f;
        ch[ReactorChannelDef.Scram] = 0f;

        // 2. Control Rods
        float rodTarget;
        if (scram)
            rodTarget = 1f;
        else
            rodTarget = Mathf.Clamp01(ch[ReactorChannelDef.RodInsertion]);

        float rodSpeed;
        if (scram)
            rodSpeed = scramSpeed;
        else if (rodTarget < rodInsertion)
            rodSpeed = rodWithdrawSpeed;
        else
            rodSpeed = rodInsertSpeed;

        rodInsertion = Mathf.MoveTowards(rodInsertion, rodTarget, rodSpeed * dt);
        rodInsertion = Mathf.Clamp01(rodInsertion);

        // 3. Reactividade total
        float rho = ComputeReactivity();
        Reactivity = rho;
        kEff = 1f + rho;

        // 4. Point kinetics (implicit Euler)
        bool engaged = started && !melted && fuelNormalized > 0f;

        if (engaged || n > 1e-8f)
        {
            StepPointKinetics(rho, dt);
        }
        else
        {
            n = 0f;
            precursorConcentration = 0f;
        }

        Power = Mathf.Clamp01(n);

        // Decay heat
        float fissionPower = n;
        StepDecayHeat(fissionPower, dt);

        float totalHeat = n;
        for (int i = 0; i < decayHeatGroups.Length; i++)
            totalHeat += decayHeatGroups[i].value;

        // 5. Termico 2 nos
        StepThermal(totalHeat, dt);

        // 6. Coolant level
        float spray = Mathf.Clamp01(ch[ReactorChannelDef.ContainmentSpray]);
        CoolantFlow = Mathf.Clamp01(ch[ReactorChannelDef.CoolantFeed]);
        float pumpEff = CoolantFlow * CoolantLevel;
        float feedwater = Mathf.Clamp01(ch[ReactorChannelDef.FeedwaterPump]);

        float consumption = totalHeat * evaporationPerSecond * dt;
        float refill = feedwater * feedwaterFillPerSecond * dt;
        float sprayLoss = spray * sprayWaterLossPerSecond * dt;
        CoolantLevel = Mathf.Clamp01(CoolantLevel - consumption + refill - sprayLoss);

        // 7. Pressao
        steamProduction = coolantTemp * coolantTemp;
        float valve = Mathf.Clamp01(ch[ReactorChannelDef.TurbineValve]);
        float load = Mathf.Clamp01(ch[ReactorChannelDef.ElectricalLoad]);
        float effectiveDraw = turbineSteamDraw * (1f + load * loadBackpressurePenalty);

        steamPressure += (steamProduction - turbineOutput * effectiveDraw * valve) * 0.3f * dt;
        if (steamProduction < 0.001f && steamPressure > 0f)
            steamPressure = Mathf.MoveTowards(steamPressure, 0f, 0.05f * dt);
        steamPressure = Mathf.Clamp01(steamPressure);

        if (steamPressure >= reliefValvePressure)
        {
            steamPressure = Mathf.MoveTowards(steamPressure, reliefValvePressure - 0.05f, 0.3f * dt);
            CoolantLevel = Mathf.Max(0f, CoolantLevel - ventWaterLossPerSecond * dt);
        }
        steamPressure = Mathf.Clamp01(steamPressure);

        // 8. Turbina
        float targetTurbine = (engaged && steamPressure > 0.01f) ? steamPressure * valve : 0f;
        turbineOutput = Mathf.MoveTowards(turbineOutput, targetTurbine, turbineRampPerSecond * dt);
        turbineRPM = Mathf.MoveTowards(turbineRPM, turbineOutput, turbineRampPerSecond * dt);

        // 9. Eletrica
        electricalOutput = (steamPressure > 0.01f && valve > 0.01f)
            ? turbineOutput * generatorEfficiency
            : 0f;

        // 10. Neutron flux (display)
        float fluxTarget = engaged ? Mathf.Clamp01((1f - rodInsertion)) : 0f;
        float fluxRamp = (scrammed || melted) ? 2f : 0.5f;
        neutronFluxVal = Mathf.MoveTowards(neutronFluxVal, fluxTarget, fluxRamp * dt);

        // 11. Combustivel
        if (!melted && Power > 0.05f && fuelNormalized > 0f)
            fuelNormalized = Mathf.Max(0f, fuelNormalized - Power * dt * 0.015f);

        // 12. Radiacao
        float tempRad = coolantTemp > 0.5f ? (coolantTemp - 0.5f) * 2f * radiationFromHeat : 0f;
        float dryRad = CoolantLevel < 0.1f ? (1f - CoolantLevel / 0.1f) * radiationFromDry : 0f;
        float meltRad = melted ? 1f : 0f;
        radiationVal = Mathf.Clamp01(baseRadiation + tempRad + dryRad + meltRad);

        // 13. Energia
        energyAccumulator += electricalOutput * dt * 0.001f;

        // 14. Estado / alarme
        bool fuelEmpty = fuelNormalized <= 0f && (started || Power > 0.01f);
        bool tempWarn  = coolantTemp >= 0.75f;
        bool tempCrit  = coolantTemp >= 0.85f;
        bool levelWarn = CoolantLevel <= 0.20f;
        bool levelCrit = CoolantLevel <= 0.10f;
        bool pressWarn = steamPressure >= 0.70f;
        bool pressCrit = steamPressure >= 0.85f;
        bool radWarn   = radiationVal >= 0.6f;

        alarmLevel = (tempCrit || levelCrit || pressCrit || radWarn) ? 2
            : (tempWarn || levelWarn || pressWarn || fuelEmpty) ? 1
            : 0;

        // 15. Meltdown
        if (coolantTemp >= meltdownThreshold && !melted)
        {
            meltdownTimer += dt;
            if (meltdownTimer >= meltdownGraceSeconds)
            {
                state = ReactorStatus.Meltdown;
                scrammed = true;
                started = false;
                alarmLevel = 2;
                onMeltdown?.Invoke();
            }
        }
        else
        {
            meltdownTimer = Mathf.Max(0f, meltdownTimer - dt * 0.5f);
        }

        // 16. Classificacao de estado
        if (melted)
            state = ReactorStatus.Meltdown;
        else if (tempCrit || levelCrit || pressCrit)
            state = ReactorStatus.Critical;
        else if (scrammed)
            state = ReactorStatus.Scram;
        else if (tempWarn || levelWarn || pressWarn || fuelEmpty)
            state = ReactorStatus.Warning;
        else if (engaged)
            state = Power > 0.05f ? ReactorStatus.Running : ReactorStatus.Starting;
        else
            state = (coolantTemp > 0.3f || Power > 0.01f) ? ReactorStatus.Shutdown : ReactorStatus.Offline;
    }

    // ===================================================================
    //  REACTIVIDADE
    //  rho = rod + Doppler + moderador + vazio
    // ===================================================================

    private float ComputeReactivity()
    {
        // Rod worth com S-curve: rods OUT = mais reactividade
        float rodReactivity = totalRodWorth * (1f - RodWorth(rodInsertion));

        // Doppler: temp combustivel alta = menos reactividade (negativo)
        float doppler = dopplerCoefficient * fuelTemp;

        // Moderador: temp coolant alta = menos reactividade (negativo)
        float moderator = moderatorCoefficient * coolantTemp;

        // Vazio: coolant level baixo = mais reactividade (BWR positivo)
        float vazio = 0f;
        if (CoolantLevel < voidThreshold)
        {
            float voidFrac = 1f - CoolantLevel / voidThreshold;
            vazio = voidFrac * voidCoefficient;
        }

        return rodReactivity + doppler + moderator + vazio;
    }

    // ===================================================================
    //  POINT KINETICS — Implicit Euler
    //
    //  dn/dt = (rho - beta)/Lambda * n + lambda * c
    //  dc/dt = beta/Lambda * n - lambda * c
    //
    //  Integracao implicita = incondicionalmente estavel.
    //  Se rho > beta prompt-critical, potencia dispara realmente.
    // ===================================================================

    private void StepPointKinetics(float rho, float dt)
    {
        float n0 = n;
        float c0 = precursorConcentration;

        float a = 1f - dt * (rho - beta) / promptNeutronLifetime;
        float denom = a * (1f + dt * decayConstant)
                    - dt * dt * decayConstant * beta / promptNeutronLifetime;

        float n1 = (n0 * (1f + dt * decayConstant) + dt * decayConstant * c0) / denom;
        float c1 = (c0 + dt * (beta / promptNeutronLifetime) * n1) / (1f + dt * decayConstant);

        n = Mathf.Clamp(n1, 0f, 200f);
        precursorConcentration = Mathf.Max(0f, c1);
    }

    // ===================================================================
    //  DECAY HEAT — 3 grupos
    //  dDi/dt = lambda_i * (fraction_i * P_fission - Di)
    // ===================================================================

    private void StepDecayHeat(float fissionPower, float dt)
    {
        for (int i = 0; i < decayHeatGroups.Length; i++)
        {
            var g = decayHeatGroups[i];
            float target = g.fraction * fissionPower;
            g.value = (g.value + dt * g.decayConstant * target) / (1f + dt * g.decayConstant);
            decayHeatGroups[i] = g;
        }
    }

    // ===================================================================
    //  TERMICO — 2 nos (combustivel + coolant)
    //
    //  dTf/dt = (P * heatGain - UA * (Tf - Tc)) / C_fuel
    //  dTc/dt = (UA * (Tf - Tc) - UA_sink * Tc * pumpEff) / C_cool
    //
    //  Spray额外冷却
    // ===================================================================

    private void StepThermal(float totalHeat, float dt)
    {
        float pumpEff = CoolantFlow * Mathf.Clamp01(CoolantLevel);

        // Combustivel: calor gerado vs transferencia ao coolant
        float fuelToCoolant = fuelCoolantUA * (fuelTemp - coolantTemp);
        fuelTemp += (totalHeat * heatGain - fuelToCoolant) / fuelCapacity * dt;
        fuelTemp = Mathf.Max(0f, fuelTemp);

        // Spray de contencao (extra cooling no coolant)
        float spray = Mathf.Clamp01(ch[ReactorChannelDef.ContainmentSpray]);
        float sprayCool = spray * sprayCoolingBonus;

        // Coolant: calor recebido vs perdido ao sumidouro
        float coolantToSink = coolantSinkUA * Mathf.Max(pumpEff, 0.02f) * coolantTemp;
        coolantTemp += (fuelToCoolant - coolantToSink - sprayCool) / coolantCapacity * dt;
        coolantTemp = Mathf.Clamp01(coolantTemp);
    }

    // ===================================================================
    //  CANAIS DE OUTPUT
    // ===================================================================

    private void WriteOutputChannels()
    {
        ch[ReactorChannelDef.Temperature]  = coolantTemp;
        ch[ReactorChannelDef.CoolantLevel] = CoolantLevel;
        ch[ReactorChannelDef.Pressure]     = steamPressure;
        ch[ReactorChannelDef.PowerOutput]  = electricalOutput;
        ch[ReactorChannelDef.NeutronFlux]  = Power;
        ch[ReactorChannelDef.TurbineRPM]   = turbineRPM;
        ch[ReactorChannelDef.Radiation]    = radiationVal;
        ch[ReactorChannelDef.FuelPercent]  = fuelNormalized;

        float status = 0f;
        if (state == ReactorStatus.Meltdown) status = 1f;
        else if (state == ReactorStatus.Critical) status = 0.5f;
        else if (state == ReactorStatus.Scram) status = 0.75f;
        else if (state == ReactorStatus.Warning) status = 0.25f;
        ch[ReactorChannelDef.Status] = status;
        ch[ReactorChannelDef.EnergyTotal] = Mathf.Clamp01(energyAccumulator / 100f);

        debugRodInsertion.value   = rodInsertion;
        debugCoolantFeed.value    = ch[ReactorChannelDef.CoolantFeed];
        debugScram.value          = ch[ReactorChannelDef.Scram];
        debugTurbineValve.value   = ch[ReactorChannelDef.TurbineValve];
        debugContainSpray.value   = ch[ReactorChannelDef.ContainmentSpray];
        debugFeedwaterPump.value  = ch[ReactorChannelDef.FeedwaterPump];
        debugElectricalLoad.value = ch[ReactorChannelDef.ElectricalLoad];
        debugTemperature.value    = coolantTemp;
        debugCoolantLevel.value   = CoolantLevel;
        debugPressure.value       = steamPressure;
        debugPower.value          = Power;
        debugReactivity.value     = Mathf.Clamp01((Reactivity + 0.01f) / 0.02f);
        debugKEff.value           = Mathf.Clamp01((kEff - 0.8f) / 0.4f);
        debugNeutronFlux.value    = neutronFluxVal;
        debugTurbineRPM.value     = turbineRPM;
        debugRadiation.value      = radiationVal;
        debugFuel.value           = fuelNormalized;
        debugEnergyMWh.value      = Mathf.Clamp01(energyAccumulator / 100f);
        debugSteamProd.value      = steamProduction;
        debugState                = ReactorStatus.GetName(state);
    }

    // ===================================================================
    //  REQUEST STEAM
    // ===================================================================

    public float RequestSteam(float requested)
    {
        if (state == ReactorStatus.Meltdown || state == ReactorStatus.Offline)
            return 0f;
        float available = steamPressure * Mathf.Clamp01(ch[ReactorChannelDef.TurbineValve]);
        return Mathf.Min(requested, available);
    }

    // ===================================================================
    //  DECODE DE COMANDOS (Node/Wire - backward compat)
    // ===================================================================

    private void OnCommand(int packet)
    {
        int target = ReactorPacket.GetTarget(packet);
        int action = ReactorPacket.GetAction(packet);
        int value  = ReactorPacket.GetValue(packet);

        switch (target)
        {
            case ReactorPacket.TargetReactor:
                if (action == ReactorPacket.ActionTrigger)
                {
                    if (value == ReactorPacket.TriggerStart) StartReactor();
                    else if (value == ReactorPacket.TriggerStop) StopReactor();
                    else if (value == ReactorPacket.TriggerScram) Scram();
                }
                else if (action == ReactorPacket.ActionEnable) StartReactor();
                else if (action == ReactorPacket.ActionDisable) StopReactor();
                break;

            case ReactorPacket.TargetRods:
                if (action == ReactorPacket.ActionSet)
                    ch[ReactorChannelDef.RodInsertion] = Mathf.Clamp01(value / 100f);
                else if (action == ReactorPacket.ActionEnable)
                    ch[ReactorChannelDef.RodInsertion] = 0f;
                else if (action == ReactorPacket.ActionDisable)
                    ch[ReactorChannelDef.RodInsertion] = 1f;
                break;

            case ReactorPacket.TargetCoolant:
                if (action == ReactorPacket.ActionSet)
                    ch[ReactorChannelDef.CoolantFeed] = Mathf.Clamp01(value / 100f);
                else if (action == ReactorPacket.ActionEnable)
                    ch[ReactorChannelDef.CoolantFeed] = 1f;
                else if (action == ReactorPacket.ActionDisable)
                    ch[ReactorChannelDef.CoolantFeed] = 0f;
                break;

            case ReactorPacket.TargetTurbine:
                if (action == ReactorPacket.ActionSet)
                    ch[ReactorChannelDef.TurbineValve] = Mathf.Clamp01(value / 100f);
                else if (action == ReactorPacket.ActionEnable)
                    ch[ReactorChannelDef.TurbineValve] = 1f;
                else if (action == ReactorPacket.ActionDisable)
                    ch[ReactorChannelDef.TurbineValve] = 0f;
                break;
        }
    }

    // ===================================================================
    //  CONTROLO
    // ===================================================================

    public void StartReactor()
    {
        if (state == ReactorStatus.Meltdown) return;
        if (fuelNormalized <= 0f) return;
        if (coolantTemp >= 0.85f) return;
        scrammed = false;
        started = true;
        ch[ReactorChannelDef.Scram] = 0f;
    }

    public void StopReactor()
    {
        started = false;
        ch[ReactorChannelDef.RodInsertion] = 1f;
    }

    public void Scram()
    {
        ScramInternal(false);
    }

    private void ScramInternal(bool automatic)
    {
        if (scrammed) return;
        scrammed = true;
        started = false;
        ch[ReactorChannelDef.Scram] = 1f;
        Debug.Log(automatic
            ? "[ReactorController] SCRAM automatico!"
            : "[ReactorController] SCRAM manual.");
    }

    // ===================================================================
    //  COMBUSTIVEL FISICO (E)
    // ===================================================================

    public void Interact()
    {
        if (state == ReactorStatus.Meltdown) return;

        var inv = ReactorBreach.InventorySystem.Inventory.Instance;
        if (inv == null) return;

        int selectedIdx = inv.GetSelectedHotbarIndex();
        if (selectedIdx < 0) return;

        if (!inv.TryGetHotbarItem(selectedIdx, out ItemSO item, out int amount) || item == null)
            return;

        if (fuelItem != null && item != fuelItem) return;
        if (fuelNormalized >= 1f) return;

        inv.RemoveItem(item, 1);
        fuelNormalized = Mathf.Clamp01(fuelNormalized + (float)fuelPerItem / maxFuel);
        Debug.Log($"[ReactorController] +1x {item.itemName}. Combustivel: {Fuel}/{maxFuel}");
    }

    // ===================================================================
    //  TELEMETRIA (Node/Wire - backward compat)
    // ===================================================================

    private static readonly int[] TelemetryChannels =
    {
        ReactorChannel.Temperature, ReactorChannel.CoolantLevel,
        ReactorChannel.Pressure, ReactorChannel.PowerOutput,
        ReactorChannel.Status, ReactorChannel.RodInsertion,
        ReactorChannel.NeutronFlux, ReactorChannel.TurbineRPM,
        ReactorChannel.Radiation, ReactorChannel.EnergyTotal,
        ReactorChannel.FuelPercent
    };

    private void UpdateTelemetry()
    {
        if (outputNode == null) return;

        telemetryTimer -= Time.deltaTime;
        if (telemetryTimer > 0f) return;

        telemetryTimer = telemetryInterval;
        int channel = TelemetryChannels[telemetryIndex];
        telemetryIndex = (telemetryIndex + 1) % TelemetryChannels.Length;

        float value;
        switch (channel)
        {
            case ReactorChannel.Temperature:  value = coolantTemp; break;
            case ReactorChannel.CoolantLevel: value = CoolantLevel; break;
            case ReactorChannel.Pressure:     value = steamPressure; break;
            case ReactorChannel.PowerOutput:  value = Power; break;
            case ReactorChannel.RodInsertion: value = rodInsertion; break;
            case ReactorChannel.NeutronFlux:  value = neutronFluxVal; break;
            case ReactorChannel.TurbineRPM:   value = turbineRPM; break;
            case ReactorChannel.Radiation:    value = radiationVal; break;
            case ReactorChannel.EnergyTotal:  value = Mathf.Clamp01(energyAccumulator / 100f); break;
            case ReactorChannel.FuelPercent:  value = fuelNormalized; break;
            case ReactorChannel.Status:
                if (state == ReactorStatus.Meltdown) value = 100f;
                else if (state == ReactorStatus.Critical) value = 50f;
                else if (state == ReactorStatus.Scram) value = 75f;
                else if (state == ReactorStatus.Warning) value = 25f;
                else value = 0f;
                break;
            default: value = 0f; break;
        }

        outputNode.SetValue(ReactorPacket.Make(ReactorPacket.TargetTelemetry, channel, Mathf.RoundToInt(value)));
    }

    private void UpdateVisual()
    {
        if (cachedRendererMaterial == null) return;
        cachedRendererMaterial.color = Color.Lerp(coldColor, hotColor, coolantTemp);
    }

    private void UpdateGrid()
    {
        if (maxPowerMegawatts <= 0f) return;
        if (!NetworkServer.active) return;
        if (NetworkPowerGrid.Instance == null) return;
        NetworkPowerGrid.Instance.SetPower(electricalOutput * maxPowerMegawatts);
    }
}
