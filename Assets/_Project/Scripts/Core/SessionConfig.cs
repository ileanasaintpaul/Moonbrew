using UnityEngine;

namespace Moonbrew.Core
{
    [CreateAssetMenu(menuName = "Moonbrew/Core/Session Config", fileName = "SO_SessionConfig")]
    public class SessionConfig : ScriptableObject
    {
        [Tooltip("Folder under the platform's persistent data path holding one file per game.")]
        [SerializeField] private string savesFolder = "Saves";
        [Tooltip("Scene the player wakes up in each morning.")]
        [SerializeField] private string wakeScene = "SC_Farm";
        [Tooltip("Interaction ID of the object the player sleeps at to end the day.")]
        [SerializeField] private string sleepInteractionId = "farm_house_door";

        [Tooltip("Real seconds the screen takes to fade out before another scene loads.")]
        [Min(0f)]
        [SerializeField] private float travelFadeSeconds = 0.35f;

        public string SavesFolder => savesFolder;

        public float TravelFadeSeconds => travelFadeSeconds;

        public string WakeScene => wakeScene;

        public string SleepInteractionId => sleepInteractionId;
    }
}
