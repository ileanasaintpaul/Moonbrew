---
date: 2026-11-23
titre: On prend les commandes
tickets: VS-26
format: court
---
But : aller parler à une table prête pour prendre la commande de tout le monde (VS-26).
Résultat :
- Un clic sur la bulle « ! », quand la patronne est à côté de la table, prend la commande de toute la table.
- Chaque client choisit parmi les recettes connues : un plat qu'il aime d'abord, sinon un plat qui le laisse neutre, et un plat qu'il déteste seulement s'il n'y a rien d'autre.
- La bulle « ! » laisse place à une bulle au-dessus de la tête de chaque client, avec le plat qu'il a choisi, et un rappel par table s'affiche en haut à gauche de l'écran.
- Une table qui choisit encore n'a pas de bulle : il ne se passe rien. Trop loin, ou avec le menu ouvert, le clic ne fait rien non plus.
- Le service n'existe pas encore : après la commande, la table attend une heure, puis repart et son rappel disparaît.
Moment fort : la table de trois dont le « ! » s'efface, et chacun qui montre son plat au-dessus de sa tête.
Problème et solution : le café n'a pas le droit de connaître les recettes, qui sont dans le module cuisine. Il demande donc le menu par un événement, et la cuisine y ajoute les plats connus.
Clips :
- 2026-11-23_prise-de-commande_01.png (image) : une table de trois, chaque client avec son plat en bulle au-dessus de la tête, et le rappel en haut à gauche
Suite : préparer la commande, puis la servir et faire payer la table.
