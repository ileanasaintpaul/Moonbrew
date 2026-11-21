using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // In a game scene the owner wears the look saved with the game, or the default look for an older save.
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
            var body = _catalog.PartsOf(AppearanceCategory.Body)[0];
            var hat = _catalog.PartsOf(AppearanceCategory.Hat)[0];
            var data = SaveData.CreateNew("slot", 1);
            data.appearance = new AppearanceSave { bodyId = body.Id, hatId = hat.Id };

            _bus.Publish(new GameLoaded(data));

            Assert.AreEqual(body.ModelChild, _owner.Body.name);
            Assert.AreEqual(1, _owner.Worn.Count, "Only the hat: no hair, beard or accessory.");
            Assert.AreEqual(_owner.Head, _owner.Worn[0].transform.parent);
        }

        [UnityTest]
        public IEnumerator GameLoaded_SaveWithoutALook_WearsTheDefaultLook()
        {
            yield return null;
            var expected = _catalog.Normalize(null);

            _bus.Publish(new GameLoaded(SaveData.CreateNew("slot", 1)));

            Assert.AreEqual(_catalog.FindPart(AppearanceCategory.Body, expected.bodyId).ModelChild, _owner.Body.name);
        }

        [UnityTest]
        public IEnumerator NewGameStarted_LookChosenAtCreation_IsWorn()
        {
            yield return null;
            var body = _catalog.PartsOf(AppearanceCategory.Body)[1];
            var data = SaveData.CreateNew("slot", 1);
            data.appearance = new AppearanceSave { bodyId = body.Id };

            _bus.Publish(new NewGameStarted(data));

            Assert.AreEqual(body.ModelChild, _owner.Body.name);
            Assert.IsEmpty(_owner.Worn);
        }

        [UnityTest]
        public IEnumerator Apply_UnknownHair_WearsNoHair()
        {
            yield return null;
            var body = _catalog.PartsOf(AppearanceCategory.Body)[0];

            _owner.Apply(new AppearanceSave { bodyId = body.Id, hairId = "hair_gone" });

            Assert.IsEmpty(_owner.Worn);
        }

        [UnityTest]
        public IEnumerator Apply_OnlyOneBodyIsShown()
        {
            yield return null;

            _owner.Apply(null);

            Assert.AreEqual(1, _owner.GetComponentsInChildren<SkinnedMeshRenderer>(false).Length);
        }

        [UnityTest]
        public IEnumerator Apply_TopAndBottomColours_EachHalfOfTheBodyWearsItsOwn()
        {
            yield return null;
            var top = _catalog.OutfitColours[1];
            var bottom = _catalog.OutfitColours[2];
            var body = _catalog.PartsOf(AppearanceCategory.Body)[0];

            _owner.Apply(new AppearanceSave { bodyId = body.Id, topColourId = top.Id, bottomColourId = bottom.Id });

            Assert.AreEqual(2, _owner.Body.sharedMesh.subMeshCount);
            Assert.Greater(_owner.Body.sharedMesh.GetIndexCount(1), 0, "The legs have triangles of their own.");
            CollectionAssert.AreEqual(new[] { top.Material, bottom.Material }, _owner.Body.sharedMaterials);
        }

        [UnityTest]
        public IEnumerator Apply_SkinAndBrowColours_TintTheBodyAndHairColourTintsTheHair()
        {
            yield return null;
            var skin = _catalog.SkinColours[_catalog.SkinColours.Count - 1];
            var hair = _catalog.HairColours[1];
            var brows = _catalog.HairColours[2];
            var look = _catalog.Normalize(null);
            look.skinColourId = skin.Id;
            look.hairColourId = hair.Id;
            look.browColourId = brows.Id;

            _owner.Apply(look);

            var colours = new MaterialPropertyBlock();
            _owner.Body.GetPropertyBlock(colours);
            AssertSameColour(skin.Swatch, colours.GetColor("_Skin_Color"));
            AssertSameColour(brows.Swatch, colours.GetColor("_Hair_Color"));
            _owner.Worn[0].GetComponentInChildren<Renderer>().GetPropertyBlock(colours);
            AssertSameColour(hair.Swatch, colours.GetColor("_Hair_Color"));
        }

        // A colour read back from a property block can differ in the last digits.
        private static void AssertSameColour(Color expected, Color actual)
        {
            Assert.Less(Vector4.Distance(expected, actual), 0.001f, $"Expected {expected}, was {actual}.");
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
