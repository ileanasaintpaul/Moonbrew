# 02\_Perimetre\_Vertical\_Slice — Moonbrew

Nov 15, 2026 · @Ileana

## Objectif

Le vertical slice est la plus petite version de Moonbrew où la boucle complète est jouable : cultiver, cuisiner, servir, gagner de l'argent, accomplir une quête. Il est réussi quand une journée entière se joue de bout en bout, sans bug bloquant, et qu'on a envie de la recommencer.

Les chiffres ci-dessous sont des propositions de départ à ajuster, pas des engagements.

## Ce qui est dans le vertical slice

| Système | Contenu du slice | Finition visée |
| --- | --- | --- |
| Rendu et caméra | Une petite zone (café, ferme, un bout de village), caméra 3/4, rendu 3D pixelisé avec une palette par zone, personnages en 3D low-poly | Le look final de référence |
| Joueur | Déplacement, interaction avec les objets | Complet |
| Temps | Horloge et phases matin, journée, soir, nuit, comme repères ; journée libre ; il faut rentrer dormir en fin de journée, sinon perte d'argent ou d'une partie de l'inventaire et stamina réduite le lendemain ; pas de saisons | Complet pour une journée |
| Inventaire | Objets, quantités, stock du café | Fonctionnel |
| Ferme | 2 cultures (blé et un fruit), plantation, arrosage, récolte ; pas d'animaux | Fonctionnel |
| Cuisine | Blé vers farine, puis 3 recettes : 1 boisson 1 étoile et 1 gâteau 1 étoile connus au départ, 1 gâteau 2 étoiles à acheter | Fonctionnel |
| Café | Commande, préparation, service ; 3 types de clients de passage et 2 habitants, avec une réplique | Fonctionnel, simple |
| Employé | 1 employé qui travaille en l'absence de la patronne, avec les recettes 1 étoile uniquement ; revenu plus élevé quand la patronne est là | Fonctionnel |
| PNJ et relations | 2 habitants avec leurs goûts ; niveau de relation qui monte quand on les sert, plus avec un plat 2 étoiles ou un plat qu'ils aiment ; leurs répliques au café donnent des indices sur leurs goûts | Minimal |
| Quêtes | 1 quête lancée par un habitant | Minimal |
| Économie | Argent, boutique de graines, de recettes et d'ingrédients de dépannage (2 à 3 fois plus chers que produits) | Fonctionnel |
| Bilan et sauvegarde | Bilan de fin de journée, sauvegarde versionnée | Fonctionnel |

## Après le slice : feuille de route

Ces éléments sont exclus du slice et reviennent après validation de la boucle, dans cet ordre. Chaque étape ajoute un système et le contenu qui va avec.

1. **Relations et quêtes.** Plus d'habitants, quêtes de chaque habitant, cadeaux (2 par semaine et par habitant au maximum), paliers d'amitié, recettes jusqu'à 3 étoiles.
2. **Aménagement et décoration.** Placement libre des tables, du comptoir et des meubles du café ; décoration de la maison.
3. **Élevage et pêche.** Animaux de ferme et leurs produits, pêche, nouveaux ingrédients pour les plats 4 étoiles.
4. **Forêt et mine.** Zones d'expédition, combat simple, ressources de monstres et minerais, plats 5 étoiles.
5. **Romance et mariage.** Relations poussées avec certains habitants.

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

## Questions ouvertes

- [ ] Nom et personnalité de l'employé, premier PNJ et tutoriel
- [ ] Écart de revenu entre patronne et employé : quelle proportion pour que la ferme et les quêtes restent aussi attractives que le café ?
- [ ] Stamina : quelles actions la consomment, et que se passe-t-il quand elle est vide ?
- [ ] Ne pas dormir : quelle part d'argent ou d'inventaire est perdue ?
