# 05\_Bible\_Artistique — Moonbrew

Nov 15, 2026 · @Ileana

## Direction visuelle

Moonbrew vise le rendu de *A Short Hike* : un monde 3D low-poly affiché en basse résolution, avec de gros pixels, dans une ambiance cozy fantasy. Le style doit rester simple et cohérent : formes douces, aplats de couleur, éclairage doux, peu de détails géométriques.

Les assets viennent surtout de packs Synty POLYGON, recolorés avec la palette de leur zone. Les personnages sont en 3D low-poly, rendus en pixel comme le reste du jeu (ils passent par le même rendu pixelisé que le décor). Les éléments qui font l'identité de Moonbrew (le café, les plats, les créatures et l'employé) sont créés sur mesure.

## Spécifications de rendu

- **Pipeline** : Unity URP (template Universal 3D).
- **Résolution** : rendu dans une Render Texture basse résolution (référence : 640x360), agrandie par un facteur entier sans lissage, avec des bandes noires si besoin. La résolution est configurable, avec un mode natif en plus du mode pixel, pour pouvoir comparer les deux looks et changer d'avis.
- **Caméra** : vue 3/4 plongeante orthographique (Cinemachine), sans perspective, calée sur la grille de pixels pour éviter le scintillement.
- **Palette** : une palette par zone (ferme, café, village, forêt...), appliquée par post-traitement et par recoloration des textures, avec un réglage pour la désactiver. Les assets d'une même zone partagent sa palette.
- **Éclairage** : doux et peu contrasté, cycle jour/nuit géré par les lumières et les matériaux.
- **Contours** : discrets, voire absents.

## Packs d'assets choisis

La colonne « Moment » est une proposition : « Slice » pour le vertical slice, « Après » pour ce qui vient ensuite. Les packs viennent de l'abonnement Synty, qui les inclut tous ; il faut relire sa licence avant de publier.

| Pack | Usage dans Moonbrew | Moment | Notes |
| --- | --- | --- | --- |
| POLYGON Farm | La ferme : bâtiments, cultures, props | Slice |  |
| POLYGON Meadow Forest | Biomes de nature, décor extérieur | Slice |  |
| POLYGON Shops | Éléments du café : comptoirs, tables, étagères, props | Slice |  |
| POLYGON Fantasy Characters | PNJ, clients, employé | Slice | Aucune animation incluse (Mixamo pour les humanoïdes) |
| POLYGON Fantasy Kingdom | Bâtiments du village, décoration intérieure et meubles | Slice | La décoration intérieure vient après le slice |
| POLYGON Fantasy Village | Bâtiments du village, nourriture, décoration intérieure et meubles | Slice |  |
| POLYGON Elven Realm | Personnages | Après |  |
| POLYGON Enchanted Forest | Biome de forêt, ressources de nature | Après | Champignons, herbe et autres ressources de nature |
| POLYGON Fantasy Rivals | Monstres de forêt | Après | Forest Guardian, Forest Witch, golems |
| POLYGON Dungeon Pack | Monstres de mine, grottes, cristaux | Après | Gobelins, squelettes, golem de roche |
| POLYGON Fantasy Dungeon Map | Carte de donjon prête à placer | Après | Nécessite Dungeon Pack |
| POLYGON Dungeon Realms | Environnements souterrains, cristaux, forge | Après | 480 assets d'environnement |
| POLYGON Dwarven Dungeon Map | Mines, forge, minerais (props) | Après | Nécessite Dungeon Realms |

## Dépendances et packs écartés

- **Fantasy Dungeon Map** a besoin de **Dungeon Pack**. Un bundle des deux existe sur la boutique Synty.
- **Dwarven Dungeon Map** a besoin de **Dungeon Realms**. Un bundle des deux existe aussi.
- **POLYGON Coffee Shop** est écarté : il ne convient pas au projet. Le café se meuble avec le pack Shops.
- Les packs sont à importer au fur et à mesure des tickets, pas tous d'un coup, pour garder un projet léger.

## Règles de cohérence

- **Alignement sur la grille isométrique** : dans la ferme, tout est posé parallèlement à la maison : clôtures, chemins, champ, machines, caisse d'expédition, décor. Avec la vue isométrique, un objet de travers se voit tout de suite. Les objets ne tournent que par quarts de tour par rapport à la maison. Même règle au café, avec les murs de la salle comme repère.
- **Une palette par zone** : chaque zone (ferme, café, village, forêt, mine...) a sa propre palette, et tous les assets d'une zone la partagent pour que les packs s'accordent.
- **Assets maison pour l'identité** : le café, les plats, les créatures et l'employé sont modélisés sur mesure.
- **Animations** : disponibles pour l'instant, les packs Synty Animation Base Locomotion, Idles et Sword Combat, plus la banque d'Adobe (Mixamo) pour les humanoïdes. Pour les monstres non humanoïdes, une source reste à trouver (par exemple Infinity PBR). Le type de licence est à vérifier pour chaque pack tiers.
- **Licences (abonnement Synty)** : usage commercial autorisé dans le jeu et pour sa promotion, mais pas de revente des assets seuls, pas d'entraînement ni de génération par IA, pas de NFT ni de blockchain, pas d'outil de création de contenu pour les joueurs. À la fin de l'abonnement, on ne peut plus utiliser les assets dans du nouveau contenu ni développer le jeu de façon importante : garder l'abonnement actif tant que le jeu évolue, ou acheter à l'unité les packs clés avant la sortie.
- **Steam et IA** : l'IA n'est utilisée que pour le code, ce qui ne se déclare pas. Tout pack tiers doit être vérifié (contenu fait main ou non).
- **Git** : assets lourds via Git LFS.

## À décider plus tard

- [ ] Packs à acheter à l'unité avant la sortie : l'abonnement doit rester actif tant que le jeu évolue ; avant la sortie, acheter sur la boutique Synty (pas sur l'Asset Store) les packs qui finissent dans le jeu, pour avoir une licence perpétuelle.
- [ ] Animations des monstres non humanoïdes (slime, araignée...) : une source reste à trouver (Infinity PBR ou autre), à traiter avec les monstres, après le slice.
