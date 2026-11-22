using System;
using System.Collections.Generic;
using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Player
{
    // Everything the owner's look can be made of, the character model it is built on, and the rules that turn a saved look into a valid one.
    [CreateAssetMenu(menuName = "Moonbrew/Player/Appearance Catalog", fileName = "SO_AppearanceCatalog")]
    public class AppearanceCatalog : ScriptableObject
    {
        // Each body gauge goes from -ShapeLimit to ShapeLimit, as saved.
        public const int ShapeLimit = 100;

        [Tooltip("The first top and the first bottom stand in for saved ones that no longer exist.")]
        [SerializeField] private AppearancePart[] parts = new AppearancePart[0];
        [Tooltip("The first colour stands in for a saved one that no longer exists.")]
        [SerializeField] private AppearanceColour[] skinColours = new AppearanceColour[0];
        [Tooltip("For the hair, the eyebrows and the beard. The first colour stands in for a saved one that no longer exists.")]
        [SerializeField] private AppearanceColour[] hairColours = new AppearanceColour[0];
        [Tooltip("For the top and the bottom. The first colour stands in for a saved one that no longer exists.")]
        [SerializeField] private AppearanceColour[] outfitColours = new AppearanceColour[0];
        [Tooltip("The look of a new owner, and of an owner from a save made before looks could be chosen.")]
        [SerializeField] private AppearanceSave defaultLook = new AppearanceSave();
        [SerializeField, Min(1)] private int maxNameLength = 16;
        [Tooltip("The owner's pronoun choices on the name page; the first is the default.")]
        [SerializeField] private PronounSet[] pronouns = new PronounSet[0];

        [Header("Character model")]
        [Tooltip("The bare body, a model per body part: a part's model of the same body part replaces it.")]
        [SerializeField] private GameObject[] baseModels = new GameObject[0];
        [Tooltip("The material every model wears; each owner gets a copy with its own colour map.")]
        [SerializeField] private Material material;
        [Tooltip("The colour map the owner's colours start from.")]
        [SerializeField] private Texture2D baseColours;
        [Tooltip("The cells a hair colour paints, each with the shade it multiplies the colour by.")]
        [SerializeField] private CellColour[] hairCells = new CellColour[0];
        [SerializeField] private CellColour[] browCells = new CellColour[0];
        [SerializeField] private CellColour[] beardCells = new CellColour[0];
        [Tooltip("The cells of the top's clothes, dyed with the top colour.")]
        [SerializeField] private RectInt[] topCells = new RectInt[0];
        [Tooltip("The cells of the bottom's clothes, dyed with the bottom colour.")]
        [SerializeField] private RectInt[] bottomCells = new RectInt[0];
        [Tooltip("Cells an outfit never brings its colours to: skin, eyes, hair.")]
        [SerializeField] private RectInt[] bodyCells = new RectInt[0];
        [Tooltip("Names of the models' blend shapes for each body gauge.")]
        [SerializeField] private string typeBlendShape = "masculineFeminine";
        [SerializeField] private string heavyBlendShape = "defaultHeavy";
        [SerializeField] private string skinnyBlendShape = "defaultSkinny";
        [SerializeField] private string muscleBlendShape = "defaultBuff";
        [Tooltip("How much of the slim blend shape the slimmest body takes, from 0 to 100: the full shape is too thin.")]
        [SerializeField, Range(0f, 100f)] private float skinnyLimit = 25f;
        [Tooltip("How much of the muscle blend shape the leanest body keeps, from 0 to 50 (the middle): none looks starved.")]
        [SerializeField, Range(0f, 50f)] private float leanFloor = 35f;

        private static readonly AppearanceColourSlot[] Slots = (AppearanceColourSlot[])Enum.GetValues(typeof(AppearanceColourSlot));
        private static readonly AppearanceCategory[] Categories = (AppearanceCategory[])Enum.GetValues(typeof(AppearanceCategory));
        private static readonly BodyShape[] Shapes = (BodyShape[])Enum.GetValues(typeof(BodyShape));

        public IReadOnlyList<AppearancePart> Parts => parts;

        public IReadOnlyList<AppearanceColour> SkinColours => skinColours;

        public IReadOnlyList<AppearanceColour> HairColours => hairColours;

        public IReadOnlyList<AppearanceColour> OutfitColours => outfitColours;

        public int MaxNameLength => maxNameLength;

        public IReadOnlyList<PronounSet> Pronouns => pronouns;

        public IReadOnlyList<GameObject> BaseModels => baseModels;

        public Material Material => material;

        public Texture2D BaseColours => baseColours;

        public IReadOnlyList<RectInt> BodyCells => bodyCells;

        public string TypeBlendShape => typeBlendShape;

        public string HeavyBlendShape => heavyBlendShape;

        public string SkinnyBlendShape => skinnyBlendShape;

        public string MuscleBlendShape => muscleBlendShape;

        public float SkinnyLimit => skinnyLimit;

        public float LeanFloor => leanFloor;

        public static bool IsOptional(AppearanceCategory category) => category != AppearanceCategory.Top && category != AppearanceCategory.Bottom;

        public static string PartId(AppearanceSave look, AppearanceCategory category)
        {
            switch (category)
            {
                case AppearanceCategory.Top: return look.topId;
                case AppearanceCategory.Bottom: return look.bottomId;
                case AppearanceCategory.Hair: return look.hairId;
                case AppearanceCategory.Brows: return look.browsId;
                case AppearanceCategory.Beard: return look.beardId;
                default: return look.hatId;
            }
        }

        public static void SetPartId(AppearanceSave look, AppearanceCategory category, string id)
        {
            switch (category)
            {
                case AppearanceCategory.Top: look.topId = id; break;
                case AppearanceCategory.Bottom: look.bottomId = id; break;
                case AppearanceCategory.Hair: look.hairId = id; break;
                case AppearanceCategory.Brows: look.browsId = id; break;
                case AppearanceCategory.Beard: look.beardId = id; break;
                default: look.hatId = id; break;
            }
        }

        public static string ColourId(AppearanceSave look, AppearanceColourSlot slot)
        {
            switch (slot)
            {
                case AppearanceColourSlot.Skin: return look.skinColourId;
                case AppearanceColourSlot.Hair: return look.hairColourId;
                case AppearanceColourSlot.Brows: return look.browColourId;
                case AppearanceColourSlot.Beard: return look.beardColourId;
                case AppearanceColourSlot.Top: return look.topColourId;
                default: return look.bottomColourId;
            }
        }

        public static void SetColourId(AppearanceSave look, AppearanceColourSlot slot, string id)
        {
            switch (slot)
            {
                case AppearanceColourSlot.Skin: look.skinColourId = id; break;
                case AppearanceColourSlot.Hair: look.hairColourId = id; break;
                case AppearanceColourSlot.Brows: look.browColourId = id; break;
                case AppearanceColourSlot.Beard: look.beardColourId = id; break;
                case AppearanceColourSlot.Top: look.topColourId = id; break;
                default: look.bottomColourId = id; break;
            }
        }

        public static int ShapeValue(AppearanceSave look, BodyShape shape)
        {
            switch (shape)
            {
                case BodyShape.Type: return look.bodyType;
                case BodyShape.Size: return look.bodySize;
                default: return look.musculature;
            }
        }

        // Kept between -ShapeLimit and ShapeLimit.
        public static void SetShapeValue(AppearanceSave look, BodyShape shape, int value)
        {
            value = Mathf.Clamp(value, -ShapeLimit, ShapeLimit);
            switch (shape)
            {
                case BodyShape.Type: look.bodyType = value; break;
                case BodyShape.Size: look.bodySize = value; break;
                default: look.musculature = value; break;
            }
        }

        // For catalogs built from code.
        public void Init(AppearancePart[] catalogParts, AppearanceColour[] skin, AppearanceColour[] hair, AppearanceColour[] outfit, int nameLength = 16, AppearanceSave startingLook = null, PronounSet[] pronounSets = null)
        {
            defaultLook = startingLook ?? new AppearanceSave();
            parts = catalogParts ?? new AppearancePart[0];
            skinColours = skin ?? new AppearanceColour[0];
            hairColours = hair ?? new AppearanceColour[0];
            outfitColours = outfit ?? new AppearanceColour[0];
            maxNameLength = Math.Max(1, nameLength);
            pronouns = pronounSets ?? new PronounSet[0];
        }

        // The set with this ID, or the first one (the default) when there is none such. Null only without any set.
        public PronounSet FindPronouns(string id)
        {
            var found = string.IsNullOrEmpty(id) ? null : Array.Find(pronouns, set => set != null && set.Id == id);
            return found != null ? found : Array.Find(pronouns, set => set != null);
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

        // The cells a colour of this slot paints on its own (with their shade), or dyes among an outfit's cells.
        public IReadOnlyList<CellColour> PaintedCells(AppearanceColourSlot slot)
        {
            switch (slot)
            {
                case AppearanceColourSlot.Hair: return hairCells;
                case AppearanceColourSlot.Brows: return browCells;
                case AppearanceColourSlot.Beard: return beardCells;
                default: return Array.Empty<CellColour>();
            }
        }

        public IReadOnlyList<RectInt> DyedCells(AppearanceColourSlot slot)
        {
            switch (slot)
            {
                case AppearanceColourSlot.Top: return topCells;
                case AppearanceColourSlot.Bottom: return bottomCells;
                default: return Array.Empty<RectInt>();
            }
        }

        // A copy of the look in which unknown IDs fall back: the top, the bottom and each colour to the first one of its list,
        // the other parts to none. A look without a top was never chosen (no look, an older save): it is the default look.
        public AppearanceSave Normalize(AppearanceSave look)
        {
            if (look == null || string.IsNullOrEmpty(look.topId))
            {
                look = defaultLook ?? new AppearanceSave();
            }

            var normal = new AppearanceSave();
            foreach (var category in Categories)
            {
                var found = FindPart(category, PartId(look, category))?.Id;
                SetPartId(normal, category, found ?? (IsOptional(category) ? "" : FirstId(PartsOf(category))));
            }

            foreach (var slot in Slots)
            {
                SetColourId(normal, slot, FindColour(slot, ColourId(look, slot))?.Id ?? FirstId(Colours(slot)));
            }

            foreach (var shape in Shapes)
            {
                SetShapeValue(normal, shape, ShapeValue(look, shape));
            }

            return normal;
        }

        private AppearanceColour[] Colours(AppearanceColourSlot slot)
        {
            switch (slot)
            {
                case AppearanceColourSlot.Skin:
                    return skinColours;
                case AppearanceColourSlot.Top:
                case AppearanceColourSlot.Bottom:
                    return outfitColours;
                default:
                    return hairColours;
            }
        }

        private static string FirstId(List<AppearancePart> list) => list.Count > 0 ? list[0].Id : "";

        private static string FirstId(AppearanceColour[] list)
        {
            var first = Array.Find(list, colour => colour != null);
            return first != null ? first.Id : "";
        }
    }
}
