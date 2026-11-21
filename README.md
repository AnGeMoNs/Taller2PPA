# Tung Tung Sahur: El bate perdido — V4.3

Proyecto fuente para Unity 6.3 (6000.3.0f1). Actualiza V3 con una escena generada desde el editor, las cuatro pruebas, recompensas y altar del bate, y persistencia de sus estados. No es una compilación ni un ejecutable verificado: Unity no está instalado en el entorno donde se preparó el ZIP.

## Actualización V4.3
Consulta `Documentacion/Cambios_V4_3.md` para instalar la corrección de Idle, la nueva oleada final y las pruebas.

## Reparación V4.2
Consulta `Documentacion/Reparacion_V4_2.md` para reparar una escena existente, incorporar los guardianes nuevos, corregir los colliders y asignar Unity Behavior.

## Abrir y crear la escena
1. Extrae todo el ZIP y agrega la carpeta `Sahur_Mazmorra_V4_3` a Unity Hub. Abre con Unity 6.3.
2. Espera la importación de assets y paquetes Unity Behavior 1.0.16 y Unity UI 2.0.0. Se requiere Internet para resolver dependencias. Usa Input Manager (Old) o Both; importa TMP Essential Resources si Unity lo solicita.
3. Acepta el diálogo de creación de escena al abrir por primera vez, o ejecuta **Taller 2 > 2. Crear mazmorra V4 completa**. Se generan y guardan `Assets/Juego/Scenes/MazmorraV4.unity`, controladores y materiales. No se pisa una escena existente; se abre la que ya existe. El script monta los prefabs originales, componentes y referencias; el archivo `.unity` aún no está serializado en el ZIP porque requiere el editor. Se añade a Build Settings automáticamente.
4. Ejecuta **Taller 2 > 5. Comprobar referencias V4**. Guarda y presiona Play. Crea un perfil nuevo. Para Unity Behavior visual sigue `Documentacion/Unity_Behavior.md`: hay nodos personalizados, pero no un grafo serializado. El modo C# de enemigos está habilitado por defecto para que el juego pueda probarse sin crear/ligar el grafo.
5. Controles: WASD, Shift correr, ratón, clic izq. patada, E interactuar, F5 guardar, Esc pausa.

## Ciclo de juego
- Oeste: derrota al esqueleto y slime; recoge la llave roja con E.
- Este: empuja las dos cajas sobre las dos placas simultáneamente; usa RESET con E si te atascas. Recoge llave verde.
- Sur: palancas 2, 1, 3; si te equivocas reinicia la secuencia. Recoge llave amarilla.
- Norte: cruza las tres bandas de daño cuando estén verdes, evita la tortuga y activa controles 1 y 2 en ese orden. Recoge gema. Se desactivan las bandas.
- Puerta norte (z=18): se abre solo cuando las cuatro salas están resueltas y las cuatro recompensas recogidas.
- Cámara final: E en cada uno de los cuatro pedestales; después E junto al bate. Recupera el bate, recibe 300 de vida y derrota la oleada de cinco enemigos para ganar.

## Guardado
F5 o pausa > Guardar. Autoguardado cuando se resuelve una sala, se recoge un objeto, se coloca un sello o se recupera el bate. Cada perfil tiene un JSON independiente en `Application.persistentDataPath/Partidas`. Restaura vida, posición, enemigos eliminados, salas, recompensas, posiciones/rotaciones de cajas, secuencia, controles de trampas, pedestales y victoria. Los enemigos vivos vuelven a su posición y vida inicial; el ciclo temporal de las trampas vuelve a iniciar. Los perfiles antiguos sin nuevos campos se cargan con valores por defecto. Se evita que un guardado de V3 deje al jugador atrapado detrás de la puerta nueva.

## Limitaciones y revisión
- No se ejecutó Unity: se revisaron código, estructura, rutas y ZIP, pero queda pendiente confirmar compilación, rig/animaciones, físicas, jugabilidad y Build.
- Unity Behavior: acciones C# y lógica fallback; grafo visual pendiente de crear/asignar en el editor. Consulta la guía.
- Audio: 17 WAV provisionales originales que no son pistas de Resident Evil o Silent Hill. El enunciado pide sonidos descargados por los estudiantes. Sustituir por recursos con licencia y conservar nombres; ver Audio.md.
- Créditos: completar nombres reales en HUDManager.cs. Revisar informe y diagramas de acuerdo con la versión efectivamente probada. El enunciado permite IA solo como apoyo y no para realizar el trabajo completo: usar esta versión como referencia de desarrollo, no presentarla como trabajo propio sin revisión del equipo y autorización del docente.

Ver `Documentacion/Pruebas_V4.md` para pruebas y `Documentacion/Estado_actual.md` para alcance.
