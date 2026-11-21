> Documento de planificación original V1/V2; el estado vigente está en Estado_actual.md.

# Implementación incremental (pendiente)

Estos son pasos de trabajo, no código terminado ni resultados de pruebas.

## 1. Jugador y animaciones

- Abrir escena base; comparar Movimiento3D con MovimientoSimple de la ayudantía. Update lee entradas, FixedUpdate cambia la velocidad física sobre X/Z, conserva Y; LateUpdate coloca la cámara.
- Configurar Animator con Idle/Walk/Attack. Clic izquierdo: comprobar enfriamiento, iniciar animación y aplicar daño una vez por golpe (por evento de animación o temporizador). Antes de hallar el bate, usar puños para no bloquear la sala de combate.
- Crear PlayerHealth con maxHealth y currentHealth privados, método TakeDamage, señal de actualización al HUD y muerte una sola vez. Al morir mostrar «¡Fin del juego!», sonido, liberar cursor y permitir reiniciar con clic.
- Pruebas: moverse diagonalmente sin ganar velocidad; paredes bloquean; una pulsación no hace daño a distancia; vida nunca negativa.

## 2. Enemigos

- Clase abstracta Enemy con vida, daño y velocidad encapsulados; TakeDamage y Die comunes; método de movimiento virtual/abstracto.
- SkeletonEnemy: patrulla puntos, detecta con distancia + Raycast, persigue dentro de visión y ataca en alcance; conectar las tres animaciones originales y sonidos de detección/ataque.
- SlimeEnemy: acercamiento lento o errático y daño por contacto con intervalo para no restar vida cada frame.
- TurtleEnemy: recorre lista de puntos en ciclo; daño por contacto. No debe perseguir al jugador como el slime.
- Todos: collider, daño recibido y sonido, muerte con efecto/animación, desactivar colisiones y desaparecer. Agregar los scripts a wrappers/prefabs propios y revisar los colliders actualmente desactivados de los modelos de muestra.
- Pruebas: verificar cada comportamiento por separado; al morir un enemigo no puede seguir atacando.

## 3. Cuatro retos y cámara final

| Sala | Reto | Criterio de éxito | Recompensa sugerida |
| --- | --- | --- | --- |
| 01 Combate | vencer esqueleto y slime | contador de enemigos vivos cero, sin contar dos veces | llave roja |
| 02 Placas | colocar dos cajas en dos placas | ambas activadas simultáneamente | llave verde |
| 03 Secuencia | palancas y pista | comparar cada pulsación con arreglo; reset al fallar | llave amarilla |
| 04 Trampas | cruzar circuito con tortuga y activar interruptor | interacción válida al final del circuito | sello o gema |
| 05 Bate | altar de cuatro pedestales | usar los cuatro objetos anteriores, permitir recuperar el bate | victoria |

La escena trae una caja y placa de ejemplo, no el puzzle completo. Extiende el ejemplo con dos cajas, placas distintas y detector de resolución con recompensa única. Usa identificadores únicos de los puzzles y objetos. No confundas resolver el puzzle con recoger la recompensa: el guardado debe distinguirlos. La cámara final tiene un collider de bloqueo que hay que sustituir por una puerta controlada por el progreso; esta base no la abre automáticamente.

## 4. Guardado JSON

La clase DatosPartida usa `[Serializable]`. Crear SaveManager y un servicio de perfiles. Usar Application.persistentDataPath, JsonUtility.ToJson/FromJson y System.IO. Definir qué caracteres permiten los nombres, no aceptar rutas ni nombres vacíos. Si un nombre existe, cargar o pedir autorización explícita antes de sobrescribir. Listar partidas de la carpeta local. Guardar durante juego y mostrar confirmación solo si se escribió correctamente.

Restaurar posición, vida, salas resueltas, objetos y estado de puertas, recompensas y altar. Definir cómo se reconstruyen los retos resueltos (no reaparecer la recompensa si ya se recogió; si no se recogió, mantenerla accesible). Añadir estado de pedestales si se permite guardar en medio del reto final.

Prueba: crear A y B, avanzar A dos salas, B una diferente, cerrar Unity, regresar, cargar ambas y revisar que no compartan progreso. Prueba nombre duplicado y archivo dañado. No se han realizado estas pruebas todavía.

## 5. UI y sonido

- Crear MainMenu, pantalla de selección/perfil, créditos, controles; botones con regreso. Una escena Dungeon y una escena MainMenu en Build Profiles. Barra de vida, progreso 0/4 y notificación de guardado.
- Descargar música en loop y efectos (máximo 5 segundos por efecto); guardar fuentes y licencias. Añadir sonido a jugador, enemigos, resolución y victoria. No se suministraron archivos de audio.
- Crear Game Over y victoria con nombres reales de integrantes. No incluir nombres inventados.

## 6. Informe, UML y entrega

- Modelo de dominio: Partida, Jugador, Mazmorra, Sala, Puzzle, Enemigo, Objeto, Inventario. Agregar atributos y métodos relevantes según la rúbrica.
- Diagrama de clases: clases realmente implementadas, tipos, visibilidad, parámetros, retorno, relaciones y multiplicidades; revisar contra scripts finales. La herencia conecta Enemy con sus tres subtipos.
- Mapa actualizado, introducción, desarrollo (decisiones), conclusión (dificultades/resultados reales), archivos editables de diagramas y TXT de solución real de principio a fin.
- Verificar compilación sin errores, build desde menú principal, prueba completa con cada perfil y muerte/reinicio. Subir ZIP del proyecto a Drive y el informe y TXT de enlace con formato NOMBRE1_RUT1_NOMBRE2_RUT2.txt al campus. Confirmar hora de cierre. El plazo en el enunciado es 22 de noviembre.

## Issues sugeridos para GitHub Projects

To do → In progress → Review → Done. Asignar responsables reales. Cada commit se hace después de realizar/probar el cambio, no se inventa historial.

- feat(player): adaptar movimiento y animaciones
- feat(combat): agregar vida y ataque
- feat(enemies): implementar clase base y tres comportamientos
- feat(puzzles): resolver cuatro desafios con recompensas
- feat(final): implementar altar y recuperar bate
- feat(save): guardar perfiles y restaurar progreso
- feat(ui): crear menu y hud
- feat(audio): integrar efectos y musica
- test(game): verificar recorrido completo y partidas independientes
- docs(uml): actualizar diagramas y solucion
