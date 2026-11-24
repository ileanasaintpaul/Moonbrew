using System;
using UnityEngine;

namespace Moonbrew.Player
{
    // The faces and poses a character can take in a dialogue portrait, by ID.
    // A face is a set of blend shape weights; a pose bends the humanoid muscles (-1 to 1) on top of the animation.
    [CreateAssetMenu(menuName = "Moonbrew/Player/Portrait Moods", fileName = "SO_PortraitMoods")]
    public class PortraitMoods : ScriptableObject
    {
        [SerializeField] private Face[] faces = new Face[0];
        [SerializeField] private Pose[] poses = new Pose[0];
        [Tooltip("How fast a face or pose blends in, per second.")]
        [SerializeField, Min(0.1f)] private float blendSpeed = 6f;
        [Tooltip("Blend shape that opens the mouth while a line is being said.")]
        [SerializeField] private string talkBlendShape = "jawOpen";
        [SerializeField, Range(0f, 100f)] private float talkWeight = 35f;
        [Tooltip("Mouth movements per second while talking.")]
        [SerializeField, Min(0f)] private float talkSpeed = 9f;
        [SerializeField] private string[] blinkBlendShapes = { "eyeBlinkUpperLeft", "eyeBlinkUpperRight" };
        [Tooltip("Seconds between two blinks.")]
        [SerializeField, Min(0.5f)] private float blinkInterval = 3.5f;
        [SerializeField, Min(0.01f)] private float blinkSeconds = 0.12f;

        public float BlendSpeed => blendSpeed;

        public string TalkBlendShape => talkBlendShape;

        public float TalkWeight => talkWeight;

        public float TalkSpeed => talkSpeed;

        public string[] BlinkBlendShapes => blinkBlendShapes;

        public float BlinkInterval => blinkInterval;

        public float BlinkSeconds => blinkSeconds;

        public OwnerAppearance.FaceBlend[] FaceOf(string id)
        {
            foreach (var face in faces)
            {
                if (face.id == id)
                {
                    return face.blends;
                }
            }

            return Array.Empty<OwnerAppearance.FaceBlend>();
        }

        public MuscleOffset[] PoseOf(string id)
        {
            foreach (var pose in poses)
            {
                if (pose.id == id)
                {
                    return pose.muscles;
                }
            }

            return Array.Empty<MuscleOffset>();
        }

        // For data built from code.
        public void Init(Face[] moodFaces, Pose[] moodPoses)
        {
            faces = moodFaces ?? new Face[0];
            poses = moodPoses ?? new Pose[0];
        }

        [Serializable]
        public struct Face
        {
            public string id;
            public OwnerAppearance.FaceBlend[] blends;
        }

        [Serializable]
        public struct Pose
        {
            public string id;
            public MuscleOffset[] muscles;
        }

        [Serializable]
        public struct MuscleOffset
        {
            [Tooltip("A humanoid muscle name, e.g. Right Arm Down-Up.")]
            public string muscle;
            [Range(-1f, 1f)] public float value;
        }
    }
}
