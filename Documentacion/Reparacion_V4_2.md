# Sahur V4.2 — reparación de físicas, personajes y enemigos

## Instalar sobre tu proyecto V4.1 (recomendado)
1. Detén Play Mode. Cierra Unity y haz un respaldo del proyecto completo.
2. Extrae el parche y copia su carpeta `Assets` sobre la carpeta `Assets` del proyecto. Acepta fusionar/reemplazar los archivos C#. No borres la carpeta Assets existente.
3. Abre Unity 6.3 y abre `Assets/Juego/Scenes/MazmorraV4.unity`.
4. Ejecuta **Taller 2 > 6. Reparar fisicas y enemigos V4.2**. Esta opción respalda el archivo de escena en `MazmorraV4_Backup*.unity` antes de modificarlo. No elimina ni vuelve a importar los modelos. Guarda la escena.
5. Ejecuta **Taller 2 > 5. Comprobar referencias V4**. Prueba una partida nueva primero. Los perfiles anteriores se conservan; sus IDs de enemigos existentes no cambian. Se restaura Y a 0.04 en el área plana de la mazmorra para corregir guardados con alturas incorrectas.

Para el ZIP completo, abre la carpeta `Sahur_Mazmorra_V4` desde Unity Hub. La opción 2 genera la escena V4 con las reparaciones V4.2 en un proyecto nuevo. El archivo .unity se crea en Unity, no está pregenerado en el ZIP.

## Qué cambia
- Piso, muro, pilar, puerta y cajas conservan BoxCollider sólido en el objeto raíz. No duplicar los colliders hijos de los modelos. Piezas de puzzle con triggers no se alteran.
- El protagonista conserva su cápsula vertical (1.85 m), Rigidbody dinámico, gravedad, rotación X/Z congelada; pies a Y≈0. La escala de la malla se calcula a partir de los vértices deformados del SkinnedMeshRenderer, no de los límites de culling. AjusteVisualSuelo corrige flotación visual en locomoción, sin desplazar el Rigidbody y sin modificar las animaciones en ataque. Acknowledging queda como idle.
- Se reutiliza el prefab original del esqueleto UD_demo_character (la malla contiene los accesorios); comprobar visualmente espada y escudo en el editor. No se duplican accesorios.
- Cámara de trampas: tortuga ampliada y un esqueleto adicional. Cámara final: esqueleto y slime nuevos. Se mantiene el puzzle original de combate y la condición de cuatro objetos; los guardianes finales no son requisito adicional de victoria. Luz de refuerzo en cámara final.
- 6 enemigos en total en la escena base: 3 originales y 3 adicionales. Se anotan muertes por ID en el guardado JSON.
- Esqueleto: visión por raycast, memoria de 2.3 s, límite de territorio, aviso, anticipación de daño 0.38 s, cooldown 1.65 s, posibilidad de esquivar; se comprueba nuevamente alcance y visibilidad al impactar. Slime persigue a corta distancia; tortuga patrulla. Recuperación ante bloqueo, desvíos cortos y cambio de punto. No se usa NavMesh, no es planificación de rutas global.

## Unity Behavior
El paquete 1.0.16 y los tres nodos personalizados se mantienen. Ahora los nodos evalúan al entrar y notifican actividad al enemigo. El respaldo C# actúa si el grafo no emite decisiones; se evita dejar inmóvil un enemigo por falta de asset. El grafo visual tiene que crearse una vez en el editor; no se serializa en este ZIP porque no hay Unity instalado aquí.
1. Project > Assets/Juego > Create > Behavior > Behavior Graph. Nombre BG_Enemigo. Ábrelo.
2. On Start: activa Repeat, desactiva Allow Multiple Repeats per Tick si aparece.
3. Añade Try In Order; conecta tres acciones de Action/Sahur en orden Atacar jugador, Perseguir jugador, Patrullar o esperar.
4. En los tres campos Agent, vincula la variable Self del Blackboard. Si usas una variable Agent de tipo GameObject, el script intenta enlazarla con el enemigo al arrancar.
5. Guarda el grafo, selecciónalo en Project y ejecuta **Taller 2 > 7. Asignar Behavior Graph seleccionado**. Esta opción agrega BehaviorGraphAgent y asigna el grafo a todos los enemigos activos de la escena y habilita usarUnityBehavior. Guarda la escena.
6. Presiona Play, inicia partida, selecciona el enemigo y observa el grafo en ejecución. Si no encuentra el asset runtime, vuelve a abrir y guardar el grafo. Si no emite decisiones se mantiene la IA C# como respaldo.

## Pruebas mínimas (pendientes de ejecutar en Unity)
- Scene: seleccionar Mazmorra > una sala > Piso/Muro/Pilar; comprobar Box Collider habilitado e Is Trigger desactivado. El collider está en el padre, no necesariamente en el prefab visual.
- Sahur: comprobar que al entrar no está encima de una estructura, los pies coinciden con el piso, puede caminar y la cápsula no atraviesa paredes. Probar movimiento y patada. Si el rig exportado sigue desplazándose, inspeccionar el Animator y sus curvas de Root; ajustar frecuencia/ajuste máximo del componente si hace falta.
- En Scene, Personajes: Esqueleto_muestra, Slime_muestra, Tortuga_muestra, Esqueleto_trampas, Esqueleto_guardian_final, Slime_guardian_final. Comprobar malla, animación y visibilidad.
- Resolver las cuatro salas, entrar a la final y comprobar que ambos guardianes son visibles. Salir del alcance antes del impacto para esquivar; ocultarse detrás de muro y comprobar persecución limitada. Comprobar muerte, no duplicación al ejecutar la reparación por segunda vez, y guardado/carga.
- Configurar Behavior Graph y revisar que los nodos se iluminen durante Play. Verificar que el arma/escudo siguen visibles con animaciones.

Limitaciones: No se ha podido ejecutar el editor Unity ni compilar C# contra sus ensamblados. Se comprobó el código fuente, las rutas de assets, el contenido de ZIP y la coherencia de nombres. Esta es una corrección de la V4.1, no una garantía de ejecución sin pruebas. El enunciado permite IA como apoyo; revisar con tu compañero y docente.
