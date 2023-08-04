using System.Collections.Generic;
using System.Linq;

namespace TikTokLiveSharp.Events.MessageData.Messages
{
    public sealed class LinkMicBattleTaskMessage : AMessageData
    {
        internal LinkMicBattleTaskMessage(Models.Protobuf.Messages.WebcastLinkmicBattleTaskMessage msg)
            : base(msg?.Header?.RoomId ?? 0, msg?.Header?.MessageId ?? 0, msg?.Header?.ServerTime ?? 0)
        {
            
        }
    }
}
