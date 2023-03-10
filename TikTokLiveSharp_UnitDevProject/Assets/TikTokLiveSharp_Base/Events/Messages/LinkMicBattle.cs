using System.Collections.Generic;
using System.Linq;
using TikTokLiveSharp.Models.Protobuf;

namespace TikTokLiveSharp.Events.MessageData.Messages
{
    public sealed class LinkMicBattle : AMessageData
    {
        public readonly IReadOnlyList<Objects.LinkMicBattleTeam> Team1;
        public readonly IReadOnlyList<Objects.LinkMicBattleTeam> Team2;
        /// <summary>
        /// UNCONFIRMED. Could be another type of Id
        /// </summary>
        public readonly ulong BattleId;

        internal LinkMicBattle(WebcastLinkMicBattle msg)
            : base(msg.Header.RoomId, msg.Header.MessageId, msg.Header.ServerTime)
        {
            BattleId = msg.Id1;
            Team1 = msg.Teams.Select(t => new Objects.LinkMicBattleTeam(t)).ToList();
            Team2 = msg.Teams2.Select(t => new Objects.LinkMicBattleTeam(t)).ToList();
        }
    }
}
