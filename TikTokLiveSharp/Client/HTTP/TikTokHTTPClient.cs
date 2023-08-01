using Newtonsoft.Json.Linq;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TikTokLiveSharp.Client.Proxy;
using TikTokLiveSharp.Errors.Permissions;
using TikTokLiveSharp.Models.Protobuf.Messages.Generic;

namespace TikTokLiveSharp.Client.HTTP
{
    /// <summary>
    /// HTTP-Client for TikTok-WebPage & Signing-Page
    /// </summary>
    internal sealed class TikTokHTTPClient
    {
        #region Properties
        /// <summary>
        /// Number of Concurrent HTTP-Clients
        /// </summary>
        private static int concurrentClients = 0;
        /// <summary>
        /// Index for this Client
        /// </summary>
        private int clientNum;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor for a HTTPClient
        /// </summary>
        /// <param name="timeout">Timeout for Connection</param>
        /// <param name="proxyHandler">Proxy for Connection</param>
        internal TikTokHTTPClient(TimeSpan timeout, RotatingProxy proxyHandler = null)
        {
            TikTokHttpRequest.Timeout = timeout;
            TikTokHttpRequest.WebProxy = proxyHandler;
            concurrentClients++;
            clientNum = concurrentClients;
        }
        /// <summary>
        /// Destructor for a HTTPClient
        /// </summary>
        ~TikTokHTTPClient()
        {
            concurrentClients--;
        }
        #endregion

        #region Methods
        #region Internal
        /// <summary>
        /// Gets WebcastResponse from WebCast-API
        /// </summary>
        /// <param name="path">Path to send Request to</param>
        /// <param name="parameters">Additional Parameters for Request</param>
        /// <param name="signURL">Whether to sign URL using API</param>
        /// <returns>Task to Await. Result is WebcastResponse</returns>
        internal async Task<WebcastResponse> GetDeserializedMessage(string path, Dictionary<string, object> parameters, bool signURL = false)
        {
            var get = await GetRequest(Constants.TIKTOK_URL_WEBCAST + path, parameters, signURL);
            return Serializer.Deserialize<WebcastResponse>(await get.ReadAsStreamAsync());
        }
        /// <summary>
        /// Gets JSON-Object from WebCast-API
        /// </summary>
        /// <param name="path">Path to send Request to</param>
        /// <param name="parameters">Additional Parameters for Request</param>
        /// <param name="signURL">Whether to sign URL using API</param>
        /// <returns>Task to Await. Result is JSON-Object</returns>
        internal async Task<JObject> GetJObjectFromWebcastAPI(string path, Dictionary<string, object> parameters, bool signURL = false)
        {
            var get = await GetRequest(Constants.TIKTOK_URL_WEBCAST + path, parameters, signURL);
            return JObject.Parse(await get.ReadAsStringAsync());
        }
        /// <summary>
        /// Gets HTML for Live-Page
        /// </summary>
        /// <param name="uniqueID">ID for Host</param>
        /// <param name="signURL">Whether to sign URL using API</param>
        /// <returns></returns>
        internal async Task<string> GetLivestreamPage(string uniqueID, bool signURL = false)
        {
            var get = await GetRequest($"{Constants.TIKTOK_URL_WEB}@{uniqueID}/live/", signURL: signURL);
            return await get.ReadAsStringAsync();
        }
        /// <summary>
        /// Gets HTML for Live-Page
        /// </summary>
        /// <param name="uniqueID">ID for Host</param>
        /// <param name="signURL">Whether to sign URL using API</param>
        /// <returns></returns>
        internal async Task<string> GetProfilePage(string uniqueID, bool signURL = false)
        {
            var get = await GetRequest($"{Constants.TIKTOK_URL_WEB}@{uniqueID}", signURL: signURL);
            return await get.ReadAsStringAsync();
        }
        /// <summary>
        /// Posts JSON-Object to WebCast-API
        /// </summary>
        /// <param name="path">Path to send to (sub-URL)</param>
        /// <param name="parameters">Additional Parameters for Request</param>
        /// <param name="json">Object to Post</param>
        /// <param name="signURL">Whether to sign URL using API</param>
        /// <returns>Task to Await. Result is result of Post</returns>
        internal async Task<JObject> PostJObjecttToWebcastAPI(string path, Dictionary<string, object> parameters, JObject json, bool signURL = false)
        {
            var post = await PostRequest(Constants.TIKTOK_URL_WEBCAST + path, json.ToString(Newtonsoft.Json.Formatting.None), parameters, signURL);
            return JObject.Parse(await post.ReadAsStringAsync());
        }
        #endregion

        #region Private
        /// <summary>
        /// Sends HTTP-GetRequest to URL
        /// </summary>
        /// <param name="url">URL to send to</param>
        /// <param name="parameters">Additional Parameters for Request</param>
        /// <param name="signURL">Whether to Sign URL using API</param>
        /// <returns>Task to await</returns>
        private async Task<HttpContent> GetRequest(string url, Dictionary<string, object> parameters = null, bool signURL = false)
        {
            var request = BuildRequest(signURL ? await GetSignedUrl(url, parameters) : url, signURL ? null : parameters);
            return await request.Get();
        }

        /// <summary>
        /// Sends HTTP-PostRequest to URL
        /// </summary>
        /// <param name="url">URL to send to</param>
        /// <param name="data">Data for Request</param>
        /// <param name="parameters">Additional Parameters for Request</param>
        /// <param name="signURL">Whether to Sign URL using API</param>
        /// <returns>Task to await</returns>
        private async Task<HttpContent> PostRequest(string url, string data, Dictionary<string, object> parameters = null, bool signURL = false)
        {
            var request = BuildRequest(signURL ? await GetSignedUrl(url, parameters) : url, signURL ? null : parameters);
            return await request.Post(new StringContent(data, Encoding.UTF8));
        }

        /// <summary>
        /// Creates HTTP-Request
        /// </summary>
        /// <param name="url">URL for Request</param>
        /// <param name="parameters">Additional Parameters for Request</param>
        /// <returns>HTTP-Request</returns>
        private ITikTokHttpRequest BuildRequest(string url, Dictionary<string, object> parameters = null)
        {
            return new TikTokHttpRequest(url).SetQueries(parameters);
        }

        /// <summary>
        /// Signs URL using WebAPI
        /// </summary>
        /// <param name="url">URL to sign</param>
        /// <param name="parameters">Additional Parameters for Request</param>
        /// <returns>Task to await. Result is Signed URL</returns>
        /// <exception cref="InsufficientPermissionException"></exception>
        private async Task<string> GetSignedUrl(string url, Dictionary<string, object> parameters = null)
        {
            string getParams = parameters != null ? $"?{string.Join("&", parameters.Select(x => $"{x.Key}={WebUtility.UrlEncode(x.Value.ToString())}"))}" : string.Empty;
            ITikTokHttpRequest request = new TikTokHttpRequest(Constants.TIKTOK_SIGN_API)
                .SetQueries(new Dictionary<string, object>()
                {
                    { "client", "ttlive-net" },
                    { "uuc", clientNum },
                    { "url", url + getParams }
                });
            HttpContent content = await request.Get();
            try
            {
                JObject jObj = JObject.Parse(await content.ReadAsStringAsync());
                string s2 = jObj["signedUrl"].Value<string>();
                string signedUrl = jObj["signedUrl"].Value<string>();
                string userAgent = jObj["User-Agent"].Value<string>();
                TikTokHttpRequest.CurrentHeaders.Remove("User-Agent");
                TikTokHttpRequest.CurrentHeaders.Add("User-Agent", userAgent);
                return signedUrl;
            }
            catch (Exception e)
            {
                throw new InsufficientPermissionException("Insufficent values have been supplied for signing. Likely due to an update. Post an issue on GitHub.", e);
            }
        }
        #endregion
        #endregion
    }
}