# La Mazmorra de Link: La Reliquia Sagrada

Dungeon Crawler en 3D desarrollado en Unity 6 (`6000.3.26f1`) con C# para el Taller 2 de Proyecto de Programacion Avanzada (Universidad Catolica del Norte).

## Escena principal
- `Assets/Juego/Scenes/MazmorraV4.unity`

## Mapa de la mazmorra
```
               [05 Camara del altar]  (0, 24)
                         |
               [04 Trampas]           (0, 12)
                         |
[01 Combate] --- [00 Entrada] --- [02 Placas]
  (-12, 0)         (0, 0)          (12, 0)
                         |
               [03 Secuencia]         (0, -12)
```

## Controles
- `WASD`: Movimiento en 3D relativo a la camara
- `Shift`: Correr (consume estamina)
- `Espacio`: Esquiva rapida con invulnerabilidad
- `Raton` / `Rueda`: Rotar camara y zoom
- `Clic izquierdo`: Combo de espada (3 golpes)
- `R` o mantener `Clic izquierdo`: Ataque giratorio 360 grados
- `Clic derecho`: Bloquear con escudo (-75% dano) y Parry Perfecto justo antes de recibir un golpe
- `Tab` / `V`: Fijar objetivo (Z-Targeting) y cambiar enemigo
- `E`: Interactuar con puzzles, recompensas, cofres y Santuario de Hadas
- `Q`: Beber pocion de curacion
- `M`: Mostrar u ocultar minimapa
- `F5`: Guardar partida
- `Esc`: Menu de pausa

## Flujo de salas y puzzles
1. **Oeste (Arena)**: Derrota al esqueleto y al slime para habilitar y recoger la **Llave Roja**.
2. **Este (Placas)**: Empuja las dos cajas sobre las placas de presion al mismo tiempo (usa `RESET` si se atascan) y recoge la **Llave Verde**.
3. **Sur (Secuencia)**: Acciona las palancas en el orden del mural (`II - I - III`) y recoge la **Llave Amarilla**.
4. **Norte (Trampas)**: Cruza las bandas de puas cuando esten en verde, evita a la tortuga y activa Control 1 y Control 2 en orden para desactivarlas y recoger la **Gema**.
5. **Camara del Altar**: Al reunir los 4 sellos se abre la reja norte (`z = 18`). Coloca los 4 sellos en los pedestales, reclama la **Reliquia Sagrada** (vida maxima 300 y dano x2) y derrota la oleada final para ganar.

## Guardado de partidas
- Archivos JSON independientes por perfil en `Application.persistentDataPath/Partidas`.
- Guarda posicion, vida, salas resueltas, sellos, estado de mecanismos, cajas, enemigos derrotados, cofres abiertos, pociones, rupias y estadisticas.
- Permite crear perfiles nuevos (validando nombres duplicados), continuar desde lista paginada o eliminar perfiles.
