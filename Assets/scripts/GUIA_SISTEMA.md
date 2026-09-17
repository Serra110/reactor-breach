# Guia do Sistema Elétrico + Sinal

## Visão Geral

O jogo tem **dois sistemas** que usam Port/Node e Wire:

| Sistema | Transporta | Valores | Exemplos |
|---------|-----------|---------|----------|
| **Elétrico** (potência) | `Port` | Integers simples (0–100) | PowerSource, Switch, NodeSplitter, NeonLight |
| **Sinal** (comandos/telemetria) | `Node` | ReactorPacket int (target\|action\|value) | SignalLever, SignalButton, ReactorController |

Ambos usam o mesmo `Wire` para ligar — um Output a um Input. O `Reactor Command Input` é uma entrada de sinal partilhada: pode receber um Wire do lever, um Wire de um botão e outros comandos, sem precisar de criar vários Nodes no dispositivo.

---

## Hierarquia de Classes

```
EletricUnit                    ← base: unitName + OnDetected/OnEnteract
├── Port                       ← ponto de conexão (value int, onValueChanged event)
│   └── Node                   ← Port + nodeLabel + signalType (Command/Telemetry)
│                                displayValue descompacta ReactorPacket automaticamente
├── ElectricDevice             ← tem inputPorts/outputPorts (List<Port>), soma e distribui potência
│   ├── NodeSplitter           ← 1 input, N outputs (divide potência igualmente)
│   ├── Switch                 ← gate on/off (passa ou bloqueia potência)
│   └── NeonLight              ← acende/se apaga conforme potência recebida
├── PowerSource                ← gera potência (0 ou maxOutput)
│   └── DieselEngine           ← PowerSource com vibração visual
├── PowerStorage               ← bateria: acumula input, outputs limitados a maxOutput
├── SignalDevice               ← base de sinal: inputNodes/outputNodes (List<Node>)
│   └── SignalModule           ← herda SignalDevice + controlUI + OnEnteract
│       ├── SignalLever        ← alavanca analógica (0–100), drag com rato
│       ├── SignalButton       ← botão momentâneo ou toggle
│       ├── SignalSwitch       ← toggle ON/OFF persistente
│       ├── SignalDisplay      ← mostra valor de um canal de telemetria
│       ├── SignalAlarm        ← alarme com threshold + hysteresis
│       ├── SignalSplitter     ← 1 input → N outputs (reencaminha pacotes)
│       ├── SignalMerger       ← N inputs → 1 output (combina pacotes)
│       └── SignalTranslator   ← "Arduino": recebe telemetria, emite comandos condicionais
└── ReactorController          ← o reactor: commandNode (input) + outputNode (telemetria)
```

---

## Sistema Elétrico (Potência)

### Como funciona
1. `PowerSource` gera um valor integer (ex: 100) no seu outputPort.
2. Um `Wire` liga esse outputPort ao inputPort de um `ElectricDevice`.
3. O device soma todos os inputs, subtrai `deviceCost`, e distribui o resto igualmente pelos outputs.
4. O `NodeSplitter` faz exactamente isto: 1 input → N outputs divididos.
5. O `Switch` é um gate: liga → passa a potência; desliga → bloqueia (outputs = 0).

### Convenção de valores
- **0** = sem potência / desligado
- **1–100** = potência (0–100%)
- Valores inteiros simples, **não** ReactorPacket

### Como ligar
```
PowerSource [output] ──Wire──> Switch [input]
                                Switch [output] ──Wire──> NodeSplitter [input]
                                                           NodeSplitter [output_1] ──> NeonLight [input]
                                                           NodeSplitter [output_2] ──> NeonLight [input]
```

### Componentes

| Componente | Input | Output | O que faz |
|-----------|-------|--------|-----------|
| `PowerSource` | — | 1 | Gera `maxOutput` quando `isGeneratingPower=true` |
| `DieselEngine` | — | 1 | PowerSource + vibração visual |
| `PowerStorage` | 1 | 1 | Acumula input, output limitado a `maxOutput` |
| `ElectricDevice` | N | M | Soma inputs, subtrai `deviceCost`, distribui pelo outputs |
| `NodeSplitter` | 1 | N | Divide input igualmente por N outputs |
| `Switch` | N | M | Gate: passa ou bloqueia potência conforme `switchState` |
| `NeonLight` | N | M | Acende se tiver potência suficiente |

---

## Sistema de Sinal (Comandos / Telemetria)

### Como funciona
1. Um módulo de sinal (Lever, Button, etc.) cria um `ReactorPacket` int:
   ```
   packet = Target (8 bits) | Action (8 bits) | Value (16 bits)
   ```
2. Publica no seu `outputNode` → `PublishValue(packet)`.
3. Um `Wire` liga ao `inputNode` do receptor (ReactorController, Display, etc.).
4. O receptor descompacta: `GetTarget()`, `GetAction()`, `GetValue()`.

### ReactorPacket

```csharp
ReactorPacket.Make(target, action, value)
ReactorPacket.GetTarget(packet)   // bits 24-31
ReactorPacket.GetAction(packet)   // bits 16-23
ReactorPacket.GetValue(packet)    // bits 0-15
```

### Targets (destinos)

| Constante | Valor | O que controla |
|-----------|-------|----------------|
| `TargetReactor` | 1 | Start/Stop/Scram do reactor |
| `TargetRods` | 2 | Posição das barras de controlo (0–100) |
| `TargetCoolant` | 3 | Fluxo de arrefecimento (0–100) |
| `TargetFeedwater` | 4 | Bomba de alimentação ON/OFF |
| `TargetTurbine` | 5 | Turbina ON/OFF + throttle |
| `TargetBreaker` | 6 | Breaker eléctrico ON/OFF |
| `TargetTelemetry` | 100 | (saída) Telemetria do reactor |

### Actions (acções)

| Constante | Valor | Efeito geral |
|-----------|-------|-------------|
| `ActionSet` | 1 | Define valor (value = 0–100) |
| `ActionEnable` | 2 | Liga |
| `ActionDisable` | 3 | Desliga |
| `ActionToggle` | 4 | Inverte estado actual |
| `ActionIncrease` | 6 | Aumenta +10 (rods) |
| `ActionDecrease` | 7 | Diminui -10 (rods) |
| `ActionTrigger` | 5 | Dispara acção (value = TriggerStart/Stop/Scram) |

### Triggers (para ActionTrigger)

| Constante | Valor | Efeito |
|-----------|-------|--------|
| `TriggerStart` | 1 | Liga reactor |
| `TriggerStop` | 2 | Desliga reactor |
| `TriggerScram` | 3 | SCRAM (emergência) |

### Convenção de valores nos nós de sinal
- O valor interno do `Node` continua a ser um `ReactorPacket` packed; nunca é o número mostrado ao jogador.
- `Node.displayValue` mostra o valor útil: `ActionSet` e telemetria usam o valor do pacote (normalmente 0–100), enquanto `Trigger`, `Enable` e `Disable` aparecem como 0/1.
- No `ElectricUI1`, ao olhar para um nó de sinal, vês o valor útil e o `nodeLabel`, não o inteiro packed.

---

## Módulos de Sinal — O que cada um faz

### Regra dos Nodes
- Cada `SignalLever`, `SignalButton` e `SignalSwitch` tem **um único Node de Output**.
- A função do Node é configurada no próprio módulo através de `commandTarget`, `commandAction` e `commandValue`.
- O `Reactor Command Input` aceita vários Wires de sinal. Portanto, lever, botão e switch podem ligar directamente ao mesmo input sem um conjunto de Nodes escondidos dentro de outro dispositivo.
- `SignalMerger` continua disponível apenas como módulo opcional para uma montagem que explicitamente precise de vários inputs visíveis.

### SignalLever (Alavanca Analógica)
- **Input:** — (é fonte)
- **Output:** 1 Node
- **O que faz:** Envia `ActionSet` com valor 0–100 conforme a posição do braço.
- **Drag:** Rato para cima/baixo altera o valor continuamente.
- **Caso de uso:** Posição das barras de controlo, fluxo de coolant.


### SignalButton (Botão)
- **Input:** — (é fonte)
- **Output:** 1 Node (commandNode)
- **Modos:**
  - `Momentary`: Envia um comando por clique (ex: SCRAM).
  - `Toggle`: Alterna entre comando ON e OFF.
- **Caso de uso:** SCRAM, Start, Stop, toggle de componentes.

### SignalSwitch (Toggle ON/OFF)
- **Input:** — (é fonte)
- **Output:** 1 Node (commandNode)
- **O que faz:** Envia `ActionEnable` (ON) ou `ActionDisable` (OFF).
- **Caso de uso:** Liga/desliga feedwater, turbine, breaker.

### SignalSplitter (Divisor de Sinal)
- **Input:** 1 Node (inputNode)
- **Output:** N Nodes (outputNodes, padrão 4)
- **O que faz:** Reencaminha o pacote recebido para todos os outputs.
- **Caso de uso:** Enviar a mesma telemetria para vários displays.

### SignalMerger (Combinador de Sinal)
- **Input:** N Nodes (inputNodes, padrão 4)
- **Output:** 1 Node (outputNode)
- **O que faz:** Qualquer input recebido é reencaminhado para o output.
- **Caso de uso:** Comandos de várias fontes para um só destino.

### SignalDisplay (Ecrã de Telemetria)
- **Input:** 1 Node (inputNode)
- **Output:** — (é sink)
- **O que faz:** Filtra pacotes `TargetTelemetry`, extrai o canal, mostra o valor.
- **Campos:** `channel` = qual canal mostrar (Power=0, Temperature=1, etc.).

### SignalAlarm (Alarme)
- **Input:** 1 Node (inputNode)
- **Output:** 1 Node opcional (outputNode)
- **O que faz:** Monitoriza um canal de telemetria. Quando o valor cruza o `threshold`, activa visual + som. Se `emitCommandOnAlarm=true`, envia comando no output.
- **Hysteresis:** Evita flicker (o threshold de desligar é diferente do de ligar).

### SignalTranslator ("Arduino")
- **Input:** 1 Node (inputNode)
- **Output:** 1 Node (outputNode)
- **O que faz:** Recebe telemetria, compara com regras (`fromChannel`, `fromMin`, `fromMax`), e emite comandos configuráveis.
- **Exemplo:** "Se Temperature >= 80, enviar SCRAM".

---

## Telemetria do Reactor (13 Canais)

O `ReactorController` emite telemetria no `outputNode` em round-robin (a cada 0.05s):

| Canal | Constante | Valor | O que mostra |
|-------|-----------|-------|-------------|
| 1 | `Power` | 0–100 | Potência actual (%) |
| 2 | `Temperature` | 0–100 | Temperatura do núcleo |
| 3 | `CoolantLevel` | 0–100 | Nível de água |
| 4 | `CoolantFlow` | 0–100 | Fluxo de arrefecimento |
| 5 | `SteamProduction` | 0–100 | Produção de vapor |
| 6 | `SteamPressure` | 0–100 | Pressão do vapor |
| 7 | `RodPosition` | 0–100 | Posição das barras (0=inseridas, 100=retiradas) |
| 8 | `Fuel` | 0–100 | Combustível restante (%) |
| 9 | `TurbineState` | 0/1 | Turbina ligada |
| 10 | `TurbineOutput` | 0–100 | Output da turbina |
| 11 | `ElectricalOutput` | 0–100 | Output eléctrico (generator) |
| 12 | `OperatingState` | 0–7 | Estado: Offline/Starting/Running/Warning/Critical/Scram/Shutdown/Meltdown |
| 13 | `AlarmLevel` | 0/1/2 | Nível de alarme: None/Warning/Critical |

---

## Estados do Reactor

| Estado | Valor | Condição |
|--------|-------|----------|
| Offline | 0 | Sem actividade |
| Starting | 1 | Ligado, potência a subir (< 5%) |
| Running | 2 | Potência > 5%, tudo OK |
| Warning | 3 | Uma ou mais variáveis fora da zona segura |
| Critical | 4 | Temperatura ≥80, nível ≤15, ou pressão ≥90 |
| Scram | 5 | SCRAM activado (barras a inserir) |
| Shutdown | 6 | Desligado mas com calor residual |
| Meltdown | 7 | Temperatura ≥100 durante ≥8 segundos |

---

## Sequência de Arranque (como o reactor deve ser operado)

```
1. Inserir combustível (E no reactor com fuelItem na hotbar)
2. Ligar SCRAM / Barras = 0 (inseridas, posição segura)
3. Ligar Feedwater (mantém nível de água)
4. Ligar Coolant (fluxo de arrefecimento)
5. SCRAM → StartReactor (reactor fica "engaged" mas sem potência)
6. Retirar barras lentamente (lever → 100) → potência sobe
7. Monitorizar temperatura e pressão
8. Quando steamPressure > 0, ligar Turbine
9. Quando turbineOutput > 0, ligar Breaker → output eléctrico
```

---

## SCRAM e Interlocks de Segurança

**SCRAM manual:** Enviar `TriggerScram` → barras inserem a `scramSpeed` (300 u/s, ~0.3s).
**SCRAM automático:** Activa quando:
- Temperatura ≥ `autoScramTemperature` (95°C)
- Pressão ≥ `autoScramPressure` (95)
- Nível de coolant ≤ `autoScramCoolantLevel` (5%)

**Limpar SCRAM:** Enviar `StartReactor` (só funciona se temp < 80°C e há combustível).

**Convenção das barras:**
- `RodPosition = 0` → barras totalmente inseridas → zero reactividade → zero potência
- `RodPosition = 100` → barras totalmente retiradas → máxima reactividade → máxima potência
- As barras deslizam a `rodInsertSpeed` (20 u/s) ou `rodWithdrawSpeed` (10 u/s) — não teleportam.

---

## SignalDevice (Base Configurável)

O `SignalDevice` (base dos módulos de sinal) tem listas configuráveis no inspector:

```csharp
public List<Node> inputNodes;   // Arrastar Nós aqui (tipo Input)
public List<Node> outputNodes;  // Arrastar Nós aqui (tipo Output)
public bool forwardPackets;     // Reencaminhar input→output automaticamente
```

**Para criar um módulo novo:**
1. Criar um GameObject.
2. Adicionar `SignalDevice` (ou herdar dele).
3. Criar Nós (Nodes) e arrastar para as listas.
4. Ligar com Wires como no sistema elétrico.
5. O `forwardPackets` reencaminha automaticamente.

**Módulos existentes** (Lever, Button, etc.) usam campos hardcoded — mas podes usar as listas do SignalDevice para módulos novos.

---

## Como Ligar Tudo (Exemplo Completo)

```
[CONTROLO DO REACTOR]

  SignalLever [outputNode] ──Wire──> ReactorController [commandNode]
  (commandTarget=Rods, commandAction=Set)
  → Envia valor 0-100, reactor ajusta RodPosition (0=inseridas, 100=retiradas)

SignalButton [outputNode] ──Wire──> ReactorController [commandNode]
  (commandTarget=Reactor, commandAction=Trigger, commandValue=TriggerScram)
  → Clique envia SCRAM

SignalSwitch [outputNode] ──Wire──> ReactorController [commandNode]
  (commandTarget=Feedwater, useEnableDisable=true)
  → Toggle liga/desliga feedwater


[MONITORAMENTO]

ReactorController [outputNode] ──Wire──> SignalSplitter [inputNode]
  SignalSplitter [output_1] ──Wire──> SignalDisplay [inputNode]  (channel=Temperature)
  SignalSplitter [output_2] ──Wire──> SignalDisplay [inputNode]  (channel=Power)
  SignalSplitter [output_3] ──Wire──> SignalAlarm [inputNode]    (channel=Temperature, threshold=80)


[AUTOMAÇÃO]

ReactorController [outputNode] ──Wire──> SignalTranslator [inputNode]
  (binding: fromChannel=Temperature, fromMin=80, toTarget=Reactor, toAction=Trigger, toValue=TriggerScram)
  SignalTranslator [outputNode] ──Wire──> ReactorController [commandNode]
  → Se temperatura ≥80, SCRAM automático


[ENERGIA ELÉCTRICA]

PowerSource [output] ──Wire──> Switch [input]
  Switch [output] ──Wire──> NodeSplitter [input]
    NodeSplitter [output_1] ──Wire──> NeonLight [input]
    NodeSplitter [output_2] ──Wire──> NeonLight [input]
```

---

## Erros Comuns

| Problema | Causa | Solução |
|----------|-------|---------|
| Números estranhos no UI | Port.value é ReactorPacket int | `Node.displayValue` descompacta automaticamente |
| Alavanca anda ao contrário | `invertDrag` serializado no prefab | Abrir prefab, toggle `invertDrag` |
| Botão mergulha no prefab | PressAnimation re-cacheava posição | Corrigido: cache só no Awake |
| Nós pequenos no game | Port.ResetPort forçava scale 0.2 | Corrigido: usa escala original do prefab |
| Toggle volta sozinho | `ApplyControlRodDefaults` + `[ExecuteAlways]` | Corrigido: defaults aplicam-se só uma vez |
