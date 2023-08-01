using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TikTokLiveSharp.Client.HTTP;

namespace TikTokLiveSharp.Client.Socket
{
    /// <summary>
    /// WebSocket-Connection to TikTok-servers
    /// </summary>
    public sealed class TikTokWebSocket : ITikTokWebSocket
    {
        /// <summary>
        /// WebSocket-Client
        /// </summary>
        private ClientWebSocket clientWebSocket;
        /// <summary>
        /// Size for Messages
        /// <para>
        /// Should be large enough to be able to read a message to the end
        /// </para>
        /// </summary>
        private uint bufferSize;
        /// <summary>
        /// Token used to Cancel this Connection
        /// </summary>
        private CancellationToken token;
        /// <summary>
        /// Buffer used to store incoming Messages
        /// </summary>
        private byte[] buffer;

        /// <summary>
        /// Creates a TikTok WebSocket instance
        /// </summary>
        /// <param name="cookieContainer">The cookie container to use</param>
        /// <param name="buffSize">Size for Message-Buffer</param>
        public TikTokWebSocket(TikTokCookieJar cookieContainer, CancellationToken? token = null, uint buffSize = 500_000)
        {
            this.token = token ?? CancellationToken.None;
            bufferSize = buffSize;
            buffer = new byte[bufferSize];
            clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.AddSubProtocol("echo-protocol");
            clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            StringBuilder cookieHeader = new StringBuilder();
            foreach (string cookie in cookieContainer)
                cookieHeader.Append(cookie);
            clientWebSocket.Options.SetRequestHeader("Cookie", cookieHeader.ToString());            
        }

        /// <summary>
        /// Connects to the websocket
        /// </summary>
        /// <param name="url">Websocket url</param>
        /// <returns>Task to await</returns>
        public async Task Connect(string url)
        {
            token.ThrowIfCancellationRequested();
            await clientWebSocket.ConnectAsync(new Uri(url), token);
        }

        /// <summary>
        /// Disconnects from the websocket
        /// </summary>
        /// <returns>Task to await</returns>
        public async Task Disconnect()
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (clientWebSocket != null)
                {
                    await clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
                }
            }
            catch (OperationCanceledException)
            { } // Do Not Throw this. Clean exit
            finally
            {
                clientWebSocket = null;
            }
        }

        /// <summary>
        /// Writes a message to the websocket
        /// </summary>
        /// <param name="arr">The bytes to write</param>
        /// <returns>Task to await</returns>
        public async Task WriteMessage(ArraySegment<byte> arr)
        {
            token.ThrowIfCancellationRequested();
            if (clientWebSocket == null)
                return; // Invalid socket (Connection was closed?)
            await clientWebSocket.SendAsync(arr, WebSocketMessageType.Binary, true, token);
        }

        /// <summary>
        /// Receives a message from websocket. Result is Response-Message from Socket
        /// </summary>
        /// <returns>Task to Await</returns>
        public async Task<TikTokWebSocketResponse> ReceiveMessage()
        {
            token.ThrowIfCancellationRequested();
            if (clientWebSocket == null)
                return null; // Socket was Closed
            ArraySegment<byte> arr = new ArraySegment<byte>(buffer);
            WebSocketReceiveResult response = await clientWebSocket.ReceiveAsync(arr, token);
            if (response.MessageType == WebSocketMessageType.Binary)
                return new TikTokWebSocketResponse(arr.Array, response.Count);
            return null;
        }

        /// <summary>
        /// Is the websocket currently connected?
        /// </summary>
        public bool IsConnected => clientWebSocket != null && clientWebSocket.State == WebSocketState.Open;

        /// <summary>
        /// State for WebSocket
        /// </summary>
        public WebSocketState State => clientWebSocket.State;
    }
}
