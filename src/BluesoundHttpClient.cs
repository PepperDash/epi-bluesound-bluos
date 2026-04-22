using System;
using System.IO;
using System.Net;
using System.Text;

namespace PepperDash.Essentials.Plugin
{
    /// <summary>
    /// Encapsulates HTTP communication with the BluOS device on port 11000.
    /// </summary>
    public class BluesoundHttpClient
    {
        private readonly string baseUrl;
        private Action<string> logWarning;

        public BluesoundHttpClient(string address, int port = 11000)
        {
            if (string.IsNullOrEmpty(address)) throw new ArgumentNullException(nameof(address));
            baseUrl = string.Format("http://{0}:{1}", address, port);
        }

        /// <summary>
        /// Set a logging callback for HTTP warnings
        /// </summary>
        public void SetLogger(Action<string> warn)
        {
            logWarning = warn;
        }

        /// <summary>
        /// Issues a GET request to the BluOS device
        /// </summary>
        /// <param name="path">HTTP path (e.g., "/Status", "/Play")</param>
        /// <param name="query">Optional query string (pass without leading ?)</param>
        /// <param name="timeoutMs">HTTP request timeout in milliseconds (default 5000)</param>
        /// <returns>Response body as string, or null on failure</returns>
        public string SendHttpGet(string path, string query = null, int timeoutMs = 5000)
        {
            try
            {
                var url = string.IsNullOrEmpty(query)
                    ? string.Format("{0}{1}", baseUrl, path)
                    : string.Format("{0}{1}?{2}", baseUrl, path, query);

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = timeoutMs;
                request.Accept = "application/xml";

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                logWarning?.Invoke(string.Format("HTTP GET {0} failed: {1}", path, ex.Message));
                return null;
            }
        }

        /// <summary>
        /// Returns the full URL for a potentially relative path
        /// </summary>
        public string ResolveUrl(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            return path.StartsWith("/") ? baseUrl + path : path;
        }
    }
}
