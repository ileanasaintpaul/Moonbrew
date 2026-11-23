---
date: 2026-11-23
titre: Le frigo du café
tickets: VS-44
format: court
---
But : donner à la patronne son propre inventaire au café et un frigo où piocher (VS-44).
Résultat :
- En entrant au café, la barre d'objets passe sur l'inventaire du café, de 10 cases. Le sac de la ferme, avec les outils et les graines, revient en sortant.
- Le four du café cuisine maintenant avec cet inventaire.
- Un clic sur le meuble bas de la cuisine, à côté du poêle, ouvre le frigo : clic gauche pour déplacer toute une pile, clic droit pour un seul objet, dans les deux sens.
- Le jeu se met en pause tant que le panneau est ouvert.
- Une nouvelle partie démarre avec 12 farines et 12 fraises au frigo, en attendant la caisse d'expédition et le garde-manger.
Moment fort : sortir la farine du frigo et la voir arriver aussitôt dans la barre d'objets.
Problème et solution : le script de mise en place perdait ses références aux données à chaque changement de scène, et l'inventaire du café se retrouvait sans réglages. Le contrôle en jeu l'a montré tout de suite : les données sont maintenant rechargées après chaque ouverture de scène.
Clips :
- 2026-11-23_frigo-cafe_01.png (image) : le panneau du frigo, avec la farine et une fraise passées dans l'inventaire et la barre d'objets
Suite : la vitrine et ses parts de gâteau, puis la préparation des commandes.
