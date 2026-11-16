# 01\_Pitch — Moonbrew

Nov 15, 2026 · @Ileana

## Concept

Moonbrew est un jeu de vie cozy en 3D pixelisée où tu es la patronne d'un café fantasy : tu cultives tes ingrédients, élèves tes animaux et pêches le matin, tu cuisines et tu sers en journée (ou ton employé le fait), puis tu explores, tu accomplis des quêtes, tu pars en forêt ou à la mine et tu tisses des liens avec les habitants le soir, jusqu'à te marier avec l'un d'eux.

Accroche de travail : *ta ferme nourrit ton café, ton café nourrit ton village.*

## Les 4 piliers

Chaque feature doit servir au moins un de ces piliers ; sinon elle va dans la liste « pas maintenant ».

1. **Une boucle où tout s'alimente.** La ferme, la pêche, la forêt et la mine donnent des ingrédients, les ingrédients des recettes de 1 à 5 étoiles, les recettes des clients, les clients de l'argent, des quêtes et des relations, qui débloquent de nouvelles recettes, de nouvelles zones et de la déco.
2. **Liberté de rythme.** Personne n'est forcé de tenir le café. Ton employé travaille même en ton absence, mais tu rapportes plus quand tu es là : meilleure cuisine, recettes spéciales, clients importants.
3. **Un service rapide, des histoires ailleurs.** Au café, la phase de travail reste fluide (commande, préparation, service, au plus une courte réplique). Les quêtes et les relations vivent le soir, en balade.
4. **Un jeu qui grandit.** Tout le contenu (ingrédients, recettes, clients, quêtes) est de la donnée dans des ScriptableObjects : on ajoute du contenu sans toucher au code.

## Une journée dans Moonbrew

La journée suit quatre phases, chacune ouverte au joueur sans être obligatoire.

1. **Matin : la ferme.** Récolte, arrosage, soin des animaux, pêche. Les produits partent au stock du café.
2. **Journée : le service.** La patronne prend les commandes, prépare et sert. Si elle est ailleurs, l'employé tient le comptoir avec les recettes basiques.
3. **Soir : le village et l'aventure.** Balade dans le village, quêtes, cadeaux et relations avec les habitants, achats, décoration ; ou expédition en forêt ou à la mine.
4. **Nuit : le bilan.** Résumé de la journée (clients servis, argent gagné, stocks consommés, relations gagnées) et sauvegarde.

Les phases sont des repères, pas des cases : la joueuse fait ce qu'elle veut de sa journée, dans l'ordre qu'elle veut, et une expédition peut durer aussi longtemps qu'elle le souhaite pendant que l'employé tient le café. Seule règle : rentrer dormir à la fin de la journée. Sinon, elle perd de l'argent ou une partie de son inventaire, et commence le lendemain avec moins de stamina.

Plus tard, d'autres employés prennent le relais et libèrent du temps pour le reste.

## Systèmes clés

### Plats de 1 à 5 étoiles

Chaque recette a un niveau de 1 à 5 étoiles. C'est le seul axe de qualité du jeu.

- **1 étoile** : plat basique, peu d'ingrédients, faciles à trouver ou à produire.
- **5 étoiles** : plat élaboré, beaucoup d'ingrédients, dont des ingrédients rares (élevage, pêche, forêt, mine).
- Plus le plat a d'étoiles, plus il rapporte d'argent et de relation.
- Une recette n'est utilisable au café qu'après l'avoir achetée. Seules quelques recettes 1 étoile sont connues au départ.

### Stock et boutique

La joueuse peut produire tous les ingrédients du café. Si un ingrédient manque, elle peut l'acheter à la boutique du village, 2 à 3 fois plus cher que ce qu'il coûte à produire : la ferme reste rentable, la boutique dépanne. La boutique vend aussi les recettes.

### L'employé

En l'absence de la patronne, l'employé tient le café avec les recettes de 1 étoile uniquement. Le café rapporte donc moins, mais ne ferme jamais. Le service de l'employé ne fait pas monter les relations : elles se gagnent en personne.

### Clients

- **Habitants du village** : on les croise aussi en balade ; on peut nouer une relation avec eux et ils donnent des quêtes.
- **Clients de passage** : aventuriers, créatures et voyageurs qu'on ne croise qu'au café ; ils font tourner le service et rapportent de l'argent, mais ne donnent pas de quêtes.

### Relations

- Servir un habitant au café fait monter la relation, d'autant plus que le plat a d'étoiles. Ça reste sans long dialogue, pour garder un service fluide.
- **Goûts** : chaque habitant a des plats et des ingrédients qu'il aime ou n'aime pas. On les découvre au fil de ses visites au café : ses répliques sont des indices. Rien n'est noté pour la joueuse : c'est à elle de s'en souvenir. Servir ce qu'il aime fait monter davantage la relation.
- **Cadeaux** : on peut offrir des cadeaux aux habitants, ce qui fait monter la relation, au maximum 2 par semaine et par habitant. Leurs goûts s'appliquent aussi aux cadeaux.
- Les quêtes et discussions du soir font aussi monter la relation et racontent l'histoire de chaque habitant.
- Certains habitants permettent une relation plus poussée : romance, puis mariage.

### Activités

- **Ferme** : cultures, puis animaux d'élevage (œufs, lait…).
- **Pêche** : poissons et ingrédients aquatiques.
- **Cuisine** : transformer les ingrédients et préparer les recettes.
- **Forêt et mine** : zones d'expédition avec un combat simple contre des monstres, pour récupérer des ressources rares (ingrédients de monstres, minerais, cristaux).

### Aménagement et décoration

- **Café** : placer librement les tables, le comptoir et les meubles, et décorer.
- **Maison** : décorer et meubler.

## Monde et ton

Le monde est une fantasy douce et cozy : les clients sont des aventuriers, des créatures et des voyageurs, et les ingrédients de la ferme ont des propriétés magiques.

- **Le cadre** : un café fantasy au cœur d'un village.
- **Le ton** : doux et poétique.
- **L'histoire** : la joueuse hérite du café de ses parents, et avec lui d'un mystère à découvrir au fil du jeu. Elle en est désormais la patronne et choisit combien de temps elle y passe.

Premier PNJ : l'employé, qui sert aussi de tutoriel (nom et personnalité à définir).

## Direction visuelle

Moonbrew vise le rendu de *A Short Hike* : un monde 3D low-poly affiché en basse résolution, avec de gros pixels.

- **Rendu** : Unity URP, caméra 3/4 vue d'en haut, rendu dans une Render Texture basse résolution (référence : 640x360, soit ×3 en 1080p et ×4 en 1440p) agrandie sans lissage, par un facteur entier avec des bandes noires si besoin. Un mode natif (rendu net) reste possible.
- **Look** : formes douces, aplats de couleur, une palette par zone, éclairage doux, contours discrets.
- **Stabilité** : caméra calée sur la grille de pixels pour éviter le scintillement.
- **Assets** : packs Synty POLYGON recolorés avec la palette de leur zone, plus des modèles maison pour l'identité du jeu (café, créatures, plats).
- **Personnages** : 3D low-poly, rendus en pixel comme le reste du jeu (décision prise).

## Ce qui distingue Moonbrew

La base « ferme + café » existe déjà : plusieurs jeux la proposent, dont certains sortis cette année. Moonbrew doit se démarquer par l'exécution et par quatre points qu'on n'a pas trouvés réunis dans ces jeux (recherche non exhaustive, état au 15 novembre 2026).

| Jeu | Ce qu'il propose | Ce que Moonbrew ajoute |
| --- | --- | --- |
| [Farm & Feast](https://store.steampowered.com/app/4521650/Farm_Feast/) | Ferme et petit restaurant, accès anticipé depuis le 11 novembre 2026 | Univers fantasy, quêtes et relations avec les PNJ |
| [Harvest Cafe](https://store.steampowered.com/app/2762360/Harvest_Cafe/) | Ferme, restaurant et exploration d'une île, avec cuisiniers et serveurs à embaucher | Patronne libre de s'absenter, avec un impact sur le revenu ; monde fantasy |
| [Tailside](https://store.steampowered.com/app/2922570) | Café, décoration et histoires de village, tag fantasy et pixel art | Ferme qui alimente le café ; rendu 3D pixelisé |
| [Coffee Talk](https://baristamagazine.com/the-world-of-coffee-talk) | Café fantasy proche du visual novel | Boucle de gestion et de ferme ; service rapide, histoires en dehors du comptoir |

**Les quatre différenciateurs :** le monde fantasy cohérent, la patronne qui peut s'absenter avec un employé qui travaille quand même, les quêtes du soir séparées d'un service fluide, et le rendu 3D pixelisé façon *A Short Hike*.

## Plateforme, public et périmètre

Moonbrew vise Steam sur PC, pour les joueurs de jeux cozy et de Stardew Valley. Le projet est mené en solo, avec Claude pour le code et la documentation, donc le périmètre doit rester maîtrisable.

- **Café d'abord** : il est le cœur du jeu et doit être excellent ; la ferme commence légère (quelques cultures) et grossit ensuite.
- **Sortie progressive** : une version jouable et publiable à chaque étape, avec l'accès anticipé comme piste à confirmer.
- **Contenu par couches** : nouveaux ingrédients, recettes, clients et quêtes ajoutés au fil des versions, grâce à l'architecture pilotée par les données.
- **Le combat en dernier** : c'est le système le plus coûteux (vie, IA des monstres, équilibrage). Il reste simple, sert surtout à récolter des ressources rares et arrive après le reste.
- **Art** : packs Synty (abonnement pendant le développement, puis achat à l'unité des packs gardés avant la sortie). L'IA n'est utilisée que pour le code, ce qui ne se déclare pas sur Steam.
