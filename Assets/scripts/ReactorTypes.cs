/// <summary>
/// Canais de comunicação entre o reactor e módulos de controlo.
/// Todos os valores normalizados 0-1.
///
/// INPUT  (1-9):  Controlados pelo jogador (lever, button).
/// OUTPUT (10-19): Emitidos pelo reactor (display, alarm, sync).
///
/// SYNC: Se lever e button partilham o mesmo canal, o lever
/// actualiza a posição visual quando o botão é premido.
/// </summary>
public static class ReactorChannelDef
{
    // ===================================================================
    //  INPUT — O jogador controla
    // ===================================================================

    /// <summary>0 = barras fora (máxima reactividade), 1 = barras dentro (zero reactividade).</summary>
    public const int RodInsertion     = 1;

    /// <summary>Bomba de refrigerante primário. 0 = parada, 1 = máxima.</summary>
    public const int CoolantFeed      = 2;

    /// <summary>&gt;0.5 = SCRAM de emergência.</summary>
    public const int Scram            = 3;

    /// <summary>Válvula de vapor para turbina. 0 = fechada, 1 = aberta.</summary>
    public const int TurbineValve     = 4;

    /// <summary>Spray de contenção de emergência. 0 = off, 1 = máxima. Arrefece núcleo mas gasta água.</summary>
    public const int ContainmentSpray = 5;

    /// <summary>Bomba de feedwater (água de retorno). 0 = parada, 1 = máxima. Repõe o nível do vessel.</summary>
    public const int FeedwaterPump    = 6;

    /// <summary>Demanda eléctrica da grid. 0 = sem consumo, 1 = demanda máxima. Afecta contra-pressão da turbina.</summary>
    public const int ElectricalLoad   = 7;

    /// <summary>Arrancar reactor. &gt;0.5 = StartReactor(). Botão momentary.</summary>
    public const int ReactorStart     = 8;

    /// <summary>Parar reactor. &gt;0.5 = StopReactor(). Botão momentary.</summary>
    public const int ReactorStop      = 9;

    // ===================================================================
    //  OUTPUT — O reactor emite
    // ===================================================================

    /// <summary>Temperatura do núcleo. 0 = ambiente, 1 = meltdown.</summary>
    public const int Temperature      = 10;

    /// <summary>Nível de refrigerante no vessel. 0 = vazio, 1 = cheio.</summary>
    public const int CoolantLevel     = 11;

    /// <summary>Pressão no vessel. 0 = atmosférica, 1 = pressão máxima.</summary>
    public const int Pressure         = 12;

    /// <summary>Potência térmica actual. 0 = zero, 1 = máxima.</summary>
    public const int PowerOutput      = 13;

    /// <summary>Estado operacional. 0=normal, 0.25=warn, 0.5=critical, 0.75=scram, 1=meltdown.</summary>
    public const int Status           = 14;

    /// <summary>Fluxo de neutrões (reatividade instantânea). 0 = sub-crítico, 1 = super-crítico.</summary>
    public const int NeutronFlux      = 15;

    /// <summary>Rotação da turbina. 0 = parada, 1 = RPM máximo.</summary>
    public const int TurbineRPM       = 16;

    /// <summary>Nível de radiação na contenção. 0 = normal, 1 = perigo máximo.</summary>
    public const int Radiation        = 17;

    /// <summary>Energia total produzida (MWh normalizado). Acumulador, nunca baixa.</summary>
    public const int EnergyTotal      = 18;

    /// <summary>Combustível restante. 0 = vazio, 1 = cheio.</summary>
    public const int FuelPercent      = 19;

    // ===== TOTAL =====
    public const int ChannelCount     = 19;

    /// <summary>
    /// Enum para dropdown no inspector — seleccionar canal sem escrever números.
    /// </summary>
    public enum Select
    {
        RodInsertion     = 1,
        CoolantFeed      = 2,
        Scram            = 3,
        TurbineValve     = 4,
        ContainmentSpray = 5,
        FeedwaterPump    = 6,
        ElectricalLoad   = 7,
        ReactorStart     = 8,
        ReactorStop      = 9,
        Temperature      = 10,
        CoolantLevel     = 11,
        Pressure         = 12,
        PowerOutput      = 13,
        Status           = 14,
        NeutronFlux      = 15,
        TurbineRPM       = 16,
        Radiation        = 17,
        EnergyTotal      = 18,
        FuelPercent      = 19,
    }

    // ===================================================================
    //  METADADOS
    // ===================================================================

    public enum ChannelDirection { Input, Output }

    [System.Serializable]
    public struct ChannelInfo
    {
        public int id;
        public string name;
        public string shortName;
        public ChannelDirection direction;
        public string description;
    }

    private static readonly ChannelInfo[] Infos =
    {
        // --- INPUT ---
        new ChannelInfo { id = 1,  name = "RodInsertion",     shortName = "RODS",
            direction = ChannelDirection.Input,
            description = "0=barras fora, 1=barras dentro" },

        new ChannelInfo { id = 2,  name = "CoolantFeed",      shortName = "COOL",
            direction = ChannelDirection.Input,
            description = "Bomba de refrigerante 0-1" },

        new ChannelInfo { id = 3,  name = "Scram",            shortName = "SCRAM",
            direction = ChannelDirection.Input,
            description = ">0.5 = paragem de emergência" },

        new ChannelInfo { id = 4,  name = "TurbineValve",     shortName = "TURB",
            direction = ChannelDirection.Input,
            description = "Válvula de vapor 0-1" },

        new ChannelInfo { id = 5,  name = "ContainmentSpray", shortName = "SPRAY",
            direction = ChannelDirection.Input,
            description = "Spray de emergência 0-1" },

        new ChannelInfo { id = 6,  name = "FeedwaterPump",    shortName = "FEED",
            direction = ChannelDirection.Input,
            description = "Bomba de feedwater 0-1" },

        new ChannelInfo { id = 7,  name = "ElectricalLoad",   shortName = "LOAD",
            direction = ChannelDirection.Input,
            description = "Demanda da grid 0-1" },

        new ChannelInfo { id = 8,  name = "ReactorStart",     shortName = "START",
            direction = ChannelDirection.Input,
            description = ">0.5 = arrancar reactor" },

        new ChannelInfo { id = 9,  name = "ReactorStop",      shortName = "STOP",
            direction = ChannelDirection.Input,
            description = ">0.5 = desligar reactor" },

        // --- OUTPUT ---
        new ChannelInfo { id = 10, name = "Temperature",      shortName = "TEMP",
            direction = ChannelDirection.Output,
            description = "Temperatura do núcleo 0-1" },

        new ChannelInfo { id = 11, name = "CoolantLevel",     shortName = "LVL",
            direction = ChannelDirection.Output,
            description = "Nível de refrigerante 0-1" },

        new ChannelInfo { id = 12, name = "Pressure",         shortName = "PRESS",
            direction = ChannelDirection.Output,
            description = "Pressão no vessel 0-1" },

        new ChannelInfo { id = 13, name = "PowerOutput",      shortName = "POWER",
            direction = ChannelDirection.Output,
            description = "Potência térmica 0-1" },

        new ChannelInfo { id = 14, name = "Status",           shortName = "STATUS",
            direction = ChannelDirection.Output,
            description = "0=ok 0.25=warn 0.5=crit 0.75=scram 1=meltdown" },

        new ChannelInfo { id = 15, name = "NeutronFlux",      shortName = "FLUX",
            direction = ChannelDirection.Output,
            description = "Fluxo de neutrões 0-1" },

        new ChannelInfo { id = 16, name = "TurbineRPM",       shortName = "TRPM",
            direction = ChannelDirection.Output,
            description = "Rotação da turbina 0-1" },

        new ChannelInfo { id = 17, name = "Radiation",        shortName = "RAD",
            direction = ChannelDirection.Output,
            description = "Radiação na contenção 0-1" },

        new ChannelInfo { id = 18, name = "EnergyTotal",      shortName = "MWh",
            direction = ChannelDirection.Output,
            description = "Energia acumulada normalizada" },

        new ChannelInfo { id = 19, name = "FuelPercent",      shortName = "FUEL",
            direction = ChannelDirection.Output,
            description = "Combustível restante 0-1" },
    };

    public static ChannelInfo GetInfo(int channel)
    {
        if (channel >= 1 && channel <= ChannelCount)
            return Infos[channel - 1];
        return new ChannelInfo { id = channel, name = "CH" + channel, shortName = "CH" + channel };
    }

    public static string GetName(int channel)       => GetInfo(channel).name;
    public static string GetShortName(int channel)   => GetInfo(channel).shortName;
    public static ChannelDirection GetDirection(int channel) => GetInfo(channel).direction;
    public static string GetDescription(int channel) => GetInfo(channel).description;

    public static string FormatValue(int channel, float value)
    {
        switch (channel)
        {
            case Status:
                if (value >= 0.9f)  return "MELTDOWN";
                if (value >= 0.7f)  return "SCRAM";
                if (value >= 0.45f) return "CRITICAL";
                if (value >= 0.2f)  return "WARNING";
                return "NORMAL";
            case Scram:
                return value > 0.5f ? "ACTIVE" : "OFF";
            case ReactorStart:
                return value > 0.5f ? "PRESSED" : "OFF";
            case ReactorStop:
                return value > 0.5f ? "PRESSED" : "OFF";
            case NeutronFlux:
                return (value * 100f).ToString("F1") + "%";
            case TurbineRPM:
                return (value * 100f).ToString("F0") + " RPM";
            case Radiation:
                return (value * 100f).ToString("F1") + " mSv";
            case EnergyTotal:
                return (value * 100f).ToString("F1") + " MWh";
            default:
                return (value * 100f).ToString("F0") + "%";
        }
    }
}
