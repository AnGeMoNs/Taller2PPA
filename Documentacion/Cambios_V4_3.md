# Sahur V4.3 — Idle estable y oleada final

## Instalar sobre V4.2 (recomendado)
1. Haz una copia de seguridad de tu proyecto, detén Play y cierra Unity.
2. Extrae el parche y copia su carpeta Assets al proyecto existente. Reemplaza los scripts, sin borrar los archivos .meta existentes. Conserva la carpeta Scenes y los grafos que hayas creado.
3. Abre Unity y espera la compilación. Abre Assets/Juego/Scenes/MazmorraV4.unity.
4. Fuera de Play, ejecuta Taller 2 > 3. Configurar animaciones de Sahur. Esta operación recrea únicamente el clip Idle cuando falta la marca Idle_V43.txt, manteniendo el GUID de Idle.anim y del Animator existente. No regenera la mazmorra.
5. Ctrl+S, limpia la consola y Play. Si tu escena no tiene los tres tipos de enemigo, aplica Taller 2 > 6. Reparar fisicas y enemigos V4.2 antes de probar.

## ZIP completo
Abre Sahur_Mazmorra_V4_3 con Unity Hub / Unity 6.3. Si no existe la escena, usa Taller 2 > 2. Crear mazmorra V4 completa. El archivo enviado por el usuario no contiene la escena jugable serializada ni el Animator generado; el generador los crea en el editor. Si ya tienes una escena personalizada, usa el parche. Importa TMP Essential Resources fuera de Play si corresponde.

## Cambios de jugabilidad
- Acknowledging sigue siendo Idle. Se fijan las curvas de giro de raíz al primer fotograma, se conserva el gesto y se elimina la compensación de pose en bucle de Idle. Se bloquea la rotación física del Rigidbody; el movimiento sigue solicitando giros con MoveRotation.
- Al recuperar el bate, la vida máxima sube de 100 a 300 y se cura completamente. La patada existente pasa de 25 a 50 de daño (no se ha creado una animación de golpear con el bate ni un bate equipado en la mano). Hay 1,5 segundos de protección al recogerlo.
- Aparecen dos esqueletos, dos slimes y una tortuga, clonados de los enemigos configurados (modelos, Animator, colliders y grafo Behavior si está asignado). La cámara final permanece accesible; no se añade una puerta de encierro. El objetivo muestra los enemigos de la oleada restantes. La victoria ocurre al eliminar los cinco nuevos enemigos, no al recoger el bate.
- El JSON guarda versión de la fase final, victoria, enemigos muertos y vida. Al cargar se recrean solo los enemigos nuevos no eliminados. Su vida y posición vuelven a sus valores iniciales, como en V4.2. Los perfiles antiguos que ya recuperaron el bate migran a la nueva fase y reciben 300 de vida; al guardar pasan a la nueva versión. Los perfiles antes del bate conservan la vida normal. No se añade un archivo de escena al ZIP.
- Unity Behavior conserva el modo anterior: grafo visual configurado por el usuario si existe; en caso contrario sigue disponible la lógica C# de respaldo.

## Pruebas manuales antes de entregar
1. Con un perfil nuevo, no mover teclado ni ratón durante 30 segundos y observar al menos 3 loops de Idle. Verificar que la raíz y la cámara no giran; caminar, correr y cambiar dirección para comprobar que el motor sigue respondiendo.
2. Recibir daño, resolver los cuatro puzzles y activar los pedestales. Verificar que al recoger el bate el HUD indica 300/300 y que aparecen exactamente cinco enemigos nuevos, una sola vez.
3. Eliminar un esqueleto de la oleada y un slime, guardar F5 y regresar al menú. Cargar ese perfil: deben faltar ambos y el HUD debe conservar la vida exacta guardada; los demás enemigos restauran su estado inicial.
4. Eliminar los restantes, verificar victoria, cargar el perfil y comprobar que vuelve a la pantalla de victoria.
5. Probar un perfil nuevo y uno antiguo que ya hubiera recuperado el bate. Comprobar que los archivos son independientes y que no hay excepciones en consola.

No se dispone de Unity en este entorno. Se inspeccionaron las curvas originales del FBX, el código y las rutas, pero todavía se necesita confirmar la importación, compilación y jugabilidad dentro del editor. Los WAV provisionales y la limitación académica del enunciado no cambian.
