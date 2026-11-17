---
date: 2026-11-16
titre: Le rendu pixelisé
tickets: VS-7, VS-8
format: court
---
But : donner au jeu son look de gros pixels en rendant la scène 3D en basse résolution (VS-7), avec une caméra qui bouge sans faire scintiller le décor (VS-8).
Résultat :
- La scène est rendue en 640×360, puis agrandie sans flou par un nombre entier (×3 en 1080p, ×4 en 1440p) ; F2 bascule entre le rendu pixelisé et le rendu net.
- La caméra orthographique en vue 3/4 est calée sur la grille de pixels : le décor glisse au lieu de scintiller.
- Un test automatique vérifie que, quand la caméra bouge, l'image ne fait que glisser.
Moment fort : la même scène, nette puis pixelisée d'un coup avec F2.
Problème et solution : une fois la caméra calée sur la grille, tout le cadre avançait par à-coups d'un pixel. Solution : rendre l'image avec un pixel de marge et la faire glisser de la fraction de pixel restante, pour un mouvement fluide sans scintillement.
Clips : aucun
Avant et après : aucun
Suite : construire une vraie scène pour juger le look : la ferme du premier jour.
