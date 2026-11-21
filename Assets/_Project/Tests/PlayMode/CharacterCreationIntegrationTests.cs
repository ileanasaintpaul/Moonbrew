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
    // The creation screen dresses the preview as cells are clicked, and passes Start (with the name and look) and Back on.
    public class CharacterCreationIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private readonly List<CharacterCreated> _createdOwners = new List<CharacterCreated>();
        private EventBusAsset _bus;
        private AppearanceCatalog _catalog;
        private OwnerAppearance _preview;
        private CharacterCreation _screen;
        private int _backs;

        [SetUp]
        public void SetUp()
        {
#if UNITY_EDITOR
            _catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<AppearanceCatalog>("Assets/_Project/Data/SO_AppearanceCatalog.asset");
            var model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/PF_Owner_Model.prefab");
            var template = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/CharacterCreation.uxml");
#else
            Assert.Ignore("The creation screen's assets are loaded from the editor asset database.");
            GameObject model = null;
            VisualTreeAsset template = null;
#endif
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _bus.Subscribe<CharacterCreated>(message => _createdOwners.Add(message));
            _bus.Subscribe<TitleScreenChosen>(message => _backs++);

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

            var screenObject = Create(new GameObject("Creation"));
            screenObject.SetActive(false);
            var document = screenObject.AddComponent<UIDocument>();
            document.panelSettings = Create(ScriptableObject.CreateInstance<PanelSettings>());
            document.visualTreeAsset = template;
            _screen = screenObject.AddComponent<CharacterCreation>();
            _screen.Init(_bus, _catalog, _preview, thumbnails, new System.Random(4));
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
            _backs = 0;
        }

        [UnityTest]
        public IEnumerator Start_ShowsABodyCellPerBodyAndDressesThePreview()
        {
            yield return null;

            Assert.AreEqual(CharacterCreation.Page.Body, _screen.CurrentPage);
            Assert.AreEqual(_catalog.PartsOf(AppearanceCategory.Body).Count, _screen.CellCount);
            var body = _catalog.FindPart(AppearanceCategory.Body, _screen.Draft.Look.bodyId);
            Assert.AreEqual(body.ModelChild, _preview.Body.name);
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
            var wornWithoutHair = _preview.Worn.Count;

            Click(_screen.CellAt(1));
            Assert.AreEqual(hairs[0].Id, _screen.Draft.Look.hairId);
            Assert.AreEqual(wornWithoutHair + 1, _preview.Worn.Count);
        }

        [UnityTest]
        public IEnumerator BodyCell_ChangesTheBodyShown()
        {
            yield return null;
            var last = _catalog.PartsOf(AppearanceCategory.Body)[_screen.CellCount - 1];

            Click(_screen.CellAt(_screen.CellCount - 1));

            Assert.AreEqual(last.Id, _screen.Draft.Look.bodyId);
            Assert.AreEqual(last.ModelChild, _preview.Body.name);
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
            Assert.AreEqual(_screen.Draft.Look.bodyId, _createdOwners[0].Appearance.bodyId);
        }

        [UnityTest]
        public IEnumerator Back_GoesBackToTheTitleScreen()
        {
            yield return null;
            _screen.ShowPage(CharacterCreation.Page.Name);

            Click(_screen.ButtonNamed("back"));

            Assert.AreEqual(1, _backs);
        }

        [UnityTest]
        public IEnumerator Random_DressesThePreviewInTheDraftLook()
        {
            yield return null;

            Click(_screen.ButtonNamed("random"));

            var body = _catalog.FindPart(AppearanceCategory.Body, _screen.Draft.Look.bodyId);
            Assert.AreEqual(body.ModelChild, _preview.Body.name);
        }

        [UnityTest]
        public IEnumerator BeardPage_BrowSwatch_ChangesTheEyebrowsButNotTheHair()
        {
            yield return null;
            _screen.ShowPage(CharacterCreation.Page.Beard);
            var hair = _screen.Draft.Look.hairColourId;
            var brows = _catalog.HairColours[_catalog.HairColours.Count - 1];

            Click(_screen.SwatchAt(AppearanceColourSlot.Brows, _catalog.HairColours.Count - 1));

            Assert.AreEqual(brows.Id, _screen.Draft.Look.browColourId);
            Assert.AreEqual(hair, _screen.Draft.Look.hairColourId);
        }

        [UnityTest]
        public IEnumerator OutfitPage_BottomSwatch_ChangesTheBottomButNotTheTop()
        {
            yield return null;
            _screen.ShowPage(CharacterCreation.Page.Outfit);
            var top = _screen.Draft.Look.topColourId;
            var bottom = _catalog.OutfitColours[_catalog.OutfitColours.Count - 1];

            Click(_screen.SwatchAt(AppearanceColourSlot.Bottom, _catalog.OutfitColours.Count - 1));

            Assert.AreEqual(bottom.Id, _screen.Draft.Look.bottomColourId);
            Assert.AreEqual(top, _screen.Draft.Look.topColourId);
            Assert.AreEqual(bottom.Material, _preview.Body.sharedMaterials[1]);
        }

        [UnityTest]
        public IEnumerator HairPage_HairSwatch_ChangesTheHairColourUnderTheCuts()
        {
            yield return null;
            _screen.ShowPage(CharacterCreation.Page.Hair);
            var hair = _catalog.HairColours[_catalog.HairColours.Count - 1];

            Click(_screen.SwatchAt(AppearanceColourSlot.Hair, _catalog.HairColours.Count - 1));

            Assert.AreEqual(hair.Id, _screen.Draft.Look.hairColourId);
            Assert.AreEqual(_catalog.PartsOf(AppearanceCategory.Hair).Count + 1, _screen.CellCount, "The cuts are still shown.");
        }

        [UnityTest]
        public IEnumerator HatPage_HasNoColourSwatches()
        {
            yield return null;
            _screen.ShowPage(CharacterCreation.Page.Hat);

            Assert.IsNull(_screen.SwatchAt(AppearanceColourSlot.Hair, 0));
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
