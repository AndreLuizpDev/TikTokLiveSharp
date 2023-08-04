using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TikTokLiveSharp.Client;
using TikTokLiveSharp.Client.Proxy;
using TikTokLiveSharp.Events;
using TikTokLiveSharp.Events.MessageData.Messages;
using TikTokLiveSharp.Events.MessageData.Objects;
using TikTokLiveUnity.Utils;
using UnityEngine;
using UnityEngine.Events;
using Debug = TikTokLiveSharp.Debugging.Debug;
using LinkMicMethod = TikTokLiveSharp.Events.MessageData.Messages.LinkMicMethod;
using Picture = TikTokLiveSharp.Events.MessageData.Objects.Picture;
using RoomMessage = TikTokLiveSharp.Events.MessageData.Messages.RoomMessage;

namespace TikTokLiveUnity
{
    /// <summary>
    /// Manager for TikTokLive-Client in Unity
    /// </summary>
    public class TikTokLiveManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>
        /// Whether this GameObject is not Root
        /// </summary>
        [SerializeField]
        [Tooltip("Enable if this GameObject is not Root")]
        private bool hasRootObject;

        /// <summary>
        /// Whether an Instance Exists (!= NULL)
        /// </summary>
        public static bool Exists => _instance != null;
        /// <summary>
        /// Singleton-Instance for TikTokLiveManager. Auto-Creates new TikTokLiveManager if none exists
        /// </summary>
        public static TikTokLiveManager Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                GameObject obj = new GameObject("TikTokLiveManager");
                _instance = obj.AddComponent<TikTokLiveManager>();
                _instance.Reset();
                if (_instance.ShouldLog(LogLevel.Warning))
                    Debug.LogWarning("TikTokLiveManager-Instance does not exist. Created new Instance", obj);
                return _instance;
            }
            protected set { _instance = value; }
        }
        /// <summary>
        /// Singleton-Instance for TikTokLiveManager
        /// </summary>
        protected static TikTokLiveManager _instance;
        #endregion

        #region Properties
        #region Public
        /// <summary>
        /// Gifts Available (for Room)
        /// </summary>
        public IReadOnlyDictionary<int, TikTokGift> AvailableGifts => (IReadOnlyDictionary<int, TikTokGift>)client?.AvailableGifts;
        /// <summary>
        /// Whether currently Connected to the TikTokLive-Servers
        /// </summary>
        public bool Connected => client?.Connected ?? false;
        /// <summary>
        /// Whether currently initializing a Connection to the TikTokLive-Servers
        /// </summary>
        public bool Connecting => client?.Connecting ?? false;
        /// <summary>
        /// Username for Host
        /// </summary>
        public string HostName => client?.HostName;
        /// <summary>
        /// RoomInfo
        /// </summary>
        public JObject RoomInfo => client?.RoomInfo;
        /// <summary>
        /// Number of Viewers in Room
        /// </summary>
        public uint? ViewerCount => client?.ViewerCount;
        #endregion

        #region Unity
        /// <summary>
        /// Max amount of Textures Cached
        /// </summary>
        [Min(1)]
        [SerializeField]
        [Header("TextureCache")]
        [Tooltip("Max amount of Textures Cached")]
        private uint texCacheSize = 2048;

        /// <summary>
        /// Whether to Connect on Start (using set HostId)
        /// </summary>
        [SerializeField]
        [Header("Auto-Connect")]
        [Tooltip("Whether to Connect on Start (using set HostId)")]
        private bool autoConnect;
        /// <summary>
        /// HostName to Connect to on Start
        /// </summary>
        [SerializeField]
        [Tooltip("HostName to Connect to on Start")]
        private string autoConnectHostId;
        /// <summary>
        /// Settings for TikTokClient
        /// </summary>
        [SerializeField]
        [Tooltip("Settings for TikTokClient")]
        private ClientSettings settings;
        #endregion

        #region Private
        /// <summary>
        /// TikTokClient used for Connection
        /// </summary>
        protected TikTokLiveClient client;
        /// <summary>
        /// Tokensource for CancelToken used with Client-Threads
        /// </summary>
        protected CancellationTokenSource tokenSource = new CancellationTokenSource();
        /// <summary>
        /// Cached Access for Textures
        /// </summary>
        private TextureCache textureCache;
        #endregion

        #region Events
        #region C#
        public event EventHandler<Exception> OnException;

        /// <summary>
        /// Event fired when the client connects.
        /// </summary>
        public event TikTokEventHandler<bool> OnConnected;

        /// <summary>
        /// Event fired when the client disconnects.
        /// </summary>
        public event TikTokEventHandler<bool> OnDisconnected;

        public event TikTokEventHandler<RoomMessage> OnRoomIntro;

        public event TikTokEventHandler<RoomMessage> OnRoomMessage;

        public event TikTokEventHandler<RoomMessage> OnSystemMessage;

        public event TikTokEventHandler<RoomVerifyMessage> OnRoomVerifyMessage; 

        /// <summary>
        /// Event fired when comments are received.
        /// </summary>
        public event TikTokEventHandler<Comment> OnComment;


        /// <summary>
        /// Event fired when the view count updates.
        /// </summary>
        public event TikTokEventHandler<RoomViewerData> OnViewerData;

        /// <summary>
        /// Event fired when the live has ended.
        /// </summary>
        public event TikTokEventHandler OnLiveEnded;
        public event TikTokEventHandler OnLivePaused;


        /// <summary>
        /// Event fired when a gift-Message is received.
        /// <para>
        /// Fires for every GiftMessage
        /// </para>
        /// </summary>
        public event TikTokEventHandler<GiftMessage> OnGiftMessage;
        /// <summary>
        /// Event fired when a gift-Message is received.
        /// <para>
        /// Fires when a gift-streak starts. Gift-Events are fired for subsequent messages.
        /// </para>
        /// </summary>
        public event TikTokEventHandler<TikTokGift> OnGift;
        /// <summary>
        /// Event fired when someone likes the stream.
        /// </summary>
        public event TikTokEventHandler<Like> OnLike;

        /// <summary>
        /// Event fired when the stream is shared.
        /// </summary>
        public event TikTokEventHandler<Share> OnShare;

        /// <summary>
        /// Event fired when someone follows.
        /// </summary>
        public event TikTokEventHandler<Follow> OnFollow;

        /// <summary>
        /// Event fired when someone joins the stream.
        /// </summary>
        public event TikTokEventHandler<Join> OnJoin;

        /// <summary>
        /// Event fired when a user subscribes.
        /// </summary>
        public event TikTokEventHandler<Subscribe> OnSubscribe;

        /// <summary>
        /// Event fired when a LinkMicBattle starts
        /// </summary>
        public event TikTokEventHandler<LinkMicBattle> OnLinkMicBattle;
        public event TikTokEventHandler<LinkMicBattleTaskMessage> OnLinkMicBattleTask;

        public event TikTokEventHandler<LinkMicArmies> OnLinkMicArmies;

        /// <summary>
        /// Data for LinkMic-Connection(s)
        /// </summary>
        public event TikTokEventHandler<LinkMicMethod> OnLinkMicMethod;

        public event TikTokEventHandler<LinkMicFanTicket> OnLinkMicFanTicket;

        public event TikTokEventHandler<RankText> OnRankText;
        public event TikTokEventHandler<RankUpdate> OnRankUpdate;

        /// <summary>
        /// Closed Caption-Message for Video
        /// </summary>
        public event TikTokEventHandler<Caption> OnClosedCaption;

        public event TikTokEventHandler<PollMessage> OnPollMessage;

        public event TikTokEventHandler<InRoomBanner> OnInRoomBanner;

        /// <summary>
        /// Event Fired when Host pins a Comment to the Stream
        /// </summary>
        public event TikTokEventHandler<RoomPinMessage> OnRoomPinMessage;

        public event TikTokEventHandler<DetectMessage> OnDetectMessage;

        public event TikTokEventHandler<BarrageMessage> OnBarrageMessage;

        public event TikTokEventHandler<GoalUpdate> OnGoalUpdate;

        public event TikTokEventHandler<UnauthorizedMember> OnUnauthorizedMember;

        public event TikTokEventHandler<LinkMessage> OnLinkMessage;

        public event TikTokEventHandler<LinkLayerMessage> OnLinkLayerMessage;

        public event TikTokEventHandler<GiftBroadcast> OnGiftBroadcast;

        public event TikTokEventHandler<ShopMessage> OnShopMessage;

        public event TikTokEventHandler<IMDelete> OnIMDelete;

        public event TikTokEventHandler<Question> OnQuestion;

        public event TikTokEventHandler<Envelope> OnEnvelope;

        public event TikTokEventHandler<SubNotify> OnSubNotify;

        public event TikTokEventHandler<Emote> OnEmote;
        /// <summary>
        /// Event fired when an unhandled social event is received from the webcast.
        /// It's up to you how you can interpret this message.
        /// </summary>
        public event TikTokEventHandler<TikTokLiveSharp.Models.Protobuf.Messages.WebcastSocialMessage> UnhandledSocialEvent;

        /// <summary>
        /// Event fired when an unhandled member event is received from the webcast.
        /// It's up to you how you can interpret this message.
        /// </summary>
        public event TikTokEventHandler<TikTokLiveSharp.Models.Protobuf.Messages.WebcastMemberMessage> UnhandledMemberEvent;

        /// <summary>
        /// Event fired when an unhandled event is received from the webcast.
        /// It's up to you how you can interpret this message.
        /// </summary>
        public event TikTokEventHandler<TikTokLiveSharp.Models.Protobuf.Messages.Generic.Message> UnhandledEvent;
        #endregion

        #region UnityEvents
        [SerializeField]
        private UnityEvent<Exception> onException;
        [SerializeField]
        private UnityEvent<bool> onConnected;
        [SerializeField]
        private UnityEvent<bool> onDisconnected;
        [SerializeField]
        private UnityEvent<RoomMessage> onRoomIntro;
        [SerializeField]
        private UnityEvent<RoomMessage> onRoomMessage;
        [SerializeField]
        private UnityEvent<RoomMessage> onSystemMessage;
        [SerializeField]
        private UnityEvent<RoomVerifyMessage> onRoomVerifyMessage;
        [SerializeField]
        private UnityEvent<Comment> onComment;
        [SerializeField]
        private UnityEvent<RoomViewerData> onViewerData;
        [SerializeField]
        private UnityEvent onLiveEnded;
        [SerializeField]
        private UnityEvent onLivePaused;
        [SerializeField]
        private UnityEvent<GiftMessage> onGiftMessage;
        [SerializeField]
        private UnityEvent<TikTokGift> onGift;
        [SerializeField]
        private UnityEvent<Like> onLike;
        [SerializeField]
        private UnityEvent<Share> onShare;
        [SerializeField]
        private UnityEvent<Follow> onFollow;
        [SerializeField]
        private UnityEvent<Join> onJoin;
        [SerializeField]
        private UnityEvent<Subscribe> onSubscribe;
        [SerializeField]
        private UnityEvent<LinkMicBattle> onLinkMicBattle;
        [SerializeField]
        private UnityEvent<LinkMicBattleTaskMessage> onLinkMicBattleTask;
        [SerializeField]
        private UnityEvent<LinkMicArmies> onLinkMicArmies;
        [SerializeField]
        private UnityEvent<LinkMicMethod> onLinkMicMethod;
        [SerializeField]
        private UnityEvent<LinkMicFanTicket> onLinkMicFanTicket;
        [SerializeField]
        private UnityEvent<RankText> onRankText;
        [SerializeField]
        private UnityEvent<RankUpdate> onRankUpdate;
        [SerializeField]
        private UnityEvent<Caption> onClosedCaption;
        [SerializeField]
        private UnityEvent<PollMessage> onPollMessage;
        [SerializeField]
        private UnityEvent<InRoomBanner> onInRoomBanner;
        [SerializeField]
        private UnityEvent<RoomPinMessage> onRoomPinMessage;
        [SerializeField]
        private UnityEvent<DetectMessage> onDetectMessage;
        [SerializeField]
        private UnityEvent<BarrageMessage> onBarrageMessage;
        [SerializeField]
        private UnityEvent<GoalUpdate> onGoalUpdate;
        [SerializeField]
        private UnityEvent<UnauthorizedMember> onUnauthorizedMember;
        [SerializeField]
        private UnityEvent<LinkMessage> onLinkMessage;
        [SerializeField]
        private UnityEvent<LinkLayerMessage> onLinkLayerMessage;
        [SerializeField]
        private UnityEvent<GiftBroadcast> onGiftBroadcast;
        [SerializeField]
        private UnityEvent<ShopMessage> onShopMessage;
        [SerializeField]
        private UnityEvent<IMDelete> onIMDelete;
        [SerializeField]
        private UnityEvent<Question> onQuestion;
        [SerializeField]
        private UnityEvent<Envelope> onEnvelope;
        [SerializeField]
        private UnityEvent<SubNotify> onSubNotify;
        [SerializeField]
        private UnityEvent<Emote> onEmote;
        [SerializeField]
        private UnityEvent<TikTokLiveSharp.Models.Protobuf.Messages.WebcastSocialMessage> onUnhandledSocialEvent;
        [SerializeField]
        private UnityEvent<TikTokLiveSharp.Models.Protobuf.Messages.WebcastMemberMessage> onUnhandledMemberEvent;
        [SerializeField]
        private UnityEvent<TikTokLiveSharp.Models.Protobuf.Messages.Generic.Message> onUnhandledEvent;
        #endregion
        #endregion
        #endregion

        #region Methods
        #region Static
        /// <summary>
        /// Checks if a User exists on TikTok by attempting to get their Profile-Page
        /// </summary>
        /// <param name="userId">@-ID for User</param>
        /// <param name="timeOut">TimeOut for HTTP-Connection (set NULL for default)</param>
        /// <param name="proxy">Proxy to use with HTTP-Client</param>
        /// <returns>True if User has a Profile-Page on TikTok</returns>
        public static async Task<bool> GetUserExists(string userId, TimeSpan? timeOut = null,
            RotatingProxy proxy = null) => await TikTokBaseClient.GetUserExists(userId, timeOut, proxy);

        /// <summary>
        /// Checks if a User is currently streaming by looking for a RoomId on their Live-Page
        /// </summary>
        /// <param name="userId">@-ID for User</param>
        /// <param name="timeOut">TimeOut for HTTP-Connection (set NULL for default)</param>
        /// <param name="proxy">Proxy to use with HTTP-Client</param>
        /// <returns>True if User is currently streaming on TikTok</returns>
        public static async Task<bool> GetUserStreaming(string userId, TimeSpan? timeOut = null,
            RotatingProxy proxy = null) => await TikTokBaseClient.GetUserStreaming(userId, timeOut, proxy);
        #endregion

        #region Public
        #region Connection
        /// <summary>
        /// Connects to a LiveStream. 
        /// This can be called from the Unity Main Thread.
        /// </summary>
        /// <param name="userID">UserName for Host of Room</param>
        /// <param name="onConnectException">Callback for Exception whilst Connecting</param>
        public async void ConnectToStreamAsync(string userID, Action<Exception> onConnectException = null)
        {
            await ConnectToStream(userID, onConnectException);
        }
        /// <summary>
        /// Connects to a LiveStream.
        /// Returns Task to Await
        /// </summary>
        /// <param name="userID">UserName for Host of Room</param>
        /// <param name="onConnectException">Callback for Exception whilst Connecting</param>
        /// <returns>Task to Await</returns>
        public async Task ConnectToStream(string userID, Action<Exception> onConnectException = null)
        {
            if (string.IsNullOrWhiteSpace(userID))
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogError("No UserID Provided");
                throw new ArgumentNullException(nameof(userID), "Did not provide a UserID to connect to");
            }
            try
            {
                if (client != null)
                {
                    if (ShouldLog(LogLevel.Information))
                        Debug.Log("Disconnecting Existing Client", gameObject);
                    await DisconnectFromLivestream();
                }
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log("Creating new TikTokLiveClient");
                client = new TikTokLiveClient(userID, settings, null);
                if (ShouldLog(LogLevel.Information))
                    Debug.Log($"Created new Client with HostName {userID}");
                // Set up Events for Client
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log("Connecting Events to Client");
                SetupEvents(client);
            }
            catch (Exception e)
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(e, gameObject);
                onConnectException?.Invoke(e);
                return;
            }
            try
            {
                await client.Start(tokenSource.Token, onConnectException, settings.RetryOnConnectionFailure);
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log("Connected");
            }
            catch (Exception e)
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(e, gameObject);
                onConnectException?.Invoke(e);
            }
        }
        /// <summary>
        /// Disconnects from current LiveStream. 
        /// This can be called from the Unity Main Thread.
        /// </summary>
        public async void DisconnectFromLivestreamAsync() => await DisconnectFromLivestream();
        /// <summary>
        /// Disconnects from current LiveStream. 
        /// Returns Task to Await
        /// </summary>
        /// <returns>Task to Await</returns>
        public async Task DisconnectFromLivestream()
        {
            if (client != null)
            {
                if (ShouldLog(LogLevel.Information))
                    Debug.Log("Disconnecting Client");
                await client.Stop();
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log("Removing EventListeners from Client");
                TearDownEvents(client);
                client = null;
            }
            await Task.Delay(200);
            if (ShouldLog(LogLevel.Verbose))
                Debug.Log("Stopping Threads");
            tokenSource?.Cancel();
            tokenSource = new CancellationTokenSource();
        }
        #endregion

        #region Images
        /// <summary>
        /// Gets Texture from TikTok
        /// </summary>
        /// <param name="picture">Picture-Object for Texture</param>
        /// <param name="onComplete">Callback for Texture-Retrieval</param>
        public void RequestImage(Picture picture, Action<Texture2D> onComplete) => RequestImage(picture.URLs, onComplete);
        /// <summary>
        /// Gets Texture from TikTok
        /// </summary>
        /// <param name="urls">URLs for Texture. First one with valid Extension is used</param>
        /// <param name="onComplete">Callback for Texture-Retrieval</param>
        public void RequestImage(IEnumerable<string> urls, Action<Texture2D> onComplete) => textureCache.RequestImage(urls, onComplete);
        /// <summary>
        /// Gets Texture from TikTok
        /// </summary>
        /// <param name="url">URL for Texture</param>
        /// <param name="onComplete">Callback for Texture-Retrieval</param>
        public void RequestImage(string url, Action<Texture2D> onComplete) => textureCache.RequestImage(url, onComplete);

        /// <summary>
        /// Gets Sprite from TikTok
        /// </summary>
        /// <param name="picture">Picture-Object for Sprite</param>
        /// <param name="onComplete">Callback for Sprite-Retrieval</param>
        public void RequestSprite(Picture picture, Action<Sprite> onComplete) => RequestSprite(picture.URLs, onComplete);
        /// <summary>
        /// Gets Sprite from TikTok
        /// </summary>
        /// <param name="urls">URLs for Sprite. First one with valid Extension is used</param>
        /// <param name="onComplete">Callback for Sprite-Retrieval</param>
        public void RequestSprite(IEnumerable<string> urls, Action<Sprite> onComplete) => textureCache.RequestSprite(urls, onComplete);
        /// <summary>
        /// Gets Sprite from TikTok
        /// </summary>
        /// <param name="url">URL for Sprite</param>
        /// <param name="onComplete">Callback for Sprite-Retrieval</param>
        public void RequestSprite(string url, Action<Sprite> onComplete) => textureCache.RequestSprite(url, onComplete);
        #endregion
        #endregion

        #region Unity
        /// <summary>
        /// Initializes Manager
        /// </summary>
        protected virtual void Awake()
        {
            // Destroy any duplicate Instance
            if (Exists && _instance != this)
            {
                if ((_instance.settings.PrintToConsole && _instance.settings.LogLevel.HasFlag(LogLevel.Error))
                    || settings.PrintToConsole && settings.LogLevel.HasFlag(LogLevel.Error))
                    Debug.LogError($"TikTokLiveManager already exists! Existing Object: {_instance.gameObject.name}. New Object: {gameObject.name}. Destroying new Object", _instance.gameObject);
                DestroyImmediate(gameObject, false);
                return;
            }
            #region Singleton
            if (transform.parent != null)
            {
                if (!hasRootObject)
                {
                    if (ShouldLog(LogLevel.Warning))
                        Debug.LogWarning("TikTokLiveManager is not a Root-Object. Did you mean to set hasRootObject? Unparenting", gameObject);
                    transform.SetParent(null, true);
                    DontDestroyOnLoad(gameObject);
                }
                else DontDestroyOnLoad(transform.root.gameObject);
            }
            else DontDestroyOnLoad(gameObject);
            // Set Instance
            if (ShouldLog(LogLevel.Verbose))
                Debug.Log("Setting Singleton-Instance");
            _instance = this;
            #endregion
            #region Init
            if (ShouldLog(LogLevel.Verbose))
                Debug.Log("Creating TextureCache");
            textureCache = new TextureCache(texCacheSize);
            if (ShouldLog(LogLevel.Verbose))
                Debug.Log("Linking UnityEvents to C#-Events");
            LinkUnityEvents();
            #endregion
        }
        /// <summary>
        /// Performs Auto-Connect
        /// </summary>
        protected virtual IEnumerator Start()
        {
            if (autoConnect)
            {
                yield return null;
                if (ShouldLog(LogLevel.Information))
                    Debug.Log($"Auto-Connecting to {autoConnectHostId}");
                ConnectToStreamAsync(autoConnectHostId, null);
            }
        }
        /// <summary>
        /// Sets default Settings
        /// </summary>
        protected virtual void Reset()
        {
            hasRootObject = transform.parent != null;
            texCacheSize = 2048;
            autoConnect = false;
            autoConnectHostId = string.Empty;
            settings = Constants.DEFAULT_SETTINGS;
        }
        /// <summary>
        /// DeInitializes Manager. Closes any Active Connection
        /// </summary>
        protected virtual void OnDestroy()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            DisconnectFromLivestream();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            if (Exists && ReferenceEquals(_instance, this))
            {
                #region DeInit
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log("Clearing TextureCache");
                textureCache = null;
                Resources.UnloadUnusedAssets();
                #endregion
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log("Removing Singleton-Instance");
                _instance = null;
            }
        }
        #endregion

        #region Private
        /// <summary>
        /// Links UnityEvents to C#-Events
        /// </summary>
        private void LinkUnityEvents()
        {
            OnException += (_, args) => onException?.Invoke(args);
            OnConnected += (_, args) => onConnected?.Invoke(args);
            OnDisconnected += (_, args) => onDisconnected?.Invoke(args);
            OnRoomIntro += (_, args) => onRoomIntro?.Invoke(args);
            OnRoomMessage += (_, args) => onRoomMessage?.Invoke(args);
            OnRoomVerifyMessage += (_, args) => onRoomVerifyMessage?.Invoke(args);
            OnSystemMessage += (_, args) => onSystemMessage?.Invoke(args);
            OnComment += (_, args) => onComment?.Invoke(args);
            OnViewerData += (_, args) => onViewerData?.Invoke(args);
            OnLiveEnded += (_, _) => onLiveEnded?.Invoke();
            OnLivePaused += (_, _) => onLivePaused?.Invoke();
            OnGiftMessage += (_, args) => onGiftMessage?.Invoke(args);
            OnGift += (_, args) => onGift?.Invoke(args);
            OnLike += (_, args) => onLike?.Invoke(args);
            OnShare += (_, args) => onShare?.Invoke(args);
            OnFollow += (_, args) => onFollow?.Invoke(args);
            OnJoin += (_, args) => onJoin?.Invoke(args);
            OnSubscribe += (_, args) => onSubscribe?.Invoke(args);
            OnLinkMicBattle += (_, args) => onLinkMicBattle?.Invoke(args);
            OnLinkMicBattleTask += (_, args) => onLinkMicBattleTask?.Invoke(args);
            OnLinkMicArmies += (_, args) => onLinkMicArmies?.Invoke(args);
            OnLinkMicMethod += (_, args) => onLinkMicMethod?.Invoke(args);
            OnLinkMicFanTicket += (_, args) => onLinkMicFanTicket?.Invoke(args);
            OnRankText += (_, args) => onRankText?.Invoke(args);
            OnRankUpdate += (_, args) => onRankUpdate?.Invoke(args);
            OnClosedCaption += (_, args) => onClosedCaption?.Invoke(args);
            OnPollMessage += (_, args) => onPollMessage?.Invoke(args);
            OnInRoomBanner += (_, args) => onInRoomBanner?.Invoke(args);
            OnRoomPinMessage += (_, args) => onRoomPinMessage?.Invoke(args);
            OnDetectMessage += (_, args) => onDetectMessage?.Invoke(args);
            OnBarrageMessage += (_, args) => onBarrageMessage?.Invoke(args);
            OnGoalUpdate += (_, args) => onGoalUpdate?.Invoke(args);
            OnUnauthorizedMember += (_, args) => onUnauthorizedMember?.Invoke(args);
            OnLinkMessage += (_, args) => onLinkMessage?.Invoke(args);
            OnLinkLayerMessage += (_, args) => onLinkLayerMessage?.Invoke(args);
            OnGiftBroadcast += (_, args) => onGiftBroadcast?.Invoke(args);
            OnShopMessage += (_, args) => onShopMessage?.Invoke(args);
            OnIMDelete += (_, args) => onIMDelete?.Invoke(args);
            OnQuestion += (_, args) => onQuestion?.Invoke(args);
            OnEnvelope += (_, args) => onEnvelope?.Invoke(args);
            OnSubNotify += (_, args) => onSubNotify?.Invoke(args);
            OnEmote += (_, args) => onEmote?.Invoke(args);
            UnhandledSocialEvent += (_, args) => onUnhandledSocialEvent?.Invoke(args);
            UnhandledMemberEvent += (_, args) => onUnhandledMemberEvent?.Invoke(args);
            UnhandledEvent += (_, args) => onUnhandledEvent?.Invoke(args);
        }
        /// <summary>
        /// Links Events to Client
        /// </summary>
        /// <param name="client">Client to Link to</param>
        private void SetupEvents(TikTokLiveClient client)
        {
            client.OnException += OnException.Invoke;
            client.OnConnected += OnConnected.Invoke;
            client.OnDisconnected += OnDisconnected.Invoke;
            client.OnRoomIntro += OnRoomIntro.Invoke;
            client.OnRoomMessage += OnRoomMessage.Invoke;
            client.OnRoomVerify += OnRoomVerifyMessage.Invoke;
            client.OnSystemMessage += OnSystemMessage.Invoke;
            client.OnComment += OnComment.Invoke;
            client.OnViewerData += OnViewerData.Invoke;
            client.OnLiveEnded += OnLiveEnded.Invoke;
            client.OnLivePaused += OnLivePaused.Invoke;
            client.OnGiftMessage += OnGiftMessage.Invoke;
            client.OnGift += OnGift.Invoke;
            client.OnLike += OnLike.Invoke;
            client.OnShare += OnShare.Invoke;
            client.OnFollow += OnFollow.Invoke;
            client.OnJoin += OnJoin.Invoke;
            client.OnSubscribe += OnSubscribe.Invoke;
            client.OnLinkMicBattle += OnLinkMicBattle.Invoke;
            client.OnLinkMicBattleTask += OnLinkMicBattleTask.Invoke;
            client.OnLinkMicArmies += OnLinkMicArmies.Invoke;
            client.OnLinkMicMethod += OnLinkMicMethod.Invoke;
            client.OnLinkMicFanTicket += OnLinkMicFanTicket.Invoke;
            client.OnRankText += OnRankText.Invoke;
            client.OnRankUpdate += OnRankUpdate.Invoke;
            client.OnClosedCaption += OnClosedCaption.Invoke;
            client.OnPollMessage += OnPollMessage.Invoke;
            client.OnInRoomBanner += OnInRoomBanner.Invoke;
            client.OnRoomPinMessage += OnRoomPinMessage.Invoke;
            client.OnDetectMessage += OnDetectMessage.Invoke;
            client.OnBarrageMessage += OnBarrageMessage.Invoke;
            client.OnGoalUpdate += OnGoalUpdate.Invoke;
            client.OnUnauthorizedMember += OnUnauthorizedMember.Invoke;
            client.OnLinkMessage += OnLinkMessage.Invoke;
            client.OnLinkLayerMessage += OnLinkLayerMessage.Invoke;
            client.OnGiftBroadcast += OnGiftBroadcast.Invoke;
            client.OnShopMessage += OnShopMessage.Invoke;
            client.OnIMDelete += OnIMDelete.Invoke;
            client.OnQuestion += OnQuestion.Invoke;
            client.OnEnvelope += OnEnvelope.Invoke;
            client.OnSubNotify += OnSubNotify.Invoke;
            client.OnEmote += OnEmote.Invoke;
            client.UnhandledSocialEvent += UnhandledSocialEvent.Invoke;
            client.UnhandledMemberEvent += UnhandledMemberEvent.Invoke;
            client.UnhandledEvent += UnhandledEvent.Invoke;
        }
        /// <summary>
        /// Unlinks Events from Client
        /// </summary>
        /// <param name="client">Client to Unlink from</param>
        private void TearDownEvents(TikTokLiveClient client)
        {
            client.OnException -= OnException.Invoke;
            client.OnConnected -= OnConnected.Invoke;
            client.OnDisconnected -= OnDisconnected.Invoke;
            client.OnRoomIntro -= OnRoomIntro.Invoke;
            client.OnRoomMessage -= OnRoomMessage.Invoke;
            client.OnRoomVerify -= OnRoomVerifyMessage.Invoke;
            client.OnSystemMessage -= OnSystemMessage.Invoke;
            client.OnComment -= OnComment.Invoke;
            client.OnViewerData -= OnViewerData.Invoke;
            client.OnLiveEnded -= OnLiveEnded.Invoke;
            client.OnLivePaused -= OnLivePaused.Invoke;
            client.OnGiftMessage -= OnGiftMessage.Invoke;
            client.OnGift -= OnGift.Invoke;
            client.OnLike -= OnLike.Invoke;
            client.OnShare -= OnShare.Invoke;
            client.OnFollow -= OnFollow.Invoke;
            client.OnJoin -= OnJoin.Invoke;
            client.OnSubscribe -= OnSubscribe.Invoke;
            client.OnLinkMicBattle -= OnLinkMicBattle.Invoke;
            client.OnLinkMicBattleTask -= OnLinkMicBattleTask.Invoke;
            client.OnLinkMicArmies -= OnLinkMicArmies.Invoke;
            client.OnLinkMicMethod -= OnLinkMicMethod.Invoke;
            client.OnLinkMicFanTicket -= OnLinkMicFanTicket.Invoke;
            client.OnRankText -= OnRankText.Invoke;
            client.OnRankUpdate -= OnRankUpdate.Invoke;
            client.OnClosedCaption -= OnClosedCaption.Invoke;
            client.OnPollMessage -= OnPollMessage.Invoke;
            client.OnInRoomBanner -= OnInRoomBanner.Invoke;
            client.OnRoomPinMessage -= OnRoomPinMessage.Invoke;
            client.OnDetectMessage -= OnDetectMessage.Invoke;
            client.OnBarrageMessage -= OnBarrageMessage.Invoke;
            client.OnGoalUpdate -= OnGoalUpdate.Invoke;
            client.OnUnauthorizedMember -= OnUnauthorizedMember.Invoke;
            client.OnLinkMessage -= OnLinkMessage.Invoke;
            client.OnLinkLayerMessage -= OnLinkLayerMessage.Invoke;
            client.OnGiftBroadcast -= OnGiftBroadcast.Invoke;
            client.OnShopMessage -= OnShopMessage.Invoke;
            client.OnIMDelete -= OnIMDelete.Invoke;
            client.OnQuestion -= OnQuestion.Invoke;
            client.OnEnvelope -= OnEnvelope.Invoke;
            client.OnSubNotify -= OnSubNotify.Invoke;
            client.OnEmote -= OnEmote.Invoke;
            client.UnhandledSocialEvent -= UnhandledSocialEvent.Invoke;
            client.UnhandledMemberEvent -= UnhandledMemberEvent.Invoke;
            client.UnhandledEvent -= UnhandledEvent.Invoke;
        }
        /// <summary>
        /// Checks whether a LogMessage should be printed
        /// </summary>
        /// <param name="msgLevel">LogLevel for Message</param>
        /// <returns>True if Message should be printed to Console</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ShouldLog(LogLevel msgLevel) => settings.PrintToConsole && settings.LogLevel.HasFlag(msgLevel);
        #endregion
        #endregion
    }
}
