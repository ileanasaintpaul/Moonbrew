# 07\_Devlog — Moonbrew

Nov 17, 2026 · @Ileana

Le devlog raconte le développement de Moonbrew en vidéo, pour faire connaître le jeu bien avant sa sortie. Il prépare le public qui mettra le jeu en liste de souhaits quand la page Steam ouvrira.

## Pourquoi commencer maintenant

- **Les avant/après font les meilleurs clips.** Les images du début ne se rattrapent pas plus tard ; il faut donc capturer chaque étape au moment où elle arrive.
- **Il y a déjà de quoi montrer :**
  - le rendu pixelisé, avec la correction du scintillement ;
  - la caméra isométrique ;
  - la ferme et le café fantasy ;
  - la patronne qui se déplace ;
  - l'horloge.
- **Raconter les débuts attache le public au projet** : pourquoi Moonbrew, et pourquoi le look *A Short Hike*.
- **La licence Synty autorise l'usage des assets pour promouvoir le jeu** (voir la bible artistique).

## Formats et rythme

| Format | Où | Durée | Rythme | Contenu |
| --- | --- | --- | --- | --- |
| Clip court | TikTok, YouTube Shorts, Instagram Reels | 15 à 30 s | Environ 1 par semaine | Un seul sujet, idéalement un avant/après |
| GIF ou image | X, Bluesky, Reddit (r/IndieDev, r/cozygames, r/Unity3D) | Boucle de 3 à 8 s | Au fil de l'eau | Un détail qui fait plaisir à voir |
| Devlog long | YouTube | 5 à 10 min | À chaque étape du vertical slice | L'histoire de l'étape : objectifs, problèmes, solutions, résultat |

Règles pour les clips courts :

- Le sujet se comprend dès la première seconde.
- On montre le jeu, pas l'éditeur.
- Un titre court à l'écran : « Avant / Après », « Jour 1 à la ferme ».
- On termine sur la plus belle image.

## Plan des épisodes longs

Les épisodes suivent les étapes du vertical slice (voir `02_Perimetre_Vertical_Slice`).

| Épisode | Étape | Tickets | Sujets |
| --- | --- | --- | --- |
| 1. Fondations | Fondations et rendu | VS-5 à VS-13, puis VS-9 | Le projet et son ambition ; le rendu pixelisé et le combat contre le scintillement ; la ferme du premier jour, du terrain vide à la scène finale ; le café fantasy ; la patronne qui bouge et la caméra isométrique ; l'horloge et le HUD ; le bus d'événements, expliqué simplement |
| 2. Lumière et couleurs | Fin de l'étape 1 | VS-9, inventaire, sauvegarde | Les palettes par zone, l'éclairage doux, le jour et la nuit : le plus fort avant/après visuel |
| 3. De la graine au gâteau | Ferme et cuisine | Étape 2 | Planter, arroser, récolter, faire de la farine, cuisiner |
| 4. Le café ouvre | Café et employé | Étape 3 | Les premiers clients, le service, l'employé qui prend le relais |
| 5. Une journée complète | PNJ, relations, quête et bilan | Étape 4 | Les habitants, une quête, le bilan du soir : la première vraie partie |

## Clips déjà possibles

Les captures sont rangées dans `Devlog/captures/`, un dossier local ignoré par Git (voir plus bas).

| Clip | Sujet | Captures existantes |
| --- | --- | --- |
| « Pourquoi mes pixels tremblaient » | Le scintillement du décor, puis la caméra calée sur la grille | `VS-10_scintillement_lucioles/` (avec et sans les grandes lucioles) |
| « La ferme en 15 versions » | Du terrain vide à la ferme finale, en accéléré | `VS-10_ferme/farm_v01` à `farm_v15`, `overview*` |
| « Un café fantasy » | Le café en plan large, puis le comptoir et les étagères en gros plan | `VS-10_cafe/` |
| « De face → isométrique » | Le changement de caméra de la ferme | `VS-11_joueur/camera_avant_iso`, `camera_iso_v1`, `camera_iso_finale` |
| « Du matin à la nuit » | Le HUD de l'horloge à 6h, le soir, puis après minuit | `VS-12_horloge/` |

**Ce qui manque pour ces clips :** des séquences vidéo jouées pour de vrai, avec la patronne qui marche. Les captures actuelles sont des images fixes. C'est le rôle du mode capture (ticket VS-42) : HUD masquable, caméra libre, contrôle de l'heure.

## Habitude à chaque ticket

À la fin de chaque ticket, avant la PR :

1. **Une image du résultat**, dans `Devlog/captures/VS-XX_sujet/`.
2. **Un clip de 10 à 20 s du résultat en jeu**, avec OBS.
3. **S'il y a un avant/après**, l'image de l'avant, prise avant de commencer le ticket.
4. **Une ligne dans le journal ci-dessous** : ce qui a changé, et le moment le plus intéressant à raconter.

## Matériel et réglages de capture

- **OBS Studio**, en capture de la fenêtre du jeu :
  - 1920×1080 à 60 i/s pour YouTube ;
  - 1080×1920 (vertical) pour les clips courts, en recadrant la scène.
- **Le jeu affiché en 1920×1080**, pour que l'image de 640×360 soit agrandie exactement ×3 : de gros pixels nets, sans flou.
- **Le Game view de l'éditeur suffit pour commencer.** Un build donnera des images plus fluides et sans interface d'éditeur.
- **Unity Recorder** est possible plus tard pour des captures image par image parfaites. C'est un package à ajouter au projet, donc à valider d'abord.

## Rangement des captures

- `Devlog/` est à la racine du projet et ignoré par Git : les vidéos sont trop lourdes pour le dépôt.
- Une capture par dossier de ticket : `Devlog/captures/VS-XX_sujet/`.
- Les montages finis vont dans `Devlog/videos/`.
- **Attention : `Devlog/` n'est sauvegardé nulle part.** Il vaut mieux le synchroniser avec un service cloud (OneDrive, Google Drive), ou le copier régulièrement sur un disque externe.

## Page Steam

- On l'ouvre quand le café et la boucle ferme → service se voient en jeu, idéalement six mois à un an avant la sortie.
- Le devlog sert à préparer ce moment : chaque vidéo renverra vers la page Steam dès qu'elle existera.
- Pour la page, il faudra une bande-annonce courte, cinq captures fortes minimum, et une phrase d'accroche (*ta ferme nourrit ton café, ton café nourrit ton village*).

## Points d'attention

- **Ne jamais montrer d'assets Synty seuls**, hors du jeu. La licence autorise la promotion du jeu, pas la diffusion des assets.
- **La musique des vidéos** doit être libre de droits, ou composée pour le jeu.
- **Ne pas montrer de secrets** à l'écran : clés, tokens, dépôt privé `moonbrew-synty`.

## Journal du devlog

| Date | Ticket | Ce qui a changé | Moment à raconter |
| --- | --- | --- | --- |
| 16/11/2026 | VS-7 | Rendu en 640×360 agrandi sans flou | Le premier « wow » pixel |
| 16/11/2026 | VS-8 | Caméra calée sur la grille de pixels | Le décor qui scintillait, puis plus du tout |
| 16/11/2026 | VS-10 | Ferme du premier jour, puis café fantasy | 15 versions de la ferme ; les lucioles qui faisaient scintiller l'écran |
| 17/11/2026 | VS-11 | Déplacement, collisions, interaction, caméra isométrique | La patronne qui tremblait d'un pixel, et la caméra mise de biais |
| 17/11/2026 | VS-12 | Horloge, phases, jours, HUD | La journée de 6h à 2h |
| 17/11/2026 | VS-13 | Bus d'événements | Comment les systèmes du jeu se parlent sans se connaître |
