# 04\_Doc\_Technique — Moonbrew

Nov 15, 2026 · @Ileana

## Principes d'architecture

Moonbrew suit six règles pour que chaque nouvelle feature s'ajoute sans casser les autres. Tout ce document est une proposition de départ, à ajuster pendant le vertical slice.

1. **Les données vivent dans des ScriptableObjects**, pas dans le code : ajouter un ingrédient, une recette ou un client revient à créer un asset.
2. **Un module par feature**, avec son dossier et son assembly definition (ferme, cuisine, café, PNJ, quêtes, économie, interface).
3. **Les modules communiquent par événements**, jamais par références directes : une feature s'abonne aux événements existants sans modifier les autres.
4. **Composition plutôt qu'héritage** : de petits composants assemblés dans les prefabs.
5. **Une seule horloge de jeu** : tout ce qui dépend du temps (pousse, service, bilan) s'abonne à elle.
6. **Tout état sauvegardé est sérialisable et versionné**, dès le premier jour.

## Vue d'ensemble

Les modules ne se parlent jamais directement : chacun émet et écoute des événements sur un bus central, fourni par le module Core. Une nouvelle feature s'abonne aux événements existants sans modifier les autres modules.

&#91;embedded content: architecture · 10 modules autour du bus d'événements\]

Le module Core fournit le bus, l'horloge et la sauvegarde ; les dix autres modules ne se connaissent que par les événements qu'ils échangent.

Le bus est un asset unique, `SO_EventBus`, branché dans l'inspecteur sur chaque composant qui publie ou écoute : pas de singleton. Les événements sont des structs déclarées dans Core (`GameEvents.cs`), donc tous les modules peuvent les partager sans se référencer. On publie avec `Publish(new DayStarted(1))`, on s'abonne avec `Subscribe<DayStarted>(...)` dans `OnEnable` et on se désabonne dans `OnDisable`.

## Modules et responsabilités

Chaque module a un dossier dans `Scripts/` et ne parle aux autres que par événements.

| Module | Rôle | Émet | Écoute |
| --- | --- | --- | --- |
| Core | Horloge, bus d'événements, état du jeu, sauvegarde | DayStarted, PhaseChanged, DayEnded | (aucun) |
| Player | Déplacement, interaction avec les objets | Interacted | (aucun) |
| Inventory | Objets du joueur et stock du café | InventoryChanged | (aucun) |
| Farming | Parcelles, plantation, arrosage, récolte | CropHarvested | PhaseChanged |
| Kitchen | Transformation des ingrédients (farine) et recettes | ItemCrafted | InventoryChanged |
| Cafe | Clients, commandes, préparation, service | OrderServed, CustomerLeft | PhaseChanged |
| Employee | Service automatique en l'absence de la patronne | OrderServed | PhaseChanged |
| Economy | Argent, boutique, revenus du café | MoneyChanged | OrderServed |
| Npc | PNJ, dialogues courts, relations | RelationChanged | PhaseChanged |
| Quests | Suivi et validation des quêtes | QuestUpdated | CropHarvested, ItemCrafted, OrderServed |
| Ui | Affichage (HUD, menus, bilan) | (aucun) | Tous les événements utiles |

## Données du jeu

Chaque type de contenu est un ScriptableObject avec un identifiant texte unique et stable (par exemple `crop_wheat`), utilisé dans les sauvegardes. On ne référence jamais un contenu par le nom de son fichier.

| Type | Champs principaux |
| --- | --- |
| ItemData | Identifiant, nom, catégorie, modèle ou icône, prix de base |
| CropData | Graine, produit récolté, durée de pousse, saisons autorisées |
| RecipeData | Ingrédients, résultat, niveau de 1 à 5 étoiles, temps de préparation, prix d'achat de la recette |
| CustomerData | Nom, type (habitant ou client de passage), goûts aimés et détestés, répliques, modèle, quête éventuelle (habitants seulement) |
| QuestData | Conditions, récompenses, étapes |
| EmployeeData | Nom, recettes connues, vitesse de service |

Le tableur d'équilibrage (07\_Donnees\_Equilibrage) sert de source pour les valeurs ; un script d'import dans l'éditeur Unity les copie dans les assets.

## Rendu pixelisé

Le décor et les personnages sont en 3D low-poly et passent par le même rendu pixelisé.

- **Pipeline** : Unity URP (template Universal 3D), caméra Cinemachine en vue 3/4.
- **Résolution** : rendu dans une Render Texture basse résolution (référence : 640x360), agrandie par un facteur entier (×3 en 1080p, ×4 en 1440p) sans lissage, avec des bandes noires si l'écran n'est pas un multiple exact. Bascule pixel / natif avec F2 (debug).
- **Mode natif** : la résolution est configurable et un mode de rendu net est prévu, pour comparer les deux looks.
- **Caméra** : Cinemachine orthographique en 3/4 (35° de plongée, 45° de lacet dans la scène de test), calée sur la grille de texels par PixelCameraSnapper juste après Cinemachine ; le reste en fraction de texel est compensé en faisant glisser la zone visible d'une image rendue avec un texel de marge. Un test PlayMode vérifie que l'image ne fait que glisser quand la caméra bouge. Les objets en mouvement (joueur, puis PNJ) portent un PixelObjectSnapper : leur modèle est recalé sur la même grille juste après la caméra, pour avancer d'un texel entier à la fois.
- **Palette par zone** : appliquée par post-traitement et par recoloration des matériaux, avec un jeu de réglages par zone (proposition : un Volume local par zone), et désactivable. Les textures d'origine des packs ne sont pas écrasées : la recoloration passe par des variantes de matériaux.
- **Animations** : Mecanim, avec les packs Synty Animation Base Locomotion, Idles et Sword Combat et la banque d'Adobe (Mixamo) pour les humanoïdes. Les monstres non humanoïdes viendront après le slice.
- **Assets** : packs Synty POLYGON importés au fur et à mesure des tickets (voir la bible artistique).

## Temps, économie du café et sauvegarde

- **Temps** : une horloge unique dans Core émet DayStarted, PhaseChanged et DayEnded. Les phases (matin, journée, soir, nuit) sont des repères et la journée est libre, mais il faut rentrer dormir en fin de journée : sinon, perte d'argent ou d'une partie de l'inventaire et stamina réduite le lendemain (valeurs à définir). La durée des phases est une donnée, pas une valeur codée en dur. Pas de saisons dans le slice.
- **Revenu du café** : clients servis × prix du plat, qui dépend de son niveau de 1 à 5 étoiles (seul axe de qualité), avec un facteur de présence de la patronne. L'employé ne sert que les recettes 1 étoile ; la patronne sert toutes les recettes connues et attire les clients importants. Un ingrédient manquant s'achète en boutique, 2 à 3 fois plus cher que produit. Toutes ces valeurs viennent des données (RecipeData, EmployeeData) pour pouvoir équilibrer sans toucher au code.
- **Patronne absente** : proposition, la phase de service est calculée avec les mêmes règles à la fin de la journée, et le bilan de la nuit en montre le résultat.
- **Sauvegarde** : l'état du jeu est sérialisable (JSON), porte un numéro de version et prévoit des migrations. Les contenus sont référencés par leurs identifiants texte stables. Les ScriptableObjects ne sont jamais modifiés à l'exécution : seul l'état est sauvegardé. Chaque module expose l'état qu'il doit sauvegarder ; la sauvegarde se fait à la nuit et à la sortie.

## Structure du projet et conventions

Structure proposée, qui reprend celle du README :

```
Assets/
  _Project/
    Art/            modèles, textures, matériaux, palettes par zone
    Audio/
    Data/           ScriptableObjects (items, recettes, clients, quêtes)
    Prefabs/
    Scenes/
    Scripts/
      Core/         horloge, événements, sauvegarde, état du jeu
      Player/ Inventory/ Farming/ Kitchen/ Cafe/
      Employee/ Economy/ Npc/ Quests/ Ui/
docs/               pitch, périmètre, doc technique, bible artistique
```

- **Un module = un dossier + une assembly definition**, avec un namespace `Moonbrew.<Module>`.
- **Événements** : nommés au passé (DayStarted, OrderServed), définis dans Core ou dans le module qui les émet.
- **Données** : un type de ScriptableObject par contenu, suffixé `Data` (ItemData, RecipeData), avec un identifiant texte stable en snake\_case (`crop_wheat`).
- **Pas de singleton statique** en dehors de Core.
- **Git** : `.gitignore` Unity à la racine et Git LFS pour les assets lourds.

## Packages et décisions techniques

**Packages à installer** : Universal RP (template Universal 3D), Input System, Cinemachine.

**Décisions prises**

- Personnages en 3D low-poly, rendus en pixel comme le reste du jeu.
- Une palette par zone, désactivable ; un mode de rendu natif possible.
- Contenu piloté par des ScriptableObjects, communication par événements.
- L'IA n'est utilisée que pour le code, ce qui ne se déclare pas sur Steam.

**À décider plus tard**

- [ ] Outil de dialogue pour le café et les PNJ (Yarn Spinner, Ink ou un système simple).
- [ ] Événements C# classiques ou canaux ScriptableObject.
- [ ] Script d'import des données depuis le tableur d'équilibrage.
- [ ] Injection de dépendances (par exemple VContainer), seulement si le besoin se confirme.
- [ ] Unity Localization pour la traduction, après le slice.
