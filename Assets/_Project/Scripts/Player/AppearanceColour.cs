using UnityEngine;

namespace Moonbrew.Player
{
    // A colour of the owner's hair, skin or outfit, and the swatch shown to pick it. An outfit colour is a material the parts wear;
    // a hair or skin colour is only its swatch, which tints the hair or the skin.
    [CreateAssetMenu(menuName = "Moonbrew/Player/Appearance Colour", fileName = "SO_LookColour_")]
    public class AppearanceColour : ScriptableObject
    {
        [Tooltip("Stable snake_case ID stored in saves, e.g. hair_auburn. Never change it once shipped.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Color swatch = Color.white;
        [Tooltip("Outfit colours only.")]
        [SerializeField] private Material material;

        public string Id => id;

        public string DisplayName => displayName;

        public Color Swatch => swatch;

        public Material Material => material;

        // For colours built from code.
        public void Init(string colourId, string colourName, Color swatchColour, Material colourMaterial)
        {
            id = colourId;
            displayName = colourName;
            swatch = swatchColour;
            material = colourMaterial;
        }
    }
}
