using UnityEngine;

namespace Moonbrew.Core
{
    // Display choices made in the settings, kept on this computer rather than in a game's save.
    // Fullscreen and resolution are already remembered by Unity.
    public static class DisplayPrefs
    {
        private const string PixelModeKey = "display.pixel_mode";

        public static bool PixelMode(bool fallback)
        {
            return PlayerPrefs.GetInt(PixelModeKey, fallback ? 1 : 0) == 1;
        }

        public static void SetPixelMode(bool pixelMode)
        {
            PlayerPrefs.SetInt(PixelModeKey, pixelMode ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
