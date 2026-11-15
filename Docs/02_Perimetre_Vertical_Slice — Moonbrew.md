# 02\_Perimetre\_Vertical\_Slice — Moonbrew

Nov 15, 2026 · @Ileana

## Objectif

Le vertical slice est la plus petite version de Moonbrew où la boucle complète est jouable : cultiver, cuisiner, servir, gagner de l'argent, accomplir une quête. Il est réussi quand une journée entière se joue de bout en bout, sans bug bloquant, et qu'on a envie de la recommencer.

Les chiffres ci-dessous sont des propositions de départ à ajuster, pas des engagements.

## Ce qui est dans le vertical slice

| Système | Contenu du slice | Finition visée |
| --- | --- | --- |
| Rendu et caméra | Une petite zone (café, ferme, un bout de village), caméra 3/4, rendu 3D pixelisé, palette commune | Le look final de référence |
| Joueur | Déplacement, interaction avec les objets | Complet |
| Temps | Horloge et phases matin, journée, soir, nuit ; pas de saisons | Complet pour une journée |
| Inventaire | Objets, quantités, stock du café | Fonctionnel |
| Ferme | 2 cultures (blé et un fruit), plantation, arrosage, récolte ; pas d'animaux | Fonctionnel |
| Cuisine | Blé vers farine, puis 3 recettes (1 boisson, 2 gâteaux) | Fonctionnel |
| Café | Commande, préparation, service ; 5 types de clients avec une réplique | Fonctionnel, simple |
| Employé | 1 employé avec recettes de base, qui travaille en l'absence de la patronne ; revenu plus élevé quand la patronne est là | Fonctionnel |
| PNJ et quêtes | 2 PNJ, 1 quête lancée par un client | Minimal |
| Économie | Argent, boutique de graines et d'ingrédients | Fonctionnel |
| Bilan et sauvegarde | Bilan de fin de journée, sauvegarde versionnée | Fonctionnel |

## Pas maintenant

Ces éléments sont volontairement exclus du slice. Ils reviennent après validation de la boucle.

- Animaux et produits d'élevage
- Saisons et météo
- Plats salés (le slice se limite aux boissons et aux gâteaux)
- Embauche de personnel supplémentaire et progression des employés
- Décoration libre de la maison et du café
- Plusieurs mondes ou plusieurs zones, grande map à explorer
- Relations poussées : cadeaux, niveaux d'amitié, romance
- Mini-jeux, pêche, combat
- Traduction, manette, co-op
- Musique et sons finaux (des sons provisoires suffisent)

## Ordre de construction

Chaque étape se termine par une version qui tourne ; on ne passe à la suivante que si le critère est atteint.

1. **Fondations et rendu.** Déplacement, caméra 3/4 calée sur la grille, rendu pixelisé, horloge, inventaire, sauvegarde. *Critère : une scène avec le café, un sol et un personnage a le look A Short Hike que tu veux.*
2. **Ferme et cuisine.** Cultures, récolte, farine, recettes. *Critère : tu peux produire un gâteau à partir de graines.*
3. **Café et employé.** Clients, commandes, service, argent, employé automatique. *Critère : une journée de service rapporte de l'argent, avec ou sans la patronne.*
4. **PNJ, quête et bilan.** Deux PNJ, une quête lancée par un client, bilan de fin de journée. *Critère : une journée complète se joue de bout en bout et donne envie de la refaire.*

## Questions ouvertes

- [ ] Cadre du café : entre deux mondes, café-auberge au carrefour, ou café de héros à la retraite ?
- [ ] Ton du jeu : doux et poétique, ou drôle et décalé ?
- [ ] Personnages : 3D low-poly ou sprites 2D dans un décor 3D ?
- [ ] Café hérité (avec un petit mystère) ou fondé par la patronne ?
- [ ] Nom et personnalité de l'employé, premier PNJ et tutoriel
- [ ] Écart de revenu entre patronne et employé : quelle proportion pour que la ferme et les quêtes restent aussi attractives que le café ?
