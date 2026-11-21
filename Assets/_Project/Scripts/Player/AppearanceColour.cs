using System;
using UnityEngine;

namespace Moonbrew.Player
{
    // A colour of the owner's skin, hair or outfit, and the swatch shown to pick it.
    [CreateAssetMenu(menuName = "Moonbrew/Player/Appearance Colour", fileName = "SO_LookColour_")]
    public class AppearanceColour : ScriptableObject
    {
        [Tooltip("Stable snake_case ID stored in saves, e.g. hair_auburn. Never change it once shipped.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Color swatch = Color.white;
        [Tooltip("Outfit colours only: keep the outfit's original colours instead of dyeing it the swatch.")]
        [SerializeField] private bool original;
        [Tooltip("Skin colours only: the exact colour of each skin cell (skin, lips, eyelids, nails...).")]
        [SerializeField] private CellColour[] paints = new CellColour[0];

        public string Id => id;

        public string DisplayName => displayName;

        public Color Swatch => swatch;

        public bool Original => original;

        public CellColour[] Paints => paints;

        // For colours built from code.
        public void Init(string colourId, string colourName, Color swatchColour, bool keepsOriginal = false, CellColour[] cellColours = null)
        {
            id = colourId;
            displayName = colourName;
            swatch = swatchColour;
            original = keepsOriginal;
            paints = cellColours ?? new CellColour[0];
        }
    }

    // A cell of the character colour map, and the colour it takes.
    [Serializable]
    public struct CellColour
    {
        public Vector2Int cell;
        public Color colour;

        public CellColour(Vector2Int colourCell, Color cellColour)
        {
            cell = colourCell;
            colour = cellColour;
        }
    }
}
