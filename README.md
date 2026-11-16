# Moonbrew

[![Checks](https://github.com/ileanasaintpaul/Moonbrew/actions/workflows/checks.yml/badge.svg)](https://github.com/ileanasaintpaul/Moonbrew/actions/workflows/checks.yml)
[![Unity tests](https://github.com/ileanasaintpaul/Moonbrew/actions/workflows/unity-tests.yml/badge.svg)](https://github.com/ileanasaintpaul/Moonbrew/actions/workflows/unity-tests.yml)
[![Build](https://github.com/ileanasaintpaul/Moonbrew/actions/workflows/build.yml/badge.svg)](https://github.com/ileanasaintpaul/Moonbrew/actions/workflows/build.yml)

A cozy fantasy farm-and-café life sim built in Unity. Grow your ingredients, bake, serve customers and complete quests in a pixelated 3D world.

> **Status:** early development. The first goal is a playable vertical slice (see `Docs/02_Perimetre_Vertical_Slice`).

## Concept

You run a café in a fantasy world. Your farm feeds your kitchen, your kitchen feeds your customers, and your customers bring you money, quests and friendships.

A day in Moonbrew:

1. **Morning**: tend your farm (harvest, water, care for animals later on).
2. **Daytime**: run the café. Take orders, prepare and serve. If you are elsewhere, your employee keeps the counter open.
3. **Evening**: explore the village, complete quests, build relationships, shop and decorate.
4. **Night**: end-of-day summary and save.

You are never forced to work at the café, but you earn more when you are there: better cooking, special recipes and important customers.

## Pillars

- **A loop where everything feeds everything else**: crops, recipes, customers, quests, unlocks.
- **Freedom of pace**: farm, serve or explore in the order you like.
- **Fast service, stories elsewhere**: the café is a smooth work phase, quests live in the village.
- **A game that grows**: all content (ingredients, recipes, customers, quests) is data, so adding content never requires touching the code.

## Visual direction

Low-poly 3D rendered at low resolution with a reduced palette, seen from a 3/4 top-down camera. The reference is *A Short Hike*.

## Tech

- Unity (URP), C#
- ScriptableObjects for all game data
- Event-driven systems, one module per feature
- Versioned save system

## Project structure

```
Assets/
  _Project/
    Art/            Models, textures, materials, palette
    Audio/
    Data/           ScriptableObjects (items, recipes, customers, quests)
    Prefabs/
    Scenes/
    Scripts/
      Core/         Time, events, save, game state
      Inventory/    Items and café stock
      Player/
      Farming/
      Kitchen/
      Cafe/
      Employee/
      Economy/
      Npc/
      Quests/
      Rendering/    Pixel rendering, camera, palettes
      Ui/
    Tests/          EditMode and PlayMode tests
Docs/               Pitch, vertical slice scope, design notes
```

## Getting started

1. Install Unity Hub and the Unity version listed in `ProjectSettings/ProjectVersion.txt`.
2. Clone the repository (Git LFS is required for large assets): `git lfs install && git clone <repo-url>`
3. Open the project from Unity Hub and open the main scene.

## Roadmap

1. Foundations and rendering: player, 3/4 pixelated camera, clock, inventory, save.
2. Farm and kitchen: crops, flour, first recipes.
3. Café and employee: customers, orders, service, income.
4. NPCs, relationships, first quest and end-of-day summary.

After the vertical slice: villager quests and friendship, café layout and home decoration, animal farming and fishing, forest and mine expeditions with light combat, romance and marriage. Later: seasons, savory dishes, more staff, more areas.

## License

All rights reserved.
