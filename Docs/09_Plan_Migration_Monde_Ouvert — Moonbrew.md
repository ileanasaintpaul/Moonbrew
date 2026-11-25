# Plan de migration vers le monde ouvert — Moonbrew

*Rédigé le 2026-11-23. Statut : proposé. Le 2026-11-25, `SC_Village` a été ajoutée avant les étapes 1 à 5, déjà placée dans les coordonnées du monde. Le 2026-11-26, elle a été refaite en village compact au sud-est de la ferme, au bout de l'ancien chemin du café (voir la doc technique).*

## Objectif

Passer d'une suite de scènes chargées l'une après l'autre à une map ouverte : plusieurs scènes chargées en additif autour de la joueuse, perçues comme un seul monde.

La migration ne change pas le format de sauvegarde : la partie s'écrit toujours à la fin de la journée, à la ferme.

## Point de départ

- `SC_Farm` et `SC_Cafe` contiennent chacune leur session, horloge, caméras, soleil, HUD, inventaires et livre de recettes.
- `GameSession` et `GameLauncher` changent de scène avec un chargement complet. `DayCarry` transporte la journée en mémoire entre deux scènes.
- `FarmField`, `Station` et `CafeFloor` ont des références sérialisées vers la joueuse, et les deux premiers vers le sac. `Station` pointe aussi vers le livre de recettes.
- La ferme et le café sont tous deux construits autour de l'origine.
- Seuls les clients du café utilisent le NavMesh.

## Structure cible

- **`SC_Persistent`**, toujours chargée : session, horloge, joueuse, caméras, soleil, HUD, inventaires, livre de recettes.
- **`SC_Farm`, `SC_Cafe`, puis `SC_Village`, etc.** : uniquement leur décor et leurs systèmes locaux. Les noms actuels sont conservés.
- **Le café reste un intérieur exclusif au début**, avec le fondu actuel, puisqu'il se superposerait à la ferme s'ils étaient chargés ensemble.

## Étapes

Le jeu reste jouable à chaque étape, et chacune fait l'objet de commits séparés.

| # | Étape | Contenu | Résultat vérifiable |
|---|---|---|---|
| 1 | Références entre scènes | Unity ne sérialise pas les références d'une scène à l'autre. Le sac, la joueuse et le livre de recettes passent par de petits ScriptableObjects d'ancrage, sur le modèle de `DayCarry`, chacun dans le module qui possède le type. | Jeu identique, tests verts |
| 2 | Cycle de vie des zones | Ajout de `ZoneLoaded` et `ZoneUnloading`, filtrés par scène, pour la ferme, les stations et le café. | Une zone rechargée retrouve son état |
| 3 | Découpage | Un script Editor déplace les objets communs vers `SC_Persistent`. Le bouton Play fonctionne depuis n'importe quelle zone. | Ferme et café jouables, une zone à la fois |
| 4 | Voyage additif | Les portails changent de zone sans recharger la joueuse ni le HUD. La ferme est chargée avant la nuit pour que les cultures poussent. | Journée complète ferme → café → nuit |
| 5 | Streaming | Une classe C# pure décide quoi charger selon la position, avec une marge pour éviter les allers-retours. Les rayons et limites vont dans un `SO_WorldConfig`. | Pas de saccade au passage d'une limite |
| 6 | Première vraie frontière | Ajout de `SC_Village` à la sortie de la ferme, avec le fondu de palette et d'ambiance entre les zones. | Marcher de la ferme au village sans écran noir |

Les étapes 1 à 4 peuvent se faire maintenant. Les étapes 5 et 6 peuvent attendre la fin du vertical slice, où la grande map est hors périmètre, sauf si le village doit être ouvert dès le slice.

## Points de vigilance

- **Ne pas réutiliser `GameLoaded` pour une zone** : il restaurerait aussi le sac avec un état périmé.
- **Ne pas réutiliser `GameSaving` en cours de journée** : `InventoryHolder` remet ses totaux à zéro à chaque appel, donc le récap perdrait des objets si la copie était jetée.
- **Charge de la ferme** : environ 1 600 prefabs, dont 629 touffes d'herbe coupables. Mesurer la saccade au chargement à l'étape 5, et répartir l'initialisation si besoin.
- **Réglages du projet** : ajouter `SC_Persistent` et les nouvelles zones au build modifie `ProjectSettings`, ce qui demande un accord préalable.
- **PNJ** : quand ils traverseront les zones, leur emploi du temps devra continuer en C# pur pendant que leur zone est déchargée, et le NavMesh devra être raccordé entre les zones.

## Bug probable à traiter à part

Une préparation en cours à la station du café ne semble pas se terminer pendant la nuit, car seule la ferme est chargée quand `NightPassed` est publié. Constaté à la lecture du code, pas encore reproduit en jeu.
