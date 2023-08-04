using System.Collections.Generic;
using System.Linq;

namespace TikTokLiveSharp.Events.MessageData.Objects
{
    public sealed class LinkMicBattleTeam
    {
        public readonly ulong TeamId;
        public readonly IReadOnlyList<User> Users;

        public LinkMicBattleTeam(ulong teamId, List<User> users)
        {
            TeamId = teamId;
            Users = users;
        }

        internal LinkMicBattleTeam(Models.Protobuf.Messages.LinkMicBattleTeam team)
        {
            TeamId = team?.Id ?? 0;
           // Users = team?.TeamPlayers?.Players?.Select(u => u == null ? null : new User(u))?.ToList();
        }
    }
}