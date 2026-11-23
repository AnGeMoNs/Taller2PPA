# Registro de cambios - La Mazmorra de Link

## Version final (Link, Reliquia Sagrada y mejoras de combate)

### Personaje y combate
- Protagonista actualizado a Link (modelo Humanoid con animaciones de Mixamo: `Idle`, `Walk`, `Run`, `Block`, `Attack1`, `Attack2`, `Attack3`, `Hit`, `PowerUp`, `Death`).
- Espada y escudo acoplados a los huesos de la mano derecha y antebrazo izquierdo, con estela luminosa (`TrailRenderer`) y aura reactiva en la hoja.
- Sistema de estamina, esquiva rapida (`Espacio`) con invulnerabilidad temporal, bloqueo con escudo (`Clic Der.`) y Parry Perfecto que aturde al atacante y potencia el siguiente golpe.
- Ataque giratorio 360 grados (`R` o mantener `Clic Izq.`), racha de combo y sistema de fijado de objetivo Z-Targeting (`Tab` / `V`).

### Camara del Altar y oleada final
- Al juntar los 4 sellos (`LlaveRoja`, `LlaveVerde`, `LlaveAmarilla`, `Gema`) se abre la reja norte (`z = 18`).
- En el altar se colocan los 4 sellos en sus pedestales para revelar la **Reliquia Sagrada**.
- Al reclamar la Reliquia Sagrada, la vida maxima sube de 100 a 300, se cura por completo, el dano de la espada se duplica (`x2`) y se invoca la oleada final de 5 guardianes (2 esqueletos liderados por un Capitan Esqueleto elite, 2 slimes y 1 tortuga).
- La victoria se obtiene al derrotar a los 5 enemigos de la oleada final, mostrando resumen de estadisticas, rango final y creditos.

### Exploracion, interfaz y audio
- Cofres del tesoro interactuables en las salas, barriles y anforas rompibles con botin (corazones, rupias y pociones) y Santuario de Hadas en el vestibulo central.
- Minimapa 2D en tiempo real (`M`), menu de opciones de audio y sensibilidad, y borrado de perfiles guardados desde el menu.
- Musica dinamica por fases con crossfade suave y 25 efectos de sonido descargados (todos menores a 5 segundos).
