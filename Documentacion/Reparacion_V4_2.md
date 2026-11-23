# Herramientas de reparacion y ajuste de escena

Desde el menu superior **Taller 2** en Unity Editor se pueden validar o reparar los elementos de `Assets/Juego/Scenes/MazmorraV4.unity`:

- `Taller 2 > 1. Preparar materiales`: ajusta materiales de la mazmorra, enemigos y Link al pipeline grafico activo.
- `Taller 2 > 2. Crear mazmorra completa (Link)`: reconstruye la escena completa desde cero si hiciera falta.
- `Taller 2 > 3. Configurar personaje, equipo y animaciones (Link)`: reconfigura `Link.controller`, Foot IK, espada y escudo mediante `PrepararAnimacionesLink` y `GeneradorEquipoLink`.
- `Taller 2 > 4. Instalar sistemas en escena abierta`: instala los managers y controladores de animacion de enemigos (`Esqueleto`, `Slime`, `Tortuga`).
- `Taller 2 > 5. Comprobar referencias`: verifica que `MazmorraPuzzles` y `GameManager` tengan todas sus referencias completas.
- `Taller 2 > 6. Reparar fisicas y enemigos`: crea un respaldo en `MazmorraV4_Backup*.unity`, revisa colliders de pisos/muros/pilares/cajas, alinea a Link (`Player_Link`) y verifica los 6 enemigos base de la escena.
- `Taller 2 > 7. Asignar Behavior Graph seleccionado`: enlaza un grafo de Unity Behavior seleccionado en Project a todos los enemigos de la escena.

## Resumen de configuracion fisica
- Pisos, muros, pilares, reja final y cajas empujables usan `BoxCollider` en el objeto raiz.
- `Player_Link` usa `CapsuleCollider` (1.85 m de altura, radio 0.38 m) y `Rigidbody` dinamico con rotacion congelada e interpolacion.
- Los enemigos usan `CapsuleCollider` y `Rigidbody` cinematico con `SweepTest` para evitar atravesar muros.
- Escena base con 6 enemigos: `Esqueleto_muestra`, `Slime_muestra`, `Tortuga_muestra`, `Esqueleto_trampas`, `Esqueleto_guardian_final` y `Slime_guardian_final`, mas los 5 enemigos invocados por `OleadaFinal` al reclamar la Reliquia Sagrada.
