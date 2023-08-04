using ProtoBuf;
using TikTokLiveSharp.Models.Protobuf.Messages.Headers;

namespace TikTokLiveSharp.Models.Protobuf.Messages
{
    /// <summary>
    /// Unknown MessageType. Does not seem to have any (valuable) data.
    /// </summary>
    [ProtoContract]
    public partial class WebcastRoomVerifyMessage : AProtoBase
    {
        [ProtoMember(1)]
        public MessageHeader Header { get; set; }

        /// <summary>
        /// Known Values:
        ///
        /// - 3
        /// </summary>
        [ProtoMember(2)]
        public uint Data1 { get; set; }
    }
}
