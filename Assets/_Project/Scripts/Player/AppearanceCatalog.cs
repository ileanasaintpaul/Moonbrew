using System;
using System.Collections.Generic;
using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Player
{
    // Everything the owner's look can be made of, and the rules that turn a saved look into a valid one.
    [CreateAssetMenu(menuName = "Moonbrew/Player/Appearance Catalog", fileName = "SO_AppearanceCatalog")]
    public class AppearanceCatalog : ScriptableObject
    {
        [Tooltip("The first body stands in for a saved one that no longer exists.")]
        [SerializeField] private AppearancePart[] parts = new AppearancePart[0];
        [Tooltip("For the hair and the eyebrows. The first colour stands in for a saved one that no longer exists.")]
        [SerializeField] private AppearanceColour[] hairColours = new AppearanceColour[0];
        [Tooltip("The first colour stands in for a saved one that no longer exists.")]
        [SerializeField] private AppearanceColour[] skinColours = new AppearanceColour[0];
        [Tooltip("For the top and the bottom. The first colour stands in for a saved one that no longer exists.")]
        [SerializeField] private AppearanceColour[] outfitColours = new AppearanceColour[0];
        [Tooltip("The look of a new owner, and of an owner from a save made before looks could be chosen.")]
        [SerializeField] private AppearanceSave defaultLook = new AppearanceSave();
        [SerializeField, Min(1)] private int maxNameLength = 16;

        private static readonly AppearanceColourSlot[] Slots = (AppearanceColourSlot[])Enum.GetValues(typeof(AppearanceColourSlot));

        public IReadOnlyList<AppearancePart> Parts => parts;

        public IReadOnlyList<AppearanceColour> HairColours => hairColours;

        public IReadOnlyList<AppearanceColour> SkinColours => skinColours;

        public IReadOnlyList<AppearanceColour> OutfitColours => outfitColours;

        public int MaxNameLength => maxNameLength;

        public static bool IsOptional(AppearanceCategory category) => category != AppearanceCategory.Body;

        public static string ColourId(AppearanceSave look, AppearanceColourSlot slot)
        {
            switch (slot)
            {
                case AppearanceColourSlot.Hair: return look.hairColourId;
                case AppearanceColourSlot.Brows: return look.browColourId;
                case AppearanceColourSlot.Skin: return look.skinColourId;
                case AppearanceColourSlot.Top: return look.topColourId;
                default: return look.bottomColourId;
            }
        }

        public static void SetColourId(AppearanceSave look, AppearanceColourSlot slot, string id)
        {
            switch (slot)
            {
                case AppearanceColourSlot.Hair: look.hairColourId = id; break;
                case AppearanceColourSlot.Brows: look.browColourId = id; break;
                case AppearanceColourSlot.Skin: look.skinColourId = id; break;
                case AppearanceColourSlot.Top: look.topColourId = id; break;
                default: look.bottomColourId = id; break;
            }
        }

        // For catalogs built from code.
        public void Init(AppearancePart[] catalogParts, AppearanceColour[] hair, AppearanceColour[] skin, AppearanceColour[] outfit, int nameLength = 16, AppearanceSave startingLook = null)
        {
            defaultLook = startingLook ?? new AppearanceSave();
            parts = catalogParts ?? new AppearancePart[0];
            hairColours = hair ?? new AppearanceColour[0];
            skinColours = skin ?? new AppearanceColour[0];
            outfitColours = outfit ?? new AppearanceColour[0];
            maxNameLength = Math.Max(1, nameLength);
        }

        public List<AppearancePart> PartsOf(AppearanceCategory category)
        {
            var found = new List<AppearancePart>();
            foreach (var part in parts)
            {
                if (part != null && part.Category == category)
                {
                    found.Add(part);
                }
            }

            return found;
        }

        // Null when no part of this category has this ID.
        public AppearancePart FindPart(AppearanceCategory category, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var part in parts)
            {
                if (part != null && part.Category == category && part.Id == id)
                {
                    return part;
                }
            }

            return null;
        }

        public IReadOnlyList<AppearanceColour> ColoursOf(AppearanceColourSlot slot) => Colours(slot);

        // Null when the colours of this slot have no such ID.
        public AppearanceColour FindColour(AppearanceColourSlot slot, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return Array.Find(Colours(slot), colour => colour != null && colour.Id == id);
        }

        // A copy of the look in which unknown IDs fall back: the body and each colour to the first one of its list,
        // the other parts to none. A look without a body was never chosen (no look, an older save): it is the default look.
        public AppearanceSave Normalize(AppearanceSave look)
        {
            if (look == null || string.IsNullOrEmpty(look.bodyId))
            {
                look = defaultLook ?? new AppearanceSave();
            }

            var normal = new AppearanceSave
            {
                bodyId = FindPart(AppearanceCategory.Body, look.bodyId)?.Id ?? FirstId(PartsOf(AppearanceCategory.Body)),
                hairId = FindPart(AppearanceCategory.Hair, look.hairId)?.Id ?? "",
                beardId = FindPart(AppearanceCategory.Beard, look.beardId)?.Id ?? "",
                hatId = FindPart(AppearanceCategory.Hat, look.hatId)?.Id ?? "",
                accessoryId = FindPart(AppearanceCategory.Accessory, look.accessoryId)?.Id ?? "",
            };
            foreach (var slot in Slots)
            {
                SetColourId(normal, slot, FindColour(slot, ColourId(look, slot))?.Id ?? FirstId(Colours(slot)));
            }

            return normal;
        }

        // Any body and colours; each other part may also be none.
        public AppearanceSave Random(System.Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var look = new AppearanceSave
            {
                bodyId = Pick(PartsOf(AppearanceCategory.Body), random, false),
                hairId = Pick(PartsOf(AppearanceCategory.Hair), random, true),
                beardId = Pick(PartsOf(AppearanceCategory.Beard), random, true),
                hatId = Pick(PartsOf(AppearanceCategory.Hat), random, true),
                accessoryId = Pick(PartsOf(AppearanceCategory.Accessory), random, true),
            };
            foreach (var slot in Slots)
            {
                SetColourId(look, slot, Pick(Colours(slot), random));
            }

            return look;
        }

        private AppearanceColour[] Colours(AppearanceColourSlot slot)
        {
            switch (slot)
            {
                case AppearanceColourSlot.Hair:
                case AppearanceColourSlot.Brows:
                    return hairColours;
                case AppearanceColourSlot.Skin:
                    return skinColours;
                default:
                    return outfitColours;
            }
        }

        private static string FirstId(List<AppearancePart> list) => list.Count > 0 ? list[0].Id : "";

        private static string FirstId(AppearanceColour[] list)
        {
            var first = Array.Find(list, colour => colour != null);
            return first != null ? first.Id : "";
        }

        // "None" counts as one more choice for the optional parts.
        private static string Pick(List<AppearancePart> list, System.Random random, bool canBeNone)
        {
            var choices = list.Count + (canBeNone ? 1 : 0);
            if (choices == 0)
            {
                return "";
            }

            var index = random.Next(choices);
            return index < list.Count ? list[index].Id : "";
        }

        private static string Pick(AppearanceColour[] list, System.Random random)
        {
            var valid = Array.FindAll(list, colour => colour != null);
            return valid.Length == 0 ? "" : valid[random.Next(valid.Length)].Id;
        }
    }
}
