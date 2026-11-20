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
| ItemData | Identifiant, nom, catégorie (graine, récolte, ingrédient, plat), icône, prix de base, taille maximale d'une pile |
| InventoryConfig | Identifiant, nom affiché, nombre de cases (un par inventaire : sac du joueur, stock du café) |
| CropData | Identifiant, graine, produit récolté, jours de pousse, jours de repousse (0 : une seule récolte), quantité récoltée (min et max), un modèle par stade de pousse (le dernier = mûr). Pas de saisons dans le slice. |
| RecipeData | Ingrédients, résultat, niveau de 1 à 5 étoiles, temps de préparation, prix d'achat de la recette |
| CustomerData | Nom, type (habitant ou client de passage), goûts aimés et détestés, répliques, modèle, quête éventuelle (habitants seulement) |
| QuestData | Conditions, récompenses, étapes |
| EmployeeData | Nom, recettes connues, vitesse de service |

Le tableur d'équilibrage (07\_Donnees\_Equilibrage) sert de source pour les valeurs ; un script d'import dans l'éditeur Unity les copie dans les assets.

**Preuve par la fraise** : la deuxième culture (la fraise) a été ajoutée uniquement avec des assets (`SO_Crop_Strawberry`, ses objets, ses stades), sans aucune ligne de code qui la nomme. Le test PlayMode `DataDrivenCropIntegrationTests` charge les vrais assets et lui fait faire tout le cycle : labour, semis, arrosage, pousse, récolte et repousse. Pour ajouter une culture : créer ses objets (graine, récolte), son `CropData` et ses prefabs de stades, puis l'ajouter à `SO_CropCatalog` et ses objets à `SO_ItemCatalog`. Les règles de `DataValidationTests` signalent ce qui manque.

## Rendu pixelisé

Le décor et les personnages sont en 3D low-poly et passent par le même rendu pixelisé.

- **Pipeline** : Unity URP (template Universal 3D), caméra Cinemachine en vue 3/4.
- **Résolution** : rendu dans une Render Texture basse résolution, agrandie par un facteur entier, sans lissage. Le facteur est l'entier le plus proche de « hauteur de la fenêtre / 360 » (×3 en 1080p, ×4 en 1440p). La taille du rendu suit ensuite la forme de la fenêtre (environ 640×360) : l'image remplit toujours la fenêtre, sans bandes noires, et l'interface (posée sur toute la fenêtre) reste alignée avec elle. Au plus un pixel d'écran est rogné par bord. Bascule pixel / natif avec F2 (debug).
- **Bulles nettes** : les bulles (calque `Bubbles`, ajouté dans les `ProjectSettings`) ne passent pas par le rendu basse résolution. `PixelRenderer.sharpLayers` les retire de la caméra pixel. Une seconde caméra, fille de la première, les dessine à la résolution de l'écran dans une texture transparente, posée par-dessus l'image avec le même glissement sub-texel : elles restent calées sur leur objet. Leurs icônes 16×16 restent donc nettes au-dessus d'un décor bien pixelisé. Elles sont toujours dessinées au premier plan, et ne passent pas par l'étalonnage d'ambiance. Le niveau de pixelisation du décor reste 640×360 ; le 960×540 a été essayé puis écarté.
- **Mode natif** : la résolution est configurable et un mode de rendu net est prévu, pour comparer les deux looks.
- **Caméra** : Cinemachine orthographique en 3/4 (35° de plongée, 45° de lacet dans la scène de test), calée sur la grille de texels par PixelCameraSnapper juste après Cinemachine ; le reste en fraction de texel est compensé en faisant glisser la zone visible d'une image rendue avec un texel de marge. Un test PlayMode vérifie que l'image ne fait que glisser quand la caméra bouge. Les objets en mouvement (joueur, puis PNJ) portent un PixelObjectSnapper : leur modèle est recalé sur la même grille juste après la caméra, pour avancer d'un texel entier à la fois.
- **Palette par zone** : deux effets cumulables.
  - Un étalonnage d'ambiance : un Volume global par scène (`VP_Farm`, `VP_Cafe`), avec contraste, saturation, balance des blancs et teinte des ombres.
  - Une réduction à une palette de 32 couleurs au plus (`SO_Palette_<Zone>`, avec une force de 0 à 100 %). Le shader `Moonbrew/PaletteFilter` l'applique sur l'image 640×360, juste avant l'agrandissement, en prenant pour chaque pixel la couleur la plus proche dans l'espace Oklab. Le composant `ZonePaletteFilter` est sur la caméra, à côté de `PixelRenderer`.

  `startActive` choisit si la palette est active au lancement de la scène. Elle est coupée pour l'instant à la ferme et au café : elle durcissait les volumes et découpait les halos des lumières ponctuelles en bandes, et le rendu doux d'origine a été préféré. La palette se coupe avec F4 dans l'éditeur et les builds de dev. Le rendu net (F2) reste sans palette, pour comparer. Les textures d'origine des packs ne sont pas recolorées : si une recoloration devient nécessaire, elle ira dans `moonbrew-synty`.
- **Animations** : Mecanim, avec les packs Synty Animation Base Locomotion, Idles et Sword Combat et la banque d'Adobe (Mixamo) pour les humanoïdes. Les monstres non humanoïdes viendront après le slice.
- **Assets** : packs Synty POLYGON importés au fur et à mesure des tickets (voir la bible artistique).

## Inventaire, barre d'objets et touches

- **Inventaires** : `ItemInventory` (C# pur) gère des cases ; chaque case empile un même objet, jusqu'à sa taille maximale. Ajouter range ce qui rentre et renvoie le nombre d'objets rangés. Retirer se fait en entier ou pas du tout. Un `InventoryHolder` par inventaire dans la scène (sac du joueur, stock du café) annonce `InventoryChanged` sur le bus.
- **Barre d'objets** : en bas au centre, elle montre les 10 premières cases du sac (`PlayerConfig.hotbarSlots`). Les touches 1 à 9 choisissent les cases 1 à 9, et 0 la dixième ; un clic gauche sur une case la choisit aussi. La sélection vit dans `Hotbar` (module Player) et s'annonce par `HotbarSlotSelected`. `HotbarHud` (Ui) ne fait qu'afficher.
- **Modèle de case** : `Art/Ui/HotbarSlot.uxml` est l'équivalent d'un prefab en UI Toolkit. Son apparence est dans `Hud.uss`.
- **Habillage Interface Fantasy** : le HUD reste en UI Toolkit. `Hud.uss` reprend les sprites, les teintes et les polices `.ttf` des prefabs du pack Fantasy Screens, depuis le paquet `com.moonbrew.synty`. Les prefabs uGUI eux-mêmes et les ressources TextMeshPro ne sont pas utilisés, et rien n'est copié dans le dépôt. Les sprites des packs sont très grands : `-unity-slice-scale` ramène leurs bords en neuf parties à la taille du HUD (640×360).
  - **Barre d'objets** (d'après `Component_FantasyScreens_HUD_HotBar_01`, réduite pour le HUD 640×360) : une barre arrondie semi-transparente qui porte des cases brunes arrondies (`Box_Rounded_Medium_01`), avec un motif de losange (`Tracery_Diamond_01`). La case choisie est plus claire. La quantité est en haut à droite, en LT Museum blanc avec une ombre (`Label_Item_Amount`). Le numéro de touche est en Grenze, dans une petite boîte gris ardoise sous la case (`Input_FantasyWarrior_Hotkey_01`).
  - **Heure** (d'après `Component_FantasyScreens_GameTime_01`) : en haut à droite, une boîte arrondie semi-transparente, avec le sablier et l'heure en grand, puis la phase en jaune et le jour (`ClockHud`). L'argent viendra à côté avec la boutique.
  - **Fenêtres** (d'après `Panel_FantasyScreens_Unit_Info_01`) : le menu du jeu, le récap et l'invite de sommeil ont un fond rouge sombre semi-transparent, un liseré rouge et un cadre aux coins dorés (`Frame_Box_Large_02`). Viennent ensuite un grand titre blanc (LT Museum) et un bandeau sombre de sous-titre. Les onglets et les boutons sont des bannières rouges à cadre doré (`Menu_Button_03`, classe `banner`). Les recettes s'affichent en lignes sombres, l'icône posée sur un losange rouge. Le texte courant utilise Alegreya Sans.
- **Icônes** : les icônes 16×16 de `Art/Icons/` sont provisoires.
- **Menu** : Tab ouvre et ferme le menu du jeu (voir « Cuisine »).
- **Touches** : le profil par défaut est QWERTY. L'Input System lie des positions physiques de touches : sur un clavier AZERTY, la touche à l'emplacement du W (le Z) marche donc déjà pour avancer. Changer de profil ou modifier les touches se fera dans un écran de paramètres, qui reste à faire.

## Champ : labour et plantation

- **Grille** : la ferme est découpée en cases de 1 m (`SO_FarmConfig`). `FarmGrid` passe d'une position à sa case et inversement.
- **Clic** : `GroundPointer`, sur la caméra, convertit un clic de souris en rayon à travers le rendu basse résolution et sa marge. Si le rayon touche un objet marqué `ClickTarget` (une machine, une bulle), il annonce `TargetClicked` ; sinon il annonce `GroundClicked` avec le point du sol. Un objet cliquable prend donc le clic, et rien ne se passe sur le sol derrière lui. Un clic sur un élément d'interface (dialogue, barre d'objets, horloge) ne touche ni le sol ni les objets.
- **Actions** : `FarmField` (module Farming) agit sur une case voisine de la patronne (portée d'une case, diagonales comprises), selon l'objet tenu dans la barre :
  - la houe (`tool_hoe`, catégorie Outil) laboure une case libre. Une case n'est pas libre si elle porte :
    - un obstacle solide, vérifié à partir de 2 cm au-dessus du sol, ce qui attrape aussi les dalles plates des chemins ;
    - une touffe d'herbe ;
  - des graines plantent la culture correspondante (`SO_CropCatalog`) sur une case labourée et vide, et une graine quitte le sac.
- **Herbe et faucille** :
  - Les 510 touffes d'herbe et les 123 fleurs de la ferme portent `CuttableGrass` : un trigger et un identifiant tiré de leur position. Elles sont sur le calque intégré « Ignore Raycast », pour que l'interaction (E), qui utilise `Physics.DefaultRaycastLayers`, ne soit pas saturée par la végétation. Les buissons fleuris restent des obstacles solides.
  - La faucille (`tool_sickle`) coupe toutes les touffes dans un demi-cercle (180°, rayon 1,5 m, réglables dans `SO_FarmConfig`) tourné vers le clic.
  - La faucille coupe aussi les fleurs, et on ne laboure pas sur une fleur.
- **Arrosage et pousse** :
  - **L'arrosoir** (`tool_watering_can`) arrose une case labourée voisine, une fois par jour. La terre s'assombrit : la couleur de la dalle labourée est multipliée par une teinte (`SO_FarmConfig.wetSoilTint`, via un `MaterialPropertyBlock` sur `_BaseColor`, sans modifier le matériau). Rien n'est posé par-dessus, donc rien ne peut se décaler.
  - **Sa réserve** fait 20 arrosages (`WateringCan`, réglable dans `SO_FarmConfig`). Un clic sur le puits (`PF_Well`) la remplit, si la patronne est à moins de 2,5 m. Arroser une case passe avant le remplissage, pour que la terre voisine du puits reste arrosable. Sur la barre, l'eau se voit dans l'icône de l'arrosoir : son corps est transparent, et une eau bleue dessinée derrière baisse à chaque arrosage, sans chiffre (`WateringCanChanged`, éléments `water` du modèle de case, calés sur le corps de l'icône dans `Hud.uss`).
  - **La nuit** : à la fin de la journée, `GameSession` publie `NightPassed`, puis `GameSaving`. Chaque culture arrosée gagne un jour de pousse, jusqu'à ses jours de pousse (`CropData.growthDays`), puis toute la terre sèche. Une culture non arrosée attend, sans jamais mourir. Elle est récoltable quand elle atteint son dernier stade.
- **Récolte** :
  - Une culture mûre porte une bulle avec l'icône de sa récolte (`SO_FarmConfig.ripeBubblePrefab`), posée juste au-dessus du plant, quelle que soit sa taille (`ModelBounds.Top` plus un petit écart). Un clic sur la bulle la récolte, comme un clic sur la culture. Le pivot de chaque icône est au centre de son dessin, pour qu'elle soit bien centrée dans la bulle.
  - Un clic sur une culture mûre voisine la récolte, quel que soit l'objet tenu (même une case vide). La quantité est tirée entre `harvestMin` et `harvestMax` (`CropData.RollHarvest`, avec une source de hasard injectable pour les tests).
  - Si le sac n'a pas la place pour toute la récolte, rien n'est récolté et la culture reste mûre.
  - Une culture sans repousse laisse la terre labourée et vide. Une culture qui repousse (la fraise) recule de `regrowDays` jours et revient à son avant-dernier stade.
  - `FarmField` publie `CropHarvested` (culture, objet, quantité). La récolte apparaît aussi dans le récap du lendemain, comme gain du sac.
- **État et sauvegarde** : l'état des cases vit dans `FarmPlots` (C# pur) : les cases labourées, la culture plantée et ses jours de pousse. Il est enregistré dans la sauvegarde avec les touffes coupées (`plots`, `cutGrass`, ajoutés au format 2) et l'eau utilisée de l'arrosoir (`wateringCanUsed`, ajouté au format 3). L'état « arrosé » n'est pas sauvegardé, puisque la terre sèche juste avant la sauvegarde. Une culture qui n'existe plus laisse sa case labourée et vide. L'herbe coupée ne repousse pas pour l'instant.
- **Modèles** : le sol labouré (`PF_Soil_Tilled`) et les stades de pousse n'ont pas de collision, la patronne marche dessus.
- **Escalier** : la collision de l'escalier de la maison est une rampe de 49°. Le `CharacterController` de la patronne accepte donc 55° de pente et 40 cm de marche.

## Transformation : le moulin

- **Données** : `ProcessData` décrit une transformation (objet d'entrée et quantité, objet de sortie et quantité, minutes de jeu). Par exemple `SO_Process_Flour` : 1 blé donne 1 farine en 60 min. `StationData` décrit une machine : ses transformations, sa capacité en fournées, sa portée. Par exemple `SO_Station_Mill`, 10 fournées.
- **Logique** (`ProcessingStation`, C# pur) : une machine est vide, au travail ou prête. On la charge seulement quand elle est vide, avec autant de fournées que le sac le permet, jusqu'à sa capacité. Toute la fournée prend le temps du `ProcessData`.
- **Dans la scène** (`Station`, module Kitchen) :
  - Un clic sur la machine, avec la patronne à moins de 2 m, la charge avec l'objet tenu, s'il est l'entrée d'une de ses transformations.
  - Le temps avance avec `TimeChanged` (l'horloge du jeu), et une fournée non finie se termine pendant la nuit (`NightPassed`).
  - **Bulle** : au-dessus de la machine, une bulle (`PF_Bubble`, tournée vers la caméra) montre un sablier pendant le travail, puis l'icône du résultat quand c'est prêt. Rien quand la machine est vide. Un clic sur la machine ou sur sa bulle (`TargetClicked`) récupère la sortie, quel que soit l'objet tenu, si tout rentre dans le sac. `ItemCrafted` (machine, objet, quantité) est publié.
- **Sauvegarde** : le contenu de chaque machine (`stations`, format 4) est enregistré. Une ancienne sauvegarde se charge avec des machines vides.
- **Modèle** : le moulin (`PF_Mill`, à côté du puits) utilise provisoirement la meule à aiguiser du pack Fantasy Kingdom, faute de vrai moulin dans les packs.

## Cuisine : recettes, four et menu du jeu

- **Données** : `RecipeData` décrit une recette : ses ingrédients (objet et quantité), son plat, sa difficulté (1 à 5 étoiles), son temps de cuisson en minutes de jeu, si elle est connue au départ, et son prix si elle s'achète. `SO_RecipeCatalog` les liste toutes. Les trois premières :

  | Recette | Ingrédients | Étoiles | Cuisson | Au départ |
  | --- | --- | --- | --- | --- |
  | Jus de fraise | 3 fraises | 1 | 20 min | connue |
  | Gâteau au blé | 2 farines | 1 | 30 min | connue |
  | Gâteau aux fraises | 2 farines, 3 fraises | 2 | 45 min | à acheter (150) |

- **Four** : `SO_Station_Oven` est une machine (`StationData`) avec des recettes au lieu de transformations, et une seule fournée. Le four (`PF_Oven`, le poêle du pack Fantasy Kingdom) est dans le café, contre le mur du fond, entre le placard et le chaudron. Son tuyau monte jusqu'au plafond, à 5 m : c'est un cylindre ajouté au prefab, avec le matériau du poêle.
  - Un clic sur le four vide, avec la patronne à moins de 2 m, publie `StoveOpened` : le menu s'ouvre sur les recettes, avec leurs boutons « Cuisiner ».
  - Le menu publie `RecipeChosen` (four, recette). Le four vérifie que la recette est connue et que le sac a les ingrédients, les retire du sac, puis cuit : même bulle et même logique que le moulin (`ProcessingStation`, qui accepte maintenant une tâche `Job` venue d'une transformation ou d'une recette). Un clic quand c'est prêt récupère le plat.
- **Livre de recettes** : `RecipeBook` (C# pur) sait quelles recettes la patronne connaît : celles connues au départ, plus celles apprises. `RecipeBookHolder` le garde dans la scène, publie `RecipeLearned` et l'enregistre dans la sauvegarde (`learnedRecipes`, format 5 ; une ancienne sauvegarde se charge avec les seules recettes de départ). La boutique (VS-30) appellera `Learn` ; en attendant, F9 apprend tout (debug).
- **Menu du jeu** (`GameMenu`, Ui) : un menu à onglets, comme celui de Stardew Valley. Il n'a que l'onglet « Recettes » pour l'instant ; la carte, les relations et le reste s'ajouteront comme d'autres onglets. Il s'ouvre et se ferme avec Tab (action `Player/OpenMenu`) n'importe où, ou par le four.
  - Chaque ligne montre l'icône du plat, son nom, ses étoiles, les ingrédients avec ce que le sac contient (« Flour 1/2 »), et pourquoi on ne peut pas la cuisiner (« To buy », « Missing ingredients »). Une recette à acheter est grisée.
  - « Cuisiner » n'est actif que si le menu a été ouvert par le four.
  - Le menu ouvert publie `GameMenuToggled` : l'horloge se met en pause et la patronne ne bouge plus.
  - Les modèles sont `Art/Ui/GameMenu.uxml` et `RecipeRow.uxml`, avec leur style dans `Hud.uss`.
- **Clic au café** : la caméra du café porte maintenant un `GroundPointer`, comme celle de la ferme.
- **Collisions des meubles** : les colliders convexes des packs ont des faces inclinées, et la patronne accepte 55° de pente (pour l'escalier de la ferme) : elle montait sur le comptoir et les chaises. Les meubles posés au sol du café ont donc une boîte aux bords verticaux à la place, haute d'au moins 1 m. Le bas arrondi de la capsule de la patronne grimpait encore sur la table basse de 47 cm.
- **Bulles** : `FaceCamera` tourne chaque bulle vers la caméra de la scène à son apparition, puisque la caméra du café n'est pas tournée comme celle de la ferme. Les caméras des scènes portent le tag `MainCamera`.
- **Ajouter une recette** : créer le plat (`ItemData`, ajouté à `SO_ItemCatalog`), puis le `RecipeData`. L'ajouter à `SO_RecipeCatalog` et aux recettes du four. `DataValidationTests` vérifie les identifiants, les ingrédients, les étoiles, le prix des recettes à acheter et le catalogue.

## Clients du café

- **Données** : `CustomerData` (module Cafe) décrit un client : identifiant, nom, type (`Passing` pour un client de passage, `Villager` pour un habitant), modèle, objets aimés et détestés, répliques. `SO_CustomerCatalog` les liste tous (`Data/Cafe`).
- **Goûts** : un goût porte sur un plat ou sur un ingrédient. `TasteOf(plat, ingrédients)` dit si le client aime, déteste ou n'a pas d'avis. Le goût pour le plat lui-même passe en premier ; sinon, un seul ingrédient détesté gâche le plat, et un ingrédient aimé suffit à le faire aimer. Les ingrédients sont passés par l'appelant, puisque le module Cafe ne connaît pas les recettes (Kitchen).
- **Habitants** : leur identifiant (`villager_…`) sera aussi celui de leur PNJ dans le village (VS-33), pour partager la relation. Leurs répliques sont des indices sur leurs goûts ; rien n'est noté pour la joueuse.
- **Clients de passage** : pas de goûts. Ils commanderont au hasard parmi les recettes connues (VS-26). Les groupes viendront avec VS-25.
- **Les 5 clients du slice** :

  | Client | Type | Modèle | Aime | Déteste |
  | --- | --- | --- | --- | --- |
  | Aventurière | de passage | chevalière (Dungeon) | | |
  | Colporteur gobelin | de passage | gobelin (Dungeon) | | |
  | Mage voyageur | de passage | magicien (Fantasy Characters) | | |
  | Rose, l'herboriste | habitante | druidesse (Fantasy Characters) | la fraise | le gâteau au blé |
  | Basile, le meunier | habitant | paysan (Fantasy Characters) | la farine | le jus de fraise |

- **Modèles** : un Prefab Variant par client dans `Prefabs/Characters` (`PF_Customer_…`), à partir de la version « FixedScale » du personnage Synty, avec l'Animator de la patronne (pose au repos pour l'instant).
- **Ajouter un client** : créer son Prefab Variant, puis son `CustomerData`, et l'ajouter à `SO_CustomerCatalog`. `DataValidationTests` vérifie l'identifiant, le nom, le modèle animé, les répliques, et qu'un habitant a au moins un goût aimé et un goût détesté.

## Service au café : arrivée, installation et choix

- **Réglages** (`SO_CafeConfig`) : arrivées de 11h à 18h, un groupe toutes les 30 à 60 minutes de jeu ; des groupes de passants de 1 à 3, ou un habitant seul (30 % de chances) ; un choix de 10 à 20 minutes ; une patience de 60 minutes ; une marche à 1,4 m/s ; la bulle et l'icône « ! » (`T_Icon_Ready`).
- **Logique** (C# pur, module Cafe) :
  - `CafeService` décide quand un groupe arrive, qui le compose et à quelle table. Un habitant vient seul. Les passants d'un groupe sont tous différents. Personne ne revient tant qu'il est encore dans la salle. La table, libre et assez grande pour tout le groupe, est tirée au hasard. Sans table libre, personne n'entre.
  - `TableGroup` suit un groupe : il arrive, choisit une fois que tout le monde est assis, est prêt à commander (« ! »), puis part, quand sa patience est épuisée ou à la fermeture.
  - Le hasard (`System.Random`) est injecté pour les tests.
- **Dans la scène** :
  - `CafeFloor` relie cette logique à l'horloge (`TimeChanged`) et à la salle. Il fait entrer les clients par la porte (`Cafe Door`, au fond à gauche), montre la bulle « ! » au-dessus de la table et les raccompagne. Il publie `CustomerLeft` à chaque départ. Le menu ouvert met les clients en pause, et la nuit vide la salle.
  - `CafeTable` porte les places d'une table : une par chaise, tournée dans le sens de la chaise.
- **Salle** :
  - **Comptoir** : un comptoir en L (bars du pack Fantasy Kingdom).
    - Le grand côté forme une seule ligne : deux bars, dont les extrémités se chevauchent de 12 cm pour cacher les jointures, puis la grande vitrine vitrée (`Market_Food_Display_04`, 2,5 m), alignée sur le devant.
    - Le petit côté revient vers la salle du côté de la cheminée. Un court tronçon du même bar le relie au mur de bouteilles, pour fermer la cuisine de ce côté.
    - Sur le comptoir, les objets sont groupés : la caisse (`Shop_Cash_Register_05`) et des tasses du côté ouvert, la bougie au milieu, une tarte près de la vitrine. Sur le petit côté, une planche avec du pain, du fromage et du miel.
  - **Cuisine** : un couloir d'environ 1 m derrière le comptoir, par lequel la patronne entre à gauche.
    - Le long du mur du fond, une seule ligne de meubles collés au mur et sans espace : le casier à vin, puis des meubles bas en bois de même hauteur, avec le poêle intégré dans la ligne. Tous ont le même devant. À droite du poêle, un seul meuble étiré sur toute la longueur remplace deux meubles côte à côte.
    - Chaque meuble a son usage : un coin café (moulin, tasses, pot à lait), un évier, le poêle avec sa bouilloire, un coin mixeur et théière, une caisse à pain. Il n'existe pas de machine à café dans les packs : il en faudra une sur mesure.
    - Au-dessus des meubles, des étagères murales en bois (`Shelf_01`) portent des chopes, des bocaux et des cruches.
    - Contre le mur de droite, l'étagère de bar garnie de bouteilles.

    Rien n'est posé à même le sol. L'armoire décorée, qui ne collait pas avec une cuisine, a été retirée.
  - **Décoration de la salle** (objets nommés `Decor …`) :
    - au-dessus de la cheminée, un portrait encadré, avec deux chandeliers sur le manteau ;
    - un second portrait sur le mur du fond, côté salle ;
    - un bougeoir sur chaque table, avec une lumière chaude sur celles qui n'en avaient pas.
  - **Fleurs** (objets nommés `Flowers …`) : une seule famille, des pots en terre cuite garnis de fleurs roses du pack Fantasy Kingdom, en deux tailles :
    - `PF_Flower_Pot_Small` : sur chaque table, près de la caisse, au milieu du manteau de la cheminée, et sur une étagère de la cuisine ;
    - `PF_Flower_Pot_Large` : deux pots au sol sous la fenêtre du mur du fond.

    S'y ajoutent un panier de lavande suspendu entre la fenêtre et la cheminée, et les vasques de roses rouges déjà en place. Les deux pots sont des prefabs qui imbriquent les pièces Synty, sans copie.

    Les petits objets de déco n'ont pas de collider : une plante posée à l'entrée soulevait le NavMesh de 40 cm.
  - **Objets Synty du café** : chaque objet Synty posé dans la salle passe par une Prefab Variant de `Prefabs/Props/Cafe/` (`PF_Bar_Bottle_01`, `PF_Cafe_Machine_01`, `PF_House_Floor_Wood_01`...). Elle porte le nom Synty, sans son préfixe `SM_Prop_`, `SM_Item_` ou `SM_Bld_`. Une variant ne contient que des références vers le paquet privé, jamais de mesh ni de texture : le dépôt public reste sans contenu Synty. Pour ajouter un objet : créer sa variant d'abord, puis la poser dans la scène.
  - **Tuyau du poêle** : sa rallonge a son propre matériau gris foncé uni (`M_Stove_Pipe`). Le matériau du poêle est un atlas de textures, qui donnait des rayures de couleur sur un cylindre.
  - **Règles de déco** : un seul matériau par ensemble (bois brun). Les meubles sont alignés sur les murs et entre eux, et les petits objets sont groupés par usage plutôt qu'éparpillés. Les scripts de mise en place calent les pièces sur leurs boîtes englobantes, pour ne laisser ni trou ni chevauchement visible.
  - **Tables** :
    - des tables rondes de deux ou trois places réparties dans la salle (sept en tout) ;
    - le coin cheminée : la table basse sur son tapis rond, avec ses deux fauteuils tournés vers le feu.
  - **Passages** : le NavMesh garde 50 cm autour des obstacles. Deux meubles à moins d'un mètre l'un de l'autre ferment donc le passage aux clients. Le script de mise en place vérifie que chaque place est accessible depuis l'entrée.
  - **Entrée** : un paillasson dans le coin bas de la salle, côté bord avant. Les clients entrent et sortent par là. L'ancienne porte du fond est remplacée par une fenêtre.
  - **Murs et sol** : chaque mur fait deux étages de prefabs. En bas, les murs reprennent le motif des murs à fenêtre (`Wall_04`). En haut, ils gardent leur motif d'origine (`Wall_02`). Le sol utilise partout la même planche (`Floor_Wood_01`).
  - `CafeCustomer` fait marcher un client sur le NavMesh (animation `Gait`). Il s'arrête dans l'allée, au plus près de sa chaise, puis glisse dessus en 0,4 s en s'asseyant (`Sitting`). Il refait ce chemin à l'envers pour se lever.
  - Un client assis devient un obstacle (`NavMeshObstacle`, sans découpe) que les autres contournent.
  - Les clients se tiennent à 0,3 m les uns des autres (`customerRadius`).
- **NavMesh** : le `NavMeshSurface` « Customer NavMesh » est calculé à partir des colliders, et ses données sont dans `Scenes/SC_Cafe/`. Seule la patronne est ignorée (`NavMeshModifier`). Les chaises restent des obstacles, pour que les clients ne les traversent pas. Après avoir déplacé un meuble, il faut recalculer le NavMesh (bouton « Bake » du composant).
- **Animations** : `AC_Customer` reprend la marche de la patronne et ajoute l'état « Sit », qui joue `SittingIdle`, une animation Mixamo en Humanoid, rangée dans le package privé `moonbrew-synty` (`Packs/Mixamo/Animations`), car on ne peut pas la publier dans ce dépôt. `SittingTalking` est importée pour plus tard. La patronne passe à travers les clients pour l'instant.
- **À suivre** : parler à une table prête pour prendre sa commande (VS-26), puis servir et faire payer (VS-28). Ces étapes ajouteront le départ après le service.

## Temps, économie du café et sauvegarde

- **Temps** : une horloge unique dans Core émet DayStarted, PhaseChanged et DayEnded. Les phases (matin, journée, soir, nuit) sont des repères et la journée est libre, mais il faut rentrer dormir en fin de journée : sinon, perte d'argent ou d'une partie de l'inventaire et stamina réduite le lendemain (valeurs à définir). La durée des phases est une donnée, pas une valeur codée en dur. Pas de saisons dans le slice.
- **Revenu du café** : clients servis × prix du plat, qui dépend de son niveau de 1 à 5 étoiles (seul axe de qualité), avec un facteur de présence de la patronne. L'employé ne sert que les recettes 1 étoile ; la patronne sert toutes les recettes connues et attire les clients importants. Un ingrédient manquant s'achète en boutique, 2 à 3 fois plus cher que produit. Toutes ces valeurs viennent des données (RecipeData, EmployeeData) pour pouvoir équilibrer sans toucher au code.
- **Patronne absente** : proposition, la phase de service est calculée avec les mêmes règles à la fin de la journée, et le bilan de la nuit en montre le résultat.
- **Sauvegarde** : un fichier JSON par partie (`game_<id>.json`), dans le dossier `Saves` des données persistantes de la plateforme. `SaveStore` liste les parties, de la plus récente à la plus ancienne, et en charge, en écrit ou en efface une. Chaque fichier porte un numéro de version (`SaveData.CurrentVersion`). Un fichier vide, corrompu ou venant d'une version plus récente est ignoré, sans erreur. Pour changer le format : augmenter la version, ajouter une étape dans `SaveSerializer.Migrate`, et un test qui charge l'ancienne version. Les contenus sont référencés par leur identifiant ; `SO_ItemCatalog` retrouve les objets à partir de leurs identifiants. Les ScriptableObjects ne sont jamais modifiés à l'exécution.
- **Cycle de la journée** (`GameSession`, dans Core) :
  - **Pas de sauvegarde en pleine journée.** Le jeu sauvegarde seul à la fin de la journée : quand la patronne dort (porte de la maison, `farm_house_door`, avec confirmation), ou à 2h du matin. Quitter en pleine journée perd ce qui s'est passé depuis le matin.
  - **Fin de journée** : la session publie `GameSaving`. Chaque module y écrit son état et sa part du récap ; l'inventaire y met ce que le sac et le stock ont gagné ou perdu depuis le matin. Puis la session écrit le fichier et recharge la scène du réveil (`SO_SessionConfig`).
  - **Au lancement et chaque matin** : la session charge la partie la plus récente et publie `GameLoaded`, ou `NewGameStarted` pour une partie neuve, qui reçoit alors les objets de départ. Le récap s'affiche s'il y en a un, et l'horloge ne démarre qu'à sa fermeture. Le récap reste dans la sauvegarde jusqu'à la fin de la journée suivante : en relançant le jeu, on le revoit.
  - **Passage entre la ferme et le café** : le chemin de droite de la ferme et la petite marche en parquet devant l'entrée du café (`Entrance Step`, comme dans Habbo ou Animal Crossing) portent chacun un `ScenePortal`, une zone invisible. Quand la patronne (tag `Player`) y entre, il publie `TravelRequested`, avec la scène visée et le point d'arrivée. La session met l'horloge en pause et publie `GameSaving` sur une copie de la journée (`SaveData.ForSameDay`), sans rien écrire sur le disque. Elle confie cette copie et l'heure à `SO_DayCarry`, puis publie `TravelStarted` : l'écran s'assombrit (`ScreenFader`, durée dans `SO_SessionConfig`) et la scène suivante se charge. Sa session reprend la journée : `GameLoaded`, puis la patronne est placée sur le `SceneArrival` demandé et l'horloge repart à la même minute. `TravelArrived` éclaircit enfin l'écran. Le trajet est instantané.
  - **Une journée sur plusieurs scènes** : chaque module remplace sa propre partie de la sauvegarde au lieu de l'ajouter, et les parties absentes de la scène sont gardées telles quelles. Le récap du sac additionne les mouvements de toutes les scènes (`DayRecap.Add`). Un moulin continue de tourner pendant l'absence de la patronne : chaque poste note l'heure du dernier passage (version 6 de la sauvegarde) et rattrape le temps écoulé au retour.
  - **Fin de journée hors de la ferme** : le café n'est pas la scène du réveil (`isWakeScene` désactivé). Si la journée finit au café, la session y emporte la journée et charge la ferme, où la nuit passe et où la sauvegarde s'écrit. Le récap ne s'affiche que dans la scène du réveil, si bien qu'un lancement direct du café démarre la journée sans récap.
  - **Plusieurs parties** : chaque partie a son fichier. F8 (debug) commence une nouvelle partie sans toucher aux autres. Le menu principal (scène `SC_Title`, première du build) choisit la partie : il la marque comme jouée à l'instant, et la session, qui ouvre toujours la partie jouée en dernier, la charge.

## Langue du jeu

- **Tout le jeu est en anglais** : HUD, fenêtres, noms des objets, recettes, clients et répliques. Les traductions, dont le français, viendront plus tard avec un système de localisation (VS-60).
- **Où vivent les textes** : les textes de contenu sont dans les ScriptableObjects (`displayName`, `lines`). Les textes d'interface sont dans les champs réglables des composants Ui (`dayFormat`, `phaseNames`, `recapTitleFormat`, `atStoveHint`...) et dans les modèles UXML. Aucune phrase affichée n'est écrite en dur dans la logique, pour qu'une table de traduction puisse les remplacer plus tard.
- **Les valeurs réglées dans les scènes** l'emportent sur les valeurs par défaut du code : quand on change un texte, il faut le changer aux deux endroits.

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
