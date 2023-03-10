﻿using Newtonsoft.Json.Linq;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TikTokLiveSharp.Client.Proxy;
using TikTokLiveSharp.Client.Requests;
using TikTokLiveSharp.Client.Sockets;
using TikTokLiveSharp.Debugging;
using TikTokLiveSharp.Errors.Connections;
using TikTokLiveSharp.Errors.FetchErrors;
using TikTokLiveSharp.Errors.Messaging;
using TikTokLiveSharp.Models;
using TikTokLiveSharp.Models.Protobuf;
using TikTokLiveSharp.Networking;
using TikTokLiveSharp.Utils;
using UnityEditor.TextCore.Text;

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

        public Dictionary<int, TikTokGift> AvailableGifts => availableGifts;

        public bool Connected => socketClient?.IsConnected ?? false;

        public string RoomID => roomID;

        public JObject RoomInfo => roomInfo;

        public string UniqueID => hostName;

        public uint? ViewerCount => viewerCount;



        protected Dictionary<string, object> clientParams;

        protected ClientSettings Settings;
        private string hostName;

        private CancellationToken token;
        private TikTokHTTPClient httpClient;
        protected TikTokWebSocket socketClient;
        protected bool isConnecting;
        protected bool isPolling;

        private Task runningTask, pollingTask; 
        
        private Dictionary<int, TikTokGift> availableGifts;
        private string roomID;
        private JObject roomInfo;
        protected uint? viewerCount;
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
            hostName = hostId;
            if (!settings.HasValue)
                settings = Constants.DEFAULT_SETTINGS;
            Settings = settings.Value;
            CheckSettings();
            roomInfo = null;
            availableGifts = new Dictionary<int, TikTokGift>();
            roomID = null;
            viewerCount = null;
            isConnecting = false;
            this.clientParams = new Dictionary<string, object>();
            foreach (var parameter in Constants.DEFAULT_CLIENT_PARAMS)
                this.clientParams.Add(parameter.Key, parameter.Value);
            if (clientParams != null)
                foreach (var param in clientParams)
                    this.clientParams[param.Key] = param.Value;
            this.clientParams["app_language"] = Settings.ClientLanguage;
            this.clientParams["webcast_language"] = Settings.ClientLanguage;

            httpClient = new TikTokHTTPClient(Settings.Timeout, Settings.Proxy);
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
            bool checkForUnparsedData = true
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
            ClientSettings s = Settings;
            if (Settings.Timeout == default)
                s.Timeout = Constants.DEFAULT_SETTINGS.Timeout;
            if (Settings.PollingInterval == default)
                s.PollingInterval = Constants.DEFAULT_SETTINGS.PollingInterval;
            if (string.IsNullOrEmpty(Settings.ClientLanguage))
                s.ClientLanguage = Constants.DEFAULT_SETTINGS.ClientLanguage;
            if (Settings.SocketBufferSize < 500_000)
                s.SocketBufferSize = Constants.DEFAULT_SETTINGS.SocketBufferSize;
            Settings = s;
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
                return await Connect(onConnectException);
            }
            catch (OperationCanceledException) // cancelled by User
            {
                if (Settings.PrintToConsole)
                    Debug.LogWarning("Cancelled by User");
                return null;
            }
            catch (AConnectionException e)
            {
                if (e is FailedConnectionException)
                {
                    // Failed to Connect, but Host was Online
                    if (retryConnection)
                    {
                        await Task.Delay(Settings.PollingInterval);
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
                onConnectException?.Invoke(e); // Failed to fetch critical Info for Connection
                throw e;
            }
            catch (Exception e) // Other type of Exception
            {
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
            if (isConnecting)
                throw new AlreadyConnectingException();
            if (Connected)
                throw new AlreadyConnectedException();
            isConnecting = true;
            if (Settings.PrintToConsole)
                Debug.Log("Fetching RoomID");
            await FetchRoomId();
            token.ThrowIfCancellationRequested();
            if (Settings.PrintToConsole)
                Debug.Log("Fetch RoomInfo");
            JObject info = await FetchRoomInfo();
            JToken status = info["data"]["status"];
            if (status == null || status.Value<int>() == 4)
                throw new LiveNotFoundException("LiveStream for HostID could not be found. Is the Host online?");
            token.ThrowIfCancellationRequested();
            if (Settings.DownloadGiftInfo)
            {
                if (Settings.PrintToConsole)
                    Debug.Log("Fetch Gifts");
                try
                {
                    await FetchAvailableGifts();
                }
                catch (FailedFetchGiftsException e)
                {
                    if (Settings.PrintToConsole)
                        Debug.LogException(e);
                    onConnectException?.Invoke(e);
                    // Continue connecting (not a critical error)
                }
            }
            token.ThrowIfCancellationRequested();
            if (Settings.PrintToConsole)
                Debug.Log("Fetch ClientData");
            WebcastResponse response = await FetchClientData();
            token.ThrowIfCancellationRequested();
            if (Settings.PrintToConsole)
                Debug.Log("Open Socket");
            await CreateWebSocket(response);
            token.ThrowIfCancellationRequested();
            return roomID;
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
                    if (clientParams.ContainsKey(param.Name))
                        clientParams[param.Name] = param.Value;
                    else clientParams.Add(param.Name, param.Value);
                }
                string url = $"{webcastResponse.SocketUrl}?{string.Join("&", clientParams.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value.ToString())}"))}";
                socketClient = new TikTokWebSocket(TikTokHttpRequest.CookieJar, Settings.SocketBufferSize);
                await socketClient.Connect(url);
                runningTask = Task.Run(WebSocketLoop, token);
                pollingTask = Task.Run(PingLoop, token);
            }
            catch (Exception e)
            {
                throw new FailedConnectionException("Failed to connect to the websocket", e);
            }
            if (Settings.HandleExistingMessagesOnConnect)
            {
                try
                {
                    HandleWebcastMessages(webcastResponse);
                }
                catch (Exception e)
                {
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
                await Disconnect();
        }

        /// <summary>
        /// Disconnects 
        /// </summary>
        /// <returns>Task to await</returns>
        protected virtual async Task Disconnect()
        {
            isPolling = false;
            roomInfo = null;
            isConnecting = false;
            if (Connected)
                await socketClient.Disconnect();
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
                html = await httpClient.GetLivestreamPage(hostName);
            }
            catch (Exception e)
            {
                throw new FailedFetchRoomInfoException("Failed to fetch room id from WebCast, see stacktrace for more info.", e);
            }
            var first = Regex.Match(html, "room_id=([0-9]*)");
            var second = Regex.Match(html, "\"roomId\":\"([0 - 9] *)\"");
            string id = first.Groups[1]?.Value ?? second.Groups[1]?.Value ?? string.Empty;
            if (!string.IsNullOrEmpty(id))
            {
                clientParams["room_id"] = id;
                roomID = id;
                return id;
            }
            else
                throw new FailedFetchRoomInfoException(html.Contains("\"og:url\"") ? "User might be offline" : "Your IP or country might be blocked by TikTok.");
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
                    availableGifts[gift.id] = gift;
                }
                return availableGifts;
            }
            catch (Exception e)
            {
                throw new FailedFetchGiftsException("Failed to fetch giftTokens from WebCast, see stacktrace for more info.", e);
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
                roomInfo = response;
                return response;
            }
            catch (Exception e)
            {
                throw new FailedFetchRoomInfoException("Failed to fetch room info from WebCast, see stacktrace for more info.", e);
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
            while (!token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
                TikTokWebSocketResponse response = await socketClient.ReceiveMessage();
                if (response == null) 
                    continue;
                try
                {
                    token.ThrowIfCancellationRequested();
                    using (var websocketMessageStream = new MemoryStream(response.Array, 0, response.Count))
                    {
                        token.ThrowIfCancellationRequested();
                        WebcastWebsocketMessage websocketMessage = Serializer.Deserialize<WebcastWebsocketMessage>(websocketMessageStream);
                        if (websocketMessage.Binary != null)
                        {
                            using (var messageStream = new MemoryStream(websocketMessage.Binary))
                            {
                                token.ThrowIfCancellationRequested();
                                WebcastResponse message = Serializer.Deserialize<WebcastResponse>(messageStream);
                                token.ThrowIfCancellationRequested();
                                await SendAcknowledgement(websocketMessage.Id);
                                token.ThrowIfCancellationRequested();
                                HandleWebcastMessages(message);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (Settings.PrintToConsole)
                        Debug.LogWarning("User Closed Connection. Stopping WebSocketLoop."); 
                    socketClient?.Disconnect(); // Disconnect for PingLoop
                    return; // Stop this Loop (Cleanly)
                }
                catch (Exception e)
                {
                    Debug.LogError("Socket Crashed!");
                    Debug.LogException(e);
                    OnException?.Invoke(this, e); // Pass Exception to Controller
                    socketClient?.Disconnect();
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
            while (socketClient.IsConnected)
            {
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
        /// Handles Response received from TikTokServer
        /// </summary>
        /// <param name="webcastResponse">The current webcast response</param>
        protected abstract void HandleWebcastMessages(WebcastResponse webcastResponse);
    }
}