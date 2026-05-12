# CLAUDE.md - FinalProject (Trabajo Final de Diplomatura)

Proyecto Unity 3D de movimiento parkour en tercera persona. Trabajo final de diplomatura.
**Unity 2021.3.45f2 LTS** - **C#** - **CharacterController** (no Rigidbody).

---

## Estructura de Scripts

```text
Assets/Scripts/
|-- Core/
|   |-- InputHandler.cs       - Lee input y expone estado como propiedades readonly
|   |-- PlayerData.cs         - ScriptableObject con todos los parametros configurables
|   `-- CharacterMotor.cs     - Capa fisica: mueve al personaje via CharacterController
|-- Character/
|   `-- PlayerStateMachine.cs - Coordina estados y llama a Motor + Abilities
|-- Camera/
|   `-- CameraController.cs   - Camara en tercera persona con colision y smooth follow
`-- Parkour/
    |-- WallRunAbility.cs     - Mecanica de wall run y wall jump
    `-- LedgeGrabAbility.cs   - Grab, hang, shimmy y climb en bordes
```

**Assets de datos:** `Assets/Content/PlayerData.asset`  
**Documentacion de arquitectura:** `Assets/docs/Architecture/`

---

## Arquitectura del Sistema

```text
InputHandler
     |
     v
PlayerStateMachine ---> WallRunAbility
     |                 `-> LedgeGrabAbility
     v
CharacterMotor  (unica clase que mueve al CharacterController)
     |
     v
CameraController  (reactiva, sin logica de gameplay)
```

**Regla central:** solo `CharacterMotor` llama a `CharacterController.Move()`.  
Las abilities y el state machine coordinan, pero no mueven directamente al personaje salvo helpers controlados por el motor como `MoveRaw()` o `ClimbVertical()`.

---

## Componentes Clave

### InputHandler (`Core/InputHandler.cs`)
- Propiedades: `MoveInput` (`Vector2`), `JumpPressed`, `JumpHeld`, `SprintHeld`, `ClimbPressed`
- Lee el Input Manager de Unity cada frame, sin logica de gameplay
- `SlidePressed` esta deshabilitado por ahora

### PlayerData (`Core/PlayerData.cs`) - ScriptableObject
Parametros principales:

| Categoria | Valores clave |
|-----------|---------------|
| Movimiento | `moveSpeed=7`, `acceleration=20`, `deceleration=15` |
| Sprint | `sprintSpeed=10`, `sprintAcceleration=8` |
| Salto | `jumpHeight=5`, `coyoteTime=0.15s`, `jumpBufferTime=0.12s` |
| Wall Jump | `wallJumpSpeed=7`, `wallJumpForce=3`, `wallJumpHeight=1.5` |
| Gravedad | `gravityScale=2.5`, `fallMultiplier=1.5`, `maxFallSpeed=20` |
| Wall Run | `wallRunSpeed=8`, `horizontalWallRunDuration=1.5s`, `wallRunGravity=0.3` |
| Ledge Grab | `ledgeDetectionDistance=0.8`, `ledgeGrabReach=1.5`, `ledgeHangTimeout=3s` |

### CharacterMotor (`Core/CharacterMotor.cs`)
- Deteccion de pared con 5 raycasts: izquierda, derecha, diagonal izquierda, diagonal derecha y frente
- Implementa **coyote time** y **jump buffer**
- `OverrideGravity` permite a las abilities deshabilitar la gravedad automatica
- Expone `IsGrounded`, `IsTouchingWall`, `WallNormal`, `Velocity`, `VerticalVelocity`, `CoyoteAvailable`
- Centraliza el movimiento real sobre `CharacterController`

### PlayerStateMachine (`Character/PlayerStateMachine.cs`)
Estados actuales: `Idle -> Running -> Sprinting -> Jumping -> Falling -> WallRunning -> LedgeGrabbing`
- Movimiento relativo a la camara (`CameraController.CameraForward/Right`)
- En el aire (`Jumping` / `Falling`): 80% de velocidad y desaceleracion permitida
- Coordina `WallRunAbility` y `LedgeGrabAbility`

### WallRunAbility (`Parkour/WallRunAbility.cs`)
- Activacion actual: `SprintHeld` + pared lateral valida + timer disponible
- Direccion de carrera: `Cross(wallNormal, up)` para obtener un vector paralelo a la pared
- Wall jump: combina la normal de la pared con la inercia horizontal
- `timerExpired` bloquea reentrada inmediata tras wall jump

### LedgeGrabAbility (`Parkour/LedgeGrabAbility.cs`)
- Solo se activa durante `Falling`
- Detecta pared frontal y superficie superior alcanzable con raycast + spherecast
- Permite hang, movimiento lateral sobre el borde, climb y ledge jump
- Durante el grab, desactiva la gravedad automatica del motor

### CameraController (`Camera/CameraController.cs`)
- Mouse look con pitch clampeado
- SphereCast para evitar clipping contra paredes
- Expone `CameraForward` y `CameraRight` horizontales para movimiento relativo

---

## Documentacion de Arquitectura Planificada

En `Assets/docs/Architecture/` hay documentos de sistemas planeados o parciales:
- **CharacterBrain** - Orquestador de input y coordinacion de alto nivel
- **CharacterSensor** - Abstraccion de deteccion (`GroundSensor`, `WallSensor`, `LedgeSensor`)
- **CharacterContext** - Objeto central para compartir referencias entre sistemas
- **CharacterAnimatorBridge** - Actualizacion de parametros del Animator
- **Otras abilities** - Dash, Slide, Vault

---

## Convenciones del Proyecto

- Idioma del codigo: **ingles**
- Idioma de comunicacion con el usuario: **espanol**
- No usar `Rigidbody`; solo `CharacterController`
- Los sistemas visuales (camara, animacion) no toman decisiones de gameplay
- `PlayerData` es el unico lugar donde se cambian valores numericos de gameplay

---

## Estado de Implementacion

| Sistema | Estado |
|---------|--------|
| InputHandler | Completo |
| PlayerData (ScriptableObject) | Completo |
| CharacterMotor | Completo |
| PlayerStateMachine | Completo |
| CameraController | Completo |
| WallRunAbility | Completo |
| LedgeGrabAbility | En progreso / funcional |
| CharacterBrain | Solo documentado |
| CharacterSensor (abstraccion) | Solo documentado |
| CharacterContext | Solo documentado |
| Dash / Slide / Vault | Solo documentados |
