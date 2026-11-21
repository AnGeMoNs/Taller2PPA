# Audio de prueba y fuentes para sustitución

Los WAV incluidos son tonos, ruido filtrado y efectos originales generados para la base, NO son sonidos ni música extraídos de Resident Evil o Silent Hill. Se incluye una pista ambiental oscura de 24 segundos y 16 efectos de menos de 5 segundos. No se pudieron descargar los packs externos desde el contenedor; no se ha incorporado ningún audio de terceros. Los audios de clase no se usan como música del juego.

Rutas del proyecto usadas por AudioManager:
- Assets/Juego/Resources/Audio/Music/Music_Dungeon_Loop.wav
- Assets/Juego/Resources/Audio/SFX/Player_Kick.wav
- Player_Hurt.wav, Player_Death.wav, Player_Footstep.wav
- Enemy_Hit.wav, Enemy_Death.wav, Skeleton_Alert.wav, Skeleton_Attack.wav, Slime_Contact.wav, Turtle_Contact.wav
- UI_Click.wav, UI_Save.wav
- Puzzle_Complete.wav, Item_Pickup.wav, Door_Open.wav, Game_Victory.wav

Door_Open y los audios del puzzle y victoria serán usados cuando se enlacen los puzzles (la API ya existe). Los clips se cargan por nombre, sin extensión. Para reemplazar un WAV por un MP3/OGG, elimina el WAV original y conserva el nombre base (no dejar duplicados con igual nombre en Resources). Revisar loop y duración.

## Fuentes encontradas
- SxTempu, Silent Hill Inspired Music Pack: https://sxtempu.itch.io/psx-horror-music-pack-1 — permiso explícito de uso en proyectos de videojuegos, créditos apreciados. Catacombs (0:43 loop), Tension (0:37 loop) y Dread (0:36 loop) son opciones de ambiente. Descarga por itch.io, no se incluyen en esta entrega.
- Jeremy Leaird-Koch, Safe Rooms: https://jjbbllkk.itch.io/safe-rooms — CC BY-NC, seguir condiciones y atribución del autor. Inspirado en save rooms de Resident Evil y cinemáticas de Silent Hill.
- yd, Dungeon Ambience: https://lpc.opengameart.org/content/dungeon-ambience — CC0.
- Kenney Impact Sounds: https://kenney.nl/assets/impact-sounds — CC0, golpes y pasos.
- Kenney Interface Sounds: https://kenney.nl/assets/interface-sounds — CC0, botones.
- Ognomo, Retro Horror SFX Pack: https://ognomo.itch.io/retro-horror-sfx-pack — CC0, efectos de monstruos, ambientes, UI, combate.

Descargar los recursos que elijan personalmente y registrar nombre original, autor, licencia y URL en los créditos. El enunciado pide sonidos descargados de máximo 5 segundos (la música de fondo debe estar en loop). No asumir que comprar una OST de Capcom/Konami permite redistribuirla dentro de un juego.
