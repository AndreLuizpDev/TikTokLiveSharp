namespace TikTokLiveSharp.Events.MessageData.Objects
{
    public sealed class Gift
    {
        public readonly ulong Id;
        public readonly string Name;
        public readonly string Description;
        public readonly uint DiamondCost;

        public readonly uint Type;

        public readonly Picture Picture;

        /// <summary>
        /// Can this Gift be sent in a Streak?
        /// <para>
        /// Gifts are only Streakable if their Type is 1
        /// </para>
        /// </summary>
        public bool IsStreakable => Type == 1;

        internal Gift(Models.Protobuf.Objects.Gift gift)
        {
            Id = gift?.Id ?? 0;
            Name = gift?.Name;
            Description = gift?.Description;
            DiamondCost = gift.CoinCount;
            Type = gift?.GiftType ?? 0;
            Picture = new Picture(gift?.Image);
        }
    }
}
