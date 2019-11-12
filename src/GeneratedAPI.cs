namespace enigma
{
    #region Usings
    using ImpromptuInterface;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    #endregion

    #region ObjectResult
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    internal class ObjectResult
    {
        [JsonProperty(Required = Required.Always)]
        public string QType { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int QHandle { get; set; }

        public string QGenericType { get; set; } = "";
        public string QGenericId { get; set; } = "";
    }
    #endregion

    #region GeneratedAPIResult
    /// <summary>
    /// Class to Handle the ondemand the result of methods calls of the GeneratedAPI Class
    /// </summary>
    public class GeneratedAPIResult : DynamicObject
    {
        #region Variables & Properties
        private Task<JToken> input = null;

        private Session session = null;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor of the GeneratedAPIResult
        /// </summary>
        /// <param name="input">The result of the engine method call as Task of a JSON Object</param>
        /// <param name="session">The enigma session</param>
        public GeneratedAPIResult(Task<JToken> input, Session session)
        {
            this.input = input;
            this.session = session;
        }
        #endregion

        #region Dynamic Methods
#pragma warning disable 1591 // no XML Comment Warning for override
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            Type gArgs = binder.ReturnType.GetGenericArguments().FirstOrDefault();
            bool tryDynamicResult = false;
            if (gArgs == typeof(object))
            {
                gArgs = typeof(JObject);
                tryDynamicResult = true;
            }
            gArgs = gArgs ?? typeof(object);
            if (!gArgs.IsAssignableFrom(typeof(JToken)))
            {
                var tcs = new TaskCompletionSource(gArgs);

                input.ContinueWith((message) =>
                {
                    if (message.IsCanceled)
                    {
                        tcs.SetCanceled();
                        return;
                    }
                    if (message.IsFaulted)
                    {
                        tcs.SetException(message.Exception);
                        return;
                    }

                    var qReturn = message?.Result?.SelectToken("qReturn");
                    if (qReturn != null && qReturn.Type == JTokenType.Object && qReturn["qHandle"] != null)
                    {
                        if (qReturn["qHandle"].Type == JTokenType.Null)
                            tcs.SetResult(null);
                        else
                        {
                            try
                            {
                                var objectResult = qReturn.ToObject<ObjectResult>();
                                var newObj = new GeneratedAPI(objectResult, session, gArgs);
                                session.GeneratedApiObjects.TryAdd(objectResult.QHandle, new WeakReference<GeneratedAPI>(newObj));
                                tcs.SetResult(newObj.ProxyClass);
                            }
                            catch (Exception ex)
                            {
                                tcs.SetException(ex);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            object newRes = null;
                            JToken resultToken = null;

                            var results = message.Result.Children().ToList();
                            if (results.Count == 1)
                            {
                                resultToken = results.FirstOrDefault().First();
                            }
                            else
                            {
                                resultToken = message.Result;
                            }

                            if (gArgs.IsAssignableFrom(typeof(JObject)) && resultToken as JObject != null)
                            {
                                if (tryDynamicResult)
                                {
                                    dynamic dt = resultToken as JObject;
                                    var tcs2 = new TaskCompletionSource<dynamic>();
                                    tcs2.SetResult(resultToken);
                                }
                                else
                                    tcs.SetResult(resultToken);
                                return;
                            }

                            if (gArgs != typeof(object))
                                newRes = resultToken.ToObject(gArgs);
                            else
                                newRes = resultToken;

                            tcs.SetResult(newRes);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    }
                }
                );

                result = tcs.Task;
                return true;
            }

            result = input;
            return true;
        }
#pragma warning restore 1591

        #endregion
    }
    #endregion

    #region GeneratedAPI
    /// <summary>
    /// Generated API Object on the Engine side, tracked by qHandle
    /// </summary>
    public class GeneratedAPI : DynamicObject
    {
        #region Variables & Properties
#pragma warning disable IDE1006
        /// <summary>
        /// qGenericId of the engine object
        /// </summary>
        public string qGenericId { get; set; }

        /// <summary>
        /// qGenericType of the engine object
        /// </summary>
        public string qGenericType { get; set; }

        /// <summary>
        /// qType of the engine object for exampe (Doc, GenericObject, GenericBookmark,...)
        /// </summary>
        public string qType { get; set; }

        /// <summary>
        /// qHandle of the engine object
        /// </summary>
        public int qHandle { get; set; }
#pragma warning restore IDE1006

        /// <summary>
        /// The current enigma Session for this Generated API Object
        /// </summary>
        public Session Session { get; private set; }

        /// <summary>
        /// The possible ProxyClass
        /// </summary>
        public object ProxyClass { get; private set; }
        #endregion

        #region Events
        /// <summary>
        /// Changed Event that is called if the engines notify the change of the Generated Object
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Closed Event that is called if the engines notify the close of the Generated Object
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Trigger the Changed Event for this  Generated Object
        /// </summary>
        public void OnChanged()
        {
            Changed?.Invoke(ProxyClass ?? this, new EventArgs());
        }

        /// <summary>
        /// Trigger the Closed Event for this  Generated Object
        /// </summary>
        public void OnClosed()
        {
            Closed?.Invoke(ProxyClass ?? this, new EventArgs());
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Construct GeneratedAPI Class
        /// </summary>
        /// <param name="objectResult">The properties for this Generated API Object</param>
        /// <param name="session">The current enigma Session for this Generated API Object</param>
        /// <param name="proxyType">Optional a proxyType</param>
        internal GeneratedAPI(ObjectResult objectResult, Session session, Type proxyType = null)
        {
            this.qGenericId = objectResult.QGenericId;
            this.qType = objectResult.QType;
            this.qGenericId = objectResult.QGenericType;
            this.qHandle = objectResult.QHandle;
            this.Session = session;

            if (proxyType != null)
            {
                this.ProxyClass = Impromptu.DynamicActLike(this, proxyType);
            }
            else
                this.ProxyClass = null;
        }
        #endregion

        #region Dynamic Methods
#pragma warning disable 1591 // no XML Comment Warning for override
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (Session == null)
            {
                result = null;
                return true;
            }

            var argList = args.ToList();
            var ctsArg = argList.OfType<CancellationToken>().ToList();
            bool hasCancellationtoken = ctsArg.Count != 0;
            CancellationToken cts = ctsArg.SingleOrDefault();

            JToken jToken = null;

            if (args.Length == 1 || (args.Length == 2 && (ctsArg.Count == 1 || args[1] == null)))
            {
                // special case one real argument beside CancellationToken
                // check for string or JToken
                if (args[0] is JToken innerJToken)
                {
                    jToken = innerJToken;
                }
                if (args[0] is string innerString)
                {
                    try
                    {
                        jToken = JToken.Parse(innerString);
                        if (jToken.Type != JTokenType.Object && jToken.Type != JTokenType.Array)
                            jToken = null;
                    }
                    catch
                    {
                    }
                }
            }

            if (jToken == null)
            {
                var jArray = new JArray();
                foreach (var item in args)
                {
                    if (item is CancellationToken)
                        continue;

                    if (item == null)
                        break;

                    var oo = JsonConvert.SerializeObject(item,
                          Newtonsoft.Json.Formatting.None,
                          new JsonSerializerSettings
                          {
                              NullValueHandling = NullValueHandling.Ignore,
                          });
                    jArray.Add(JToken.Parse(oo));
                }
                jToken = jArray;
            }

            // ToDo: enhance parametercheck for enigma Parametermode with loaded schema file
            var request = new JsonRpcGeneratedAPIRequestMessage
            {
                Handle = this.qHandle,
                Method = binder.Name
            };
            if (request.Method.EndsWith("Async"))
                request.Method = request.Method.Substring(0, request.Method.Length - 5);

            if (Char.IsLower(request.Method[0]))
            {
                request.Method = char.ToUpper(request.Method[0]) + request.Method.Substring(1);
            }
            request.Parameters = jToken ?? JToken.Parse("{}");
            result = new GeneratedAPIResult(Session?.SendAsync(request, cts, hasCancellationtoken, $"{request?.Method ?? "missing Method"}  - {request?.Parameters?.ToString() ?? "no parameters"}"), Session);

            return true;
        }
#pragma warning restore  1591
        #endregion
    }
    #endregion
}
