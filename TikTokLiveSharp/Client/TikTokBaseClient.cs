using Newtonsoft.Json.Linq;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TikTokLiveSharp.Client.HTTP;
using TikTokLiveSharp.Client.Proxy;
using TikTokLiveSharp.Client.Socket;
using TikTokLiveSharp.Debugging;
using TikTokLiveSharp.Errors.Connections;
using TikTokLiveSharp.Errors.FetchErrors;
using TikTokLiveSharp.Errors.Messaging;
using TikTokLiveSharp.Models.HTTP;
using TikTokLiveSharp.Models.Protobuf.Messages.Generic;

namespace TikTokLiveSharp.Client
{
    /// <summary>
    /// Base-Client for TikTokLive. Handles Connections, Fetching of initial Info & Messaging
    /// </summary>
    public abstract class TikTokBaseClient
    {
        #region Events
        /// <summary>
        /// Event thrown if an Operation threw an Exception
        /// <para>
        /// Used to ensure Exceptions can be handled even if a Thread crashes
        /// </para>
        /// </summary>
        public event EventHandler<Exception> OnException;
        #endregion

        #region Properties
        #region Public
        /// <summary>
        /// Available Gifts for Room
        /// </summary>
        public IReadOnlyDictionary<int, TikTokGift> AvailableGifts => availableGifts;
        /// <summary>
        /// Whether currently Connected to the TikTokServers
        /// </summary>
        public bool Connected => socketClient?.IsConnected ?? false;
        /// <summary>
        /// Whether currently Connecting to the TikTokServers
        /// </summary>
        public bool Connecting { get; protected set; }
        /// <summary>
        /// RoomID for Connected Room
        /// </summary>
        public string RoomID { get; protected set; }
        /// <summary>
        /// RoomInfo for Connected Room
        /// </summary>
        public JObject RoomInfo { get; protected set; }
        /// <summary>
        /// UserName of Host for Connected Room
        /// </summary>
        public string HostName { get; protected set; }
        /// <summary>
        /// Number of Viewers in Connected Room
        /// </summary>
        public uint? ViewerCount { get; protected set; }
        #endregion

        #region Protected
        /// <summary>
        /// Available Gifts for Room
        /// </summary>
        protected Dictionary<int, TikTokGift> availableGifts;
        /// <summary>
        /// Additional Parameters for HTTP-Client
        /// </summary>
        protected Dictionary<string, object> clientParams;
        /// <summary>
        /// Settings for this Client
        /// </summary>
        protected ClientSettings settings;
        /// <summary>
        /// WebSocket-Client
        /// </summary>
        public TikTokWebSocket socketClient;
        #endregion

        #region Private
        /// <summary>
        /// HTTP-Client
        /// </summary>
        private TikTokHTTPClient httpClient;
        /// <summary>
        /// Token used to Cancel this Client
        /// </summary>
        private CancellationToken token;
        /// <summary>
        /// Running Task(s) for this Client
        /// </summary>
        private Task runningTask, pollingTask;
        /// <summary>
        /// Url that socket is connected to
        /// </summary>
        private string connectedSocketUrl;
        #endregion
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor for a TikTokBaseClient
        /// </summary>
        /// <param name="hostId">ID for Room to Connect to</param>
        /// <param name="settings">Settings for Client</param>
        /// <param name="clientParams">Additional Parameters for HTTP-Client</param>
        public TikTokBaseClient(string hostId, ClientSettings? settings = null, Dictionary<string, object> clientParams = null)
        {
            HostName = hostId;
            if (!settings.HasValue)
            {
                Debug.Log("Using Default Settings");
                settings = Constants.DEFAULT_SETTINGS;
            }
            this.settings = settings.Value;
            CheckSettings();
            RoomInfo = null;
            availableGifts = new Dictionary<int, TikTokGift>();
            RoomID = null;
            ViewerCount = null;
            Connecting = false;
            this.clientParams = new Dictionary<string, object>();
            foreach (var parameter in Constants.DEFAULT_CLIENT_PARAMS)
                this.clientParams.Add(parameter.Key, parameter.Value);
            if (clientParams != null)
                foreach (var param in clientParams)
                    this.clientParams[param.Key] = param.Value;
            this.clientParams["app_language"] = this.settings.ClientLanguage;
            this.clientParams["webcast_language"] = this.settings.ClientLanguage;
            httpClient = new TikTokHTTPClient(this.settings.Timeout, this.settings.Proxy);
        }


        /// <summary>
        /// Constructor for a TikTokBaseClient
        /// </summary>
        /// <param name="uniqueID">Host-Name for Room to Connect to</param>
        /// <param name="timeout">Timeout for Connections</param>
        /// <param name="pollingInterval">Polling Interval for WebSocket-Connection</param>
        /// <param name="clientParams">Additional Parameters for HTTP-Client</param>
        /// <param name="processInitialData">Whether to process Data received when Connecting</param>
        /// <param name="enableExtendedGiftInfo">Whether to download List of Gifts on Connect</param>
        /// <param name="proxyHandler">Proxy for Connection</param>
        /// <param name="lang">ISO-Language for Client</param>
        /// <param name="socketBufferSize">BufferSize for WebSocket-Messages</param>
        /// <param name="logDebug">Whether to log messages to the Console</param>
        /// <param name="logLevel">LoggingLevel for debugging</param>
        /// <param name="printMessageData">Whether to print Base64-Data for Messages to Console</param>
        /// <param name="checkForUnparsedData">Whether to check Messages for Unparsed Data</param>
        public TikTokBaseClient(string uniqueID,
            TimeSpan? timeout,
            TimeSpan? pollingInterval,
            Dictionary<string, object> clientParams = null,
            bool processInitialData = true,
            bool enableExtendedGiftInfo = true,
            RotatingProxy proxyHandler = null,
            string lang = "en-US",
            uint socketBufferSize = 10_000,
            bool logDebug = true, 
            LogLevel logLevel = LogLevel.Error | LogLevel.Warning,
            bool printMessageData = false,
            bool checkForUnparsedData = false
            )
            : this(uniqueID,
                  new ClientSettings
                  {
                      Timeout = timeout ?? TimeSpan.FromSeconds(Constants.DEFAULT_TIMEOUT),
                      PollingInterval = pollingInterval ?? TimeSpan.FromSeconds(Constants.DEFAULT_POLLTIME),
                      HandleExistingMessagesOnConnect = processInitialData,
                      DownloadGiftInfo = enableExtendedGiftInfo,
                      Proxy = proxyHandler,
                      ClientLanguage = lang,
                      SocketBufferSize = socketBufferSize,
                      PrintToConsole = logDebug,
                      LogLevel = logLevel,
                      PrintMessageData = printMessageData,
                      CheckForUnparsedData = checkForUnparsedData
                  },
                  clientParams)
        { }

        /// <summary>
        /// Checks if ClientSettings are Valid
        /// </summary>
        private void CheckSettings()
        {
            ClientSettings s = settings;
            if (settings.Timeout == default)
                s.Timeout = Constants.DEFAULT_SETTINGS.Timeout;
            if (settings.PollingInterval == default)
                s.PollingInterval = Constants.DEFAULT_SETTINGS.PollingInterval;
            if (string.IsNullOrEmpty(settings.ClientLanguage))
                s.ClientLanguage = Constants.DEFAULT_SETTINGS.ClientLanguage;
            if (settings.SocketBufferSize < 500_000)
                s.SocketBufferSize = Constants.DEFAULT_SETTINGS.SocketBufferSize;
            settings = s;
        }
        #endregion

        #region Static
        /// <summary>
        /// Checks if a User exists on TikTok by attempting to get their Profile-Page
        /// </summary>
        /// <param name="userId">@-ID for User</param>
        /// <param name="timeOut">TimeOut for HTTP-Connection (set NULL for default)</param>
        /// <param name="proxy">Proxy to use with HTTP-Client</param>
        /// <returns>True if User has a Profile-Page on TikTok</returns>
        public static async Task<bool> GetUserExists(string userId, TimeSpan? timeOut = null, RotatingProxy proxy = null)
        {
            if (!timeOut.HasValue)
                timeOut = Constants.DEFAULT_SETTINGS.Timeout;
            TikTokHTTPClient tempClient = new TikTokHTTPClient(timeOut.Value, proxy);
            string html;
            try
            {
                html = await tempClient.GetProfilePage(userId);
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("NOT_FOUND"))
                    return false; // Profile returned 404.
                FailedFetchRoomInfoException exc =
                    new FailedFetchRoomInfoException(
                        "Failed to fetch html from ProfilePage, see stacktrace for more info.", ex);
                Debug.LogException(exc);
                return false;
            }
            catch (Exception e)
            {
                FailedFetchRoomInfoException exc =
                    new FailedFetchRoomInfoException(
                        "Failed to fetch html from ProfilePage, see stacktrace for more info.", e);
                Debug.LogException(exc);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if a User is currently streaming by looking for a RoomId on their Live-Page
        /// </summary>
        /// <param name="userId">@-ID for User</param>
        /// <param name="timeOut">TimeOut for HTTP-Connection (set NULL for default)</param>
        /// <param name="proxy">Proxy to use with HTTP-Client</param>
        /// <returns>True if User is currently streaming on TikTok</returns>
        public static async Task<bool> GetUserStreaming(string userId, TimeSpan? timeOut = null, RotatingProxy proxy = null)
        {
            if (!timeOut.HasValue)
                timeOut = Constants.DEFAULT_SETTINGS.Timeout;
            TikTokHTTPClient tempClient = new TikTokHTTPClient(timeOut.Value, proxy);
            string html;
            try
            {
                html = await tempClient.GetLivestreamPage(userId);
            }
            catch (Exception e)
            {
                FailedFetchRoomInfoException exc = new FailedFetchRoomInfoException("Failed to fetch html from Livestream-Page, see stacktrace for more info.", e);
                Debug.LogException(exc);
                return false;
            }
            var first = Regex.Match(html, "room_id=([0-9]*)");
            var second = Regex.Match(html, "\"roomId\":\"([0 - 9] *)\"");
            string id = first.Groups[1]?.Value ?? second.Groups[1]?.Value ?? string.Empty;
            return !string.IsNullOrEmpty(id);
        }
        #endregion

        #region Connect
        /// <summary>
        /// Creates Threads for & Runs Connection with TikTokServers
        /// </summary>
        /// <param name="cancellationToken">Token used to Cancel Client</param>
        /// <param name="onConnectException">Callback for Errors during Exception</param>
        /// <param name="retryConnection">Whether to Retry connections that might be recoverable</param>
        public void Run(CancellationToken? cancellationToken = null, Action<Exception> onConnectException = null, bool retryConnection = false)
        {
            token = cancellationToken ?? new CancellationToken();
            token.ThrowIfCancellationRequested();
            if (ShouldLog(LogLevel.Information))
                Debug.Log("Starting Threads");
            var run = Task.Run(() => Start(token, null, retryConnection), token);
            run.Wait();
            runningTask.Wait();
            pollingTask.Wait();
        }

        /// <summary>
        /// Starts Connection with TikTokServers
        /// </summary>
        /// <param name="cancellationToken">Token used to Cancel Client</param>
        /// <param name="onConnectException">Callback for Errors during Exception</param>
        /// <param name="retryConnection">Whether to Retry connections that might be recoverable</param>
        /// <exception cref="AlreadyConnectedException">Exception thrown if Already Connected</exception>
        /// <exception cref="AlreadyConnectingException">Exception thrown if Already Connecting</exception>
        /// <returns>Task to Await. Result is RoomID</returns>
        public async Task<string> Start(CancellationToken? cancellationToken = null, Action<Exception> onConnectException = null, bool retryConnection = false)
        {
            token = cancellationToken ?? new CancellationToken();
            try
            {
                token.ThrowIfCancellationRequested();
                if (ShouldLog(LogLevel.Information))
                    Debug.Log("Connecting");
                return await Connect(onConnectException);
            }
            catch (OperationCanceledException) // cancelled by User
            {
                if (ShouldLog(LogLevel.Warning))
                    Debug.LogWarning("Cancelled by User");
                return null;
            }
            catch (AConnectionException e)
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(e);
                if (e is FailedConnectionException)
                {
                    // Failed to Connect, but Host was Online
                    if (retryConnection)
                    {
                        if (ShouldLog(LogLevel.Information))
                            Debug.Log("Retrying");
                        await Task.Delay(settings.PollingInterval);
                        return await Start(cancellationToken, onConnectException, retryConnection);
                    }
                    else
                    {
                        onConnectException?.Invoke(e);
                        throw e;
                    }
                }
                else if (e is AlreadyConnectedException || e is AlreadyConnectingException)
                {
                    onConnectException?.Invoke(e); // Already Connected
                    return null;  // Exit Quietly
                }
                else if (e is LiveNotFoundException)
                {
                    onConnectException?.Invoke(e); // LiveStream was not Found (or Host is not Online)
                    throw e;
                }
                return null;
            }
            catch (AFetchException e)
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(e);
                onConnectException?.Invoke(e); // Failed to fetch critical Info for Connection
                throw e;
            }
            catch (Exception e) // Other type of Exception
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(e);
                onConnectException?.Invoke(e);
                throw e;
            }
        }

        /// <summary>
        /// Connects to TikTok-Servers
        /// </summary>
        /// <param name="onConnectException">Callback for Exceptions thrown whilst Connecting</param>
        /// <returns>Task to Await. Result is RoomID</returns>
        /// <exception cref="AlreadyConnectingException">Exception thrown if Already Connecting</exception>
        /// <exception cref="AlreadyConnectedException">Exception thrown if Already Connected</exception>
        /// <exception cref="LiveNotFoundException">Exception thrown if Room could not be found for Host</exception>
        protected virtual async Task<string> Connect(Action<Exception> onConnectException = null)
        {
            if (Connecting)
                throw new AlreadyConnectingException();
            if (Connected)
                throw new AlreadyConnectedException();
            Connecting = true;
            if (ShouldLog(LogLevel.Verbose))
                Debug.Log("Fetching RoomID");
            RoomID = await FetchRoomId();
            token.ThrowIfCancellationRequested();
            if (ShouldLog(LogLevel.Verbose))
                Debug.Log("Fetch RoomInfo");
            JObject info = await FetchRoomInfo();
            JToken status = info["data"]?["status"];
            if (status == null || status.Value<int>() == 4)
                throw new LiveNotFoundException("LiveStream for HostID could not be found. Is the Host online?");
            token.ThrowIfCancellationRequested();
            if (settings.DownloadGiftInfo)
            {
                try
                {
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log("Fetch GiftInfo");
                    await FetchAvailableGifts();
                }
                catch (FailedFetchGiftsException e)
                {
                    if (ShouldLog(LogLevel.Error))
                        Debug.LogException(e);
                    onConnectException?.Invoke(e);
                    // Continue connecting (not a critical error)
                }
            }
            token.ThrowIfCancellationRequested();
            if (ShouldLog(LogLevel.Verbose))
                Debug.Log("Fetch ClientData");
            WebcastResponse response = await FetchClientData();
            token.ThrowIfCancellationRequested();
            if (ShouldLog(LogLevel.Information))
                Debug.Log("Creating WebSocketClient");
            await CreateWebSocket(response);
            token.ThrowIfCancellationRequested();
            Connecting = false;
            return RoomID;
        }

        /// <summary>
        /// Creates WebSocket-Connection for Client
        /// </summary>
        /// <param name="webcastResponse">Response with Client-Data</param>
        /// <returns>Task to await</returns>
        /// <exception cref="LiveNotFoundException">Thrown if Room could not be found (Invalid WebcastResponse)</exception>
        /// <exception cref="FailedConnectionException">Thrown if WebSocket could not Connect</exception>
        /// <exception cref="WebcastMessageException">Thrown if an Error occurred during Parse of initial Messages</exception>
        protected async Task CreateWebSocket(WebcastResponse webcastResponse)
        {
            if (string.IsNullOrEmpty(webcastResponse.SocketUrl) || webcastResponse.SocketParams == null)
                throw new LiveNotFoundException("Could not find Room");
            try
            {
                for (int i = 0; i < webcastResponse.SocketParams.Count; i++)
                {
                    WebsocketRouteParam param = webcastResponse.SocketParams[i];
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log($"Adding Custom Param {param.Name}-{param.Value}");
                    clientParams[param.Name] = param.Value;
                }
                string url = $"{webcastResponse.SocketUrl}?{string.Join("&", clientParams.Select(x => $"{x.Key}={WebUtility.UrlEncode(x.Value.ToString())}"))}";
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log($"Creating Socket with URL {url}");
                socketClient = new TikTokWebSocket(TikTokHttpRequest.CookieJar, token, settings.SocketBufferSize);
                connectedSocketUrl = url;
                await socketClient.Connect(url);
                if (ShouldLog(LogLevel.Information))
                    Debug.Log($"Starting Socket-Threads");
                runningTask = Task.Run(WebSocketLoop, token);
                pollingTask = Task.Run(PingLoop, token);
            }
            catch (Exception e)
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(e);
                connectedSocketUrl = null;
                throw new FailedConnectionException("Failed to connect to the websocket", e);
            }
            if (settings.HandleExistingMessagesOnConnect)
            {
                try
                {
                    HandleWebcastMessages(webcastResponse);
                }
                catch (Exception e)
                {
                    if (ShouldLog(LogLevel.Error))
                        Debug.LogException(e);
                    throw new WebcastMessageException("Error Handling Initial Messages", e);
                }
            }
        }
        #endregion

        #region Disconnect
        /// <summary>
        /// Stops this Client
        /// </summary>
        /// <returns>Task to await</returns>
        public async Task Stop()
        {
            if (Connected)
            {
                try
                {
                    await Disconnect();
                }
                catch (WebSocketException) { }
            }
        }

        /// <summary>
        /// Disconnects 
        /// </summary>
        /// <returns>Task to await</returns>
        protected virtual async Task Disconnect()
        {
            RoomInfo = null;
            Connecting = false;
            connectedSocketUrl = null;
            if (Connected)
            {
                if (ShouldLog(LogLevel.Information))
                    Debug.Log("Disconnecting SocketClient");
                await socketClient.Disconnect();
            }
            clientParams["cursor"] = string.Empty;
            await runningTask;
            await pollingTask;
        }
        #endregion

        #region FetchData
        /// <summary>
        /// Fetches RoomID for Host
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FailedFetchRoomInfoException">Thrown if valid RoomID for Host could not be parsed</exception>
        protected async Task<string> FetchRoomId()
        {
            string html;
            try
            {
                html = await httpClient.GetLivestreamPage(HostName);
            }
            catch (Exception e)
            {
                FailedFetchRoomInfoException exc = new FailedFetchRoomInfoException("Failed to fetch room id from WebCast, see stacktrace for more info.", e);
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(exc);
                throw exc;
            }
            var first = Regex.Match(html, "room_id=([0-9]*)");
            var second = Regex.Match(html, "\"roomId\":\"([0 - 9] *)\"");
            string id = first.Groups[1]?.Value ?? second.Groups[1]?.Value ?? string.Empty;
            if (!string.IsNullOrEmpty(id))
            {
                clientParams["room_id"] = id;
                RoomID = id;
                return id;
            }
            else
            {
                if (html.Contains("Please wait..."))
                    return await FetchRoomId();
                FailedFetchRoomInfoException exc = new FailedFetchRoomInfoException(html.Contains("\"og:url\"") ? "User might be offline" : "Your IP or country might be blocked by TikTok.");
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(exc);
                throw exc;
            }
        }

        /// <summary>
        /// Fetches List of available Gifts (for Room)
        /// </summary>
        /// <returns>Task to await. Result is Gifts by ID</returns>
        /// <exception cref="FailedFetchGiftsException">Thrown if Operation had an Error</exception>
        public async Task<Dictionary<int, TikTokGift>> FetchAvailableGifts()
        {
            try
            {
                JObject response = await httpClient.GetJObjectFromWebcastAPI("gift/list/", clientParams);
                var giftTokens = response.SelectTokens("..gifts")?.FirstOrDefault()?.Children() ?? null;
                if (giftTokens == null)
                    return new Dictionary<int, TikTokGift>();
                foreach (JToken giftToken in giftTokens)
                {
                    TikTokGift gift = giftToken.ToObject<TikTokGift>();
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log($"Found Available Gift {gift.name} with ID {gift.id}");
                    availableGifts[gift.id] = gift;
                }
                return availableGifts;
            }
            catch (Exception e)
            {
                FailedFetchGiftsException exc = new FailedFetchGiftsException("Failed to fetch giftTokens from WebCast, see stacktrace for more info.", e);
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(exc);
                throw exc;
            }
        }

        /// <summary>
        /// Fetches Client-Cursor for User
        /// </summary>
        /// <returns>Task to Await. Result is ResponseMessage</returns>
        protected async Task<WebcastResponse> FetchClientData()
        {
            WebcastResponse webcastResponse = await httpClient.GetDeserializedMessage("im/fetch/", clientParams, true);
            clientParams["cursor"] = webcastResponse.Cursor;
            clientParams["internal_ext"] = webcastResponse.AckIds;
            return webcastResponse;
        }

        /// <summary>
        /// Fetches MetaData for Room
        /// </summary>
        /// <returns>Task to await. Result is JSON for RoomInfo</returns>
        /// <exception cref="FailedFetchRoomInfoException">Thrown if Operation had an Error</exception>
        protected async Task<JObject> FetchRoomInfo()
        {
            try
            {
                JObject response = await httpClient.GetJObjectFromWebcastAPI("room/info/", clientParams);
                RoomInfo = response;
                return response;
            }
            catch (Exception e)
            {
                FailedFetchRoomInfoException exc = new FailedFetchRoomInfoException("Failed to fetch room info from WebCast, see stacktrace for more info.", e);
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(exc);
                throw exc;
            }
        }
        #endregion

        #region SocketLoop
        /// <summary>
        /// Receives Messages from WebSocket. Sends back Acknowledgements for each
        /// </summary>
        /// <returns>Task to await</returns>
        /// <exception cref="WebSocketException">Thrown if WebSocket crashed with an Error</exception>
        protected async Task WebSocketLoop()
        {
            while (!token.IsCancellationRequested && socketClient.IsConnected)
            {
                token.ThrowIfCancellationRequested();
                await CheckSocketConnection(); // Check SocketConnection
                token.ThrowIfCancellationRequested();
                TikTokWebSocketResponse response = await socketClient.ReceiveMessage();
                if (response == null) 
                    continue;
                try
                {
                    token.ThrowIfCancellationRequested();
                    Debug.Log("Received Message");
                    using (var websocketMessageStream = new MemoryStream(response.Array, 0, response.Count))
                    {
                        token.ThrowIfCancellationRequested();
                        WebcastWebsocketMessage websocketMessage = Serializer.Deserialize<WebcastWebsocketMessage>(websocketMessageStream);
                        //if (websocketMessage.Binary != null)
                        //{
                        //    using (var messageStream = new MemoryStream(websocketMessage.Binary))
                        //    {
                        //        token.ThrowIfCancellationRequested();
                        //        WebcastResponse message = Serializer.Deserialize<WebcastResponse>(messageStream);
                        //        token.ThrowIfCancellationRequested();
                        //        await SendAcknowledgement(websocketMessage.Id);
                        //        token.ThrowIfCancellationRequested();
                        //        HandleWebcastMessages(message);
                        //    }
                        //}
                    }
                }
                catch (OperationCanceledException)
                {
                    if (ShouldLog(LogLevel.Information))
                        Debug.LogWarning("User Closed Connection. Stopping WebSocketLoop.");
                    await Disconnect(); // Disconnect for PingLoop
                    return; // Stop this Loop (Cleanly)
                }
                catch (Exception e)
                {
                    Debug.LogError("Socket Crashed!");
                    Debug.LogException(e);
                    await Disconnect();
                    OnException?.Invoke(this, e); // Pass Exception to Controller
                    throw new WebSocketException("Websocket saw an Error and Closed", e); // Crash this Thread (Violently)
                }
            }
        }

        /// <summary>
        /// Pings the websocket
        /// </summary>
        /// <returns>Task to await</returns>
        protected async Task PingLoop()
        {
            while (socketClient != null && socketClient.IsConnected)
            {
                token.ThrowIfCancellationRequested();
                await CheckSocketConnection(); // Check SocketConnection
                token.ThrowIfCancellationRequested();
                using (var messageStream = new MemoryStream())
                    await socketClient.WriteMessage(new ArraySegment<byte>(new byte[] { 58, 2, 104, 98 }));
                await Task.Delay(10);
            }
        }

        /// <summary>
        /// Send an acknowlegement to the websocket
        /// </summary>
        /// <param name="id">Acknowledgment id</param>
        /// <returns>Task to await</returns>
        protected async Task SendAcknowledgement(ulong id)
        {
            token.ThrowIfCancellationRequested();
            if (socketClient == null || !socketClient.IsConnected)
                return; // Socket invalid (closed?)
            await CheckSocketConnection(); // Check SocketConnection
            token.ThrowIfCancellationRequested();
            using (var messageStream = new MemoryStream())
            {
                Serializer.Serialize(messageStream, new WebcastWebsocketAck
                {
                    Type = "ack",
                    Id = id
                });
                await socketClient.WriteMessage(new ArraySegment<byte>(messageStream.ToArray()));
            }
        }
        #endregion

        #region Exceptions
        /// <summary>
        /// Calls OnException-Event in Base-Class
        /// </summary>
        /// <param name="exception">Exception for Event</param>
        protected void CallOnException(Exception exception)
        {
            OnException?.Invoke(this, exception);
        }
        #endregion

        /// <summary>
        /// Checks whether a LogMessage should be printed
        /// </summary>
        /// <param name="msgLevel">LogLevel for Message</param>
        /// <returns>True if Message should be printed to Console</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ShouldLog(LogLevel msgLevel) => settings.PrintToConsole && settings.LogLevel.HasFlag(msgLevel);

        /// <summary>
        /// Handles Response received from TikTokServer
        /// </summary>
        /// <param name="webcastResponse">The current webcast response</param>
        protected abstract void HandleWebcastMessages(WebcastResponse webcastResponse);

        /// <summary>
        /// Checks if SocketConnection was Aborted, and reconnects if this was the case.
        /// </summary>
        /// <returns>Task to await</returns>
        protected virtual async Task CheckSocketConnection()
        {
            if (socketClient == null)
                return;
            if (socketClient.State == WebSocketState.Aborted)
            {
                if (ShouldLog(LogLevel.Information))
                    Debug.LogWarning("Reconnecting SocketClient");
                // Disconnect existing Client
                await socketClient.Disconnect();
                // Reconnect with new SocketClient
                socketClient = new TikTokWebSocket(TikTokHttpRequest.CookieJar, token, settings.SocketBufferSize);
                await socketClient.Connect(connectedSocketUrl);
            }
        }
    }
}