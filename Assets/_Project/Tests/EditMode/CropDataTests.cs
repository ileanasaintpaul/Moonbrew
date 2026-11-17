using System;
using Moonbrew.Farming;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Moonbrew.Tests.EditMode
{
    public class CropDataTests
    {
        private CropData _crop;
        private GameObject[] _stages;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_crop);
            if (_stages != null)
            {
                foreach (var stage in _stages)
                {
                    Object.DestroyImmediate(stage);
                }
            }
        }

        [Test]
        public void StageForDay_FourStagesOverFourDays_OneStagePerDay()
        {
            CreateCrop(growthDays: 4, stageCount: 4);

            Assert.AreEqual(new[] { 0, 0, 1, 2, 3 }, Array.ConvertAll(new[] { 0, 1, 2, 3, 4 }, _crop.StageForDay));
        }

        [Test]
        public void StageForDay_ThreeStagesOverSixDays_RipeOnlyOnTheLastDay()
        {
            CreateCrop(growthDays: 6, stageCount: 3);

            Assert.AreEqual(0, _crop.StageForDay(2));
            Assert.AreEqual(1, _crop.StageForDay(5));
            Assert.AreEqual(2, _crop.StageForDay(6));
        }

        [Test]
        public void StageForDay_NegativeDays_IsFirstStage()
        {
            CreateCrop(growthDays: 4, stageCount: 3);

            Assert.AreEqual(0, _crop.StageForDay(-2));
        }

        [Test]
        public void StageForDay_PastTheGrowthDays_StaysRipe()
        {
            CreateCrop(growthDays: 4, stageCount: 3);

            Assert.AreEqual(2, _crop.StageForDay(40));
        }

        [Test]
        public void StageForDay_NoStages_Throws()
        {
            CreateCrop(growthDays: 4, stageCount: 0);

            Assert.Throws<InvalidOperationException>(() => _crop.StageForDay(1));
        }

        [Test]
        public void StagePrefab_OutOfRange_ReturnsNull()
        {
            CreateCrop(growthDays: 4, stageCount: 2);

            Assert.IsNotNull(_crop.StagePrefab(1));
            Assert.IsNull(_crop.StagePrefab(2));
            Assert.IsNull(_crop.StagePrefab(-1));
        }

        [Test]
        public void Regrows_ZeroRegrowDays_IsFalse()
        {
            CreateCrop(growthDays: 4, stageCount: 2);

            Assert.IsFalse(_crop.Regrows);
        }

        private void CreateCrop(int growthDays, int stageCount)
        {
            _crop = ScriptableObject.CreateInstance<CropData>();
            _stages = new GameObject[stageCount];
            var serialized = new SerializedObject(_crop);
            serialized.FindProperty("growthDays").intValue = growthDays;
            var stages = serialized.FindProperty("stagePrefabs");
            stages.arraySize = stageCount;
            for (var i = 0; i < stageCount; i++)
            {
                _stages[i] = new GameObject("Stage " + i);
                stages.GetArrayElementAtIndex(i).objectReferenceValue = _stages[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
