using System.Collections.Generic;
using Moonbrew.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // Fades the screen out when the player leaves for another scene and back in once they arrive.
    [RequireComponent(typeof(UIDocument))]
    public class ScreenFader : MonoBehaviour
    {
        private const string FadeClass = "screen-fade";
        private const string DarkClass = "screen-fade--dark";

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private SessionConfig sessionConfig;
        [SerializeField] private StyleSheet styleSheet;

        private UIDocument _document;
        private VisualElement _cover;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (eventBus != null)
            {
                eventBus.Subscribe<TravelStarted>(OnTravelStarted);
                eventBus.Subscribe<TravelArrived>(OnTravelArrived);
            }
        }

        private void OnDisable()
        {
            if (eventBus != null)
            {
                eventBus.Unsubscribe<TravelStarted>(OnTravelStarted);
                eventBus.Unsubscribe<TravelArrived>(OnTravelArrived);
            }

            _cover?.RemoveFromHierarchy();
            _cover = null;
        }

        private void OnTravelStarted(TravelStarted message)
        {
            EnsureCover()?.AddToClassList(DarkClass);
        }

        // The new scene opens dark, then clears once the cover has been drawn once.
        private void OnTravelArrived(TravelArrived message)
        {
            var cover = EnsureCover();
            if (cover == null)
            {
                return;
            }

            cover.style.transitionDuration = new List<TimeValue>();
            cover.AddToClassList(DarkClass);
            cover.schedule.Execute(() =>
            {
                SetDuration(cover);
                cover.RemoveFromClassList(DarkClass);
            });
        }

        private VisualElement EnsureCover()
        {
            if (_cover != null)
            {
                return _cover;
            }

            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null)
            {
                return null;
            }

            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            _cover = new VisualElement { pickingMode = PickingMode.Ignore };
            _cover.AddToClassList(FadeClass);
            SetDuration(_cover);
            root.Add(_cover);
            return _cover;
        }

        private void SetDuration(VisualElement cover)
        {
            var seconds = sessionConfig != null ? sessionConfig.TravelFadeSeconds : 0f;
            cover.style.transitionDuration = new List<TimeValue> { new TimeValue(seconds, TimeUnit.Second) };
        }
    }
}
