# Unity Behavior: integración manual y prueba

Paquete com.unity.behavior 1.0.16 (versión publicada para Unity 6.3 según documentación oficial). Referencias:
- https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-behavior
- https://docs.unity.cn/Packages/com.unity.behavior@1.0/manual/create-custom-node.html
- https://docs.unity.cn/Packages/com.unity.behavior@1.0/manual/create-behavior-graph.html

La base incluye tres acciones reales que heredan de Unity.Behavior.Action. Cada una consulta una clase Enemy, y usa OnStart/OnUpdate, BlackboardVariable<GameObject> y Node Status. NO se incluye un asset Behavior Graph serializado: créalo en el editor para asegurar compatibilidad.

## Crear el grafo
1. En Assets/Juego/Behavior (crear carpeta si no existe), clic derecho > Create > Behavior > Behavior Graph. Nombre BG_Enemigo.
2. Abrir el grafo. Crear variable GameObject Agent en el Blackboard (se asignará al objeto que tiene Enemy). Si el grafo incluye Self por defecto, puedes enlazar Self en los tres nodos en vez de crear Agent.
3. En On Start, activar Repeat. Agregar un nodo de selección Try In Order (selector) y conectar como hijo.
4. Añadir tres acciones en Action > Sahur, conectarlas al selector en este orden: Atacar jugador, Perseguir jugador, Patrullar o esperar. En los tres nodos, enlazar Agent con la variable Agent (o Self).
5. Agregar componente Behavior Graph Agent en los GameObjects Esqueleto_muestra, Slime_muestra, Tortuga_muestra (los padres que contienen Enemy). Asignar BG_Enemigo. En Blackboard overrides, enlazar Agent al propio GameObject de cada enemigo si no usaste Self. Verificar en el editor que enlaza al GameObject, no al hijo del modelo.
6. Marcar Use/Usar Unity Behavior en el componente del enemigo (campo usarUnityBehavior). Guardar. En Play, seleccionar el agente y ver el grafo en ejecución.

```
On Start (Repeat)
  └── Try In Order
        ├── Atacar jugador [Agent]
        ├── Perseguir jugador [Agent]
        └── Patrullar o esperar [Agent]
```
El nodo Atacar devuelve Failure si el enemigo no puede atacar. El selector intenta Perseguir; si tampoco es posible, intenta Patrullar. Se reevalúa en el siguiente ciclo. Para la tortuga atacar y perseguir siempre devuelven Failure; para el slime sólo se usa perseguir/esperar, y el daño proviene del contacto. El esqueleto usa rango, raycast, ataque con cooldown y patrullaje. El movimiento se aplica mediante Rigidbody en FixedUpdate. La animación se controla desde Enemy.Animar. Los nodos no reemplazan el método RecibirDano común.

## Validación
- Esqueleto: acercarse, ocultarse detrás de muro, verificar disparos de audio y animación Walk/Attack. Si el rig de las animaciones no coincide con el modelo, revisar Import Settings > Rig y Avatar del FBX antes de seguir.
- Slime: aproximación lenta y daño al tocarlo; enemigo inmóvil al perder al jugador.
- Tortuga: ruta local, sin perseguir, daño por contacto.
- Cada enemigo recibe daño, deja de colisionar al morir, reproduce Death y desaparece. Ajustar el tiempo de destrucción a la duración real del clip si es necesario.
- Si Unity Behavior no funciona, desmarcar usarUnityBehavior temporalmente para depurar los métodos sin grafo. No entregar con esa opción desmarcada si quieres demostrar el uso del paquete.

## V4.2
Los nodos evalúan en OnStart y notifican actividad. Usa Self, desactiva Allow Multiple Repeats per Tick y guarda el grafo. Selecciónalo en Project y ejecuta Taller 2 > 7. Asignar Behavior Graph seleccionado para configurar todos los enemigos. Si no hay decisiones recientes del grafo, se activa el respaldo C#. Ver Reparacion_V4_2.md.
