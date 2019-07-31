namespace tests
{
    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using enigma;
    using ImpromptuInterface;
    using Newtonsoft.Json.Linq;
    using Qlik.EngineAPI;
    using Xunit;
    #endregion

    public class UnitTest1
    {
        IGlobal global;
        dynamic dynamicGlobal;
        IDoc doc;

        public UnitTest1()
        {
            var session = Enigma.Create(new EnigmaConfigurations()
            {
                Url = $"ws://127.0.0.1:4848/app/engineData/identity/{Guid.NewGuid()}",
                CreateSocket
            });

            // connect to the engine
            dynamicGlobal = session.OpenAsync().Result;
            global = Impromptu.ActLike<IGlobal>(dynamicGlobal);

            doc = global.CreateSessionAppAsync().Result;
        }

        [Fact]
        public async Task Global_EngineVersion()
        {
            var engineVersion = await global.EngineVersionAsync();
            string version = engineVersion.qComponentVersion;
            Assert.Equal("12", version.Substring(0, 2));
        }

        [Fact]
        public async Task DynamicGlobal_EngineVersion()
        {
            dynamic engineVersion = await (Task<JObject>)dynamicGlobal.EngineVersionAsync();
            string version = engineVersion.qComponentVersion;
            Assert.Equal("12", version.Substring(0, 2));
        }

        [Fact]
        public async Task Global_IsDesktop()
        {
            Assert.True(await global.IsDesktopModeAsync());
        }

        [Fact]
        async Task Doc_Evaluate()
        {
            Assert.Equal("42", await doc.EvaluateAsync("=40+2"));
        }

        [Fact]
        async Task Doc_BigEvaluate()
        {
            var taskList = new List<Task>();
            Parallel.For(1, 1000, index =>
            {
                var number1 = new Random().Next(0, 10000);
                var number2 = new Random().Next(0, 10000);
                var task = doc.EvaluateExAsync($"{number1}+{number2}");
                taskList.Add(task);
            });

            await Task.WhenAll(taskList.ToArray());
        }

        [Fact]
        async Task Doc_Changed()
        {
            bool changed = false;
            void Doc_Changed1(object sender, EventArgs e)
            {
                Console.WriteLine("changed");
                changed = true;
            }

            Assert.False(changed);
            doc.Changed += Doc_Changed1;
            
            await doc.SetScriptAsync(Guid.NewGuid().ToString()).ConfigureAwait(false);
            await doc.LockAllAsync("$");
            var re = await doc.EvaluateAsync("=40+2");

            await Task.Delay(5000).ConfigureAwait(false);
           // Assert.True(changed);
            //doc.Changed -= Doc_Changed1;
        }

        //[Fact]
        //public async Task ReloadTest()
        //{
        //    var script = "";

        //    await doc.EvaluateAsync()

        //    await doc.SetScriptAsync(script);
        //    await doc.DoSaveAsync();
        //    Assert.Equal(script, await doc.GetScriptAsync());
        //    await doc.DoReloadAsync();

        //    Assert.True(await global.IsDesktopModeAsync());
        //}
    }
}
