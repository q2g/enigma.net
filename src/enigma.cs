namespace enigma
{
    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    #endregion

    #region Protocoll
    /// <summary>
    ///  Helper Class for Delta Protocoll
    /// </summary>
    public class Protocoll : IProtocoll
    {
        /// <summary>
        /// Set to false to disable the use of the bandwidth-reducing delta protocol.
        /// </summary>
        public bool Delta { get; set; } = false;
    }
    #endregion

    #region EnigmaConfigurations
    /// <summary>
    /// Class that stores the Configuration of the current Enigma Connections
    /// </summary>
    public class EnigmaConfigurations
    {
        #region Variables & Properties
        /// <summary>
        /// Optional
        /// NOT used, later for client side Call checks
        /// Object containing the specification for the API to generate.
        /// Corresponds to a specific version of the QIX Engine API.
        /// </summary>
        public JsonToken Schema { get; set; }

        private string url;
        /// <summary>
        /// String containing a proper websocket URL to QIX Engine.
        /// </summary>        
        public string Url
        {
            get { return url; }
            set {
                if (value != url)
                {
                    url = value;
                    if (url.ToLowerInvariant().StartsWith("http"))
                        url = "ws" + url.Substring(4);
                }
            }
        }

        /// <summary>
        /// A function to use when instantiating the WebSocket
        /// </summary>
        public Func<string, Task<WebSocket>> CreateSocket { get; set; }

        ///// <summary>
        ///// Add later to enable Promise with then, instead of Tasks
        ///// </summary>
        //object Promise;

        /// <summary>
        /// Set to true if the session should be suspended instead of closed
        /// when the websocket is closed.
        /// </summary>
        public bool SuspendOnClose { get; set; }

        /// <summary>
        /// Mixins to extend/augment the QIX Engine API.
        /// See Mixins section for more information how each entry in this array
        /// should look like. Mixins are applied in the array order.
        /// </summary>
        public List<IMixin> Mixins { get; set; }

        /// <summary>
        /// Interceptors for augmenting requests before they are sent to QIX Engine.
        /// See Interceptors section for more information how each entry in this array should look like.
        /// Interceptors are applied in the array order.
        /// </summary>
        public List<IRequestInterceptors> RequestInterceptors { get; set; }

        /// <summary>
        /// Interceptors for augmenting responses before they are passed into mixins and end-users.
        /// See Interceptors section for more information how each entry in this array should look like.
        /// Interceptors are applied in the array order.
        /// </summary>
        public List<IResponseInterceptors> ResponseInterceptors { get; set; }

        /// <summary>
        /// An object containing additional JSON-RPC request parameters.
        /// </summary>
        public IProtocoll Protocol { get; set; } = new Protocoll();
        #endregion

        internal Task<WebSocket> CreateSocketCall(CancellationToken ct)
        {
            var internalCreateSocket = CreateSocket;
            if (internalCreateSocket == null)
            {
                internalCreateSocket = async (url) =>
                {
                    var ws = new ClientWebSocket();
                    await ws.ConnectAsync(new Uri(url), ct).ConfigureAwait(false);
                    return ws;
                };
            }
            return internalCreateSocket.Invoke(this.Url);
        }
    }
    #endregion

    #region Enigma
    /// <summary>
    /// Class to handle make the code close to enigma.js
    /// </summary>
    public class Enigma
    {
        /// <summary>
        /// Static Method to generate a new Session with the Config
        /// </summary>
        /// <param name="config">Config Class for this Session</param>
        /// <returns></returns>
        public static Session Create(EnigmaConfigurations config)
        {
            return new Session(config);
        }
    }
    #endregion
}
