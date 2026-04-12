# DATN Co-op Project Architecture Documentation

This document provides an overview of the technical architecture and core systems of the **DATN Co-op** project.

## 1. Core Communication: EventBus
The `EventBus` (`Assets/_Game/Scripts/Core/EventBus.cs`) is a static hub for decoupled communication. It uses C# `Action` events to bridge different modules without direct hard-references.

### Key Event Categories:
- **Player:** `OnPlayerDied`, `OnPlayerRespawned`.
- **Level:** `OnLevelCompleted`, `OnCheckpointReached`.
- **Interaction:** `OnInteractableActivated`, `OnCoopInteractablePlayerReady`.
- **Game State:** `OnGamePaused`, `OnGameResumed`.
- **Networking:** `OnClientConnected`, `OnClientDisconnected`, `OnAllPlayersReady`.
- **Systems:** `OnCameraPresetChanged`, `OnSettingsChanged`, `OnInputBindingChanged`, `OnScreenShakeRequested`.

---

## 2. Player System: State Machine
The player logic is managed by `PlayerStateMachine` (`Assets/_Game/Scripts/Player/PlayerStateMachine.cs`).

- **Architecture:** Uses a State pattern with ~15+ states (Idle, Walk, Run, Jump, WallHang, etc.).
- **Networking:** State is synchronized across all clients using `NetworkVariable<PlayerStateType>`. 
  - Only the **Owner** can initiate transitions via `TransitionTo()`.
  - All clients (including proxies) react to state changes via `OnStateValueChanged` to play appropriate animations and FX.
- **Physics:** `PlayerController` handles the actual movement logic based on the current active state.

---

## 3. Enemy & AI System
AI is driven by Unity's **Behavior** package.

- **Logic:** Defined in `.asset` behavior graphs.
- **Movement:** `EnemyMovement.cs` handles server-authoritative movement using NavMesh pathfinding combined with Root Motion for high-quality animations.
- **Detection:** Uses `VisionSensor` for target tracking.

---

## 4. Networking: Unity Netcode for GameObjects (NGO)
The game is built for multiplayer using Unity NGO.

- **Management:** `NetworkManagerWrapper` handles the NGO lifecycle and connects network events to the `EventBus`.
- **Synchronization:** 
  - `ClientNetworkTransform` (custom) or standard `NetworkTransform` for movement.
  - `NetworkVariable` for high-level state synchronization (like player health or current animation state).
- **Services:** Integrated with **Unity Authentication** and **Unity Relay** for internet connectivity.

---

## 5. Data-Driven Design
Extensive use of `ScriptableObjects` (`SO...Config.cs`) for:
- Player movement parameters.
- Enemy stats and behavior settings.
- VFX and Screen Shake configurations.
- Audio settings.

---

## 6. Directory Structure Overview
- `Assets/_Game/Scripts/Core`: High-level managers, Game State Machine, EventBus.
- `Assets/_Game/Scripts/Player`: Player FSM, Controller, Input, Combat.
- `Assets/_Game/Scripts/Enemies`: AI logic, Enemy movement, Sensors.
- `Assets/_Game/Scripts/Network`: NGO wrappers, Auth, Relay, Player Syncing.
- `Assets/_Game/Scripts/UI`: HUD, Menu systems, Prompt management.
- `Assets/_Game/Scripts/Environment`: Interactables, Hazards, Level triggers.

---

*Last Updated: 2026-04-12*
