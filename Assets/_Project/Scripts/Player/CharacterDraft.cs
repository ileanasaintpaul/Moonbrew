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
            PronounsId = catalog.FindPronouns(null)?.Id ?? "";
        }

        public string Name { get; private set; } = "";

        // Empty only when the catalog has no pronoun choices.
        public string PronounsId { get; private set; }

        public bool CanConfirm => !string.IsNullOrWhiteSpace(Name);

        // A copy: changing it does not change the draft.
        public AppearanceSave Look => _catalog.Normalize(_look);

        // Empty for none.
        public string ChosenId(AppearanceCategory category) => AppearanceCatalog.PartId(_look, category);

        public string ColourId(AppearanceColourSlot slot) => AppearanceCatalog.ColourId(_look, slot);

        public int ShapeValue(BodyShape shape) => AppearanceCatalog.ShapeValue(_look, shape);

        // An empty ID takes the part off, except for the top and the bottom. False when the choice is not in the catalog.
        public bool Choose(AppearanceCategory category, string partId)
        {
            var none = string.IsNullOrEmpty(partId);
            if (none ? !AppearanceCatalog.IsOptional(category) : _catalog.FindPart(category, partId) == null)
            {
                return false;
            }

            AppearanceCatalog.SetPartId(_look, category, none ? "" : partId);
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

        // Kept within the gauge's limits.
        // Steps through the pronoun choices, wrapping round: 1 picks the next, -1 the previous.
        public void StepPronouns(int step)
        {
            var sets = _catalog.Pronouns;
            if (sets.Count == 0)
            {
                return;
            }

            var index = 0;
            for (var i = 0; i < sets.Count; i++)
            {
                if (sets[i] != null && sets[i].Id == PronounsId)
                {
                    index = i;
                }
            }

            index = ((index + step) % sets.Count + sets.Count) % sets.Count;
            PronounsId = sets[index] != null ? sets[index].Id : PronounsId;
        }

        public void SetShape(BodyShape shape, int value)
        {
            AppearanceCatalog.SetShapeValue(_look, shape, value);
        }

        // Cut to the catalog's longest name.
        public void Rename(string name)
        {
            name ??= "";
            Name = name.Length > _catalog.MaxNameLength ? name.Substring(0, _catalog.MaxNameLength) : name;
        }
    }
}
