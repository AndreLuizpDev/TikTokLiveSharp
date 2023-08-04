using TikTokLiveSharp.Models.Protobuf.Messages;

namespace TikTokLiveSharp.Events.MessageData.Messages
{
    public sealed class RoomVerifyMessage : AMessageData
    {
        public RoomVerifyMessage(WebcastRoomVerifyMessage msg) : base(msg.Header.RoomId, msg.Header.MessageId,
            msg.Header.ServerTime)
        {
            MessageData = msg.Data1;
        }

        public readonly uint MessageData;
    }
}