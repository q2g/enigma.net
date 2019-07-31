namespace enigma
{
    #region Usings
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    #endregion

    #region JsonRpcRequestMessage
    /// <summary>
    /// A JsonRpc RequestMessage
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class JsonRpcRequestMessage
    {
        #region Constructor
        internal JsonRpcRequestMessage()
        {
            this.JsonRpcVersion = "2.0";
        }
        #endregion

        #region Properties
        /// <summary>
        /// The Rpc Version of the Call
        /// </summary>
        [JsonProperty("jsonrpc")]
        public string JsonRpcVersion { get; private set; }

        /// <summary>
        /// The name of the called Method
        /// </summary>
        [JsonProperty("method")]
        public string Method { get; set; }

        /// <summary>
        /// The params of the called Method
        /// </summary>
        [JsonProperty("params")]
        public JToken Parameters { get; set; }

        /// <summary>
        /// The id of the call to return an possible response with the same id.
        /// </summary>
        [JsonProperty("id", Order = 1)]
        public int Id { get; set; }
        #endregion
    }
    #endregion

    #region JsonRpcGeneratedAPIRequestMessage
    [JsonObject(MemberSerialization.OptIn)]
    internal class JsonRpcGeneratedAPIRequestMessage : JsonRpcRequestMessage
    {
        #region Properties
        [JsonProperty("handle")]
        public int Handle { get; set; }

        [DefaultValue(false)]
        [JsonProperty("delta", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Delta { get; set; }
        #endregion
    }
    #endregion

    #region JsonRpcResponseMessage
    [JsonObject(MemberSerialization.OptIn)]
    internal class JsonRpcResponseMessage
    {
        #region Properties
        [JsonProperty("jsonrpc")]
        [DefaultValue("2.0")]
        public string JsonRpcVersion { get; set; }

        [JsonProperty("result")]
        public JToken Result { get; private set; }

        [JsonProperty("error")]
        public JToken Error { get; set; }

        [JsonProperty("id")]
        public int? Id { get; private set; }
        #endregion
    }
    #endregion

    #region JsonRpcGeneratedAPIResponseMessage
    [JsonObject(MemberSerialization.OptIn)]
    internal class JsonRpcGeneratedAPIResponseMessage : JsonRpcResponseMessage
    {
        #region Properties
        [JsonProperty("change")]
        public List<int> Change { get; set; }

        [JsonProperty("closed")]
        public List<int> Closed { get; set; }
        #endregion
    }
    #endregion
}
