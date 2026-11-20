using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Rendering
{
    // Lights the scene for the time of day: the sun, the ambient light and the daylight through the windows.
    public class DayLightingDriver : MonoBehaviour
    {
        [SerializeField] private DayLightingConfig config;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private Light sun;
        [Tooltip("Lights standing for daylight through the windows: they take the sun's color.")]
        [SerializeField] private Light[] windows = new Light[0];

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(DayLightingConfig lightingConfig, EventBusAsset bus, Light sunLight, Light[] windowLights)
        {
            config = lightingConfig;
            eventBus = bus;
            sun = sunLight;
            windows = windowLights ?? new Light[0];
        }

        private void OnEnable()
        {
            if (config == null || eventBus == null || sun == null || config.Keys.Count == 0)
            {
                Debug.LogError($"{nameof(DayLightingDriver)} on {name} needs a config with keys, the event bus and a sun.", this);
                enabled = false;
                return;
            }

            eventBus.Subscribe<TimeChanged>(OnTimeChanged);
        }

        private void OnDisable()
        {
            if (eventBus != null)
            {
                eventBus.Unsubscribe<TimeChanged>(OnTimeChanged);
            }
        }

        private void OnTimeChanged(TimeChanged message)
        {
            var light = DayLighting.Evaluate(config.Keys, message.Minutes);
            sun.color = light.SunColor;
            sun.intensity = light.SunIntensity;
            RenderSettings.ambientSkyColor = light.AmbientSky;
            RenderSettings.ambientEquatorColor = light.AmbientEquator;
            RenderSettings.ambientGroundColor = light.AmbientGround;

            foreach (var window in windows)
            {
                if (window != null)
                {
                    window.color = light.SunColor;
                    window.intensity = light.WindowIntensity;
                    window.enabled = light.WindowIntensity > 0f;
                }
            }
        }
    }
}
