---
date: 2026-11-21
titre: Une lune et des nuages en pixel art, un dégradé sous les menus
tickets:
format: court
---
But : donner à l'écran titre une vraie lune et de vrais nuages en pixel art, et rapprocher le menu principal et la pause de l'écran du pack Fantasy Menus avec un dégradé qui fond les menus dans l'image derrière eux.
Résultat :
- La lune et les nuages viennent d'images Pixabay ; les nuages, bleu-vert à l'origine, passent au violet.
- Le ciel est un violet un peu plus clair, avec davantage de petites étoiles lavande.
- Dans le menu principal, un violet très sombre part de la gauche, sous le logo et les boutons, et se fond dans le ciel ; le bouton survolé s'allume d'une barre lavande qui s'efface vers la droite.
- La pause n'est plus une bande unie : le même dégradé s'étend sur la gauche et se fond dans le jeu, qui reste visible.
Moment fort : la lune pixelisée à moitié cachée par un nuage violet, au-dessus de « Moonbrew ».
Problème et solution : les images Pixabay font 5000 pixels de côté pour une soixantaine de pixels d'art ; elles sont réduites à un texel par pixel d'art, puis affichées à deux ou trois fois cette taille pour que chaque pixel reste net. Leur licence permet de les utiliser dans le jeu mais pas de les publier telles quelles : elles vont dans le paquet privé, comme les packs Synty. UI Toolkit ne sait pas dessiner de dégradés : une petite texture blanche qui passe de l'opaque au transparent est teintée par chaque menu.
Clips :
- 2026-11-21_ciel-pixabay_01.png (image) : l'écran titre sous la nouvelle lune
- 2026-11-21_ciel-pixabay_02.png (image) : le menu principal, « New Game » survolé
- 2026-11-21_ciel-pixabay_03.png (image) : la pause dans la ferme
Suite : la création du personnage après New Game.
