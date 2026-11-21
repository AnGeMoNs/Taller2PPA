# Estado V4

**Implementado en código, pendiente de ejecución real en Unity:**
- Importación de Sahur y bate, Acknowledging como Idle, Walk, Run y MMA Kick; cámara y Rigidbody.
- Vida y combate por eventos, tres tipos de enemigos diferenciados por herencia, acciones Unity Behavior y modo C# por defecto.
- Generación de seis salas con módulos, antorchas, rótulos, dos guardianes, tortuga, placas y cajas, tres palancas, dos controles, tres bandas de daño, cuatro recompensas, puerta y altar de cuatro pedestales.
- Registro por sala y objeto, guardado/carga por perfil con restauración de mecanismos y victoria.
- Canvas en ejecución con menú, HUD, controles, pausa, derrota y victoria, avisos de guardado y objetivos contextuales.

**Pendiente:** abrir/importar/generar escena, comprobar compilación y animaciones, crear/asignar grafo Unity Behavior (si se desea usar el paquete visual), reemplazar audios provisionales por recursos descargados con licencia, completar créditos, pruebas de Build y documentación académica. El enunciado limita uso de IA; revisar con el docente.

## Mapa
```
               [05 Cámara del bate]  (0,24)
                        |
               [04 Trampas]          (0,12)
                        |
[01 Combate] --- [00 Entrada] --- [02 Placas]
 (-12,0)            (0,0)           (12,0)
                        |
               [03 Secuencia]       (0,-12)
```
Cada sala mide 12 x 12 unidades. El acceso final requiere las cuatro recompensas.
