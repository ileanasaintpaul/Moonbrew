# 07\_Devlog — Moonbrew

Nov 17, 2026 · @Ileana

Le devlog raconte le développement de Moonbrew en vidéo, pour faire connaître le jeu bien avant sa sortie. Il prépare le public qui mettra le jeu en liste de souhaits quand la page Steam ouvrira.

**La référence pour écrire une entrée, ranger les clips et produire une vidéo est `08_Directives_Devlog`.** Ce document-ci donne le plan d'ensemble, les épisodes prévus et les outils de capture.

## Pourquoi commencer maintenant

- **Les avant/après font les meilleurs clips.** Les images du début ne se rattrapent pas plus tard ; il faut donc capturer chaque étape au moment où elle arrive.
- **Il y a déjà de quoi montrer :**
  - le rendu pixelisé, avec la correction du scintillement ;
  - la caméra isométrique ;
  - la ferme et le café fantasy ;
  - la patronne qui se déplace ;
  - l'horloge.
- **Raconter les débuts attache le public au projet** : pourquoi Moonbrew, et pourquoi le look *A Short Hike*.
- **La licence Synty autorise l'usage de captures du jeu pour sa promotion** (voir `08_Directives_Devlog`, section licences).

## Entrées

Une entrée par fonctionnalité terminée, dans `Docs/Devlog/AAAA-MM-JJ_sujet.md`, sur le modèle `Docs/Devlog/_modele.md`. Les clips et captures sont dans `Devlog/clips/` (hors du dépôt), avec le même préfixe que l'entrée.

Entrées déjà écrites :

| Entrée | Tickets | Format |
| --- | --- | --- |
| `2026-11-16_fondations-projet` | VS-5, VS-6 | long |
| `2026-11-16_rendu-pixel` | VS-7, VS-8 | court |
| `2026-11-16_ferme-du-premier-jour` | VS-10 | court |
| `2026-11-16_cafe-fantasy` | VS-10 | court |
| `2026-11-17_deplacement-patronne` | VS-11 | court |
| `2026-11-17_horloge` | VS-12 | court |
| `2026-11-17_bus-evenements` | VS-13 | long |
| `2026-11-17_mode-capture` | VS-42 | long |
| `2026-11-17_palettes` | VS-9 | court |
| `2026-11-17_inventaire` | VS-14 | court |
| `2026-11-17_sauvegarde` | VS-15 | court |
| `2026-11-17_cultures` | VS-17 | court |
| `2026-11-17_plantation` | VS-18 | court |
| `2026-11-17_arrosage` | VS-19 | court |
| `2026-11-17_recolte` | VS-20 | court |
| `2026-11-17_fraise` | VS-21 | long |
| `2026-11-17_moulin` | VS-22 | court |
| `2026-11-18_recettes` | VS-23 | court |
| `2026-11-18_clients` | VS-24 | court |
| `2026-11-18_service` | VS-25 | court |
| `2026-11-18_passage` | VS-48 | court |
| `2026-11-18_hud` | VS-58 | court |
| `2026-11-19_anglais` | VS-60 | court |
| `2026-11-20_lumiere-cafe` | — | court |
| `2026-11-20_amenagement-cafe` | — | court |
| `2026-11-20_jour-nuit-cafe` | — | court |
| `2026-11-20_patronne-lisible` | — | court |
| `2026-11-20_parquet-cafe` | — | court |
| `2026-11-20_jour-nuit-ferme` | — | court |
| `2026-11-20_interface-pixel` | — | court |
| `2026-11-20_retouches-cafe` | — | court |
| `2026-11-20_menu-principal` | — | court |
| `2026-11-20_menu-pause` | — | court |
| `2026-11-21_ciel-pixabay` | — | court |
| `2026-11-21_creation-patronne` | — | court |
| `2026-11-21_patronne-modulaire` | — | court |
| `2026-11-23_creation-style-synty` | — | court |
| `2026-11-23_prise-de-commande` | VS-26 | court |
| `2026-11-23_frigo-cafe` | VS-44 | court |
| `2026-11-23_vitrine` | VS-45 | court |
| `2026-11-23_preparation-commande` | VS-27 | court |
| `2026-11-23_service-paiement` | VS-28 | court |
| `2026-11-23_argent` | VS-29 | court |
| `2026-11-23_cafe-sans-patronne` | VS-49 | court |
| `2026-11-23_employe` | VS-31 | court |

Les entrées jusqu'à VS-42 ne contiennent que des images fixes, prises pendant le développement : aucune vidéo n'a encore été filmée.

## Plan des épisodes longs

Les vidéos courtes viennent d'abord, une par fonctionnalité terminée. Les épisodes longs viendront plus tard et suivront les étapes du vertical slice (voir `02_Perimetre_Vertical_Slice`).

| Épisode | Étape | Tickets | Sujets |
| --- | --- | --- | --- |
| 1. Fondations | Fondations et rendu | VS-5 à VS-13, VS-42, puis VS-9 | Le projet et son ambition ; le rendu pixelisé et le combat contre le scintillement ; la ferme du premier jour, du terrain vide à la scène finale ; le café fantasy ; la patronne qui bouge et la caméra isométrique ; l'horloge et le HUD ; le bus d'événements, expliqué simplement |
| 2. Lumière et couleurs | Fin de l'étape 1 | VS-9, inventaire, sauvegarde | Les palettes par zone, l'éclairage doux, le jour et la nuit : le plus fort avant/après visuel |
| 3. De la graine au gâteau | Ferme et cuisine | Étape 2 | Planter, arroser, récolter, faire de la farine, cuisiner |
| 4. Le café ouvre | Café et employé | Étape 3 | Les premiers clients, le service, l'employé qui prend le relais |
| 5. Une journée complète | PNJ, relations, quête et bilan | Étape 4 | Les habitants, une quête, le bilan du soir : la première vraie partie |

## Mode capture (VS-42)

Ces raccourcis sont actifs dans l'éditeur et dans les builds de développement seulement. Ils sont définis dans la carte d'actions « Debug » de `InputSystem_Actions`, et les vitesses et limites de zoom se règlent dans `SO_CaptureConfig`. Les touches sont données pour un clavier QWERTY.

| Touche | Effet |
| --- | --- |
| F1 | Cacher ou réafficher le HUD |
| F2 | Basculer entre le rendu pixelisé et le rendu net |
| F3 | Caméra libre (mode photo) : la patronne ne bouge plus, la caméra se détache |
| WASD (QWERTY) ou flèches (caméra libre) | Déplacer la caméra |
| Molette ou Page préc. / Page suiv. (caméra libre) | Zoomer ou dézoomer |
| F4 | Activer ou couper la palette de la zone (pour un avant/après) |
| F5 | Mettre l'horloge en pause, ou la relancer |
| F6 | Avancer d'une heure |
| F7 | Passer à la phase suivante (matin, journée, soir, nuit, puis le lendemain matin) |
| F8 | Commencer une nouvelle partie (les autres parties gardent leur sauvegarde) |
| F9 | Apprendre toutes les recettes, en attendant la boutique |

En repassant F3, la caméra suit de nouveau la patronne, avec le zoom d'origine.

## Matériel et réglages de capture

- **OBS Studio**, en capture de la fenêtre du jeu :
  - 1080×1920 (vertical, 9:16) pour les vidéos courtes, en recadrant la scène ;
  - 1920×1080 à 60 i/s pour les vidéos longues, plus tard.
- **Le jeu affiché en 1920×1080**, pour que l'image de 640×360 soit agrandie exactement ×3 : de gros pixels nets, sans flou.
- **Un build de développement** donne des images plus fluides que l'éditeur, sans fenêtre d'éditeur visible.
- **Unity Recorder** est possible plus tard pour des captures image par image parfaites. C'est un package à ajouter au projet, donc à valider d'abord.
- **Attention : `Devlog/` n'est sauvegardé nulle part,** puisque Git l'ignore. Il vaut mieux le synchroniser avec un service cloud (OneDrive, Google Drive), ou le copier régulièrement sur un disque externe.

## Page Steam

- On l'ouvre quand le café et la boucle ferme → service se voient en jeu, idéalement six mois à un an avant la sortie.
- Le devlog sert à préparer ce moment : chaque vidéo renverra vers la page Steam dès qu'elle existera.
- Pour la page, il faudra une bande-annonce courte, cinq captures fortes minimum, et une phrase d'accroche (*ta ferme nourrit ton café, ton café nourrit ton village*).
