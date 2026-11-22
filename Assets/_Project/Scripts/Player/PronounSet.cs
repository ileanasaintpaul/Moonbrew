using UnityEngine;

namespace Moonbrew.Player
{
    // A choice of pronouns for the owner, picked on the creation screen.
    [CreateAssetMenu(menuName = "Moonbrew/Player/Pronoun Set", fileName = "SO_Pronouns_")]
    public class PronounSet : ScriptableObject
    {
        [Tooltip("Stable snake_case ID stored in saves, e.g. pronouns_she. Never change it once shipped.")]
        [SerializeField] private string id;
        [Tooltip("As shown on the creation screen, e.g. She / Her.")]
        [SerializeField] private string displayName;

        public string Id => id;

        public string DisplayName => displayName;

        // For sets built from code.
        public void Init(string setId, string setName)
        {
            id = setId;
            displayName = setName;
        }
    }
}
