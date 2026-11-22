using System.Collections.Generic;
using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Player
{
    // Dresses the character skeleton in a look: puts the models of the chosen parts on it (each replacing the bare body's model
    // of the same body part), paints its colour map and sets the body gauges' blend shapes.
    // With the event bus, it wears the look of the game loaded in the scene.
    [RequireComponent(typeof(Animator))]
    public class OwnerAppearance : MonoBehaviour
    {
        [SerializeField] private AppearanceCatalog catalog;
        [Tooltip("Optional: set in game scenes, where the owner wears the look saved with the game.")]
        [SerializeField] private EventBusAsset eventBus;

        private static readonly int ColourMapId = Shader.PropertyToID("_ColorMap");
        private static readonly AppearanceCategory[] WornCategories =
        {
            AppearanceCategory.Top, AppearanceCategory.Bottom, AppearanceCategory.Hat,
            AppearanceCategory.Hair, AppearanceCategory.Brows, AppearanceCategory.Beard,
        };

        private readonly Dictionary<string, SkinnedMeshRenderer> _worn = new Dictionary<string, SkinnedMeshRenderer>();
        private readonly Dictionary<string, GameObject> _wornModels = new Dictionary<string, GameObject>();
        // The skeleton's own bones, and the bones each worn model added.
        private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>();
        private readonly Dictionary<string, List<Transform>> _ownBones = new Dictionary<string, List<Transform>>();
        private Transform _rootBone;
        private Transform _head;
        private Material _material;
        private Texture2D _colours;
        private Color[] _basePixels;
        private Color[] _pixels;

        public Transform Head => _head;

        // The models worn, by body part code (see BodyPartSlot).
        public IReadOnlyDictionary<string, SkinnedMeshRenderer> Worn => _worn;

        public Texture2D Colours => _colours;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(AppearanceCatalog looks, EventBusAsset bus = null)
        {
            catalog = looks;
            eventBus = bus;
        }

        public void Apply(AppearanceSave look)
        {
            if (_rootBone == null)
            {
                return;
            }

            look = catalog.Normalize(look);
            var wanted = new Dictionary<string, GameObject>();
            foreach (var model in catalog.BaseModels)
            {
                Want(wanted, model);
            }

            // A hat that covers the head hides the hair: the haircuts would show through it.
            var hat = catalog.FindPart(AppearanceCategory.Hat, look.hatId);
            var hairHidden = hat != null && hat.HidesHair;
            foreach (var category in WornCategories)
            {
                var part = catalog.FindPart(category, AppearanceCatalog.PartId(look, category));
                if (part != null && !(hairHidden && category == AppearanceCategory.Hair))
                {
                    foreach (var model in part.Models)
                    {
                        Want(wanted, model);
                    }
                }
            }

            Dress(wanted);
            Paint(look);
            Shape(look);
        }

        private void Awake()
        {
            var animator = GetComponent<Animator>();
            _head = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            var own = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (catalog == null || _head == null || own.Length == 0 || catalog.Material == null || catalog.BaseColours == null)
            {
                Debug.LogError($"{nameof(OwnerAppearance)} on {name} needs the appearance catalog with its material and colours, and a humanoid character model.", this);
                enabled = false;
                return;
            }

            // The model's own meshes only lend their skeleton: the look's models replace them.
            _rootBone = own[0].rootBone;
            foreach (var renderer in own)
            {
                renderer.gameObject.SetActive(false);
            }

            foreach (var bone in _rootBone.GetComponentsInChildren<Transform>(true))
            {
                _bones.TryAdd(bone.name, bone);
            }

            _basePixels = catalog.BaseColours.GetPixels();
            _pixels = new Color[_basePixels.Length];
            _colours = new Texture2D(catalog.BaseColours.width, catalog.BaseColours.height, TextureFormat.RGBA32, false)
            {
                name = "Owner colours",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _material = new Material(catalog.Material) { name = "Owner" };
            _material.SetTexture(ColourMapId, _colours);
            Apply(null);
        }

        private void OnDestroy()
        {
            Destroy(_material);
            Destroy(_colours);
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

        private static void Want(Dictionary<string, GameObject> wanted, GameObject model)
        {
            var slot = model != null ? BodyPartSlot.Of(model.name) : null;
            if (slot != null)
            {
                wanted[slot] = model;
            }
        }

        // Keeps the models already worn, takes off the others and puts on the new ones.
        private void Dress(Dictionary<string, GameObject> wanted)
        {
            var slots = new List<string>(_worn.Keys);
            foreach (var slot in slots)
            {
                if (!wanted.TryGetValue(slot, out var model) || model != _wornModels[slot])
                {
                    // Hidden at once: Destroy waits for the end of the frame, and thumbnails draw several looks in one frame.
                    _worn[slot].gameObject.SetActive(false);
                    Destroy(_worn[slot].gameObject);
                    foreach (var bone in _ownBones[slot])
                    {
                        if (bone != null)
                        {
                            Destroy(bone.gameObject);
                        }
                    }

                    _worn.Remove(slot);
                    _wornModels.Remove(slot);
                    _ownBones.Remove(slot);
                }
            }

            foreach (var pair in wanted)
            {
                if (!_worn.ContainsKey(pair.Key))
                {
                    var source = pair.Value.GetComponentInChildren<SkinnedMeshRenderer>(true);
                    if (source != null)
                    {
                        var ownBones = new Dictionary<string, Transform>();
                        _worn[pair.Key] = Put(source, ownBones);
                        _wornModels[pair.Key] = pair.Value;
                        _ownBones[pair.Key] = new List<Transform>(ownBones.Values);
                    }
                }
            }
        }

        // A copy of the model's renderer, skinned to this skeleton. Bones the skeleton lacks (a skirt's, long hair's) are added
        // under their parent, for this model alone: a hair and a beard may both have a hair_dyn_ bone, each in its own place.
        private SkinnedMeshRenderer Put(SkinnedMeshRenderer source, Dictionary<string, Transform> ownBones)
        {
            var sourceBones = source.bones;
            var bones = new Transform[sourceBones.Length];
            for (var i = 0; i < sourceBones.Length; i++)
            {
                bones[i] = BoneFor(sourceBones[i], ownBones);
            }

            var renderer = new GameObject(source.name).AddComponent<SkinnedMeshRenderer>();
            renderer.transform.SetParent(transform, false);
            renderer.sharedMesh = source.sharedMesh;
            renderer.bones = bones;
            renderer.rootBone = source.rootBone != null ? BoneFor(source.rootBone, ownBones) : _rootBone;
            var materials = new Material[source.sharedMesh.subMeshCount];
            for (var i = 0; i < materials.Length; i++)
            {
                materials[i] = _material;
            }

            renderer.sharedMaterials = materials;
            return renderer;
        }

        private Transform BoneFor(Transform sourceBone, Dictionary<string, Transform> ownBones)
        {
            if (sourceBone == null)
            {
                return null;
            }

            if (_bones.TryGetValue(sourceBone.name, out var bone) || ownBones.TryGetValue(sourceBone.name, out bone))
            {
                return bone;
            }

            var parent = BoneFor(sourceBone.parent, ownBones) ?? _rootBone;
            bone = new GameObject(sourceBone.name).transform;
            bone.SetParent(parent, false);
            bone.localPosition = sourceBone.localPosition;
            bone.localRotation = sourceBone.localRotation;
            bone.localScale = sourceBone.localScale;
            ownBones[bone.name] = bone;
            return bone;
        }

        // The colour map: the base colours, each part's own colours (a hair's ties, an outfit's fabrics; the top's win),
        // then the chosen skin, hair and outfit colours.
        private void Paint(AppearanceSave look)
        {
            var width = _colours.width;
            System.Array.Copy(_basePixels, _pixels, _pixels.Length);
            var top = catalog.FindPart(AppearanceCategory.Top, look.topId);
            var bottom = catalog.FindPart(AppearanceCategory.Bottom, look.bottomId);
            var hat = catalog.FindPart(AppearanceCategory.Hat, look.hatId);
            var hair = catalog.FindPart(AppearanceCategory.Hair, look.hairId);
            var beard = catalog.FindPart(AppearanceCategory.Beard, look.beardId);
            foreach (var outfit in new[] { beard, hair, hat, bottom, top })
            {
                if (outfit != null && outfit.Colours != null)
                {
                    ColourMap.CopyCells(_pixels, outfit.Colours.GetPixels(), width, outfit.Cells, catalog.BodyCells);
                }
            }

            var skin = catalog.FindColour(AppearanceColourSlot.Skin, look.skinColourId);
            if (skin != null)
            {
                foreach (var paint in skin.Paints)
                {
                    ColourMap.Paint(_pixels, width, paint.cell, paint.colour);
                }
            }

            PaintCells(AppearanceColourSlot.Hair, look, width);
            PaintCells(AppearanceColourSlot.Brows, look, width);
            PaintCells(AppearanceColourSlot.Beard, look, width);
            DyeOutfit(AppearanceColourSlot.Top, top, look, width);
            DyeOutfit(AppearanceColourSlot.Bottom, bottom, look, width);
            _colours.SetPixels(_pixels);
            _colours.Apply();
        }

        private void PaintCells(AppearanceColourSlot slot, AppearanceSave look, int width)
        {
            var colour = catalog.FindColour(slot, AppearanceCatalog.ColourId(look, slot));
            if (colour == null)
            {
                return;
            }

            foreach (var cell in catalog.PaintedCells(slot))
            {
                ColourMap.Paint(_pixels, width, cell.cell, colour.Swatch * cell.colour);
            }
        }

        private void DyeOutfit(AppearanceColourSlot slot, AppearancePart outfit, AppearanceSave look, int width)
        {
            var colour = catalog.FindColour(slot, AppearanceCatalog.ColourId(look, slot));
            if (outfit != null && colour != null && !colour.Original)
            {
                ColourMap.Dye(_pixels, width, outfit.Cells, catalog.DyedCells(slot), colour.Swatch);
            }
        }

        private void Shape(AppearanceSave look)
        {
            foreach (var renderer in _worn.Values)
            {
                var mesh = renderer.sharedMesh;
                for (var i = 0; i < mesh.blendShapeCount; i++)
                {
                    var blendName = mesh.GetBlendShapeName(i);
                    if (blendName.Contains(catalog.TypeBlendShape))
                    {
                        renderer.SetBlendShapeWeight(i, BodyShapeWeights.Type(look.bodyType));
                    }
                    else if (blendName.Contains(catalog.HeavyBlendShape))
                    {
                        renderer.SetBlendShapeWeight(i, BodyShapeWeights.Heavy(look.bodySize));
                    }
                    else if (blendName.Contains(catalog.SkinnyBlendShape))
                    {
                        renderer.SetBlendShapeWeight(i, BodyShapeWeights.Skinny(look.bodySize, catalog.SkinnyLimit));
                    }
                    else if (blendName.Contains(catalog.MuscleBlendShape))
                    {
                        renderer.SetBlendShapeWeight(i, BodyShapeWeights.Muscle(look.musculature, catalog.LeanFloor));
                    }
                }
            }
        }
    }
}
