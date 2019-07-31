namespace enigma
{
    #region Usings
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NLog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    #endregion

    #region Session
    /// <summary>
    /// Class for Enigma Sessions
    /// </summary>
    public class Session
    {
        #region Logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        #endregion

        #region Variables / Properties
        internal ConcurrentDictionary<int, WeakReference<GeneratedAPI>> GeneratedApiObjects = new ConcurrentDictionary<int, WeakReference<GeneratedAPI>>();
        private ConcurrentDictionary<int, TaskCompletionSource<JToken>> OpenRequests = new ConcurrentDictionary<int, TaskCompletionSource<JToken>>();
        private ConcurrentQueue<SendRequest> OpenSendRequest = new ConcurrentQueue<SendRequest>();

        private WebSocket socket = null;

        private EnigmaConfigurations config;

        private int requestID = 0;

        internal class SendRequest
        {
            public ArraySegment<byte> message;
            public int id;
            public CancellationToken ct;
            public TaskCompletionSource<JToken> tcs;
        }

        public string LastOpenError { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for the Session Class
        /// </summary>
        /// <param name="config">The enigma configuration</param>
        public Session(EnigmaConfigurations config)
        {
            this.config = config;
        }
        #endregion

        #region RPCMethodCalled Event
        /// <summary>
        /// Eventhandler to handle RPC Method calls from the server.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">RPC Request Message</param>
        public delegate void RPCMethodCalledHandler(object sender, JsonRpcRequestMessage e);

        /// <summary>
        /// Occures if the server sends a RPC Method request.
        /// </summary>
        public event RPCMethodCalledHandler RPCMethodCalled;
        #endregion

        #region Close Helpers
        private void CloseOpenRequests()
        {
            lock (OpenRequests)
            {
                foreach (var item in OpenRequests.Keys)
                {
                    try
                    {
                        if (OpenRequests.TryRemove(item, out var value))
                        {
                            value?.SetCanceled();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                }
            }
        }

        private void ClearGeneratedApiObjects()
        {
            lock (GeneratedApiObjects)
            {
                foreach (var item in GeneratedApiObjects.Keys)
                {
                    try
                    {
                        if (GeneratedApiObjects.TryRemove(item, out var value))
                        {
                            GeneratedAPI target = null;
                            if (value?.TryGetTarget(out target) ?? false)
                                _ = Task.Run(() =>
                                {
                                    try
                                    {
                                        target?.OnClosed();
                                    }
                                    catch { }
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                }
            }
        }

        private void ClearSendRequests()
        {
            lock (OpenSendRequest)
            {
                while (!OpenSendRequest.IsEmpty) OpenSendRequest.TryDequeue(out var _);
            }
        }
        #endregion

        #region Public Methods
        #region OpenAsync
        /// <summary>
        /// Establishes the websocket against the configured URL.
        /// Try to get the QIX global interface when the connection has been established.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> OpenAsync(CancellationToken? ctn = null)
        {
            if (socket != null)
                await CloseAsync(ctn);
            LastOpenError = null;
            CancellationToken ct = ctn ?? CancellationToken.None;

            socket = await config.CreateSocketCall(ct).ConfigureAwait(false);

            // Todo add here the global Cancelation Token that is
            // triggered from the CloseAsync
            bool? connected = null;

            void RPCMethodCall(object sender, JsonRpcRequestMessage e)
            {
                logger.Trace($"RPCMethodCall - {e.Method}:{e.Parameters}");
                if (e.Method == "OnAuthenticationInformation" && (bool?)e.Parameters["mustAuthenticate"] == true)
                    connected = false;
                if (e.Method == "OnConnected")
                    connected = true;
                if (!connected.HasValue)
                {
                    string message = "";
                    try
                    {
                        message = (string)e.Parameters["message"];
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                    string severity = "";
                    try
                    {
                        severity = (string)e.Parameters["severity"];
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                    if (!string.IsNullOrEmpty(severity) && severity == "fatal")
                    {
                        try
                        {
                            LastOpenError = e.Method + "\n" + message;
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                            LastOpenError = "Communication Error";
                        }
                        connected = false;
                    }
                }
            }

            RPCMethodCalled += RPCMethodCall;
            StartReceiveLoop(ct);
            while (connected == null && !ct.IsCancellationRequested)
            {
                Thread.Sleep(10);
            }
            RPCMethodCalled -= RPCMethodCall;

            if (connected == false)
            {
                socket?.CloseAsync(WebSocketCloseStatus.InternalServerError, "", ct);
                throw new Exception("Connection Error");
            }

            // start SendLoop only if connection is opened
            StartSendLoop(ct);
            var global = new GeneratedAPI(new ObjectResult() { QHandle = -1, QType = "Global" }, this);
            GeneratedApiObjects.TryAdd(-1, new WeakReference<GeneratedAPI>(global));
            return global;
        }
        #endregion

        #region CloseAsync
        /// <summary>
        /// Closes the websocket and cleans up internal caches, also triggers the closed event on all generated APIs.
        /// Eventually resolved when the websocket has been closed.
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync(CancellationToken? ct = null)
        {
            if (socket == null)
                return;

            await socket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "", ct ?? CancellationToken.None);

            try
            {
                ClearGeneratedApiObjects();

                CloseOpenRequests();

                ClearSendRequests();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

            try
            {
                socket?.Dispose();
                socket = null;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
        #endregion

        /// <summary>
        /// Suspends the enigma.js session by closing the websocket and rejecting all method calls until it has been resumed again.
        /// </summary>
        /// <returns></returns>
        public async Task SuspendAsync()
        {
            await Task.Run(() => { throw new NotSupportedException(); });
        }

        /// <summary>
        /// Resume a previously suspended enigma.js session by re-creating the websocket and, if possible, re-open the document as well as refreshing the internal caches. If successful, changed events will be triggered on all generated APIs, and on the ones it was unable to restore, the closed event will be triggered.
        ///
        /// Eventually resolved when the websocket (and potentially the previously opened document, and generated APIs) has been restored, rejected when it fails any of those steps, or when onlyIfAttached is true and a new QIX Engine session was created.
        /// </summary>
        /// <param name="onlyIfAttached">onlyIfAttached can be used to only allow resuming if the QIX Engine session was reattached properly.</param>
        /// <returns></returns>
        public async Task ResumedAsync(bool onlyIfAttached = false)
        {
            await Task.Run(() => { throw new NotSupportedException(); });
        }
        #endregion

        #region SendAsync
        internal Task<JToken> SendAsync(JsonRpcRequestMessage request, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<JToken>();
            try
            {
                request.Id = Interlocked.Increment(ref requestID);

                string json = "";
                try
                {
                    json = JsonConvert.SerializeObject(request,
                           Newtonsoft.Json.Formatting.None,
                           new JsonSerializerSettings
                           {
                               NullValueHandling = NullValueHandling.Ignore,
                           });
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
                logger.Trace("Send Request " + json);
                var bt = Encoding.UTF8.GetBytes(json);
                ArraySegment<byte> nm = new ArraySegment<byte>(bt);
                OpenRequests.TryAdd(request.Id, tcs);
                OpenSendRequest.Enqueue(new SendRequest() { ct = ct, tcs = tcs, message = nm, id = request.Id });
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }
        #endregion

        #region SendLoop
        private void StartSendLoop(CancellationToken cancellationToken)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested && socket != null && socket?.State == WebSocketState.Open)
                    {
                        while (!OpenSendRequest.IsEmpty)
                        {
                            if (OpenSendRequest.TryDequeue(out SendRequest sr))
                            {
                                socket.SendAsync(sr.message, WebSocketMessageType.Text, true, sr.ct)
                                      .ContinueWith(
                                           result =>
                                           {
                                               if (result.IsFaulted || result.IsCanceled)
                                                   OpenRequests.TryRemove(sr.id, out var _);
                                               if (result.IsFaulted)
                                                   sr.tcs.SetException(result.Exception);
                                               if (result.IsCanceled)
                                                   sr.tcs.SetCanceled();
                                           })
#if NET452
                                  .Wait(sr.ct)
#endif
                                       ;
                            }
                        }

                        Thread.Sleep(10);
                    }

                    CloseOpenRequests();

                    ClearSendRequests();
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            });
        }
        #endregion

        #region ReceiveLoop
        private void StartReceiveLoop(CancellationToken cancellationToken)
        {
            _ = Task.Run(async () =>
            {
                byte[] buffer = new byte[4096 * 8];

                #region Helper to Notify API Objects
                void notifyGeneratedAPI(WeakReference<GeneratedAPI> wrGeneratedAPI, bool close)
                {
                    Console.WriteLine("notifyGeneratedAPI");
                    GeneratedAPI generatedAPI = null;
                    wrGeneratedAPI?.TryGetTarget(out generatedAPI);
                    if (generatedAPI != null)
                    {
                        Console.WriteLine("notifyGeneratedAPI - genAPI");
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                Console.WriteLine("notifyGeneratedAPI - RUN");
                                if (close)
                                    generatedAPI?.OnClosed();
                                else
                                    generatedAPI?.OnChanged();
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex);
                            }
                        }).ConfigureAwait(false);
                    }
                }
                #endregion

                try
                {
                    while (!cancellationToken.IsCancellationRequested && socket != null && socket.State == WebSocketState.Open)
                    {
                        var writeSegment = new ArraySegment<byte>(buffer);
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await socket.ReceiveAsync(writeSegment, cancellationToken).ConfigureAwait(false);
                            writeSegment = new ArraySegment<byte>(buffer, writeSegment.Offset + result.Count, writeSegment.Count - result.Count);

                            // check buffer overflow
                            if (!result.EndOfMessage && writeSegment.Count == 0)
                            {
                                // autoIncreaseRecieveBuffer)
                                Array.Resize(ref buffer, buffer.Length * 2);
                                writeSegment = new ArraySegment<byte>(buffer, writeSegment.Offset, buffer.Length - writeSegment.Offset);
                            }
                        } while (!result.EndOfMessage);

                        var message = Encoding.UTF8.GetString(buffer, 0, writeSegment.Offset);
                        logger.Trace("Reponse" + message);
                        try
                        {
                            var responseMessage = JsonConvert.DeserializeObject<JsonRpcGeneratedAPIResponseMessage>(message);

                            if (responseMessage != null &&
                                    (responseMessage.Result != null
                                   || responseMessage.Error != null
                                   || responseMessage.Change?.Count > 0
                                   || responseMessage.Closed?.Count > 0
                                   ))
                            {
                                if (responseMessage.Id != null)
                                {
                                    OpenRequests.TryRemove(responseMessage.Id.Value, out var tcs);
                                    if (responseMessage.Error != null)
                                    {
                                        tcs?.SetException(new Exception(responseMessage.Error?.ToString()));
                                    }
                                    else
                                        tcs?.SetResult(responseMessage.Result);
                                }

                                #region Notify Changed or Closed API Objects
                                if (responseMessage.Change != null)
                                {
                                    foreach (var item in responseMessage.Change)
                                    {
                                        logger.Trace($"Object Id: {item} changed.");
                                        GeneratedApiObjects.TryGetValue(item, out var wkValues);
                                        notifyGeneratedAPI(wkValues, false);
                                    }
                                }

                                if (responseMessage.Closed != null)
                                {
                                    foreach (var item in responseMessage.Closed)
                                    {
                                        logger.Trace($"Object Id: {item} closed.");
                                        GeneratedApiObjects.TryRemove(item, out var wkValues);
                                        notifyGeneratedAPI(wkValues, true);
                                    }
                                }
                                #endregion
                            }
                            else
                            {
                                var requestMessage = JsonConvert.DeserializeObject<JsonRpcRequestMessage>(message);
                                if (requestMessage != null)
                                    _ = Task.Run(() =>
                                     {
                                         try
                                         {
                                             this.RPCMethodCalled?.Invoke(this, requestMessage);
                                         }
                                         catch (Exception ex)
                                         {
                                             logger.Error(ex);
                                         }
                                     });
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                        }
                    }

                    ClearGeneratedApiObjects();
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            });
        }
        #endregion
    }
    #endregion
}
