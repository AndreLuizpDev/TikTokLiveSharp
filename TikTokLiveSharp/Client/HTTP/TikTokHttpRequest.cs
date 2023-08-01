using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TikTokLiveSharp.Client.HTTP
{
    /// <summary>
    /// HTTP-Request for TikTok-Page
    /// </summary>
    public class TikTokHttpRequest : ITikTokHttpRequest
    {
        /// <summary>
        /// Cookies for Requests
        /// </summary>
        public static TikTokCookieJar CookieJar => cookieJar;
        /// <summary>
        /// (Default) Headers for Requests
        /// </summary>
        public static HttpRequestHeaders CurrentHeaders => client?.DefaultRequestHeaders;

        /// <summary>
        /// Client used for Requests
        /// </summary>
        private static HttpClient client;
        /// <summary>
        /// Handler for Request-Data
        /// </summary>
        private static HttpClientHandler handler;
        /// <summary>
        /// Cookies for Requests
        /// </summary>
        private static TikTokCookieJar cookieJar;

        /// <summary>
        /// Timeout for Requests
        /// </summary>
        private static TimeSpan timeout;
        /// <summary>
        /// Proxy for Requests
        /// </summary>
        private static IWebProxy webProxy;

        /// <summary>
        /// Query for this Request
        /// </summary>
        private string query;
        /// <summary>
        /// HTTP-Message for this Request
        /// </summary>
        private readonly HttpRequestMessage request;
        /// <summary>
        /// Whether this Request has been sent to the TikTok-Server
        /// </summary>
        private bool sent;

        /// <summary>
        /// Creates a TikTok http request instance.
        /// </summary>
        /// <param name="url">The url to send to.</param>
        /// <exception cref="ArgumentException">Throws exception if URL is invalid.</exception>
        public TikTokHttpRequest(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result)) 
                throw new ArgumentException("Invalid Url", nameof(url));
            if (cookieJar == null)
                cookieJar = new TikTokCookieJar();
            if (handler == null)
                handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    Proxy = WebProxy,
                    UseProxy = WebProxy != null,
                    UseCookies = false
                };
            if (client == null)
            {
                client = new HttpClient(handler)
                {
                    Timeout = Timeout
                };
                foreach (var header in Constants.DEFAULT_REQUEST_HEADERS)
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            request = new HttpRequestMessage
            {
                RequestUri = result
            };
            sent = false;
        }

        /// <summary>
        /// The timeout value used.
        /// </summary>
        public static TimeSpan Timeout
        {
            get => timeout;
            set
            {
                if (timeout != value)
                {
                    if (client != null) 
                        throw new Exception("Timeout cannot be set after client has been initalised.");
                    timeout = value;
                }
            }
        }

        /// <summary>
        /// The web proxy used.
        /// </summary>
        public static IWebProxy WebProxy
        {
            get => webProxy;
            set
            {
                if (webProxy != value)
                {
                    if (client != null) 
                        throw new Exception("Web proxy cannot be set after client has been initalised.");
                    webProxy = value;
                }
            }
        }

        /// <summary>
        /// Sends an async get request.
        /// </summary>
        /// <returns>HttpContent returned.</returns>
        /// <exception cref="Exception">Requests should not be reused.</exception>
        public async Task<HttpContent> Get()
        {
            if (sent) 
                throw new InvalidOperationException("Requests should not be reused");
            request.Method = HttpMethod.Get;
            return await GetContent();
        }

        /// <summary>
        /// Sends an async post request.
        /// </summary>
        /// <param name="data">The data to be sent.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Requests should not be reused.</exception>
        public async Task<HttpContent> Post(HttpContent data)
        {
            if (sent) 
                throw new InvalidOperationException("Requests should not be reused");
            request.Method = HttpMethod.Post;
            request.Content = data;
            return await GetContent();
        }

        /// <summary>
        /// Sets the queries for the request.
        /// </summary>
        /// <param name="queries">The queries to append to the URL.</param>
        /// <returns>Request with queries added.</returns>
        public ITikTokHttpRequest SetQueries(IDictionary<string, object> queries)
        {
            if (queries == null) 
                return this;
            query = string.Join("&", queries.Select(x => $"{x.Key}={WebUtility.UrlEncode(x.Value.ToString())}"));
            return this;
        }

        /// <summary>
        /// Sends the request and returns the response.
        /// </summary>
        /// <returns>The value in the response.</returns>
        /// <exception cref="HttpRequestException">If the request was unsuccessful</exception>
        private async Task<HttpContent> GetContent()
        {
            if (query != null)
                request.RequestUri = new Uri($"{request.RequestUri.AbsoluteUri}?{query}");
            var response = await client.SendAsync(request);
            request.Dispose();
            sent = true;
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new HttpRequestException($"Request responded with 404 NOT_FOUND");
            if (!response.IsSuccessStatusCode) 
                throw new HttpRequestException($"Request was unsuccessful [{(int)response.StatusCode}]");
            var ct = response.Content.Headers?.ContentType;
            if (ct?.CharSet != null)
                ct.CharSet = ct.CharSet.Replace("\"", "");
            response.Headers.TryGetValues("Set-Cookie", out var vals);
            if (vals != null)
                foreach (string val in vals)
                {
                    string[] cookie = val.Split(';')[0].Split('=');
                    cookieJar[cookie[0]] = cookie[1];
                }
            return response.Content;
        }
    }
}