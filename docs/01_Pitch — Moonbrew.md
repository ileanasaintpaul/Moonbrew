# 01\_Pitch — Moonbrew

Nov 15, 2026 · @Ileana

## Concept

Moonbrew est un jeu de vie cozy en 3D pixelisée où tu es la patronne d'un café fantasy : tu cultives tes ingrédients le matin, tu cuisines et tu sers en journée (ou ton employé le fait), puis tu explores, tu accomplis des quêtes et tu tisses des liens avec les habitants le soir.

Accroche de travail : *ta ferme nourrit ton café, ton café nourrit ton village.*

## Les 4 piliers

Chaque feature doit servir au moins un de ces piliers ; sinon elle va dans la liste « pas maintenant ».

1. **Une boucle où tout s'alimente.** Les cultures donnent des ingrédients, les ingrédients des recettes, les recettes des clients, les clients de l'argent, des quêtes et des relations, qui débloquent de nouvelles recettes et de la déco.
2. **Liberté de rythme.** Personne n'est forcé de tenir le café. Ton employé travaille même en ton absence, mais tu rapportes plus quand tu es là : meilleure cuisine, recettes spéciales, clients importants.
3. **Un service rapide, des histoires ailleurs.** Au café, la phase de travail reste fluide (commande, préparation, service, au plus une courte réplique). Les quêtes et les relations vivent le soir, en balade.
4. **Un jeu qui grandit.** Tout le contenu (ingrédients, recettes, clients, quêtes) est de la donnée dans des ScriptableObjects : on ajoute du contenu sans toucher au code.

## Une journée dans Moonbrew

La journée suit quatre phases, chacune ouverte au joueur sans être obligatoire.

1. **Matin : la ferme.** Récolte, arrosage, soin des animaux. Les produits partent au stock du café.
2. **Journée : le service.** La patronne prend les commandes, prépare et sert. Si elle est ailleurs, l'employé tient le comptoir avec les recettes de base.
3. **Soir : le village.** Balade dans la map, quêtes, cadeaux et relations avec les PNJ, achats, décoration.
4. **Nuit : le bilan.** Résumé de la journée (clients servis, argent gagné, stocks consommés) et sauvegarde.

Plus tard, d'autres employés prennent le relais du service et libèrent du temps pour le reste.

## Monde et ton

Le monde est une fantasy douce et cozy : les clients sont des aventuriers, des créatures et des voyageurs, et les ingrédients de la ferme ont des propriétés magiques. Deux choix restent ouverts.

- **Le cadre du café** : un café entre deux mondes (une porte magique, un monde de plus = un pack de contenu), un café-auberge au carrefour (le monde vient à toi, peu de map), ou un café de héros à la retraite (plus humoristique).
- **Le ton** : plutôt doux et poétique, ou plutôt drôle et décalé.

Le café est repris ou fondé par la joueuse, qui en est la patronne et choisit combien de temps elle y passe. Premier PNJ : l'employé, qui sert aussi de tutoriel (nom et personnalité à définir).

## Direction visuelle

Moonbrew vise le rendu de *A Short Hike* : un monde 3D low-poly affiché en basse résolution, avec de gros pixels.

- **Rendu** : Unity URP, caméra 3/4 vue d'en haut, rendu dans une Render Texture basse résolution (référence : 480x270) agrandie sans lissage.
- **Look** : formes douces, aplats de couleur, palette réduite commune à tous les assets, éclairage doux, contours discrets.
- **Stabilité** : caméra calée sur la grille de pixels pour éviter le scintillement.
- **Assets** : packs 3D low-poly recolorés avec la palette du jeu, plus des modèles maison pour l'identité du jeu (café, créatures, plats).
- **Personnages** : 3D low-poly ou sprites 2D dans un décor 3D (style Octopath). À trancher après le prototype de rendu.

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
- **Art** : si de l'IA est utilisée pour une partie des assets, Steam impose de le déclarer sur la page du jeu.
