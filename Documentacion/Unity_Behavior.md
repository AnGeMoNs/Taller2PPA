# Unity Behavior en enemigos

El proyecto incluye el paquete `com.unity.behavior` (`1.0.16`) y tres nodos personalizados que heredan de `Unity.Behavior.Action`:
- `AtacarEnemigo` (`Action/Mazmorra`)
- `PerseguirEnemigo` (`Action/Mazmorra`)
- `PatrullarEnemigo` (`Action/Mazmorra`)

Cada nodo recibe `BlackboardVariable<GameObject> Agent`, consulta `Enemy.PuedeActuar`, llama a `NotificarNodo()` y ejecuta el metodo polimorfico correspondiente (`IntentarAtacar()`, `IntentarPerseguir()` o `Patrullar()`). Si no hay un grafo asignado o activo, `Enemy.Update()` mantiene la logica C# de respaldo para que los enemigos funcionen siempre.

## Como crear y asignar el grafo en Unity Editor
1. En `Assets/Juego/Behavior`, clic derecho > `Create > Behavior > Behavior Graph` y nombrarlo `BG_Enemigo`.
2. En el nodo `On Start`, activar `Repeat`.
3. Conectar un selector `Try In Order` con las tres acciones de `Action > Mazmorra` en este orden:
   - `Atacar jugador`
   - `Perseguir jugador`
   - `Patrullar o esperar`
4. Enlazar el campo `Agent` de los tres nodos con la variable `Self` (o `Agent` del Blackboard).
5. Guardar `BG_Enemigo`, seleccionarlo en la ventana Project y ejecutar `Taller 2 > 7. Asignar Behavior Graph seleccionado`.
6. Guardar la escena y probar en Play Mode.

```
On Start (Repeat)
  └── Try In Order
        ├── Atacar jugador [Agent]
        ├── Perseguir jugador [Agent]
        └── Patrullar o esperar [Agent]
```

## Comportamiento por subclase
- `SkeletonEnemy`: ataca si esta en rango y con linea de vista; si no, persigue recordando brevemente la ultima posicion; en caso contrario patrulla.
- `SlimeEnemy`: no usa ataque a distancia (`IntentarAtacar` devuelve `false`), persigue en zig-zag si ve al jugador y dana al contacto.
- `TurtleEnemy`: recorre su ruta predefinida, se protege en su caparazon al recibir dano y realiza una breve embestida al salir de defensa.
- Las animaciones se envian mediante hashes de parametros (`Caminando`, `Defendiendo`, `Atacar`, `Golpe`, `Morir`) desde `Enemy`.
