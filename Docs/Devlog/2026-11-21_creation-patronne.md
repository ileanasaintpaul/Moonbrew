---
date: 2026-11-21
titre: Créer sa patronne
tickets:
format: court
---
But : choisir l'apparence et le nom de sa patronne au début d'une partie, comme sur les écrans de création du pack Fantasy Menus, sans classe, statistiques, voix ni difficulté.
Résultat :
- New Game ouvre un écran de création : des onglets en haut (Body, Hair, Beard, Hat, Accessory, Outfit, Name, avec Q et E), un panneau à cadre doré à gauche, la patronne à droite sur fond violet.
- Chaque case montre la patronne avec la pièce de la case : 8 tenues, 6 coiffures, une barbe, 5 chapeaux, 4 paires de boucles d'oreilles, tirés du pack Fantasy Village.
- Cinq couleurs au choix, chacune à part et sous sa pièce : la peau sous les corps, les cheveux sous les coupes, les sourcils sous les barbes (11, 11 et 8 teintes) ; le haut et le bas dans Outfit (12 palettes du pack), avec des vignettes cadrées sur le haut ou le bas seul.
- La patronne est rendue lisse, sans pixelisation, comme sur les écrans du pack.
- Random tire une apparence au hasard ; Start n'est possible qu'avec un nom ; Back ramène à l'écran titre.
- La partie est créée avec le nom et l'apparence, et la patronne les porte dans la ferme et au café. Une ancienne partie garde un look par défaut : robe rouge et cheveux ondulés.
Moment fort : les cheveux qui passent au bleu canard dans toutes les vignettes à la fois.
Problème et solution : les cheveux restaient de la même couleur avec un matériau uni. Le shader des personnages Synty teint en fait les cheveux avec sa propre propriété de couleur : elle est passée à chaque pièce, ce qui garde le détail de la texture ; les sourcils, qui font partie du corps, reçoivent leur propre couleur de la même façon, et la peau aussi. Le corps est dessiné d’un seul tenant avec une seule palette : pour colorer le haut et le bas à part, son mesh est copié en jeu et coupé en deux selon les os qui font bouger chaque triangle (les jambes pour le bas). Plusieurs accessoires du pack sont taillés pour les têtes d'animaux (lunettes, casque, grands chapeaux) : ils n'ont pas été gardés.
Clips :
- 2026-11-21_creation-patronne_01.png (image) : les tenues
- 2026-11-21_creation-patronne_02.png (image) : les coiffures, en bleu canard
- 2026-11-21_creation-patronne_03.png (image) : les couleurs
- 2026-11-21_creation-patronne_04.png (image) : le nom
- 2026-11-21_creation-patronne_05.png (image) : la patronne dans la ferme
Suite : l'apparence modifiable plus tard auprès d'une coiffeuse ou d'un tailleur du village, et le nom de la patronne dans les dialogues.
