# CLAUDE.md — FinalProject (Trabajo Final de Diplomatura)

Proyecto Unity 3D de movimiento parkour en tercera persona. Trabajo final de diplomatura.
**Unity 2021.3.45f2 LTS** — C# — CharacterController (no Rigidbody).

---

## Estructura de Scripts

```
Assets/Scripts/
├── Core/
│   ├── InputHandler.cs       — Lee input, expone estado como propiedades readonly
│   ├── PlayerData.cs         — ScriptableObject con todos los parámetros configurables
│   └── CharacterMotor.cs     — Capa física: mueve al personaje vía CharacterController
├── Character/
│   └── PlayerStateMachine.cs — Coordina estados y llama a Motor + Abilities
├── Camera/
│   └── CameraController.cs   — Cámara en tercera persona con colisión y smooth follow
└── Parkour/
    └── WallRunAbility.cs     — Mecánica de wall run y wall jump
```

**Assets de datos:** `Assets/Content/PlayerData.asset`
**Documentación de arquitectura:** `Assets/docs/Architecture/`

---

## Arquitectura del Sistema

```
InputHandler
     │
     ▼
PlayerStateMachine  ──►  WallRunAbility
     │
     ▼
CharacterMotor  (única clase que mueve al CharacterController)
     │
     ▼
CameraController  (reactiva, sin lógica de gameplay)
```

**Regla central:** Solo `CharacterMotor` llama a `CharacterController.Move()`.
Las abilities y el state machine coordinan pero no mueven directamente.

---

## Componentes Clave

### InputHandler (`Core/InputHandler.cs`)
- Propiedades: `MoveInput` (Vector2), `JumpPressed`, `JumpHeld`, `WallRunHeld` (Left Shift)
- Lee Unity Input system cada frame, sin lógica de gameplay
- Slide input deshabilitado (hardcodeado a false)

### PlayerData (`Core/PlayerData.cs`) — ScriptableObject
Parámetros principales:
| Categoría | Valores clave |
|-----------|---------------|
| Movimiento | moveSpeed=7, acceleration=20, deceleration=15 |
| Salto | jumpHeight=5, coyoteTime=0.15s, jumpBufferTime=0.12s |
| Wall Jump | wallJumpSpeed=15, wallJumpForce=15, wallJumpHeight=1.5 |
| Gravedad | gravityScale=2.5, fallMultiplier=2, maxFallSpeed=20 |
| Wall Run | wallRunSpeed=8, wallRunDuration=3s, wallRunGravity=0.1 |

### CharacterMotor (`Core/CharacterMotor.cs`)
- Detección de pared: 5 raycasts (izquierda, derecha, diag-izq, diag-der, frente)
- **Coyote time**: permite saltar brevemente después de caer de un borde
- **Jump buffer**: encola el salto si se presiona justo antes de aterrizar
- `OverrideGravity`: flag para que las abilities deshabiliten la gravedad automática
- Propiedades públicas: `IsGrounded`, `IsTouchingWall`, `WallNormal`, `Velocity`, `CoyoteAvailable`

### PlayerStateMachine (`Character/PlayerStateMachine.cs`)
Estados: `Idle → Running → Jumping → Falling → WallRunning`
- Movimiento relativo a la cámara (`CameraController.CameraForward/Right`)
- En estado Jumping/Falling (airborne): 80% de velocidad, permite decelerar
- Activa `WallRunAbility` si las condiciones se cumplen

### WallRunAbility (`Parkour/WallRunAbility.cs`)
Activación requiere: Left Shift + tocar pared lateral + `dot(wallNormal, up) > 0.5` + timer disponible
- Dirección de carrera: `Cross(wallNormal, up)` — paralela a la pared
- Wall jump: combina normal de la pared + preservación de inercia
- `timerExpired` bloquea re-entrada inmediata tras wall jump

### CameraController (`Camera/CameraController.cs`)
- Mouse look con pitch clampeado (-20° a 60°)
- SphereCast para evitar clipping contra paredes
- `CameraForward` / `CameraRight`: vectores horizontales para movimiento relativo

---

## Documentación de Arquitectura Planificada

En `Assets/docs/Architecture/` hay docs de sistemas **aún no implementados** pero planificados:
- **CharacterBrain** — Orquestador de input (reemplazaría parte de PlayerStateMachine)
- **CharacterSensor** — Abstracción de detección (GroundSensor, WallSensor, LedgeSensor)
- **CharacterContext** — Objeto central que pasa referencias entre sistemas
- **CharacterAnimatorBridge** — Actualiza parámetros de Animator
- **Otras abilities** — Dash, Slide, Vault (documentadas, sin implementar)

---

## Convenciones del Proyecto

- Idioma del código: **inglés** (variables, métodos, comentarios en scripts)
- Idioma de comunicación con el usuario: **español**
- No usar Rigidbody — solo `CharacterController`
- Los sistemas visuales (cámara, animación futura) no contienen lógica de gameplay
- `PlayerData` es el único lugar donde se cambian valores numéricos de gameplay

---

## Estado de Implementación

| Sistema | Estado |
|---------|--------|
| InputHandler | Completo |
| PlayerData (ScriptableObject) | Completo |
| CharacterMotor | Completo |
| PlayerStateMachine | Completo |
| CameraController | Completo |
| WallRunAbility | Completo |
| CharacterBrain | Solo documentado |
| CharacterSensor (abstracción) | Solo documentado |
| CharacterContext | Solo documentado |
| Dash / Slide / Vault | Solo documentados |
