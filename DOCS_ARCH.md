# DATN Co-op Project Architecture Documentation

This document provides a comprehensive overview of the technical architecture, core systems, and development patterns of the **DATN Co-op** project.

## 1. Core Architecture

### Event-Driven Communication (`EventBus.cs`)
The project uses a centralized `EventBus` (`Assets/_Game/Scripts/Core/EventBus.cs`) as the primary communication hub. This pattern ensures high decoupling between systems.
- **Mechanism:** Static C# `Action` events with static "Raise" methods.
- **Categories:** Player (Health/Respawn), Level (Completion/Checkpoints), Interaction (Co-op/Single), Game State (Pause/Resume), Networking (Connect/Disconnect/Ready), Camera, FX (Screen Shake), CutScenes, and Input.
- **Standard:** Modules should never call each other directly if a corresponding event exists in the `EventBus`.

### Game State Management (`GameStateMachine.cs`)
The high-level game flow is managed by a state machine that controls transitions between:
- `Loading`
- `MainMenu`
- `Playing`

### Data-Driven Design (`ScriptableObjects`)
Constants and configurations are externalized into `ScriptableObjects` (`SO...Config.cs`) to allow designers to tweak gameplay without code changes:
- `SOPlayerConfig`: Movement speeds, jump heights, dash distances.
- `SOLevelConfig`: Level-specific settings.
- `SOVFXConfig` & `SOScreenShakeConfig`: Visual feedback parameters.
- `SOAudioClip`: Audio mapping and settings.

---

## 2. Player System

### State Machine Architecture
The player uses a Finite State Machine (FSM) implemented across several classes:
- **`PlayerStateMachine`**: Manages the current active state and handles transitions.
- **`PlayerState`**: Abstract base class for all player states.
- **States**: Located in `Assets/_Game/Scripts/Player/States`. Includes ~15+ specialized states like `IdleState`, `RunState`, `JumpState`, `WallHangState`, `Attack1/2/3State`, `DashState`, `AirGlideState`, etc.
- **Network Sync:** The `PlayerStateMachine` synchronizes the `CurrentState` across the network using a `NetworkVariable<PlayerStateType>`. Only the owner can transition states, and proxies react to these changes to update visuals.

### Core Components
- **`PlayerController`**: Handles physical movement logic, gravity, and collision detection using a `CharacterController`.
- **`PlayerInputHandler`**: Bridges the Unity Input System Package to gameplay logic, converting raw inputs into actionable commands.
- **`PlayerAnimator`**: Manages animation transitions and triggers, often listening to state changes from the FSM.
- **`AttackComboController`**: Manages the timing and sequencing of combat combos.
- **`PlayerHealth`**: Manages health and death logic, notifying the `EventBus` on death.

---

## 3. AI & Enemy System

### Unity Behavior Package
AI logic is implemented using the new **Unity Behavior** package (Unity 6).
- **Behavior Graphs**: Logic is visually defined in `.asset` files (e.g., `Armadil_03_Behavior`).
- **Custom Actions**: C# scripts that extend the behavior package:
  - `ChaseTargetAction`: Pathfinding towards a player.
  - `MoveToPositionAction`: Moving to specific coordinates.
  - `GetWaypointAction`: Interacting with the `WaypointManager`.
- **Sensors**: `VisionSensor.cs` handles target detection and field-of-view logic.

### Enemy Components
- **`EnemyMovement`**: Server-authoritative movement using `NavMeshAgent` and Root Motion.
- **`EnemyCombat`**: Handles attack logic and damage output.
- **`EnemyHealth`**: Manages enemy durability and implements `IDamageableEnemy`.

---

## 4. Networking (NGO)

### Core Integration
The project uses **Netcode for GameObjects (NGO)** for multiplayer functionality.
- **`NetworkManagerWrapper`**: Manages the NGO lifecycle and maps network events (like client connection) to the `EventBus`.
- **`PlayerSpawner`**: Handles spawning player prefabs for connected clients at designated spawn points.

### Synchronization Strategies
- **Movement**: Uses a combination of `ClientNetworkTransform` and `NGOPlayerSync` for smooth, interpolated movement on all clients.
- **States**: High-level gameplay states (Player State, Health) are synchronized via `NetworkVariable`.
- **Actions**: One-shot events (like jumping or attacking) are often triggered via `[ServerRpc]` and `[ClientRpc]`.

### Backend & Services
Integrated with **Unity Gaming Services (UGS)**:
- **`AuthManager`**: Handles anonymous or account-based authentication.
- **`RelayManager`**: Manages Unity Relay for P2P connection without port forwarding.

---

## 5. Environment & Gameplay

### Interaction System
- **`InteractableBase`**: Abstract class for interactive objects.
- **`CoopInteractable`**: Specialized interactables requiring both players to be present or ready (e.g., synchronized switches).
- **`SimpleSlidingDoor`**: Example of an environment object reacting to interactions.

### Systems
- **`RespawnManager`**: Listens to `OnPlayerDied` and handles spawning players back at the last checkpoint.
- **`CheckpointTrigger`**: Updates the `RespawnManager`'s active spawn position.
- **`CameraZoneTrigger`**: Dynamically changes camera presets based on player location using `OnCameraPresetChanged`.

---

## 6. Project Organization

- `Assets/_Game/Scripts/Core`: High-level systems (EventBus, Game State, Scene Loading).
- `Assets/_Game/Scripts/Player`: Everything related to player mechanics and state.
- `Assets/_Game/Scripts/Enemies`: AI logic and enemy behaviors.
- `Assets/_Game/Scripts/Network` & `Networking`: NGO wrappers, Sync logic, UGS integration.
- `Assets/_Game/Scripts/UI`: HUD, Menus, Prompt UI.
- `Assets/_Game/Scripts/Environment`: World objects, hazards, checkpoints.
- `Assets/_Game/Scripts/Data`: ScriptableObject configuration classes.

---

*Last Updated: 2026-04-16*
