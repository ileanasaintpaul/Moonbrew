using UnityEngine;

namespace Moonbrew.Player
{
    // One choice of the owner's look: the models it puts on the character skeleton (a top is a torso, arms and hands),
    // and, for an outfit, its original colours.
    [CreateAssetMenu(menuName = "Moonbrew/Player/Appearance Part", fileName = "SO_Look_")]
    public class AppearancePart : ScriptableObject
    {
        [Tooltip("Stable snake_case ID stored in saves, e.g. hair_braid. Never change it once shipped.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private AppearanceCategory category;
        [Tooltip("The models of the part, each skinned to the character skeleton. A model replaces the base model of the same body part.")]
        [SerializeField] private GameObject[] models = new GameObject[0];
        [Tooltip("The colour map holding the part's original colours (an outfit's fabrics, a hair's ties), if it brings any.")]
        [SerializeField] private Texture2D colours;
        [Tooltip("The colour map cells the models use, the most used first.")]
        [SerializeField] private Vector2Int[] cells = new Vector2Int[0];
        [Tooltip("Hats only: the hat covers the head, so the hair is not drawn under it.")]
        [SerializeField] private bool hidesHair;

        public string Id => id;

        public string DisplayName => displayName;

        public AppearanceCategory Category => category;

        public GameObject[] Models => models;

        public Texture2D Colours => colours;

        public Vector2Int[] Cells => cells;

        public bool HidesHair => hidesHair;

        // For parts built from code.
        public void Init(string partId, string partName, AppearanceCategory partCategory, GameObject[] partModels = null, Texture2D partColours = null, Vector2Int[] partCells = null, bool coversHair = false)
        {
            id = partId;
            displayName = partName;
            category = partCategory;
            models = partModels ?? new GameObject[0];
            colours = partColours;
            cells = partCells ?? new Vector2Int[0];
            hidesHair = coversHair;
        }
    }
}
