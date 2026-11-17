# 08\_Directives\_Devlog — Moonbrew

Nov 17, 2026 · @Ileana

## Objectif

Ce document fixe comment écrire le devlog de Moonbrew pour que Claude puisse en tirer des vidéos à publier sur les réseaux, sans avoir à poser de questions. Il s'adresse à deux lectrices :

- **Ileana**, qui écrit une entrée de devlog après chaque session ou chaque fonctionnalité terminée et qui filme des clips ;
- **Claude**, qui lit les entrées et prépare le script, la liste de plans, les sous-titres et la légende de chaque vidéo.

Le devlog sert aussi de journal de bord privé : les décisions et les problèmes y sont notés avec leur raison.

## Où ranger les fichiers

- **Entrées** : un fichier Markdown par entrée dans `D``ocs/``D``evlog/` du dépôt, nommé `AAAA-MM-JJ_sujet.md` (par exemple `2026-11-20_rendu-pixel.md`).
- **Clips et captures** : dans un dossier hors du dépôt (les vidéos sont trop lourdes pour Git), par exemple `Devlog/clips/`. Chaque fichier reprend la date et le sujet de l'entrée : `2026-11-20_rendu-pixel_avant.mp4`, `2026-11-20_rendu-pixel_apres.mp4`, `2026-11-20_rendu-pixel_01.png`.
- **Vidéos finies** : dans `Devlog/``V``ideos/`, avec le même préfixe.
- **Modèle** : `D``ocs/``D``evlog/_modele.md`, copié à chaque nouvelle entrée.

Le dépôt ne contient jamais de fichiers des packs Synty : les captures et les clips montrent notre jeu, ils ne les contiennent pas en tant qu'assets.

## Format d'une entrée

Chaque entrée commence par un en-tête (date, titre, tickets, format voulu) puis contient les champs ci-dessous. Un champ qui n'a pas de contenu est écrit « aucun », jamais laissé vide : Claude sait alors que ce n'est pas un oubli.

- **But** : une phrase sur ce que la session devait faire, avec le ticket VS concerné.
- **Résultat** : ce qui marche maintenant, en trois puces au maximum.
- **Moment fort** : le plan ou la chose qui fait « waouh », en une ligne. Il devient l'accroche de la vidéo.
- **Problème et solution** : un bug ou un choix, et comment il a été réglé. C'est souvent la partie la plus intéressante.
- **Clips** : pour chaque fichier, son nom, sa durée et ce qu'on y voit.
- **Avant et après** : si possible, un clip ou une capture de l'état précédent et du nouvel état.
- **Suite** : la prochaine étape, qui termine la vidéo sur une ouverture.
- **Format** : court (vertical), long (horizontal) ou les deux.

Modèle à copier :

```
---
date: 2026-11-20
titre: Le rendu pixelisé
tickets: VS-7, VS-8
format: court
---
But : ...
Résultat :
- ...
- ...
Moment fort : ...
Problème et solution : ...
Clips :
- 2026-11-20_rendu-pixel_avant.mp4 (8 s) : la scène sans pixelisation
- 2026-11-20_rendu-pixel_apres.mp4 (8 s) : la même scène pixelisée
Avant et après : ...
Suite : ...
```

## Règles de captation vidéo

- **Clips courts** : 5 à 15 secondes, un par fonctionnalité, avec et sans l'interface (HUD) quand c'est possible.
- **Résolution** : celle du jeu, en plein écran, sans bordure ni fenêtre d'éditeur visible.
- **Caméra** : pas de mouvement inutile, pour que le clip se lise facilement.
- **Avant** : filmer l'état précédent avant de modifier quelque chose. C'est la partie la plus difficile à refaire après coup.
- **Captures** : prendre des images nettes aux bons moments (lever du soleil, café allumé, client servi).
- **Nommage immédiat** : renommer les fichiers dès l'enregistrement, avec le préfixe de l'entrée.
- **Vie privée** : masquer les notifications, la barre des tâches et toute information personnelle avant de filmer.

## Directives pour Claude : de l'entrée à la vidéo

Quand Ileana demande une vidéo à partir d'une entrée, Claude suit ces étapes.

1. **Lire l'entrée et la vérifier.** Si un champ manque sans la mention « aucun », ou si un clip listé n'existe pas, Claude le signale avant d'écrire quoi que ce soit.
2. **Choisir l'angle.** L'accroche est le moment fort de l'entrée ; le reste suit l'ordre but, déroulé, problème et solution, résultat, suite.
3. **Écrire le script** en anglais, selon le format voulu (voir la section suivante). Les entrées de devlog peuvent être écrites en français ou en anglais.
4. **Dresser la liste de plans.** Chaque plan indique le clip (par son nom de fichier), sa durée, le texte affiché à l'écran et le sous-titre.
5. **Proposer la publication** : titre, légende et hashtags adaptés à chaque plateforme, avec l'attribution de la musique si elle est exigée.
6. **Livrer** le script en anglais, la liste de plans, le texte à l'écran et les sous-titres dans un fichier séparé. La voix off est générée à partir de la voix d'Ileana avec ElevenLabs (forfait Starter, clonage instantané). Le montage se fait ensuite avec un outil de montage si la session en propose, sinon Claude donne un plan précis à suivre dans le logiciel d'Ileana.

Règles à respecter pour chaque vidéo :

- **Ne jamais inventer** une fonctionnalité, un chiffre ou un résultat qui n'est ni dans l'entrée ni visible dans le clip.
- **N'utiliser que les clips listés** dans l'entrée.
- **Texte à l'écran et accessibilité** : tout ce qui est dit est aussi affiché à l'écran, en quelques mots par plan, lisibles sur un téléphone. Les sous-titres complets sont livrés dans un fichier séparé.
- **Rester honnête** sur les difficultés : un problème résolu vaut mieux qu'un faux « tout fonctionne ».
- **Ton** : doux et poétique, comme le jeu.
- **Respecter les règles de licence** de la section dédiée.

## Formats de vidéo

Les durées sont des propositions de départ à ajuster.

| Format | Plateformes | Durée | Structure |
| --- | --- | --- | --- |
| Court, vertical (9:16) | TikTok, YouTube Shorts | 15 à 45 secondes | Accroche avec le moment fort dans les 3 premières secondes, une phrase de contexte, 1 à 3 plans, puis la suite en une ligne |
| Long, horizontal (16:9) | YouTube (plus tard) | 3 à 8 minutes | Accroche, rappel du projet en une phrase, but, déroulé, problème et solution, résultat, suite |

Au début, seules les vidéos courtes sont produites, une par fonctionnalité terminée ; la vidéo longue viendra plus tard. Le champ « Format » de l'entrée précise ce qui est voulu.

## Licences et règles de publication

- **Captures de notre jeu** : autorisées pour la promotion, avec l'abonnement comme avec l'achat à l'unité. Les images, rendus et logos de Synty eux-mêmes ne sont pas utilisables.
- **Ne pas faire croire que les assets sont faits maison** : citer les packs Synty (par exemple dans la description ou le générique).
- **Abonnement et nouvelles vidéos** : après la fin de l'abonnement, on ne peut plus créer de nouveaux contenus promotionnels avec les assets, même si ceux déjà publiés peuvent rester en ligne. Tant que les packs ne sont pas achetés à l'unité, il faut un abonnement actif pour produire de nouvelles vidéos.
- **IA** : l'IA sert au code, à la rédaction des scripts et à la voix off, générée à partir de la voix d'Ileana. TikTok demande de signaler les contenus avec des voix ou des médias réalistes générés par IA, avec son réglage « contenu généré par IA ». YouTube demande de déclarer, dans YouTube Studio, le contenu « altéré ou synthétique » qui paraît réaliste ; selon les sources, le cas d'une voix clonée qui est la sienne n'est pas traité de la même façon. Par prudence, activer ces réglages pour les vidéos avec voix générée, et relire les règles à jour au moment de publier. Steam demande de déclarer le contenu généré par IA qui est consommé par les joueurs, ce qui peut concerner les supports de la page du jeu.
- **Musique** : un morceau par vidéo, pris dans l'une de ces trois bibliothèques gratuites : Pixabay Music (sans attribution), Uppbeat en formule gratuite (attribution obligatoire, à mettre dans la description avec la formule exacte demandée par Uppbeat) ou Mixkit (sans attribution). Les vidéos de Moonbrew sont promotionnelles : ne pas utiliser les sons tendance de TikTok, et valider la confirmation d'usage de la musique à l'envoi. Garder la preuve de la licence de chaque morceau (capture de la page et du texte de licence) et noter le titre, la source et l'attribution éventuelle dans la livraison. Relire les conditions de chaque bibliothèque, qui peuvent changer.
- **Vie privée** : aucune information personnelle visible dans les clips.
