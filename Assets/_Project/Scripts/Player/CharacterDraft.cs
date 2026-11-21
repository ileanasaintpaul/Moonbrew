using System;
using Moonbrew.Core;

namespace Moonbrew.Player
{
    // The owner being created: a name and a look that always stays valid for the catalog.
    public class CharacterDraft
    {
        private readonly AppearanceCatalog _catalog;
        private AppearanceSave _look;

        public CharacterDraft(AppearanceCatalog catalog)
        {
            _catalog = catalog != null ? catalog : throw new ArgumentNullException(nameof(catalog));
            _look = catalog.Normalize(null);
        }

        public string Name { get; private set; } = "";

        public bool CanConfirm => !string.IsNullOrWhiteSpace(Name);

        // A copy: changing it does not change the draft.
        public AppearanceSave Look => Copy(_look);

        // Empty for none.
        public string ChosenId(AppearanceCategory category)
        {
            switch (category)
            {
                case AppearanceCategory.Body: return _look.bodyId;
                case AppearanceCategory.Hair: return _look.hairId;
                case AppearanceCategory.Beard: return _look.beardId;
                case AppearanceCategory.Hat: return _look.hatId;
                default: return _look.accessoryId;
            }
        }

        public string ColourId(AppearanceColourSlot slot) => AppearanceCatalog.ColourId(_look, slot);

        // An empty ID takes the part off, except for the body. False when the choice is not in the catalog.
        public bool Choose(AppearanceCategory category, string partId)
        {
            var none = string.IsNullOrEmpty(partId);
            if (none ? !AppearanceCatalog.IsOptional(category) : _catalog.FindPart(category, partId) == null)
            {
                return false;
            }

            var id = none ? "" : partId;
            switch (category)
            {
                case AppearanceCategory.Body: _look.bodyId = id; break;
                case AppearanceCategory.Hair: _look.hairId = id; break;
                case AppearanceCategory.Beard: _look.beardId = id; break;
                case AppearanceCategory.Hat: _look.hatId = id; break;
                default: _look.accessoryId = id; break;
            }

            return true;
        }

        // False when the colours of this slot have no such ID.
        public bool ChooseColour(AppearanceColourSlot slot, string colourId)
        {
            if (_catalog.FindColour(slot, colourId) == null)
            {
                return false;
            }

            AppearanceCatalog.SetColourId(_look, slot, colourId);
            return true;
        }

        public void Randomize(Random random)
        {
            _look = _catalog.Normalize(_catalog.Random(random));
        }

        // Cut to the catalog's longest name.
        public void Rename(string name)
        {
            name ??= "";
            Name = name.Length > _catalog.MaxNameLength ? name.Substring(0, _catalog.MaxNameLength) : name;
        }

        private static AppearanceSave Copy(AppearanceSave look)
        {
            return new AppearanceSave
            {
                bodyId = look.bodyId,
                hairId = look.hairId,
                beardId = look.beardId,
                hatId = look.hatId,
                accessoryId = look.accessoryId,
                hairColourId = look.hairColourId,
                browColourId = look.browColourId,
                skinColourId = look.skinColourId,
                topColourId = look.topColourId,
                bottomColourId = look.bottomColourId,
            };
        }
    }
}
