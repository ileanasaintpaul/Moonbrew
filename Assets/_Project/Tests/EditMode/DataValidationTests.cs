using System.Collections.Generic;
using System.Linq;
using Moonbrew.Core;
using Moonbrew.Player;
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
    }
}
