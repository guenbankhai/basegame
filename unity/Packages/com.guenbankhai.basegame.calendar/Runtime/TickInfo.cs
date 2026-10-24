namespace BaseGame.Calendar
{
    /// <summary>One step's data. Only GameClock creates these.</summary>
    public readonly struct TickInfo
    {
        internal TickInfo(CalendarDate date, long deltaMinutes, float speed, bool isFastForwarding)
        {
            Date = date;
            DeltaMinutes = deltaMinutes;
            Speed = speed;
            IsFastForwarding = isFastForwarding;
        }

        public CalendarDate Date { get; }
        /// <summary>Minutes since the last Ticked, Skipped or Restored; always one step.</summary>
        public long DeltaMinutes { get; }
        public float Speed { get; }
        public bool IsFastForwarding { get; }
    }
}
