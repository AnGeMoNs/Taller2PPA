# Pruebas manuales pendientes (no ejecutadas en este entorno)

| Caso | Acción | Resultado esperado |
|---|---|---|
| 01 | Generar escena y ejecutar Comprobar referencias V4 | Se guardan escena y controladores; sin referencias vacías |
| 02 | Nueva partida `PruebaA` | Aparece jugador con 100 de vida y 0/4 objetos |
| 03 | Intentar entrar en cámara final sin recompensas | La puerta impide pasar |
| 04 | Derrotar esqueleto y slime | Recompensa roja aparece solo cuando ambos están muertos |
| 05 | E junto a llave roja | HUD 1/4; llave desaparece y se autoguarda |
| 06 | Empujar cajas a dos placas diferentes | Ambas verdes; aparece llave verde |
| 07 | Mover cajas a posiciones arbitrarias, F5, volver al menú y cargar | Posiciones restauradas; RESET devuelve cajas si no se resolvió |
| 08 | Palancas 1, 2 y luego 2, 1, 3 | Primer intento reinicia; segundo resuelve y concede llave amarilla |
| 09 | F5 después de una palanca correcta, salir y cargar | Secuencia parcial restaurada |
| 10 | Cruzar bandas rojas / verdes | Daño solo durante rojo, con cooldown |
| 11 | Activar control 2 primero y luego 1, 2 | Se exige orden; se desactivan bandas; aparece gema |
| 12 | Recoger las cuatro recompensas | HUD 4/4 y puerta final abierta |
| 13 | Activar dos pedestales, guardar y cargar | Dos ranuras verdes, no se vuelve a consumir recompensa |
| 14 | Activar cuatro pedestales y recoger bate | Victoria, sonido y créditos; cargar devuelve pantalla victoria |
| 15 | Crear `PruebaB` y volver a `PruebaA` | Ambos JSON conservan progresos independientes |
| 16 | Crear nombre ya existente | No se sobrescribe |
| 17 | Vida llega a cero, clic en Reintentar | Carga último guardado sin duplicar recompensas |
| 18 | Crear/asignar Behavior Graph y activar usarUnityBehavior | Revisar grafo en ejecución; gestiones de ataque/persecución/patrulla correctas |
| 19 | Build y Play en Windows | No hay errores de compilación; revisar cámaras, modelos, texturas y audio |

Si el editor no muestra el menú Taller 2, revisar Console y resolver primero errores de compilación/importación. No marcar pruebas como aprobadas hasta ejecutarlas.
