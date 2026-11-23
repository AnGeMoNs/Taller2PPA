# Lista de pruebas - La Mazmorra de Link

| Caso | Accion | Resultado esperado |
|---|---|---|
| 01 | Abrir `MazmorraV4.unity` y ejecutar `Taller 2 > 5. Comprobar referencias` | Sin errores ni referencias vacias |
| 02 | Crear nueva partida `PruebaA` | Link inicia en el vestibulo con 100/100 HP, 1 pocion y 0/4 sellos |
| 03 | Intentar cruzar la reja norte sin los 4 sellos | La puerta permanece cerrada y bloquea el paso |
| 04 | Probar movimiento, esquiva (`Espacio`), combo x3 (`Clic Izq.`), giro (`R`), escudo (`Clic Der.`) y `Tab` | Animaciones, estamina, Z-Targeting y sonidos responden bien |
| 05 | Sala Oeste: derrotar al esqueleto y al slime | Se encienden los orbes y aparece la llave roja |
| 06 | Presionar `E` en la llave roja | HUD muestra 1/4, desaparece la llave y se autoguarda |
| 07 | Sala Este: empujar las 2 cajas sobre ambas placas | Placas en verde, aparece llave verde; `RESET` devuelve cajas si aun no se resuelve |
| 08 | Mover cajas, guardar (`F5`), volver al menu y cargar `PruebaA` | Posiciones de cajas y estado restaurados correctamente |
| 09 | Sala Sur: probar orden incorrecto y luego `II - I - III` | Primer intento reinicia con destello rojo; segundo entrega llave amarilla |
| 10 | Sala Norte: cruzar bandas de puas en verde y activar Control 1 y luego Control 2 | En rojo aplican dano (esquivables); al activar ambos controles se apagan y aparece la gema |
| 11 | Romper barriles/anforas, abrir cofres (`E`) y visitar el Santuario de Hadas | Sueltan botin, cofres entregan rupias/pociones y se guardan como abiertos |
| 12 | Reunir los 4 sellos | HUD muestra 4/4 y la reja norte se eleva con sonido |
| 13 | Camara del Altar: colocar los 4 sellos en los pedestales | Conductos dorados se iluminan y aparece la Reliquia Sagrada en el centro |
| 14 | Reclamar la Reliquia Sagrada (`E`) | Vida sube a 300/300, dano x2, espada con aura dorada y aparece la oleada final de 5 enemigos |
| 15 | Derrotar los 5 enemigos de la oleada final | Suena fanfarria de victoria, aparece pantalla de victoria con estadisticas, rango y creditos |
| 16 | Crear segundo perfil `PruebaB` y alternar con `PruebaA` | Cada perfil conserva su propio archivo JSON sin mezclar progreso |
| 17 | Intentar crear un perfil con nombre ya existente | Muestra aviso y no sobrescribe la partida existente |
| 18 | Dejar que la vida llegue a 0 y hacer clic en pantalla | Suena muerte, aparece `¡Fin del juego!` y reinicia desde el ultimo guardado |
| 19 | Probar menu de Opciones (volumen musica/efectos, sensibilidad y minimapa `M`) | Los cambios se aplican de inmediato y se mantienen al reiniciar |
