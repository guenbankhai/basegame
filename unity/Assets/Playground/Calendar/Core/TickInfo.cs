namespace BaseGame.Playground.Calendar
{
    public readonly struct TickInfo
    {
        public TickInfo(CalendarDate date, long deltaMinutes, float speed, bool isFastForwarding)
        {
            Date = date;
            DeltaMinutes = deltaMinutes;
            Speed = speed;
            IsFastForwarding = isFastForwarding;
        }

        public CalendarDate Date { get; }
        /// <summary>Minutes since the last update (Ticked, Skipped or Restored).</summary>
        public long DeltaMinutes { get; }
        public float Speed { get; }
        public bool IsFastForwarding { get; }
    }
}
