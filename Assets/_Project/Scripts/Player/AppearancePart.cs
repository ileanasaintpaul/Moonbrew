using UnityEngine;

namespace Moonbrew.Player
{
    // One choice of the owner's look: a body (with its outfit) of the character model, or a model worn on the head.
    [CreateAssetMenu(menuName = "Moonbrew/Player/Appearance Part", fileName = "SO_Look_")]
    public class AppearancePart : ScriptableObject
    {
        [Tooltip("Stable snake_case ID stored in saves, e.g. hair_braid. Never change it once shipped.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private AppearanceCategory category;
        [Tooltip("Body only: name of the body's mesh in the character model, which holds every body on one skeleton.")]
        [SerializeField] private string modelChild;
        [Tooltip("Every other part: the model worn on the head bone.")]
        [SerializeField] private GameObject attachment;

        public string Id => id;

        public string DisplayName => displayName;

        public AppearanceCategory Category => category;

        public string ModelChild => modelChild;

        public GameObject Attachment => attachment;

        // For parts built from code.
        public void Init(string partId, string partName, AppearanceCategory partCategory, string bodyModelChild = null, GameObject headAttachment = null)
        {
            id = partId;
            displayName = partName;
            category = partCategory;
            modelChild = bodyModelChild;
            attachment = headAttachment;
        }
    }
}
