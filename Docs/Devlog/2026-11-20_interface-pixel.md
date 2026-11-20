---
date: 2026-11-20
titre: L'interface passe au pixel, ses textes restent nets
tickets:
format: court
---
But : accorder l'interface au jeu : elle était lisse et vectorielle au-dessus d'une image pixelisée.
Résultat :
- Les cadres de l'horloge, de la barre d'objets et des fenêtres sont dessinés à la même résolution que le jeu, puis agrandis sans lissage : leurs bords sont pixelisés comme le reste.
- Les textes, eux, restent lisses et nets : ils sont redessinés par-dessus, à la résolution de l'écran, avec les polices du HUD Synty (LT Museum, Alegreya Sans).
- Sous chaque case, le numéro de touche reprend la boîte du pack, entaille comprise, aux proportions du prefab Synty : ce petit cadre est redessiné net avec son chiffre, sinon l'entaille disparaissait dans les gros pixels, et d'une couleur unie au lieu du dégradé du pack. Les chiffres sont centrés et tous sur la même ligne ; les quantités sont petites, en haut à droite.
- Le HUD quitte le marron et le jaune pour un violet terne : cases, case choisie, fond de barre, horloge et nom de la phase.
- Les menus se cliquent comme avant, et la touche F2, qui repasse le jeu en rendu net, rend aussi toute l'interface nette.
Moment fort : l'horloge aux bords pixelisés, avec une heure parfaitement lisible.
Problème et solution : dessinés en basse résolution, les textes devenaient flous ; une police raster sans lissage les rendait nets mais déformait les lettres des polices décoratives. Solution : les textes sont rendus invisibles dans l'interface pixelisée, sans changer ni leur couleur ni la mise en page, et une copie à pleine résolution les redessine au même endroit, dans le même style, coupée dans les listes qui défilent. Au passage, cette copie se recopiait elle-même : en enfant de l'objet du HUD, Unity la rattachait au même document. Et les chiffres de touche du pack (Grenze) n'étaient pas alignés : ses chiffres à l'ancienne descendent sous la ligne ; LT Museum les remplace. Enfin, l'heure et les titres gardaient un halo : leur contour d'un pixel de jeu faisait trois pixels d'écran ; ces contours sont retirés, et chaque texte est calé sur un pixel entier.
Clips :
- 2026-11-20_interface-pixel_01.png (image) : le café avec l'interface pixelisée et ses textes nets
- 2026-11-20_interface-pixel_02.png (image) : gros plan sur les cases unies des numéros
- 2026-11-20_interface-pixel_03.png (image) : le livre de recettes
Avant et après : 2026-11-20_jour-nuit-cafe_02.png (interface lisse) et 2026-11-20_interface-pixel_01.png (interface pixel).
Suite : passer aussi les grandes fenêtres au violet, si besoin.
