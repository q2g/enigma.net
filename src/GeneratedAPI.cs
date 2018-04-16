﻿namespace enigma
{    
    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    #endregion

    public class GeneratedAPIResult : DynamicObject
    {
        private Task<JToken> input = null;

        private Session session = null;

        public GeneratedAPIResult(Task<JToken> input, Session session)
        {
            this.input = input;
            this.session = session;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            var gArgs = binder.ReturnType.GetGenericArguments();

          //  if (typeof(IGeneratedAPI).IsAssignableFrom(gArgs[0]))
            if (! typeof(JToken).IsAssignableFrom(gArgs[0]))
            {
                Type gTCS = typeof(TaskCompletionSource<>).MakeGenericType(gArgs);
                object tcsO = Activator.CreateInstance(gTCS);

                var SetResult = gTCS.GetMethod("SetResult");

                input.ContinueWith((message) => {
                    var qReturn = message?.Result?.SelectToken("qReturn");
                    if (qReturn != null && qReturn.Type == JTokenType.Object && qReturn["qHandle"] != null)
                    {
                        int handle = int.Parse(qReturn["qHandle"].ToString());
                        Console.WriteLine($"new OBJECT handle: {handle}");
                        var newObj = new GeneratedAPI(qReturn["qGenericId"].ToString(), "", "", session, handle);
                        IGeneratedAPI ia = ImpromptuInterface.Impromptu.ActLike(newObj, gArgs);                   
                        session.GeneratedApiObjects.TryAdd(handle, new WeakReference<IGeneratedAPI>(ia));
                        //tcs.SetResult(ia);
                        SetResult.Invoke(tcsO, new object[] { ia });
                    }
                    else
                    {
                        try
                        {
                            object newRes = message?.Result?.SelectToken("qVersion").ToObject(gArgs[0]);
                            SetResult.Invoke(tcsO, new object[] { newRes });
                        }
                        catch(Exception ex)
                        {

                        }
                    }
                }
                );

                var getTask = gTCS.GetProperty("Task");
                result = getTask.GetValue(tcsO);
                return true;
            }

            result = input;
            return true;

        }

    }

    public interface IGeneratedAPI
    {
        string Id { get; }

        string Type { get; }

        string GenericType { get; }

        //public Session Session;

        int Handle { get; }

        event EventHandler Changed;
        event EventHandler Closed;

        void OnChanged();
    }

    #region GeneratedAPI
    public class GeneratedAPI : DynamicObject, IGeneratedAPI
    {
        #region Variables & Properties
        public string Id { get; private set; }

        public string Type { get; private set; }

        public string GenericType { get; private set; }

        public Session Session { get; private set; }

        public int Handle { get; private set; }
        #endregion

        #region Events
        public event EventHandler Changed;

        public event EventHandler Closed;

        public void OnChanged()
        {
            Changed?.Invoke(this, new EventArgs());
        }

        public void OnClosed()
        {
            Closed?.Invoke(this, new EventArgs());
        }
        #endregion

        #region Constructor
        public GeneratedAPI(string Id, string Type, string GenericType, Session Session, int Handle)
        {
            // ToDo: check if all Parameters are okay?
            this.Id = Id;
            this.Type = Type;
            this.GenericType = GenericType;
            this.Session = Session;
            this.Handle = Handle;
        }
        #endregion

        #region Dynamic Methods
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            return base.TryConvert(binder, out result);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (Session == null)
            {
                result = null;
                return true;
            }

            CancellationToken cts = CancellationToken.None;
            JToken jToken = null;

            // ToDo: enhance parametercheck for enigma Parametermode with loaded schema file
            foreach (var item in args)
            {
                if (item is CancellationToken innerCTS)
                {
                    if (cts != CancellationToken.None)
                        throw new Exception();

                    cts = innerCTS;
                }

                if (item is JToken innerJToken)
                {
                    if (jToken != null)
                        throw new Exception();

                    jToken = innerJToken;
                }

                if (item is string JTokenString)
                {
                    // ToDo: if parameter mode is working Check for valid JToken and if not valid check if parameters mode function fits                    
                    if (jToken != null)
                        throw new Exception();

                    jToken = JToken.Parse(JTokenString);
                }
            }

            var request = new JsonRpcGeneratedAPIRequestMessage();         
            request.Handle = this.Handle;            
            request.Method = binder.Name;
            if (request.Method.EndsWith("Async"))
                request.Method=request.Method.Substring(0, request.Method.Length - 5);

            //if (Char.IsLower(request.Method[0]))
            //    request.Method[0] = char.ToUpper(request.Method[0]);
            request.Parameters = jToken ?? JToken.Parse("{}");
            result = new GeneratedAPIResult(Session?.SendAsync(request, cts), Session);
            return true;
        }
        #endregion
    } 
    #endregion
}
