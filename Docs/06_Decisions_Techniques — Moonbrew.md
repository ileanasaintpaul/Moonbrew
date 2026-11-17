# 06\_Decisions\_Techniques — Moonbrew

Nov 17, 2026 · état du projet après VS-13

Ce document explique les choix techniques faits jusqu'ici. Pour chacun, il donne les raisons du choix, mais aussi ses limites et ses risques, sans les cacher : le but est un projet de qualité, optimisé et réfléchi, donc chaque décision doit pouvoir être remise en cause avec de bons arguments.

Chaque section suit le même plan :

- **Décision** : ce qui a été fait.
- **Pourquoi c'est un bon choix** : les arguments.
- **Limites et risques** : ce qui peut mal tourner.
- **Ce qu'on fait pour les limiter** : en place, ou à faire.
- **Alternatives écartées** : et pourquoi.

La dernière partie donne des mesures réelles de la scène de la ferme et une liste de dette technique, par priorité.

## 1. Architecture en modules et assemblies

**Décision.** Le code est découpé en modules (`Core`, `Inventory`, `Player`, `Farming`, `Kitchen`, `Cafe`, `Employee`, `Economy`, `Npc`, `Quests`, `Rendering`, `Ui`), chacun avec son assembly (`.asmdef`). Les dépendances vont dans un seul sens : `Ui` → modules de fonctionnalité → modules de base (`Core`, puis `Inventory`). Un module de fonctionnalité ne référence jamais un autre module de fonctionnalité.

**Pourquoi c'est un bon choix.**

- **Les règles sont vérifiées par le compilateur.** Si `Cafe` essaie d'utiliser une classe de `Farming`, ça ne compile pas. Une règle écrite dans un document finit toujours par être contournée ; une règle imposée par le compilateur, non.
- **Compilation plus rapide.** Unity ne recompile que l'assembly modifié et ceux qui en dépendent, pas tout le projet.
- **Tests ciblés.** Chaque assembly de test ne référence que ce qu'il teste.
- **Des modules remplaçables.** On peut réécrire la cuisine sans toucher au café, tant que les événements échangés ne changent pas.

**Limites et risques.**

- **Plus de fichiers et de friction au début.** Il faut ajouter une référence d'assembly à chaque nouveau besoin, et les tests doivent référencer chaque module qu'ils testent.
- **`Core` peut grossir.** Tout ce qui doit être partagé (événements, types communs) finit dans `Core`. Sans vigilance, `Core` devient un fourre-tout dont tout dépend.
- **Douze modules pour un jeu solo, c'est un découpage ambitieux.** Certains modules ne contiendront que quelques classes pendant longtemps.

**Ce qu'on fait pour les limiter.** `Core` ne contient que ce qui est vraiment transversal : horloge, bus, types d'événements, et plus tard sauvegarde et état du jeu. Un module vide ne coûte presque rien. On ne le supprimera que s'il reste vide après le vertical slice.

**Alternatives écartées.**

- **Un seul assembly (`Assembly-CSharp`).** C'est plus simple au départ, mais rien n'empêche les dépendances croisées, et tout recompile à chaque modification.
- **Des namespaces sans assemblies.** On organise le code, mais rien n'est vérifié.

## 2. Bus d'événements typé dans un asset

**Décision.**

- Les modules communiquent par un bus d'événements unique, `SO_EventBus` (classe `EventBusAsset`, qui enveloppe un `EventBus` en C# pur).
- Les événements sont des `readonly struct` déclarées dans `Core` (`GameEvents.cs`) : `DayStarted`, `DayEnded`, `PhaseChanged`, `TimeChanged`, `Interacted`.
- On publie avec `Publish(new DayStarted(1))`, on s'abonne avec `Subscribe<DayStarted>(...)` dans `OnEnable`, et on se désabonne dans `OnDisable`.
- Chaque composant reçoit le bus par une référence dans l'inspecteur, ou par `Init` dans les tests.

**Pourquoi c'est un bon choix.**

- **Découplage réel.** L'horloge (`Core`) ne sait pas que le HUD (`Ui`) existe, et le HUD n'a aucune référence vers l'horloge. Ajouter un auditeur, par exemple les cultures qui poussent à chaque `DayStarted`, ne demande aucune modification de l'émetteur. C'est exactement ce que la règle d'architecture demande, et c'est le critère de VS-13.
- **Pas de singleton.** Le bus n'est pas une variable statique globale. C'est un asset qu'on voit dans l'inspecteur : la dépendance est explicite, et un test peut créer son propre bus isolé (`ScriptableObject.CreateInstance<EventBusAsset>()`). Les tests ne se polluent donc pas entre eux.
- **Typage fort.** Un abonné à `DayStarted` reçoit un `DayStarted`, avec ses champs. Une faute de frappe ne compile pas, contrairement à un bus à base de chaînes (« day_started »).
- **Performances correctes.** Les événements sont des structs passées à des `Action<T>` génériques, donc pas de boxing ni d'allocation à la publication. Une publication coûte une recherche dans un dictionnaire, un cast et l'appel des délégués. Seuls l'abonnement et le désabonnement allouent, une fois par activation.
- **Robuste pendant une publication.** Les délégués sont immuables : un abonné qui se désabonne pendant une publication ne casse pas la boucle. Un test le vérifie.
- **Propre entre deux sessions Play.** Un asset survit au mode Play dans l'éditeur, donc le bus se vide quand on quitte le mode Play : un abonnement oublié ne fuit pas dans la session suivante.
- **Un seul asset à brancher**, au lieu d'un asset par événement.

**Limites et risques.**

- **Échec silencieux si on oublie de brancher le bus.** Pour l'instant, un composant dont le champ `eventBus` est vide ne publie rien, sans erreur. C'est le risque le plus concret aujourd'hui : un oubli dans une scène, et une fonctionnalité ne réagit plus, sans message.
- **On voit mal qui écoute quoi.** Avec un bus, on ne peut plus suivre une chaîne d'appels dans l'IDE : « qui réagit à `DayStarted` ? » demande une recherche dans le code. Sur un gros projet, c'est la critique principale du pattern.
- **Un abonné qui arrive en retard rate les événements passés.** Le HUD n'affiche rien tant que l'horloge n'a pas publié. Ça marche aujourd'hui parce que l'horloge publie dans `Start`, après tous les `OnEnable`. Mais un HUD ouvert en cours de partie, ou une scène chargée plus tard, attendrait le prochain événement.
- **L'ordre entre abonnés n'est pas fiable.** Les abonnés sont appelés dans l'ordre d'abonnement, qui dépend de l'ordre des `OnEnable`, que Unity ne garantit pas entre objets. Un système qui dépendrait d'un autre auditeur « déjà passé » serait fragile.
- **Une exception arrête la chaîne.** Si un abonné lève une exception, les abonnés suivants ne reçoivent pas l'événement.
- **`Core` connaît le vocabulaire de tout le jeu.** Tous les types d'événements y sont déclarés, y compris `OrderServed` ou `CropHarvested` plus tard. C'est voulu (contrat partagé), mais `GameEvents.cs` va grossir.
- **Le bus ne sert pas aux questions.** Un bus diffuse des faits (« la journée a commencé »). Pour une question qui attend une réponse, comme « combien d'argent j'ai ? », il faut lire un état, pas publier un événement. Utiliser le bus pour ça produirait des allers-retours difficiles à suivre.
- **Thread principal seulement.** Le bus n'est pas thread-safe. Ce n'est pas un problème en Unity tant qu'on ne publie pas depuis un job ou une tâche asynchrone.

**Ce qu'on fait pour les limiter.**

- **Fait** : des tests sur le bus (ordre, désabonnement pendant une publication, types séparés, nettoyage), et le bus vidé en sortie du mode Play.
- **À faire, priorité haute** : un test qui parcourt les scènes et les prefabs et échoue si un champ `EventBusAsset` est vide. On peut aussi ajouter un avertissement dans `OnValidate`. Ça supprime le risque d'échec silencieux.
- **À faire** : une petite fenêtre de debug, ou un log activable, qui liste les abonnés par type d'événement et trace les publications. Ça compense le manque de visibilité.
- **À faire quand un cas apparaîtra** : un mode « dernier état connu » pour les événements qui décrivent un état (jour, phase, heure). Un nouvel abonné recevrait alors immédiatement la dernière valeur. L'autre solution est de donner au HUD un accès en lecture à l'état du jeu.
- **Règles d'usage** : un événement est un fait passé et immuable ; un abonné ne compte jamais sur l'ordre des autres ; le bus ne sert pas aux questions.
- **Isoler les exceptions dans chaque abonné** : seulement si le besoin se présente, parce que ça masquerait les erreurs pendant le développement.

**Alternatives écartées.**

- **Événements C# entre composants** : c'est ce qu'on avait avant VS-13. C'est simple et traçable, mais l'émetteur et l'auditeur doivent se connaître, ce qui crée des références directes entre modules, interdites par les règles.
- **UnityEvent** : câblé dans l'inspecteur, donc visible. Mais plus lent, introuvable par une recherche dans le code, et facile à casser en renommant une méthode.
- **Un asset par événement** (pattern « event channel ») : très lisible dans l'inspecteur, mais des dizaines d'assets à créer et à brancher.
- **Bus statique ou singleton** : rien à brancher, mais une dépendance globale cachée, des tests qui se polluent entre eux, et une violation de la règle « pas de singleton ».
- **Framework d'injection de dépendances** (VContainer, Zenject) : puissant, mais lourd à apprendre et à maintenir pour un projet solo, et plus que ce dont le jeu a besoin.

## 3. Logique en C# pur, MonoBehaviour minces

**Décision.**

- Les règles du jeu vivent dans des classes C# sans Unity : `GameClock`, `EventBus`, `PlayerMotor`, `InteractionTargeting`, `PixelSnap`, `PixelScaler`, `ClockLabel`.
- Les `MonoBehaviour` ne font que relier Unity à cette logique : entrées, transform, rendu.

**Pourquoi c'est un bon choix.**

- **Des tests rapides et sûrs.** Les tests EditMode s'exécutent en millisecondes, sans scène ni mode Play, et couvrent tous les cas limites : 99 tests aujourd'hui.
- **Déterministe.** On injecte le temps (`Tick(deltaSeconds)`) au lieu de lire `Time` : un test peut simuler trois jours en une ligne.
- **Réutilisable et lisible.** La logique ne dépend pas de l'ordre d'exécution de Unity.

**Limites et risques.**

- **Deux fichiers par fonctionnalité** (logique et composant), et un peu de code de liaison.
- **Les tests unitaires ne voient pas les problèmes de câblage.** Une mauvaise référence dans une scène passe les tests EditMode. C'est pour ça qu'il faut aussi des tests PlayMode, et un test de validation des références (section 2).

**Alternatives écartées.** Mettre la logique dans les `MonoBehaviour` : plus rapide à écrire, mais difficile à tester et fragile dès qu'on change l'ordre d'exécution.

## 4. Données et réglages dans des ScriptableObjects

**Décision.** Tous les réglages sont des assets dans `Assets/_Project/Data` : vitesses du joueur (`SO_PlayerConfig`), durées et heures des phases (`SO_ClockConfig`), résolution (`SO_PixelRenderSettings`). `DataValidationTests` vérifie leurs valeurs : vitesses positives, heures croissantes, etc.

**Pourquoi c'est un bon choix.**

- **Équilibrer sans toucher au code.** On ne recompile pas pour changer une valeur.
- **Pas de nombres magiques dans le code.**
- **Une valeur aberrante est attrapée** par les tests de validation avant d'arriver dans le jeu.
- **Préparé pour le contenu.** C'est le même mécanisme qui servira aux objets, recettes, clients et quêtes, avec des identifiants stables.

**Limites et risques.**

- **En mode Play dans l'éditeur, modifier un ScriptableObject le modifie pour de vrai** : la valeur reste après l'arrêt. Une modification de réglage en jouant peut donc être sauvegardée sans qu'on le veuille.
- **Les tests de validation ne couvrent que `Assets/_Project/Data`.** Un réglage rangé ailleurs ne serait pas vérifié.
- **Les assets de réglages n'ont pas d'identifiant stable**, contrairement à ce que demandera le contenu. Ce n'est pas un problème tant qu'ils ne sont pas sauvegardés.

**Ce qu'on fait pour les limiter.** Le code ne modifie jamais un ScriptableObject pendant le jeu : l'état vit dans des classes à part (`GameClock`). La règle est écrite dans la doc technique (section Sauvegarde).

## 5. Rendu pixelisé

**Décision.**

- La scène est rendue dans une Render Texture de 640×360, avec un texel de marge (642×362), puis agrandie par un facteur entier sans lissage (`PixelRenderer`, `PixelScaler`).
- La caméra orthographique est calée sur la grille de texels après Cinemachine (`PixelCameraSnapper`). La fraction de texel restante est compensée en faisant glisser l'image affichée.
- Depuis VS-11, les objets en mouvement calent leur modèle sur la même grille (`PixelObjectSnapper`), tandis que leur racine physique garde sa position exacte.

**Pourquoi c'est un bon choix.**

- **Le look « A Short Hike » par construction.** On obtient de gros pixels nets sans retravailler les assets 3D.
- **Le décor ne scintille plus.** Un test PlayMode le vérifie : quand la caméra bouge, l'image ne fait que glisser.
- **Mouvement fluide malgré la grille,** grâce au glissement de l'image d'une fraction de pixel.
- **Peu coûteux.** Rendre 640×360 coûte bien moins cher qu'un rendu en pleine résolution ; l'agrandissement n'est qu'une copie.
- **Physique intacte.** Seul le modèle visible est recalé ; les collisions utilisent la position réelle.

**Limites et risques.**

- **Caméra orthographique obligatoire.** L'alignement sur la grille n'a pas de sens en perspective : pas d'effet de profondeur.
- **Les rotations ne sont pas calées.** Un modèle qui tourne lentement peut encore scintiller sur ses bords. Le calage ne corrige que la position.
- **À vitesse lente, le mouvement avance par crans d'un texel.** Ça se remarque plus à fort zoom : un texel fait 4,4 cm au zoom 8.
- **L'interface est en pleine résolution par-dessus l'image pixelisée.** Le texte net du HUD contraste avec le monde pixelisé. Il faudra choisir : police pixel, ou interface rendue elle aussi en basse résolution.
- **Les effets de particules** doivent avoir une taille minimale d'un pixel (`minParticleSize`), sinon ils scintillent. On l'a vu avec les grandes lucioles.

**Alternatives écartées.**

- **Shader de pixellisation en post-traitement** : plus simple, mais l'image reste rendue en haute résolution (plus cher), et le scintillement reste.
- **Pixel Perfect Camera 2D** : c'est un outil pour la 2D, pas pour une scène 3D.

## 6. Horloge de jeu unique

**Décision.**

- `GameClock` (C# pur) est la seule source du temps de jeu. Elle enchaîne les phases sur des durées réelles réglables : environ 15 min par journée, de 6h à 2h.
- Elle annonce les changements par des événements, et une longue image qui traverse plusieurs phases les annonce toutes, dans l'ordre.
- `GameClockRunner` est le seul endroit qui lit `Time.deltaTime`. Le HUD se met à jour sur `TimeChanged` (tous les 10 minutes de jeu), pas à chaque image.

**Pourquoi c'est un bon choix.**

- **Une seule vérité.** Toutes les durées du jeu (pousse, service, fin de journée) se baseront sur la même horloge. Pas de décalage entre systèmes.
- **Testable et déterministe** (section 3).
- **Pas de travail inutile.** Les écouteurs réagissent quand le temps affiché change, pas soixante fois par seconde.

**Limites et risques.**

- **Une horloge par scène.** Changer de scène (ferme vers café) recrée une horloge qui repart au jour 1, 6h. Le numéro du jour ne survit pas tant qu'il n'y a pas d'état de jeu persistant (VS-38).
- **Pas de pause.** `Time.timeScale = 0` arrêterait l'horloge, mais aussi tout le reste. Une pause propre (menu ouvert) doit être un état de l'horloge.
- **`Time.deltaTime` est plafonné par Unity** (`maximumDeltaTime`, 0,33 s par défaut). Après un gel de l'application, le temps de jeu perdu n'est pas rattrapé. C'est souvent souhaitable, mais il faut le savoir.
- **Le déplacement du joueur utilise le temps image**, pas l'horloge. C'est correct (le mouvement n'est pas du temps de jeu), mais la frontière doit rester claire.

**Ce qu'on fait pour les limiter.** Un état de jeu persistant dans `Core` est à prévoir : une scène de démarrage qui garde les services (horloge, bus, sauvegarde) et charge les scènes de jeu par-dessus. Il sera câblé par références, sans singleton.

## 7. Déplacement et interaction du joueur

**Décision.**

- `CharacterController` plutôt que `Rigidbody`. Le déplacement est relatif à la caméra et calculé dans `PlayerMotor`.
- Les entrées passent par les callbacks de l'Input System (`InputActionReference`), pas par une lecture à chaque image.
- L'interaction cherche l'objet `Interactable` le plus proche avec `Physics.OverlapSphereNonAlloc`, sur un tableau réutilisé, puis publie `Interacted` sur le bus.

**Pourquoi c'est un bon choix.**

- **`CharacterController` donne un contrôle précis et prévisible :** pas d'inertie ni de rebonds physiques, des pentes et des marches gérées, ce qui convient à un jeu cozy.
- **Pas d'allocation par image.** Les callbacks d'entrée n'interrogent rien en continu, et la recherche d'interaction réutilise ses tableaux et listes.
- **Le déplacement relatif à la caméra reste juste** même si on tourne la caméra : on l'a fait deux fois sur la ferme.

**Limites et risques.**

- **`CharacterController` ne pousse rien** et ne réagit pas aux forces. Pousser une caisse, ou être bousculé par un PNJ, demanderait du code dédié.
- **Le rayon d'interaction se mesure au sol** et ignore la direction du regard : on peut interagir avec un objet derrière soi. C'est acceptable pour l'instant ; un cône devant le joueur serait plus naturel.
- **Les actions viennent de l'asset du template** (`Assets/InputSystem_Actions.inputactions`), rangé à la racine et pas dans `_Project`.

## 8. Construction des scènes, décor et collisions

**Décision.**

- Les scènes ont d'abord été générées par des scripts d'éditeur temporaires, supprimés avant chaque PR. Ensuite, elles ont été retouchées à la main et par des scripts ponctuels.
- Le placement des objets évite les chevauchements grâce à une emprise au sol par objet.
- Sur la ferme, tout bloque le joueur sauf l'herbe, les fleurs et les dalles.

**Pourquoi c'est un bon choix.**

- **On ne modifie jamais le YAML d'une scène à la main,** ce qui évite les fichiers corrompus.
- **Les générateurs ont permis d'itérer vite** sur la composition (dizaines de versions), puis de rendre la main pour les retouches artistiques.

**Limites et risques.** C'est la partie avec le plus de dette.

- **Pas de Prefab Variants.** Les objets du décor sont des instances directes des prefabs Synty, modifiées dans la scène : colliders ajoutés, échelles. La règle du projet demande une Prefab Variant dans `Assets/_Project/Prefabs` pour chaque objet utilisé. Aujourd'hui, ajuster le collider de toutes les souches veut dire en modifier des dizaines une par une.
- **Des scènes énormes.** `SC_Farm.unity` fait plusieurs mégaoctets et contient environ 1 600 objets de décor. Deux personnes, ou deux branches, qui la modifient en même temps, c'est un conflit Git quasi impossible à résoudre.
- **Les générateurs supprimés** ne permettent plus de régénérer une scène. Les retouches manuelles l'auraient de toute façon empêché.
- **Les petits débris** (brindilles, galets) ont une boîte de collision d'un mètre de haut pour ne pas être enjambés. C'est voulu, mais ça peut rendre le déplacement pénible ; c'est à valider en jouant.

**Ce qu'on fait pour les limiter.**

- Créer des Prefab Variants pour les objets les plus utilisés (herbe, rochers, souches, buissons, arbres), avec leurs colliders, puis remplacer les instances dans la scène par un script. Ça réduit aussi la taille de la scène, puisqu'il n'y a plus d'overrides par instance.
- Plus tard, découper la ferme en sous-scènes chargées ensemble (décor, gameplay, lumière), pour limiter les conflits.

## 9. Assets Synty dans un package privé

**Décision.** Les packs Synty sont dans un dépôt privé (`moonbrew-synty`), installé comme package Unity par SSH. Le dépôt du jeu est public et ne contient aucun asset Synty, et `.gitignore` sert de filet de sécurité.

**Pourquoi c'est un bon choix.**

- **On respecte la licence Synty** tout en gardant le code public.
- **Les versions sont figées par tag.**
- **Le package est en lecture seule dans Unity,** donc on ne modifie pas un asset Synty par accident.

**Limites et risques.**

- **La CI a besoin d'une clé SSH** pour récupérer le package, une configuration de plus à maintenir.
- **Les matériaux Synty ne se modifient pas dans le package.** Tout réglage passe par un matériau à nous qui référence la texture, comme `M_Ground_Grass`.
- **Les noms de prefabs sont en double entre packs** (`SM_Env_Flowers_01` existe dans plusieurs packs). Une recherche par nom peut tomber sur le mauvais.

## 10. Qualité : tests et CI

**Décision.**

- Le niveau 1 tourne à chaque push : fichiers `.meta`, gros fichiers, formatage.
- Les tests Unity (EditMode, et PlayMode à la main) tournent dans GitHub Actions avec GameCI.
- En local : 99 tests EditMode et 13 tests PlayMode, tous verts.

**Pourquoi c'est un bon choix.** Les erreurs bêtes (`.meta` oublié, formatage) sont bloquées automatiquement, et la logique est couverte par des tests rapides.

**Limites et risques.**

- **Le check « Unity tests » échoue depuis le début** à l'activation de la licence Unity. Les tests ne sont donc jamais exécutés en CI : un test cassé sur une PR ne serait vu que si on lance les tests à la main. C'est le risque qualité numéro 1.
- **Les tests PlayMode ne tournent pas automatiquement**, pour économiser les minutes de CI.

**Ce qu'on fait pour les limiter.**

- Régler la licence en priorité, voir `Docs/CI.md`. Si la licence Personal ne peut pas être activée sur les runners GitHub, l'alternative est un runner auto-hébergé sur le PC.
- Lancer les tests PlayMode sur chaque PR qui touche au rendu, au joueur ou aux scènes.

## 11. Mesures actuelles (ferme, mode Play, éditeur)

Mesuré le 17 novembre 2026 sur `SC_Farm`, en mode Play dans l'éditeur. Les chiffres de l'éditeur sont plus pessimistes qu'un build : ils donnent un ordre de grandeur, pas une garantie.

| Mesure | Valeur | Lecture |
| --- | --- | --- |
| Temps d'image | ~4 ms (~240 i/s) | Large marge pour l'instant |
| Batches / draw calls | 632 / 676 | Élevé pour une si petite image |
| SetPass calls | 66 | Bas : le SRP Batcher fonctionne |
| Triangles rendus | ~672 000 | Très élevé pour une image de 640×360 |
| Renderers dans la scène | 3 825 (LOD compris) | Pour seulement 85 meshes différents |
| Objets marqués statiques | 400 sur 3 825 | Le décor n'est presque pas statique |
| Colliders | 702 mesh + 504 boîtes | Statiques, peu coûteux tant que rien ne bouge |
| Ombres | 1 lumière, 81 objets qui projettent | Distance d'ombre 50 m |

**Ce que ça montre.**

- **Le CPU et le GPU ont de la marge,** mais la scène est mal optimisée pour ce qu'elle affiche : environ 670 000 triangles pour 230 000 pixels, soit environ trois triangles par pixel. La plupart sont trop petits pour être visibles en basse résolution.
- **Le même mesh est affiché des milliers de fois** (85 meshes pour 3 825 renderers). C'est le cas idéal pour l'instanciation GPU.

**Pistes, à mesurer une par une :**

1. **Activer le GPU Resident Drawer d'URP** (Unity 6). Il regroupe automatiquement les objets qui partagent un mesh et un matériau, ce qui devrait faire chuter les draw calls de l'herbe, des fleurs et des cailloux. Il demande le chemin de rendu Forward+ : ce sont des réglages du projet, donc à valider avant de toucher à `ProjectSettings`.
2. **Marquer le décor immobile comme statique.** C'est utile pour l'occlusion, et pour l'éclairage précalculé si on en ajoute. Le static batching, lui, n'est intéressant que si le GPU Resident Drawer n'est pas utilisé, car il consomme de la mémoire.
3. **Réduire les triangles.** Utiliser des niveaux de détail plus agressifs : en vue orthographique, les LOD se basent sur la taille à l'écran, et à 640×360 presque tout peut passer au niveau le plus simple. On peut aussi remplacer les touffes d'herbe les plus lourdes.
4. **Réduire les objets qui projettent une ombre.** L'herbe, les fleurs et les petits cailloux n'ont pas besoin d'ombre à cette résolution.

## 12. Dette technique, par priorité

1. **Les tests Unity ne tournent pas en CI** à cause de la licence (section 10).
2. **Une référence `SO_EventBus` oubliée échoue en silence.** Ajouter un test de validation des scènes et des prefabs (section 2).
3. **Pas de Prefab Variants pour le décor,** et des scènes énormes (section 8).
4. **Une horloge par scène.** Prévoir un état de jeu persistant avant que plusieurs scènes soient jouées à la suite (section 6).
5. **Optimisation du rendu :** GPU Resident Drawer, ombres, LOD (section 11).
6. **Cohérence visuelle de l'interface** avec le rendu pixelisé (section 5).
7. **Petits points :**
   - l'asset d'entrées du template est à ranger dans `_Project` ;
   - le rayon d'interaction ignore la direction du regard ;
   - les rotations ne sont pas calées sur la grille.
