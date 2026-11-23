# Estado del proyecto - La Mazmorra de Link

## Implementado y compilado en Unity 6 (6000.3.26f1)
- Protagonista: Link con rig Humanoid, espada y escudo 3D, combo de 3 golpes, ataque giratorio, escudo con parry perfecto, esquiva con estamina y Z-Targeting.
- Camara en tercera persona controlada por raton, zoom con rueda, colision suavizada contra muros y sacudida en impactos.
- Enemigos con herencia desde `Enemy`: `SkeletonEnemy`, `SlimeEnemy` y `TurtleEnemy`, cada uno con Animator, sonidos propios, barra de vida 3D, aturdimiento y botin al morir. Compatibles con Unity Behavior y respaldo en C#.
- Mazmorra de 6 salas con iluminacion de antorchas, 4 puzzles distintos (Combate, Placas/Cajas, Palancas en secuencia y Trampas de puas), 4 recompensas, reja norte bloqueada, altar de 4 pedestales, Reliquia Sagrada y oleada final.
- Contenido adicional: minimapa (`M`), cofres del tesoro, barriles y anforas rompibles, pociones (`Q`), rupias y Santuario de Hadas.
- Sistema de guardado por perfil en archivos JSON independientes (`Application.persistentDataPath/Partidas`) con creacion, carga, validacion de nombres repetidos y eliminacion.
- Interfaz completa con menu principal, controles, opciones de audio/sensibilidad, creditos, HUD en tiempo real, pausa, derrota (`¡Fin del juego!`) y pantalla de victoria con estadisticas y creditos.
- Musica en loop por fases y 25 efectos de sonido descargados (<= 4 segundos).

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
Cada sala mide 12 x 12 unidades. El acceso a la Camara del Altar permanece bloqueado hasta reunir las cuatro recompensas.
