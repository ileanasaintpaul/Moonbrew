using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Player
{
    // Brings a dialogue portrait to life on top of its animation: blends in faces and poses, moves the mouth while it talks, and blinks.
    [RequireComponent(typeof(Animator))]
    public class PortraitPoser : MonoBehaviour
    {
        private static readonly int TalkingHash = Animator.StringToHash("Talking");

        [SerializeField] private PortraitMoods moods;

        private readonly List<FaceWeight> _face = new List<FaceWeight>();
        private Animator _animator;
        private OwnerAppearance _appearance;
        private HumanPoseHandler _poseHandler;
        private HumanPose _pose;
        private float[] _muscles;
        private float[] _targetMuscles;
        private bool _talking;
        private bool _posing;
        private bool _hasTalkingParameter;
        private float _time;

        public string Expression { get; private set; }

        public string Pose { get; private set; }

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(PortraitMoods portraitMoods)
        {
            moods = portraitMoods;
        }

        public void Show(string expression, string pose)
        {
            Expression = expression;
            Pose = pose;
            _posing = true;
            if (moods == null)
            {
                return;
            }

            if (_muscles != null)
            {
                _targetMuscles = MoodBlend.Targets(moods.PoseOf(pose), HumanTrait.MuscleName);
            }

            foreach (var weight in _face)
            {
                weight.Target = 0f;
            }

            foreach (var blend in moods.FaceOf(expression))
            {
                foreach (var weight in Weights(blend.blendShape))
                {
                    weight.Target = blend.weight;
                }
            }
        }

        public void SetTalking(bool talking)
        {
            _talking = talking;
            if (_hasTalkingParameter)
            {
                _animator.SetBool(TalkingHash, talking);
            }
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _appearance = GetComponent<OwnerAppearance>();
            foreach (var parameter in _animator.parameters)
            {
                _hasTalkingParameter |= parameter.nameHash == TalkingHash;
            }

            if (_animator.isHuman && _animator.avatar != null)
            {
                _poseHandler = new HumanPoseHandler(_animator.avatar, transform);
                _muscles = new float[HumanTrait.MuscleCount];
                _targetMuscles = new float[HumanTrait.MuscleCount];
            }
        }

        private void OnDestroy()
        {
            _poseHandler?.Dispose();
        }

        // Visual animation only, after the Animator has posed the body.
        private void LateUpdate()
        {
            if (moods == null)
            {
                return;
            }

            var seconds = Time.unscaledDeltaTime;
            _time += seconds;
            BlendMuscles(seconds);
            BlendFace(seconds);
        }

        private void BlendMuscles(float seconds)
        {
            // Only on a portrait, which stands still: on a walking customer the rewritten pose would leave the body behind.
            if (_poseHandler == null || !_posing)
            {
                return;
            }

            _poseHandler.GetHumanPose(ref _pose);
            for (var i = 0; i < _muscles.Length; i++)
            {
                _muscles[i] = MoodBlend.Toward(_muscles[i], _targetMuscles[i], moods.BlendSpeed, seconds);
                _pose.muscles[i] = Mathf.Clamp(_pose.muscles[i] + _muscles[i], -1f, 1f);
            }

            _poseHandler.SetHumanPose(ref _pose);
        }

        private void BlendFace(float seconds)
        {
            var mouth = _talking ? MoodBlend.Mouth(_time, moods.TalkSpeed) * moods.TalkWeight : 0f;
            var blinking = MoodBlend.Blinking(_time, moods.BlinkInterval, moods.BlinkSeconds);
            foreach (var weight in _face)
            {
                weight.Current = MoodBlend.Toward(weight.Current, weight.Target, moods.BlendSpeed * 100f, seconds);
                var value = weight.Current;
                if (weight.IsMouth)
                {
                    value = Mathf.Max(value, mouth);
                }

                if (weight.IsBlink && blinking)
                {
                    value = 100f;
                }

                weight.Renderer.SetBlendShapeWeight(weight.Index, value);
            }

            if (_face.Count == 0)
            {
                // The first frame: the worn models exist now, so the mouth and the eyes can be found.
                Weights(moods.TalkBlendShape);
                foreach (var blink in moods.BlinkBlendShapes)
                {
                    Weights(blink);
                }
            }
        }

        // The blend shapes of every worn model whose name ends with this one, created on first use.
        private List<FaceWeight> Weights(string blendShape)
        {
            var found = new List<FaceWeight>();
            if (string.IsNullOrEmpty(blendShape) || _appearance == null)
            {
                return found;
            }

            foreach (var weight in _face)
            {
                if (weight.Name.EndsWith(blendShape))
                {
                    found.Add(weight);
                }
            }

            if (found.Count > 0)
            {
                return found;
            }

            foreach (var renderer in _appearance.Worn.Values)
            {
                var mesh = renderer != null ? renderer.sharedMesh : null;
                for (var i = 0; mesh != null && i < mesh.blendShapeCount; i++)
                {
                    var name = mesh.GetBlendShapeName(i);
                    if (!name.EndsWith(blendShape))
                    {
                        continue;
                    }

                    var weight = new FaceWeight
                    {
                        Renderer = renderer,
                        Index = i,
                        Name = name,
                        Current = renderer.GetBlendShapeWeight(i),
                        IsMouth = name.EndsWith(moods.TalkBlendShape),
                        IsBlink = System.Array.Exists(moods.BlinkBlendShapes, blink => name.EndsWith(blink)),
                    };
                    _face.Add(weight);
                    found.Add(weight);
                }
            }

            return found;
        }

        private class FaceWeight
        {
            public SkinnedMeshRenderer Renderer;
            public int Index;
            public string Name;
            public float Current;
            public float Target;
            public bool IsMouth;
            public bool IsBlink;
        }
    }
}
