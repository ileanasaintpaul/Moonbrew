using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace Moonbrew.Ui
{
    // Draws looks on a second owner model, out of the player's sight, into small textures for the creation screen's cells.
    public class AppearanceThumbnails : MonoBehaviour
    {
        [Tooltip("Disabled camera: it only renders when asked, with a transparent background.")]
        [SerializeField] private Camera thumbnailCamera;
        [SerializeField] private OwnerAppearance model;
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

        private readonly List<RenderTexture> _textures = new List<RenderTexture>();
        private Animator _bones;

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

        // Each slot keeps its texture, redrawn on the next call: a cell shows the same slot whatever the page.
        // The textures are released with this component.
        public RenderTexture Draw(AppearanceSave look, Framing framing, int slot)
        {
            model.Apply(look);
            Frame(framing);
            while (_textures.Count <= slot)
            {
                var texture = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32) { name = "Look thumbnail" };
                texture.Create();
                _textures.Add(texture);
            }

            var target = _textures[slot];
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

        // Top: from the tops of the legs to the neck. Bottom: from the feet (the model's root) to the tops of the legs.
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
                case Framing.Body when model.Body != null:
                    var bounds = model.Body.bounds;
                    centre = bounds.center;
                    radius = bounds.extents.y;
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
            foreach (var texture in _textures)
            {
                if (texture != null)
                {
                    texture.Release();
                    Destroy(texture);
                }
            }

            _textures.Clear();
        }
    }
}
