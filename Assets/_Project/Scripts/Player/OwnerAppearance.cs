using System.Collections.Generic;
using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Player
{
    // Dresses the character model in a look: shows one body, puts the hair, beard, hat and accessory on the head bone,
    // and gives them the chosen colours. With the event bus, it wears the look of the game loaded in the scene.
    // Top and bottom colours are the pack's palette materials, each on its half of the body (see OutfitSplit).
    // Skin and hair colours are properties of the pack's character shader, which tints the skin and the hair of any palette:
    // the body takes the eyebrow colour as its hair colour, and so does the beard.
    [RequireComponent(typeof(Animator))]
    public class OwnerAppearance : MonoBehaviour
    {
        [SerializeField] private AppearanceCatalog catalog;
        [Tooltip("Optional: set in game scenes, where the owner wears the look saved with the game.")]
        [SerializeField] private EventBusAsset eventBus;

        private static readonly int HairColourId = Shader.PropertyToID("_Hair_Color");
        private static readonly int SkinColourId = Shader.PropertyToID("_Skin_Color");

        private readonly List<GameObject> _worn = new List<GameObject>();
        private readonly List<Mesh> _splitMeshes = new List<Mesh>();
        private readonly Material[] _outfit = new Material[2];
        private MaterialPropertyBlock _face;
        private MaterialPropertyBlock _hair;
        private SkinnedMeshRenderer[] _bodies;
        private Material _packMaterial;
        private Transform _head;

        // The body shown, for tests and thumbnails; null before the first look.
        public SkinnedMeshRenderer Body { get; private set; }

        public Transform Head => _head;

        public IReadOnlyList<GameObject> Worn => _worn;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(AppearanceCatalog looks, EventBusAsset bus = null)
        {
            catalog = looks;
            eventBus = bus;
        }

        public void Apply(AppearanceSave look)
        {
            if (_bodies == null)
            {
                return;
            }

            look = catalog.Normalize(look);
            var body = catalog.FindPart(AppearanceCategory.Body, look.bodyId);
            var top = OutfitMaterial(AppearanceColourSlot.Top, look);
            _outfit[0] = top;
            _outfit[1] = OutfitMaterial(AppearanceColourSlot.Bottom, look);
            var skin = Swatch(AppearanceColourSlot.Skin, look);
            _face.SetColor(SkinColourId, skin);
            _face.SetColor(HairColourId, Swatch(AppearanceColourSlot.Brows, look));
            _hair.SetColor(SkinColourId, skin);
            _hair.SetColor(HairColourId, Swatch(AppearanceColourSlot.Hair, look));

            Body = null;
            foreach (var model in _bodies)
            {
                var shown = body != null && model.name == body.ModelChild;
                model.gameObject.SetActive(shown);
                if (shown)
                {
                    Body = model;
                    model.sharedMaterials = _outfit;
                    model.SetPropertyBlock(_face);
                }
            }

            // Hidden at once: Destroy waits for the end of the frame, and thumbnails render several looks in one frame.
            foreach (var worn in _worn)
            {
                worn.SetActive(false);
                Destroy(worn);
            }

            _worn.Clear();
            Wear(AppearanceCategory.Hair, look.hairId, top, _hair);
            Wear(AppearanceCategory.Beard, look.beardId, top, _face);
            Wear(AppearanceCategory.Hat, look.hatId, top, _hair);
            Wear(AppearanceCategory.Accessory, look.accessoryId, top, _hair);
        }

        private void Awake()
        {
            var animator = GetComponent<Animator>();
            _head = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            if (catalog == null || _head == null)
            {
                Debug.LogError($"{nameof(OwnerAppearance)} on {name} needs the appearance catalog and a humanoid model.", this);
                enabled = false;
                return;
            }

            // The model comes dressed by its pack: what it wears on the head is replaced by the chosen parts.
            foreach (var attached in _head.GetComponentsInChildren<MeshRenderer>(true))
            {
                attached.gameObject.SetActive(false);
            }

            _face = new MaterialPropertyBlock();
            _hair = new MaterialPropertyBlock();
            _bodies = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            _packMaterial = _bodies.Length > 0 ? _bodies[0].sharedMaterial : null;
            SplitBodies(animator);
            Apply(null);
        }

        private void OnDestroy()
        {
            foreach (var mesh in _splitMeshes)
            {
                Destroy(mesh);
            }
        }

        private void OnEnable()
        {
            if (eventBus != null)
            {
                eventBus.Subscribe<GameLoaded>(OnGameLoaded);
                eventBus.Subscribe<NewGameStarted>(OnNewGameStarted);
            }
        }

        private void OnDisable()
        {
            if (eventBus != null)
            {
                eventBus.Unsubscribe<GameLoaded>(OnGameLoaded);
                eventBus.Unsubscribe<NewGameStarted>(OnNewGameStarted);
            }
        }

        private void OnGameLoaded(GameLoaded message)
        {
            Apply(message.Data?.appearance);
        }

        private void OnNewGameStarted(NewGameStarted message)
        {
            Apply(message.Data?.appearance);
        }

        // Each body gets its own copy of its mesh in two parts, top then bottom, one per outfit material.
        private void SplitBodies(Animator animator)
        {
            var legs = new HashSet<Transform>();
            legs.UnionWith(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).GetComponentsInChildren<Transform>(true));
            legs.UnionWith(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).GetComponentsInChildren<Transform>(true));
            var top = new List<int>();
            var bottom = new List<int>();
            foreach (var model in _bodies)
            {
                var mesh = Instantiate(model.sharedMesh);
                mesh.name = model.sharedMesh.name;
                var bones = model.bones;
                var weights = mesh.boneWeights;
                var onLegs = new bool[weights.Length];
                for (var v = 0; v < weights.Length; v++)
                {
                    onLegs[v] = legs.Contains(bones[weights[v].boneIndex0]);
                }

                OutfitSplit.Split(mesh.triangles, onLegs, top, bottom);
                mesh.subMeshCount = 2;
                mesh.SetTriangles(top, 0);
                mesh.SetTriangles(bottom, 1);
                model.sharedMesh = mesh;
                _splitMeshes.Add(mesh);
            }
        }

        private Material OutfitMaterial(AppearanceColourSlot slot, AppearanceSave look)
        {
            var material = catalog.FindColour(slot, AppearanceCatalog.ColourId(look, slot))?.Material;
            return material != null ? material : _packMaterial;
        }

        private Color Swatch(AppearanceColourSlot slot, AppearanceSave look)
        {
            var colour = catalog.FindColour(slot, AppearanceCatalog.ColourId(look, slot));
            return colour != null ? colour.Swatch : Color.white;
        }

        private void Wear(AppearanceCategory category, string id, Material material, MaterialPropertyBlock colours)
        {
            var part = catalog.FindPart(category, id);
            if (part == null || part.Attachment == null)
            {
                return;
            }

            var worn = Instantiate(part.Attachment, _head, false);
            foreach (var renderer in worn.GetComponentsInChildren<Renderer>(true))
            {
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }

                renderer.SetPropertyBlock(colours);
            }

            _worn.Add(worn);
        }
    }
}
