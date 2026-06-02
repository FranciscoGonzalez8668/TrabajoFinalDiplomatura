# Qerlk Keeper — Trabajo Final de Diplomatura en Desarrollo de Videojuegos

Juego de acción y parkour 3D desarrollado en Unity como trabajo final de diplomatura. El jugador debe escapar de una construcción en colapso usando mecánicas de parkour encadenadas, superando obstáculos y resolviendo puzzles de entorno.

---

## Tecnologías y herramientas

- **Motor:** Unity 2021.3 LTS — C#
- **Control de versiones:** Git
- **Pipeline de renderizado:** Built-in con Post Processing Stack (Bloom)
- **Física del personaje:** CharacterController (sin Rigidbody)
- **Input:** Input Manager clásico de Unity

---

## Sistemas implementados

### Personaje y movimiento
- Sistema de movimiento con aceleración, deceleración y sprint
- Salto con **coyote time** e **input buffering** para respuesta fluida
- **Wall run lateral** — el jugador corre sobre paredes con impulso vertical inicial y gravedad reducida
- **Wall run vertical** — el jugador sube por paredes frontales con curva de velocidad personalizable
- **Wall jump** — salto desde paredes con dirección relativa a la cámara
- **Ledge grab y climb** — agarre y escalada de bordes con soporte para plataformas móviles
- Rotación suave del personaje con Quaternion.Slerp según el estado activo
- Sistema de coyote time en wall run lateral para evitar cancelaciones accidentales

### Arquitectura de habilidades
- Interfaz `IMovementAbility` (CanStart / StartAbility / UpdateAbility / StopAbility / ForceStop)
- `PlayerStateMachine` como coordinador central — gestiona prioridades, estados y transiciones
- Cada habilidad es un componente independiente sin referencias cruzadas entre sí

### Cámara
- Cámara en tercera persona con rotación por mouse
- Exposición de `CameraRight` para cálculo de direcciones relativas en saltos y wall jumps

### Objetos de nivel
- `ObjectMover` — plataformas y obstáculos con cuatro modos: Linear, Pendulum, PendulumBob y Vanishing
- Sistema de parenting dinámico para que el jugador se mueva con plataformas móviles
- `Physics.SyncTransforms()` para evitar desfase entre posición visual y colisión física
- `SpikeMeshGenerator` — generación procedural de meshes de pinchos sobre un área definida

### Sistema de interacción
- Interfaz `IInteractable` con hint text y locked text por objeto
- Interfaz `IActivatable` (Play / Stop / Reset) para cualquier objeto activable
- `ActivatorSwitch` — switch con modos OneShot, Toggle y Restart, soporte de requerimiento de ítem
- `PickableItem` — ítem recogible con estado persistente en `LevelState` (ScriptableObject)
- `PlayerInteractor` — detección por `Physics.OverlapSphere`, muestra hints en UI en tiempo real

### Progreso y transición de niveles
- `LevelManager` — singleton DontDestroyOnLoad, gestiona transición entre pisos
- `GameProgress` — ScriptableObject que registra pisos completados e índice actual
- `ExitZone` — trigger que avanza al siguiente nivel al pisarlo
- `PlayerRespawn` — maneja muerte con delay, teleport directo y cancelación de estado activo
- `KillZone` — trigger de muerte reutilizable con opción de destruirse tras activarse

### UI
- `UIManager` — singleton con panel de hint de interacción y panel de notificaciones temporales
- Hints contextuales ("[ E ] Recoger Llave") que aparecen al entrar en rango de un objeto
- Notificaciones que se auto-ocultan con duración configurable

### Estética visual
- Pipeline neon sobre fondo oscuro con Bloom (Post Processing Stack)
- `NeonEdges` — componente que genera 12 tiras neon proceduralmente sobre las aristas de cualquier cubo
- `NeonConfig` — ScriptableObject global para controlar grosor y material de todos los bordes desde un único lugar, con posibilidad de override por objeto
- Paleta de tres colores: cyan (estructuras), violeta (peligros), blanco (objetivos)

### Datos de configuración
- `PlayerData` — ScriptableObject con todos los parámetros de gameplay (velocidades, alturas, tiempos, umbrales de detección). Sin valores mágicos en el código.

---

## Decisiones técnicas destacadas

- **CharacterController sobre Rigidbody** — mayor control sobre el movimiento, sin dependencia del motor de física para el personaje
- **Separación estricta de responsabilidades** — InputHandler solo lee, CharacterMotor solo mueve, las abilities solo deciden
- **Estado persistente en ScriptableObjects** — `LevelState` y `GameProgress` sobreviven recargas de escena sin sistemas de guardado externos
- **Detección de pared con abanico de raycasts** — tres rayos con apertura de ±15° para evitar falsos negativos por desalineación leve
- **Parenting dinámico con grace timer** — el jugador se convierte en hijo de plataformas móviles; un timer evita que flickering de `isGrounded` rompa el parenting
- **NeonEdges procedural** — generación en runtime de geometría de bordes que se adapta automáticamente a cambios de escala

---

## Estructura del proyecto

```
Assets/
├── Scripts/
│   ├── Core/          → sistemas genéricos reutilizables (interfaces, motor, datos)
│   ├── Character/     → player, respawn, interactor
│   ├── Parkour/       → abilities de parkour
│   ├── Objects/       → objetos de nivel (plataformas, switches, items)
│   ├── UI/            → UIManager
│   └── Camera/        → CameraController
├── Content/           → ScriptableObjects de configuración
└── Scenes/            → escenas de nivel
```
