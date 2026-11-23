# Plan de implementacion - La Mazmorra de Link

## 1. Jugador, camara y combate
- `Movimiento3D`: lectura de teclado (`WASD`, `Shift`, `Espacio`, `R`) en `Update` y movimiento fisico con `Rigidbody` en `FixedUpdate`.
- `CamaraTerceraPersona`: rotacion por raton, zoom, deteccion de muros con `SphereCastNonAlloc` y seguimiento suave con `SistemaFijadoObjetivo`.
- `VidaJugador` y `CombateJugador`: encapsulamiento de salud, eventos (`VidaCambiada`, `Murio`, `ComboCambiado`), combo x3, ataque giratorio, bloqueo y parry perfecto.

## 2. Enemigos e IA
- Clase base abstracta `Enemy` con vida, dano, velocidad, deteccion por raycast, aturdimiento, barra de vida 3D y muerte con efecto y botin.
- Subclases diferenciadas:
  - `SkeletonEnemy`: patrulla en circuito, detecta con linea de vista, alerta, persigue y ataca cuerpo a cuerpo.
  - `SlimeEnemy`: avance lento y erratico en zig-zag con dano por contacto.
  - `TurtleEnemy`: ruta definida, defensa con caparazon (-50% dano) y embestida breve.
- Nodos personalizados de Unity Behavior (`AtacarEnemigo`, `PerseguirEnemigo`, `PatrullarEnemigo`) conectados a los metodos de `Enemy`.

## 3. Salas, puzzles y camara final
| Sala | Puzzle | Condicion de resolucion | Recompensa |
| --- | --- | --- | --- |
| 01 Combate (Oeste) | Arena de guardianes | Derrotar al esqueleto y al slime | Llave roja |
| 02 Placas (Este) | Cajas y placas | Colocar las 2 cajas sobre las 2 placas a la vez | Llave verde |
| 03 Secuencia (Sur) | Palancas | Accionar palancas en orden II - I - III | Llave amarilla |
| 04 Trampas (Norte) | Corredor de puas | Activar Control 1 y luego Control 2 | Gema |
| 05 Altar (Final) | Pedestales y Reliquia | Colocar los 4 sellos, reclamar la Reliquia Sagrada y vencer la oleada final | Victoria |

## 4. Guardado JSON, UI y sonido
- `DatosPartida` (`[Serializable]`) y `SaveManager` usando `JsonUtility` y `StreamWriter` en `Application.persistentDataPath/Partidas`.
- `HUDManager` y `MinimapaMazmorra` actualizados por eventos sin consultar vida cada frame.
- `AudioManager` con musica en loop por fases (`DekuMenu`, `MusicaMazmorra`, `MusicaJefe`, `MusicaSinEnemigos`) y efectos de sonido descargados en `Resources/Audio/SFX/`.

## 5. Flujo de trabajo en Git
- `feat(player)`: movimiento 3D, animaciones de Link, combate y Z-Targeting
- `feat(enemies)`: clase base Enemy, subclases y controladores Animator
- `feat(puzzles)`: cuatro salas, puerta norte, altar de sellos, Reliquia Sagrada y oleada final
- `feat(world)`: cofres, barriles/anforas rompibles, botin y Santuario de Hadas
- `feat(save)`: guardado/carga JSON independiente por perfil
- `feat(ui)`: menu principal, opciones, minimapa, HUD y pantalla de victoria
- `feat(audio)`: musica por fases con crossfade y SFX descargados
