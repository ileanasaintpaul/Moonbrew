namespace Moonbrew.Core
{
    // Events shared by all modules, named in the past tense. Publish them on the EventBusAsset.
    public readonly struct DayStarted
    {
        public DayStarted(int day)
        {
            Day = day;
        }

        public int Day { get; }
    }

    public readonly struct DayEnded
    {
        public DayEnded(int day)
        {
            Day = day;
        }

        public int Day { get; }
    }

    public readonly struct PhaseChanged
    {
        public PhaseChanged(int day, DayPhase phase)
        {
            Day = day;
            Phase = phase;
        }

        public int Day { get; }

        public DayPhase Phase { get; }
    }

    // Minutes since midnight of the current day, past 24 h after midnight.
    public readonly struct TimeChanged
    {
        public TimeChanged(int minutes)
        {
            Minutes = minutes;
        }

        public int Minutes { get; }
    }

    public readonly struct Interacted
    {
        public Interacted(string interactionId)
        {
            InteractionId = interactionId;
        }

        public string InteractionId { get; }
    }
}
