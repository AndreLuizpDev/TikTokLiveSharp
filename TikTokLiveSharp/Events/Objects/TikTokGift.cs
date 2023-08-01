using TikTokLiveSharp.Models.Protobuf.Messages;

namespace TikTokLiveSharp.Events.MessageData.Objects
{
    public class TikTokGift
    {
        public delegate void TikTokGiftEventHandler<TEventArgs>(TikTokGift gift, TEventArgs args);

        public event TikTokGiftEventHandler<uint> OnAmountChanged;
        public event TikTokGiftEventHandler<uint> OnStreakFinished;

        public readonly Gift Gift;

        public readonly User Sender;

        public uint Amount { get; protected set; }
        public bool StreakFinished { get; protected set; }


        public TikTokGift(WebcastGiftMessage message)
        {
            Gift = new Gift(message?.GiftDetails); 
            if (message?.Sender != null)
                Sender = new User(message.Sender);
            Amount = message?.Amount ?? 0;
            if (Gift.IsStreakable)
                StreakFinished = message?.RepeatEnd ?? true;
            else
                StreakFinished = true;
        }

        internal virtual void FinishStreak()
        {
#if UNITY // This Code is strictly for TikTokLive-Unity
            TikTokLiveUnity.Utils.Dispatcher.RunOnMainThread(() => {
#endif
                StreakFinished = true;
                OnStreakFinished?.Invoke(this, Amount);
#if UNITY
            });
#endif
        }

        internal void UpdateGiftAmount(uint amount)
        {
#if UNITY // This Code is strictly for TikTokLive-Unity
            TikTokLiveUnity.Utils.Dispatcher.RunOnMainThread(() => {
#endif
                Amount = amount;
                OnAmountChanged?.Invoke(this, amount);
#if UNITY
            });
#endif
        }
    }
}