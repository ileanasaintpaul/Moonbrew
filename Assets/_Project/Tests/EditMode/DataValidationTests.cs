using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Moonbrew.Core;
using Moonbrew.Inventory;
using Moonbrew.Player;
using Moonbrew.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    /// <summary>
    /// Checks every ScriptableObject under Assets/_Project/Data (items, recipes, customers, quests...).
    /// Adding content should never break the game: these tests catch it in CI.
    /// </summary>
    public class DataValidationTests
    {
        private const string DataFolder = "Assets/_Project/Data";
        private static readonly Regex SnakeCase = new Regex("^[a-z0-9]+(_[a-z0-9]+)*$");

        private static IEnumerable<string> DataAssetPaths()
        {
            if (!AssetDatabase.IsValidFolder(DataFolder))
            {
                return Enumerable.Empty<string>();
            }

            return AssetDatabase.FindAssets("t:Object", new[] { DataFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".asset"))
                .Distinct();
        }

        [Test]
        public void DataAssets_LoadWithoutMissingScript()
        {
            var broken = DataAssetPaths()
                .Where(path => AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) == null)
                .ToList();

            Assert.IsEmpty(broken, "Assets whose script is missing or renamed:\n" + string.Join("\n", broken));
        }

        [Test]
        public void DataAssets_HaveNoBrokenReferences()
        {
            var problems = new List<string>();

            foreach (var path in DataAssetPaths())
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                {
                    continue; // reported by DataAssets_LoadWithoutMissingScript
                }

                var property = new SerializedObject(asset).GetIterator();
                while (property.Next(true))
                {
                    // A reference that points to a deleted asset: not null in the file, null once loaded
                    if (property.propertyType == SerializedPropertyType.ObjectReference
                        && property.objectReferenceValue == null
                        && property.objectReferenceInstanceIDValue != 0)
                    {
                        problems.Add($"{path} -> {property.propertyPath}");
                    }
                }
            }

            Assert.IsEmpty(problems, "References to deleted assets:\n" + string.Join("\n", problems));
        }

        [Test]
        public void PlayerConfig_HaveUsableValues()
        {
            var problems = new List<string>();

            foreach (var path in DataAssetPaths())
            {
                var settings = AssetDatabase.LoadAssetAtPath<PlayerConfig>(path);
                if (settings == null)
                {
                    continue;
                }

                if (settings.WalkSpeed <= 0f)
                {
                    problems.Add($"{path}: walk speed must be positive");
                }

                if (settings.RunSpeed < settings.WalkSpeed)
                {
                    problems.Add($"{path}: run speed must not be slower than walk speed");
                }

                if (settings.TurnSpeed <= 0f)
                {
                    problems.Add($"{path}: turn speed must be positive");
                }

                if (settings.Gravity >= 0f)
                {
                    problems.Add($"{path}: gravity must pull down (negative)");
                }

                // The number row has ten keys, 1 to 0.
                if (settings.HotbarSlots < 1 || settings.HotbarSlots > 10)
                {
                    problems.Add($"{path}: the hotbar needs 1 to 10 slots");
                }

                if (settings.InteractRadius <= 0f)
                {
                    problems.Add($"{path}: interact radius must be positive");
                }
            }

            Assert.IsEmpty(problems, "Invalid player settings:\n" + string.Join("\n", problems));
        }

        [Test]
        public void ClockConfig_HasPositivePhasesAndFirstDay()
        {
            var problems = new List<string>();

            foreach (var path in DataAssetPaths())
            {
                var config = AssetDatabase.LoadAssetAtPath<ClockConfig>(path);
                if (config == null)
                {
                    continue;
                }

                if (config.PhaseDurations().Any(seconds => seconds <= 0f))
                {
                    problems.Add($"{path}: every phase must last more than 0 seconds");
                }

                if (config.FirstDay < 1)
                {
                    problems.Add($"{path}: the first day must be 1 or more");
                }

                var boundaries = config.PhaseStartMinutes();
                for (var i = 1; i < boundaries.Length; i++)
                {
                    if (boundaries[i] <= boundaries[i - 1])
                    {
                        problems.Add($"{path}: phase hours must strictly increase");
                    }
                }

                if (config.MinuteStep < 1)
                {
                    problems.Add($"{path}: the minute step must be at least 1");
                }
            }

            Assert.IsEmpty(problems, "Invalid clock config:\n" + string.Join("\n", problems));
        }

        [Test]
        public void CaptureConfig_HasUsableValues()
        {
            var problems = new List<string>();

            foreach (var path in DataAssetPaths())
            {
                var config = AssetDatabase.LoadAssetAtPath<CaptureConfig>(path);
                if (config == null)
                {
                    continue;
                }

                if (config.AdvanceMinutes < 1)
                {
                    problems.Add($"{path}: the advance shortcut must add at least 1 minute");
                }

                if (config.CameraMinZoom > config.CameraMaxZoom)
                {
                    problems.Add($"{path}: the minimum zoom must not be above the maximum");
                }

                if (config.CameraZoomStep <= 0f || config.CameraPanSpeed <= 0f)
                {
                    problems.Add($"{path}: the camera zoom step and pan speed must be positive");
                }
            }

            Assert.IsEmpty(problems, "Invalid capture config:\n" + string.Join("\n", problems));
        }

        [Test]
        public void ZonePalettes_HaveUniqueIdsAndUsableColors()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();

            foreach (var path in DataAssetPaths())
            {
                var palette = AssetDatabase.LoadAssetAtPath<ZonePalette>(path);
                if (palette == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(palette.Id))
                {
                    problems.Add($"{path}: the palette has no ID");
                }
                else if (!ids.Add(palette.Id))
                {
                    problems.Add($"{path}: the palette ID '{palette.Id}' is already used");
                }

                if (palette.Colors.Count == 0 || palette.Colors.Count > ZonePalette.MaxColors)
                {
                    problems.Add($"{path}: a palette needs 1 to {ZonePalette.MaxColors} colors");
                }

                if (palette.Colors.Any(color => color.a < 1f))
                {
                    problems.Add($"{path}: palette colors must be opaque");
                }
            }

            Assert.IsEmpty(problems, "Invalid zone palette:\n" + string.Join("\n", problems));
        }

        [Test]
        public void Items_HaveUniqueSnakeCaseIdsAndUsableValues()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();

            foreach (var path in DataAssetPaths())
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.Id) || !SnakeCase.IsMatch(item.Id))
                {
                    problems.Add($"{path}: the item ID '{item.Id}' must be snake_case, e.g. crop_wheat");
                }
                else if (!ids.Add(item.Id))
                {
                    problems.Add($"{path}: the item ID '{item.Id}' is already used");
                }

                if (string.IsNullOrWhiteSpace(item.DisplayName))
                {
                    problems.Add($"{path}: the item has no display name");
                }

                if (item.BasePrice < 0 || item.MaxStack < 1)
                {
                    problems.Add($"{path}: the price must not be negative and a stack must hold at least 1");
                }
            }

            Assert.IsEmpty(problems, "Invalid item:\n" + string.Join("\n", problems));
        }

        [Test]
        public void InventoryConfigs_HaveUniqueIdsAndSlots()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();

            foreach (var path in DataAssetPaths())
            {
                var config = AssetDatabase.LoadAssetAtPath<InventoryConfig>(path);
                if (config == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(config.Id) || !SnakeCase.IsMatch(config.Id))
                {
                    problems.Add($"{path}: the inventory ID '{config.Id}' must be snake_case, e.g. inventory_player");
                }
                else if (!ids.Add(config.Id))
                {
                    problems.Add($"{path}: the inventory ID '{config.Id}' is already used");
                }

                if (string.IsNullOrWhiteSpace(config.DisplayName))
                {
                    problems.Add($"{path}: the inventory has no display name");
                }

                if (config.SlotCount < 1)
                {
                    problems.Add($"{path}: an inventory needs at least one slot");
                }
            }

            Assert.IsEmpty(problems, "Invalid inventory config:\n" + string.Join("\n", problems));
        }
    }
}
