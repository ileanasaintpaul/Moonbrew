---
date: 2026-11-17
titre: La patronne se met en marche
tickets: VS-11
format: court
---
But : faire marcher, courir et interagir la patronne avec l'Input System (VS-11).
Résultat :
- Elle marche et court dans toutes les directions par rapport à la caméra, avec des animations d'attente, de marche et de course.
- La caméra la suit en vue isométrique, et les maisons, arbres, clôtures, rochers et débris la bloquent.
- La touche E déclenche une interaction avec l'objet le plus proche, comme la boîte aux lettres ou le chaudron du café.
Moment fort : la caméra qui passe d'une vue de face à une vraie vue isométrique, la maison vue de biais.
Problème et solution : en mouvement, la patronne tremblait d'un pixel, parce que sa position tombait entre deux pixels. Son modèle est maintenant recalé sur la même grille que la caméra, et elle avance pixel par pixel sans trembler. Autre souci : la ferme était vue de face, sans effet isométrique ; la caméra a été tournée de biais et rapprochée.
Clips :
- 2026-11-17_deplacement-patronne_01.png (image) : la patronne immobile devant sa maison
- 2026-11-17_deplacement-patronne_02.png (image) : la patronne qui marche, suivie par la caméra
- 2026-11-17_deplacement-patronne_04.png (image) : la patronne au pied de l'escalier, près de la boîte aux lettres, avant la vue isométrique
- 2026-11-17_deplacement-patronne_05.png (image) : la patronne dans le café, suivie par la caméra
- 2026-11-17_deplacement-patronne_avant.png (image) : la ferme vue de face, avant la vue isométrique
- 2026-11-17_deplacement-patronne_06.png (image) : la première vue isométrique, zoomée
- 2026-11-17_deplacement-patronne_apres.png (image) : la vue isométrique finale, la maison vue par sa façade et son côté gauche
Avant et après : 2026-11-17_deplacement-patronne_avant.png (vue de face) et 2026-11-17_deplacement-patronne_apres.png (vue isométrique).
Suite : faire passer le temps, du matin à la nuit.
