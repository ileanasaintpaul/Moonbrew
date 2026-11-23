using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace Moonbrew.Ui
{
    // Draws each part once on a generic mannequin, out of the player's sight, into a small texture for the creation screen's cells.
    public class AppearanceThumbnails : MonoBehaviour
    {
        [Tooltip("Disabled camera: it only renders when asked, with a transparent background.")]
        [SerializeField] private Camera thumbnailCamera;
        [SerializeField] private OwnerAppearance model;
        [Tooltip("Flat colour of the mannequin the parts are shown on.")]
        [SerializeField] private Color mannequinColour = new Color32(84, 62, 104, 255);
        [Tooltip("Width and height of a thumbnail, in pixels.")]
        [SerializeField, Min(8)] private int size = 96;
        [Tooltip("Radius around the head bone that a head thumbnail shows, in metres.")]
        [SerializeField, Min(0.05f)] private float headRadius = 0.32f;
        [Tooltip("How far above the head bone a head thumbnail is centred, in metres: hair and hats sit above it.")]
        [SerializeField] private float headLift = 0.12f;
        [Tooltip("Room around the framed head, body, top or bottom: 1 fits it exactly.")]
        [SerializeField, Min(1f)] private float padding = 1.15f;
        [Tooltip("The camera turns this far round the model, in degrees, for a three-quarter view.")]
        [SerializeField] private float yaw = 20f;

        // What is left of the body on a head thumbnail.
        private static readonly HashSet<string> HeadSlots = new HashSet<string> { "HEAD", "EYEL", "EYER", "EARL", "EARR", "NOSE", "TETH", "TONG" };

        private readonly Dictionary<string, RenderTexture> _textures = new Dictionary<string, RenderTexture>();
        private readonly HashSet<string> _partSlots = new HashSet<string>();
        private Animator _bones;
        private Material _mannequin;
        private Texture2D _mannequinMap;
        private bool _posed;

        public enum Framing
        {
            Head,
            Body,
            Top,
            Bottom,
        }

        // For objects built from code.
        public void Init(Camera renderCamera, OwnerAppearance thumbnailModel)
        {
            thumbnailCamera = renderCamera;
            model = thumbnailModel;
        }

        // A part is drawn once: its thumbnail stays the same whatever the player picks.
        // The textures are released with this component.
        public RenderTexture Draw(AppearancePart part, AppearanceSave look, Framing framing)
        {
            if (_textures.TryGetValue(part.Id, out var drawn))
            {
                return drawn;
            }

            HoldPose();
            model.Apply(look);
            ShowOnMannequin(part, framing);
            Frame(framing);
            var target = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32) { name = "Look thumbnail" };
            target.Create();
            _textures[part.Id] = target;
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(thumbnailCamera, request))
            {
                RenderPipeline.SubmitRenderRequest(thumbnailCamera, request);
            }
            else
            {
                thumbnailCamera.targetTexture = target;
                thumbnailCamera.Render();
                thumbnailCamera.targetTexture = null;
            }

            return target;
        }

        // The idle's first frame, then the animator stops: a running idle would turn each head a little differently.
        private void HoldPose()
        {
            if (_posed)
            {
                return;
            }

            _posed = true;
            _bones.Rebind();
            _bones.Update(0f);
            _bones.enabled = false;

            // The owner's own shader with a flat colour map: a new shader may still be compiling when the thumbnails are drawn once.
            _mannequinMap = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "Mannequin colour" };
            _mannequinMap.SetPixel(0, 0, mannequinColour);
            _mannequinMap.Apply();
            _mannequin = new Material(model.Material) { name = "Mannequin" };
            _mannequin.SetTexture(OwnerAppearance.ColourMapId, _mannequinMap);
        }

        // The part keeps its colours, the rest of the body turns to mannequin; a head thumbnail shows only the head.
        private void ShowOnMannequin(AppearancePart part, Framing framing)
        {
            _partSlots.Clear();
            foreach (var partModel in part.Models)
            {
                var slot = partModel != null ? BodyPartSlot.Of(partModel.name) : null;
                if (slot != null)
                {
                    _partSlots.Add(slot);
                }
            }

            foreach (var worn in model.Worn)
            {
                var isPart = _partSlots.Contains(worn.Key);
                worn.Value.gameObject.SetActive(isPart || framing != Framing.Head || HeadSlots.Contains(worn.Key));
                var material = isPart ? model.Material : _mannequin;
                var materials = worn.Value.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                worn.Value.sharedMaterials = materials;
            }
        }

        // Body: from the feet (the model's root) to above the head. Top: from the tops of the legs to the neck. Bottom: from the feet to the tops of the legs.
        // Not the hips bone: in the Fantasy Village rig it sits on the ground.
        private void Frame(Framing framing)
        {
            Vector3 centre;
            float radius;
            var hips = (_bones.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position + _bones.GetBoneTransform(HumanBodyBones.RightUpperLeg).position) * 0.5f;
            var neck = _bones.GetBoneTransform(HumanBodyBones.Neck).position;
            var feet = model.transform.position;
            switch (framing)
            {
                case Framing.Body:
                    var top = model.Head.position.y + headLift;
                    centre = new Vector3(hips.x, (top + feet.y) * 0.5f, hips.z);
                    radius = (top - feet.y) * 0.5f;
                    break;
                case Framing.Top:
                    centre = (hips + neck) * 0.5f;
                    radius = (neck.y - hips.y) * 0.5f;
                    break;
                case Framing.Bottom:
                    centre = new Vector3(hips.x, (hips.y + feet.y) * 0.5f, hips.z);
                    radius = (hips.y - feet.y) * 0.5f;
                    break;
                default:
                    centre = model.Head.position + Vector3.up * headLift;
                    radius = headRadius;
                    break;
            }

            var distance = radius * padding / Mathf.Tan(thumbnailCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var direction = Quaternion.AngleAxis(yaw, Vector3.up) * model.transform.forward;
            thumbnailCamera.transform.position = centre + direction * distance;
            thumbnailCamera.transform.LookAt(centre);
        }

        private void Awake()
        {
            if (thumbnailCamera == null || model == null)
            {
                Debug.LogError($"{nameof(AppearanceThumbnails)} on {name} needs its camera and model.", this);
                enabled = false;
                return;
            }

            _bones = model.GetComponent<Animator>();
        }

        private void OnDestroy()
        {
            foreach (var texture in _textures.Values)
            {
                if (texture != null)
                {
                    texture.Release();
                    Destroy(texture);
                }
            }

            _textures.Clear();
            Destroy(_mannequin);
            Destroy(_mannequinMap);
        }
    }
}
