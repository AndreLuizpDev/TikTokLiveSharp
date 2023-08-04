using ProtoBuf;
using System.ComponentModel;

namespace TikTokLiveSharp.Models.Protobuf.Objects.DataObjects
{
    [ProtoContract]
    public partial class IdData : AProtoBase
    {
        [ProtoMember(1)]
        [DefaultValue("")]
        public string Data1 { get; set; } = "";

        [ProtoMember(2)]
        [DefaultValue("")]
        public string Id1 { get; set; } = "";

        [ProtoMember(3)]
        [DefaultValue("")]
        public string Data2 { get; set; } = "";

        [ProtoMember(4)]
        [DefaultValue("")]
        public string Id2 { get; set; } = "";
        
        /// <summary>
        /// Known Values:
        ///
        /// - 0 (in UserRanking)
        /// - 36 (in Badge)
        /// </summary>
        [ProtoMember(5)]
        [DefaultValue("")]
        public string Data3 { get; set; } = "";
    }
}
