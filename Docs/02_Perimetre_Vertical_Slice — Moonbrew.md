# 02\_Perimetre\_Vertical\_Slice — Moonbrew

Nov 15, 2026 · @Ileana

## Objectif

Le vertical slice est la plus petite version de Moonbrew où la boucle complète est jouable : cultiver, cuisiner, servir, gagner de l'argent, accomplir une quête. Il est réussi quand une journée entière se joue de bout en bout, sans bug bloquant, et qu'on a envie de la recommencer.

Les chiffres ci-dessous sont des propositions de départ à ajuster, pas des engagements.

## Ce qui est dans le vertical slice

| Système | Contenu du slice | Finition visée |
| --- | --- | --- |
| Rendu et caméra | Une petite zone (café, ferme, un bout de village), caméra 3/4, rendu 3D pixelisé avec une palette par zone, personnages en 3D low-poly | Le look final de référence |
| Joueur | Déplacement, interaction avec les objets ; barre d'endurance, que les outils de la ferme consomment | Complet |
| Temps | Horloge et phases matin, journée, soir, nuit, comme repères ; journée libre ; il faut rentrer dormir en fin de journée, sinon perte d'argent ou d'une partie de l'inventaire et stamina réduite le lendemain ; pas de saisons | Complet pour une journée |
| Inventaire | Objets, quantités ; sac de la ferme et inventaire de la patronne au café (10 cases chacun), frigo du café comme gros stock | Fonctionnel |
| Ferme | 2 cultures (blé et un fruit), plantation, arrosage, récolte ; pas d'animaux ; caisse d'expédition vers le frigo du café (livrée le lendemain), ou ingrédients apportés soi-même au garde-manger | Fonctionnel |
| Cuisine | Blé vers farine, puis 3 recettes : 1 boisson 1 étoile et 1 gâteau 1 étoile connus au départ, 1 gâteau 2 étoiles à acheter ; vitrine où un gâteau donne plusieurs parts | Fonctionnel |
| Café | Clients seuls ou en groupe qui s'assoient à une table vide, choisissent, puis montrent une bulle « ! » ; on prend la commande de la table en leur parlant, et on la sert quand l'inventaire de la patronne la contient en entier ; 3 types de clients de passage et 2 habitants, avec une réplique | Fonctionnel, simple |
| Encyclopédie | Onglet du menu du jeu : recettes cuisinées et cultures récoltées, avec silhouettes et progression ; les poissons arrivent avec la pêche | Minimal |
| Employé | 1 employé qui travaille en l'absence de la patronne, avec les recettes 1 étoile uniquement ; revenu plus élevé quand la patronne est là | Fonctionnel |
| PNJ et relations | 2 habitants avec leurs goûts ; niveau de relation qui monte quand on les sert, plus avec un plat 2 étoiles ou un plat qu'ils aiment ; leurs répliques au café donnent des indices sur leurs goûts | Minimal |
| Quêtes | 1 quête lancée par un habitant | Minimal |
| Économie | Argent, boutique de graines, de recettes et d'ingrédients de dépannage (2 à 3 fois plus chers que produits) | Fonctionnel |
| Bilan et sauvegarde | Bilan de fin de journée, sauvegarde versionnée | Fonctionnel |
| Interface | HUD et fenêtres avec les assets Interface Fantasy (barre d'objets, heure et date, argent, fenêtres à cadre doré) ; dialogues avec le portrait du personnage et ses expressions ; écran de chargement au lancement | Le look final de référence |

## Après le slice : feuille de route

Ces éléments sont exclus du slice et reviennent après validation de la boucle, dans cet ordre. Chaque étape ajoute un système et le contenu qui va avec.

1. **Relations et quêtes.** Plus d'habitants, quêtes de chaque habitant, cadeaux (2 par semaine et par habitant au maximum), paliers d'amitié, recettes jusqu'à 3 étoiles.
2. **Personnage et progression.** Personnalisation de l'apparence (au lancement d'une partie, puis auprès d'un personnage du village). Niveaux et expérience gagnée dans toutes les activités. Points à placer dans un arbre de compétences, avec une branche par activité. Récompenses de niveau (argent, recettes, objets). Emplacements d'équipement sans effet sur l'apparence. La barre de vie arrive avec le combat, à l'étape Forêt et mine.
3. **Aménagement et décoration.** Placement libre des tables, du comptoir et des meubles du café ; décoration de la maison.
4. **Élevage et pêche.** Animaux de ferme et leurs produits, pêche, nouveaux ingrédients pour les plats 4 étoiles.
5. **Forêt et mine.** Zones d'expédition, combat simple, ressources de monstres et minerais, plats 5 étoiles.
6. **Romance et mariage.** Relations poussées avec certains habitants.

Sans ordre fixé :

- Saisons et météo
- Plats salés (le slice se limite aux boissons et aux gâteaux)
- Embauche de personnel supplémentaire et progression des employés
- Plusieurs mondes, grande map à explorer
- Mini-jeux
- Traduction, manette, co-op
- Musique et sons finaux (des sons provisoires suffisent)

## Ordre de construction

Chaque étape se termine par une version qui tourne ; on ne passe à la suivante que si le critère est atteint.

1. **Fondations et rendu.** Déplacement, caméra 3/4 calée sur la grille, rendu pixelisé, horloge, inventaire, sauvegarde. *Critère : une scène avec le café, un sol et un personnage a le look A Short Hike que tu veux.*
2. **Ferme et cuisine.** Cultures, récolte, farine, recettes. *Critère : tu peux produire un gâteau à partir de graines.*
3. **Café et employé.** Clients, commandes, service, argent, employé automatique. *Critère : une journée de service rapporte de l'argent, avec ou sans la patronne.*
4. **PNJ, relations, quête et bilan.** Deux habitants avec un niveau de relation, une quête lancée par un habitant, bilan de fin de journée. *Critère : une journée complète se joue de bout en bout et donne envie de la refaire.*

## Décisions prises

- [x] Cadre : un café fantasy dans un village.
- [x] Ton : doux et poétique.
- [x] Personnages : 3D low-poly, rendus en pixel comme le reste du jeu.
- [x] Rendu : une palette par zone, désactivable ; un mode natif (rendu net) reste possible.
- [x] Histoire : café hérité des parents, avec un mystère.
- [x] Le service de l'employé ne fait pas monter les relations.
- [x] Seuls les habitants donnent des quêtes.
- [x] Expéditions : aussi longues que la joueuse le souhaite, à n'importe quel moment ; seule contrainte, rentrer dormir en fin de journée.
- [x] Ingrédients en boutique : 2 à 3 fois plus chers que produits.
- [x] Cadeaux : 2 par semaine et par habitant.
- [x] Goûts découverts : rien n'est noté, la joueuse doit s'en souvenir.
- [x] IA : utilisée uniquement pour le code, ce qui ne se déclare pas sur Steam.
- [x] Ferme → café : caisse d'expédition livrée le lendemain dans le frigo, ou ingrédients apportés soi-même au garde-manger pour le jour même.
- [x] Au café, la barre passe sur l'inventaire de la patronne au café (10 cases) ; on pioche dans le frigo.
- [x] Une commande se sert par table, et seulement quand l'inventaire contient toute la commande de la table.
- [x] Gâteaux en vitrine : 6 parts par défaut, réglable par recette.
- [x] Encyclopédie dans le slice, en version minimale.
- [x] Ferme : tout est parallèle à la maison (vue isométrique).
- [x] Personnage et progression (apparence, niveaux, arbre de compétences, équipement) : après le slice. Dans le slice, seule l'endurance est là, consommée par les outils de la ferme.
- [x] L'épée ne consomme pas d'endurance. La pêche et la mine en consomment.
- [x] L'expérience vient de toutes les activités : café, ferme, cuisine, quêtes, relations, combat, exploration.
- [x] L'arbre de compétences a une branche par activité.
- [x] L'équipement ne change pas l'apparence du personnage.
- [x] Langue : le jeu est entièrement en anglais. Les traductions, dont le français, viendront plus tard (VS-60).

## Questions ouvertes

- [ ] Nom et personnalité de l'employé, premier PNJ et tutoriel
- [ ] Écart de revenu entre patronne et employé : quelle proportion pour que la ferme et les quêtes restent aussi attractives que le café ?
- [ ] Stamina : que se passe-t-il quand elle est vide ? (Elle est consommée par les outils de la ferme, la pêche et la mine, pas par l'épée.)
- [ ] Ne pas dormir : quelle part d'argent ou d'inventaire est perdue ?
