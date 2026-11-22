using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // The creation screen dresses the preview as cells, swatches and gauges change, and passes Start (with the name and look) on.
    public class CharacterCreationIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private readonly List<CharacterCreated> _createdOwners = new List<CharacterCreated>();
        private EventBusAsset _bus;
        private AppearanceCatalog _catalog;
        private OwnerAppearance _preview;
        private CharacterCreation _screen;

        [SetUp]
        public void SetUp()
        {
#if UNITY_EDITOR
            _catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<AppearanceCatalog>("Assets/_Project/Data/SO_AppearanceCatalog.asset");
            var model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/PF_Owner_Model.prefab");
            var tabs = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/CharacterCreation.uxml");
            var panelTree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/CharacterCreationPanel.uxml");
#else
            Assert.Ignore("The creation screen's assets are loaded from the editor asset database.");
            GameObject model = null;
            VisualTreeAsset tabs = null;
            VisualTreeAsset panelTree = null;
#endif
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _bus.Subscribe<CharacterCreated>(message => _createdOwners.Add(message));

            _preview = Create(Object.Instantiate(model)).GetComponent<OwnerAppearance>();
            var thumbnailModel = Create(Object.Instantiate(model, new Vector3(60f, 0f, 0f), Quaternion.identity)).GetComponent<OwnerAppearance>();
            var cameraObject = Create(new GameObject("Thumbnail Camera"));
            var thumbnailCamera = cameraObject.AddComponent<Camera>();
            thumbnailCamera.enabled = false;
            var thumbnailObject = Create(new GameObject("Thumbnails"));
            thumbnailObject.SetActive(false);
            var thumbnails = thumbnailObject.AddComponent<AppearanceThumbnails>();
            thumbnails.Init(thumbnailCamera, thumbnailModel);
            thumbnailObject.SetActive(true);

            var panelObject = Create(new GameObject("Panel"));
            var panel = panelObject.AddComponent<UIDocument>();
            panel.panelSettings = Create(ScriptableObject.CreateInstance<PanelSettings>());
            panel.visualTreeAsset = panelTree;

            var screenObject = Create(new GameObject("Creation"));
            screenObject.SetActive(false);
            var document = screenObject.AddComponent<UIDocument>();
            document.panelSettings = Create(ScriptableObject.CreateInstance<PanelSettings>());
            document.visualTreeAsset = tabs;
            _screen = screenObject.AddComponent<CharacterCreation>();
            _screen.Init(_bus, _catalog, _preview, thumbnails, panel);
            screenObject.SetActive(true);
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
            _createdOwners.Clear();
        }

        [UnityTest]
        public IEnumerator Start_ShowsTheBodyGaugesAndDressesThePreview()
        {
            yield return null;

            Assert.AreEqual(CharacterCreation.Page.Body, _screen.CurrentPage);
            var top = _catalog.FindPart(AppearanceCategory.Top, _screen.Draft.Look.topId);
            Assert.IsTrue(_preview.Worn.ContainsKey(BodyPartSlot.Of(top.Models[0].name)));
            Assert.AreEqual(top.Models[0].name, _preview.Worn[BodyPartSlot.Of(top.Models[0].name)].name);
        }

        [UnityTest]
        public IEnumerator HairCells_NoneTakesTheHairOffAndAHairPutsItOn()
        {
            yield return null;
            _screen.ShowPage(CharacterCreation.Page.Hair);
            var hairs = _catalog.PartsOf(AppearanceCategory.Hair);
            Assert.AreEqual(hairs.Count + 1, _screen.CellCount, "A cell per hair, plus None.");

            Click(_screen.CellAt(0));
            Assert.AreEqual("", _screen.Draft.Look.hairId);
            Assert.IsFalse(_preview.Worn.ContainsKey("HAIR"));

            Click(_screen.CellAt(1));
            Assert.AreEqual(hairs[0].Id, _screen.Draft.Look.hairId);
            Assert.IsTrue(_preview.Worn.ContainsKey("HAIR"));
        }

        [UnityTest]
        public IEnumerator TopPage_TopCellAndColour_ChangeTheTopButNotTheBottom()
        {
            yield return null;
            _screen.ShowPage(CharacterCreation.Page.Top);
            var tops = _catalog.PartsOf(AppearanceCategory.Top);
            var bottom = _screen.Draft.Look.bottomId;
            Assert.AreEqual(tops.Count, _screen.CellCount, "No None cell: a top is required.");

            Click(_screen.CellAt(tops.Count - 1));
            Click(_screen.SwatchAt(_catalog.OutfitColours.Count - 1));

            Assert.AreEqual(tops[tops.Count - 1].Id, _screen.Draft.Look.topId);
            Assert.AreEqual(_catalog.OutfitColours[_catalog.OutfitColours.Count - 1].Id, _screen.Draft.Look.topColourId);
            Assert.AreEqual(bottom, _screen.Draft.Look.bottomId);
        }

        [UnityTest]
        public IEnumerator BeardPage_Swatch_ChangesTheBeardColourButNotTheHair()
        {
            yield return null;
            _screen.ShowPage(CharacterCreation.Page.Beard);
            var hair = _screen.Draft.Look.hairColourId;
            var last = _catalog.HairColours.Count - 1;

            Click(_screen.SwatchAt(last));

            Assert.AreEqual(_catalog.HairColours[last].Id, _screen.Draft.Look.beardColourId);
            Assert.AreEqual(hair, _screen.Draft.Look.hairColourId);
        }

        [UnityTest]
        public IEnumerator BodyPage_GaugeAndSkinSwatch_ChangeTheBody()
        {
            yield return null;

            _screen.SliderOf(BodyShape.Size).value = 70;
            Click(_screen.SwatchAt(_catalog.SkinColours.Count - 1));

            Assert.AreEqual(70, _screen.Draft.Look.bodySize);
            Assert.AreEqual(_catalog.SkinColours[_catalog.SkinColours.Count - 1].Id, _screen.Draft.Look.skinColourId);
        }

        [UnityTest]
        public IEnumerator NamePage_PronounArrows_StepAndPassThePronounsOn()
        {
            yield return null;
            _screen.ShowPage(CharacterCreation.Page.Name);
            _screen.NameField.value = "Rose";
            yield return null;

            Click(_screen.ButtonNamed("pronouns-next"));
            Click(_screen.ButtonNamed("start"));

            Assert.AreEqual(_catalog.Pronouns[1].Id, _createdOwners[0].PronounsId);
        }

        [UnityTest]
        public IEnumerator HatPage_HasNoColour()
        {
            yield return null;

            _screen.ShowPage(CharacterCreation.Page.Hat);

            Assert.IsNull(_screen.SwatchAt(0));
        }

        [UnityTest]
        public IEnumerator Start_WithoutAName_IsDisabledAndPublishesNothing()
        {
            yield return null;
            _screen.ShowPage(CharacterCreation.Page.Name);

            Click(_screen.ButtonNamed("start"));

            Assert.IsFalse(_screen.ButtonNamed("start").enabledSelf);
            Assert.IsEmpty(_createdOwners);
        }

        [UnityTest]
        public IEnumerator Start_WithAName_PassesTheNameAndLookOn()
        {
            yield return null;
            _screen.ShowPage(CharacterCreation.Page.Name);
            _screen.NameField.value = " Rose ";
            yield return null;

            Click(_screen.ButtonNamed("start"));

            Assert.AreEqual(1, _createdOwners.Count);
            Assert.AreEqual("Rose", _createdOwners[0].OwnerName);
            Assert.AreEqual(_screen.Draft.Look.topId, _createdOwners[0].Appearance.topId);
        }

        private static void Click(Button button)
        {
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
