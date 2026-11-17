---
date: 2026-11-16
titre: Les fondations du projet
tickets: VS-5, VS-6
format: long
---
But : créer le projet Unity Universal 3D avec son arborescence (VS-5) et installer l'Input System et Cinemachine (VS-6), avant d'écrire la moindre ligne de jeu.
Résultat :
- Un projet Unity 6 en URP, découpé en modules (Core, Player, Farming, Cafe…) qui ne peuvent pas dépendre les uns des autres n'importe comment : le compilateur l'interdit.
- Une intégration continue sur GitHub qui vérifie à chaque push les fichiers .meta, la taille des fichiers et le formatage du code.
- Les assets Synty rangés dans un package privé, pour que le dépôt du jeu puisse rester public sans jamais contenir leurs fichiers.
Moment fort : le dépôt du jeu est public, mais les assets Synty n'y sont jamais : ils arrivent par un package privé.
Problème et solution : les packs Synty ne peuvent pas être publiés, alors que le code du jeu doit pouvoir l'être. Solution : un dépôt privé séparé, installé comme package Unity par SSH, en lecture seule dans le projet, avec un .gitignore en filet de sécurité.
Clips : aucun
Avant et après : aucun
Suite : rendre la scène en gros pixels, façon A Short Hike.
