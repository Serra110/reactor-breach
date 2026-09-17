# Estado do MVP — Reactor Breach

Última revisão: 15 Set 2026 (auditoria completa + limpeza).

## Objetivo do MVP

Jogável de ponta a ponta, com menos lixo e menos bugs: reactor, electric grid, building, inventory, NPC/SCP, multiplayer e containment. Pronto para dar build e testar.

---

## Fluxo de cenas (Build Settings — 5 cenas, sem órfãs)

| Ordem | Cena | Papel | Estado |
|-------|------|-------|--------|
| 1 | `TitleScreen` | Splash inicial (qualquer tecla → MainMenu) | Enabled |
| 2 | `MainMenu` | Menu: New Game (offline) / Button (online lobby) / Quit | Enabled |
| 3 | `gameonline` | Mapa **online** (players spawn via RoomManager) | Enabled |
| 4 | `lobbyonline` | Lobby online (OnlineRoomManager + KcpTransport :7777) | Enabled |
| 5 | `scene2` | Mapa **offline** (player pré-instancionado; Bootstrapper constrói facility) | Enabled |

- **Offline:** `MainMenu → scene2` — sem Mirror; `OfflineMvpBootstrap` limpa scenery legacy, constrói a facility e ativa o player.
- **Online:** `MainMenu → lobbyonline → StartOnlineGame → gameonline` — `OnlineRoomManager` (max 10 conns, min 1 player).
- **Removidas nesta ronda (órfãs, 0 refs):** `scene1`, `NetworkTest`, `SplashScreen`, `Scenes/SampleScene` — também purgadas do `EditorBuildSettings`.

## Fixes críticos aplicados (auditoria completa)

### Críticos (gameplay quebrar sem exceção)
- **`npcai.cs`** — `GenerateWaypoints()` nunca era chamado → o SCP ficava preso Idle↔Patrol sem patrulhar. Agora é chamado no `Start()`.
- **`PlayerInteraction.cs`** — apanhar item com inventário cheio destruía o item no chão. Agora usa a sobra do `Inventory.AddItem`; o item só é destruído se `remaining <= 0`, senão fica no chão com a quantidade restante.
- **`Ore.cs`** — minério era destruído mesmo sem nada o aceitar (Conveyor/Storage cheios). Removido o ramo Storage incorreto (duplicava o `void AddItem` sem sobra) e o inventário agora só destrói quando `remaining < amount`.

### Médios (corrigidos)
- **`WireTool`** — `Update()` fazia `if (!NetworkClient.active) return;` → offline não dava para ligar fios. Guard removido; o ramo offline do `CreateWire` já existia e agora é alcançável.
- **`CameraFollower`** — ramo parented ignorava `eyeHeight` (câmara ao nível do chão). Corrigido com `parent.InverseTransformPoint(playerBody + up*eyeHeight)`.
- **`SecurityDoor`** — clearance nunca comparava níveis. Agora usa `Keycard.clearanceLevel >= requiredLevel` e mantém fallback para keycardItem específico.
- **`ElectricDevice`** — event leak de `port.onValueChanged` (Start sem OnDestroy). Adicionado `OnDestroy` com unsubscribe.
- **`ElectricUI1` / `Inventory.Instance`** — singletons sem limpeza entre cenas. `OnDestroy` agora limpa se `instance == this`.
- **`PauseMenu`** — sem `OnDestroy`: se destruído enquanto pausado, `Time.timeScale` ficava 0 para sempre. Corrigido.
- **`SaveManager1.LoadGame`** — `player.position` sem null-check → NRE. Corrigido.
- **`PlayerSaveHandler`** — `OnEnable` usava `SaveManager.Instance` sem guard. Corrigido.
- **`OnlineRoomManager`** — `ResolveLobbyInterface()` fazia 5× `GameObject.Find` por frame. Agora throttled (1s).
- **`StorageContainer`** — `enableDebugKeys = true` por defeito expunha atalhos G/H em build. Agora `false`.

### Logs
- **`DebugFlags.electricLogs`** adicionado; `PowerSource`/`PowerStorage` deixam de spammar a consola em build.

## Limpeza do mapa (removido/limpo)
- **`OfflineMvpBootstrap.HideLegacyScenery`** — o array `names` existia mas nunca era usado (só escondia `pb_Mesh`). Agora esconde Planes/Cubes/PolyShapes/CutGemsVar1/Buildobjects/gameobjects **mas não esconde objetos com componentes de gameplay** (`HasGameplayComponents`: ReactorController, Storage, SmelterController, Conveyor, Inventory, Port, NetworkIdentity) — protege o reactor (anexado a um "Cube") e a rede de energia.
- Prefabs/backups/scripts mortos já removidos em rondas anteriores (ver abaixo).

## Verificação de consistência
- `QuotaSystem` reconhece `scene2` e `gameonline` — OK com o fluxo real.
- `TitleManager` → MainMenu; `MenuManager` → scene2/lobbyonline — OK.
- **Nota arquitetural:** `ReactorController` emite telemetria com IDs legacy (`ReactorChannel`) enquanto `SignalButton`/`SignalLever` usam `ReactorChannelDef` com IDs numéricos sobrepostos — em funcionamento, mas risco futuro de confusão. Não alterado para não partir sinais.

## Recursos confirmados
- TMP essentials completos (LiberationSans SDF + Fallback + materiais).
- Todos os prefabs usados pela scene2 vivem fora das pastas de backup (removidas).

## A fazer antes de dar build / ir para testes
1. **AudioClips do NPC** (se não atribuídos): passos → `Footsteps - Essentials/Footsteps_Metal`, rugido → `wolf_monster.mp3`, ataque → `impactsplat03.mp3`.
2. **Containment na cena**: montar `SecurityDoor`, `Keycard`, `ContainmentCell` no mapa.
3. **Testar o fluxo online** com 2 instâncias (Host + Client) antes de ship.
4. `DebugFlags` em `false` no build final (o padrão).
5. `ThirdPersonBuildingView` existe mas não é usado — design atual (first-person ao construir).

## Notas de arquitetura
- `OfflineMvpBootstrap` e `MenuManager` usam `[RuntimeInitializeOnLoadMethod]` — funcionam sem objetos pré-armados na cena.
- `OnlineRoomManager` define por código nomes de cenas (`lobbyonline`, `gameonline`, `MainMenu`) e endereço default (`localhost`), porta KCP `7777`.
- Sem `.asmdef` próprio do jogo (só os do Mirror) — scripts no Assembly-CSharp.