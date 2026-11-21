using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // In a game scene the owner wears the look saved with the game, or the default look for an older save:
    // the parts' models on the skeleton, the colours in its colour map and the body gauges on the blend shapes.
    public class OwnerAppearanceIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private AppearanceCatalog _catalog;
        private OwnerAppearance _owner;

        [SetUp]
        public void SetUp()
        {
#if UNITY_EDITOR
            _catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<AppearanceCatalog>("Assets/_Project/Data/SO_AppearanceCatalog.asset");
            var model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/PF_Owner_Model.prefab");
#else
            Assert.Ignore("The owner model is loaded from the editor asset database.");
            GameObject model = null;
#endif
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            var owner = Create(Object.Instantiate(model));
            owner.SetActive(false);
            _owner = owner.GetComponent<OwnerAppearance>();
            _owner.Init(_catalog, _bus);
            owner.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                if (created != null)
                {
                    Object.Destroy(created);
                }
            }

            _created.Clear();
        }

        [UnityTest]
        public IEnumerator GameLoaded_SavedLook_IsWorn()
        {
            yield return null;
            var top = _catalog.PartsOf(AppearanceCategory.Top)[1];
            var hat = _catalog.PartsOf(AppearanceCategory.Hat)[0];
            var data = SaveData.CreateNew("slot", 1);
            data.appearance = new AppearanceSave { topId = top.Id, hatId = hat.Id };

            _bus.Publish(new GameLoaded(data));

            Assert.AreEqual(top.Models[0].name, _owner.Worn[BodyPartSlot.Of(top.Models[0].name)].name);
            Assert.IsTrue(_owner.Worn.ContainsKey(BodyPartSlot.Of(hat.Models[0].name)));
            Assert.IsFalse(_owner.Worn.ContainsKey("HAIR"), "No hair saved: none worn.");
        }

        [UnityTest]
        public IEnumerator GameLoaded_SaveWithoutALook_WearsTheDefaultLook()
        {
            yield return null;
            var expected = _catalog.FindPart(AppearanceCategory.Top, _catalog.Normalize(null).topId);

            _bus.Publish(new GameLoaded(SaveData.CreateNew("slot", 1)));

            Assert.AreEqual(expected.Models[0].name, _owner.Worn[BodyPartSlot.Of(expected.Models[0].name)].name);
        }

        [UnityTest]
        public IEnumerator Apply_BareBodyPartsNotCovered_StayOn()
        {
            yield return null;

            _owner.Apply(null);

            foreach (var model in _catalog.BaseModels)
            {
                Assert.IsTrue(_owner.Worn.ContainsKey(BodyPartSlot.Of(model.name)), model.name);
            }
        }

        [UnityTest]
        public IEnumerator Apply_HairColour_PaintsTheHairCells()
        {
            yield return null;
            var colour = _catalog.HairColours[_catalog.HairColours.Count - 1];
            var look = _catalog.Normalize(null);
            look.hairColourId = colour.Id;

            _owner.Apply(look);

            var cell = _catalog.PaintedCells(AppearanceColourSlot.Hair)[0];
            var painted = _owner.Colours.GetPixel(cell.cell.x * ColourMap.CellSize, cell.cell.y * ColourMap.CellSize);
            var expected = colour.Swatch * cell.colour;
            Assert.AreEqual(expected.r, painted.r, 0.01f);
            Assert.AreEqual(expected.b, painted.b, 0.01f);
        }

        [UnityTest]
        public IEnumerator Apply_BodySize_SetsTheHeavyBlendShape()
        {
            yield return null;
            var look = _catalog.Normalize(null);
            look.bodySize = 80;

            _owner.Apply(look);

            var torso = _owner.Worn["TORS"];
            var heavy = torso.sharedMesh.GetBlendShapeIndex(torso.sharedMesh.GetBlendShapeName(0).Split('.')[0] + "." + _catalog.HeavyBlendShape);
            Assert.GreaterOrEqual(heavy, 0);
            Assert.AreEqual(80f, torso.GetBlendShapeWeight(heavy), 0.01f);
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
