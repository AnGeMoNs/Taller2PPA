# Audio del proyecto

Todos los archivos de audio se cargan desde `Assets/Juego/Resources/Audio/` mediante `AudioManager`, sin necesidad de indicar la extension al llamarlos.

## Musica de fondo (en loop con crossfade)
Ubicacion: `Assets/Juego/Resources/Audio/Music/`
- `DekuMenu.mp3`: musica del menu principal.
- `MusicaMazmorra.mp3`: musica principal al explorar las salas.
- `MusicaJefe.mp3`: musica al entrar a la Camara del Altar y enfrentar la oleada final.
- `MusicaSinEnemigos.mp3`: musica tranquila al derrotar la oleada final y en la pantalla de victoria.

## Efectos de sonido (SFX, todos <= 4 segundos)
Ubicacion: `Assets/Juego/Resources/Audio/SFX/`

Jugador:
- `Player_Kick.mp3`: corte de espada.
- `Player_Footstep.ogg`: pasos al caminar/correr.
- `Player_Hurt.ogg`: golpe recibido.
- `Player_Death.wav`: derrota del jugador.
- `SFX_Esquiva.wav`: esquiva rapida con Espacio.
- `SFX_Parry.wav`: parry perfecto con escudo.
- `SFX_AtaqueGiratorio.wav`: ataque giratorio 360 grados.
- `SFX_Curacion.wav`: pocion de vida, corazones y Santuario de Hadas.
- `SFX_FijarObjetivo.ogg`: fijado de objetivo (Z-Targeting).

Enemigos:
- `Enemy_Hit.ogg`: impacto sobre enemigos.
- `Enemy_Death.wav`: muerte de enemigo.
- `Skeleton_Alert.wav`: alerta del esqueleto al ver al jugador.
- `Skeleton_Attack.wav`: ataque del esqueleto.
- `Slime_Contact.wav`: contacto del slime.
- `Turtle_Contact.wav`: contacto de la tortuga.
- `SFX_Aturdimiento.wav`: aturdimiento por parry o giro.

Puzzles, mundo e interfaz:
- `Door_Open.wav`: apertura de la reja norte.
- `Puzzle_Complete.wav`: sala resuelta.
- `Item_Pickup.wav`: recoger llaves, gema o reliquia.
- `Game_Victory.wav`: fanfarria de victoria.
- `UI_Click.wav`: botones de menu, palancas y bloqueo.
- `UI_Save.mp3`: confirmacion al guardar partida.
- `SFX_Rupia.wav`: recoger rupias.
- `SFX_Cofre.ogg`: abrir cofres del tesoro.
- `SFX_RomperObjeto.ogg`: romper barriles y anforas.

## Fuentes utilizadas para los SFX
- OpenGameArt - RPG Sound Pack (artisticdude, CC0): https://opengameart.org/content/rpg-sound-pack
- OpenGameArt - 50 RPG Sound Effects (Kenney, CC0): https://opengameart.org/content/50-rpg-sound-effects
- OpenGameArt - Fantasy Sound Effects Library (LittleRobotSoundFactory, CC-BY 3.0): https://opengameart.org/content/fantasy-sound-effects-library
- OpenGameArt - 100 CC0 SFX (rubberduck, CC0): https://opengameart.org/content/100-cc0-sfx
- OpenGameArt - Win Sound Effect / Completion Sound / Game Over Sound (CC0)
