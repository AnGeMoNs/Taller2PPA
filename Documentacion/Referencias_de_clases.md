# Relacion con los contenidos de PPA

- **Ayudantia 01**: configuracion del proyecto en Unity 6, estructura de carpetas y scripts C#.
- **Ayudantia 02**: desarrollo incremental, ramas en GitHub y convencion de commits (`feat`, `fix`, `refactor`, `docs`).
- **MovimientoSimple.cs**: separacion entre `Start` (referencias), `Update` (lectura de entradas y Animator) y `FixedUpdate` (fisicas con `Rigidbody`), adaptada al entorno 3D en `Movimiento3D.cs`.
- **Clase 5 (UML)**: correspondencia entre el modelo de dominio, el diagrama de clases y los scripts reales (encapsulamiento, asociaciones y herencia de `Enemy`).
- **Clase 9 (Animaciones)**: configuracion de rig Humanoid para Link (`Link.controller`) y controladores con parametros para los tres enemigos (`Esqueleto.controller`, `Slime.controller`, `Tortuga.controller`).
- **Clase 10 (Fisicas y colisiones)**: `Rigidbody`, `CapsuleCollider`, `BoxCollider`, triggers de trampas y consultas fisicas `NonAlloc` (`OverlapSphereNonAlloc`, `RaycastNonAlloc`, `SphereCastNonAlloc`, `SweepTest`).
- **Clase 11 (Interfaz grafica)**: actualizacion del HUD (`HUDManager` y `MinimapaMazmorra`) mediante eventos de C# en vez de recalcular textos de vida cada frame en `Update`.
- **Clase 12 (Sonidos y musica)**: organizacion con regiones (`#region CLASS_VARIABLES`, `#region UNITY_METHODS`, `#region CLASS_METHODS`), uso de `AudioSource.Play()` para musica en loop y `PlayOneShot()` para efectos de sonido en `AudioManager`.
- **Clase 13 (Rendimiento)**: uso de colliders simples en muros/pisos/pilares, `MaterialPropertyBlock` para cambios de color y buffers reutilizados para evitar asignaciones de memoria.
- **Clase 14 (Guardado de datos)**: clase `[Serializable] DatosPartida`, serializacion con `JsonUtility.ToJson` / `FromJson` y escritura con `StreamWriter` en `SaveManager` (un archivo JSON por perfil en `Application.persistentDataPath/Partidas`).
- **Clase 15 (Patrones de diseno)**: Managers (`GameManager`, `HUDManager`, `AudioManager`, `SaveManager`), Singleton en `GameManager` y `AudioManager`, Observer mediante eventos (`VidaCambiada`, `Cambio`, `Murio`, `ComboCambiado`), Object Pool de fuentes 3D en `AudioManager` e interfaz `IInteractuable`.
