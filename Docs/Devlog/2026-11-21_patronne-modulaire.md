---
date: 2026-11-21
titre: Une patronne en pièces détachées
tickets:
format: court
---
But : refaire la création de la patronne comme sur les écrans du pack Fantasy Menus : des hauts et des bas différents, des jauges pour le corps, beaucoup de coiffures et de barbes, et un personnage arrondi plutôt qu'en polygones.
Résultat :
- La patronne est un personnage Sidekick (Synty) : chaque pièce est un modèle à part sur un squelette commun. 7 hauts, 6 bas et 10 chapeaux de Fantasy Villagers, 48 coiffures, 48 barbes et 10 sourcils (Villagers, base humaine, Vikings, Pirates, Outlaws, Knights, Sci-Fi Civilians).
- Body : trois jauges, masculin à féminin, mince à costaud, menu à musclé, comme l'écran de réglages du pack.
- Une page par sujet, avec des icônes : Body, Face, Hair, Beard, Top, Bottom, Hat et le nom. Chaque couleur se choisit sous sa page (la peau sous Body, les sourcils sous Face...) ; le haut et le bas gardent leurs couleurs d'origine ou prennent une teinte parmi 16. La caméra se rapproche du visage pour Face, Hair, Beard et Hat.
- On fait pivoter la patronne en la faisant glisser à la souris.
- Le panneau est en perspective, son côté gauche le plus proche, et le ciel du menu principal passe, très pâle, derrière la patronne.
- Plus d'accessoires, de bouton Random ni de Back.
Moment fort : le curseur Body Size qui fait grossir la patronne en direct, tenue comprise.
Problème et solution : l'outil Sidekick cherche ses pièces dans le dossier Assets et ouvre sa base de données à un chemin fixe, ce qui ne marche ni depuis le paquet privé ni dans une build. La patronne est donc montée par notre code : les modèles des pièces sont accrochés au squelette (les os d'une jupe ou de cheveux longs sont ajoutés au besoin), et sa petite texture de couleurs est composée case par case. Les premiers essais avaient des trous : chaque pièce a deux sous-maillages, et le matériau n'allait que sur le premier. Dans les vignettes, les cheveux longs se dressaient en antennes : toutes les coiffures nomment leurs os de mèches pareil, à des places différentes, et le modèle des vignettes gardait celles de la première coiffure essayée.
Clips :
- 2026-11-21_patronne-modulaire_01.png (image) : la page Body et ses jauges
- 2026-11-21_patronne-modulaire_02.png (image) : les barbes, visage zoomé
- 2026-11-21_patronne-modulaire_03.png (image) : la patronne dans la ferme
Suite : retoucher les icônes de la tenue et du chapeau, et proposer la tête et le nez dans Face.
