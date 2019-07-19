﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using Clio.Utilities;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Enums;
using ff14bot.Helpers;
using ICSharpCode.SharpZipLib.Zip;
using System.Text;
using ff14bot.Managers;
using Newtonsoft.Json;
using TreeSharp;
using Action = TreeSharp.Action;

namespace MagitekLoader
{
    public class CombatRoutineLoader : CombatRoutine
    {
        private const string ProjectName = "Magitek";
        private const int ProjectId = 1;
        private const string ProjectMainType = "Magitek.Magitek";
        private const string ProjectAssemblyName = "Magitek.dll";
        private static readonly Color LogColor = Colors.CornflowerBlue;
        public override bool WantButton => true;

        private static readonly object Locker = new object();
        private static readonly string ProjectAssembly = Path.Combine(Environment.CurrentDirectory, $@"Routines\{ProjectName}\{ProjectAssemblyName}");
        private static readonly string GreyMagicAssembly = Path.Combine(Environment.CurrentDirectory, @"GreyMagic.dll");
        private static readonly string VersionPath = Path.Combine(Environment.CurrentDirectory, $@"Routines\{ProjectName}\version.txt");
        private static readonly string BaseDir = Path.Combine(Environment.CurrentDirectory, $@"Routines\{ProjectName}");
        private static readonly string ProjectTypeFolder = Path.Combine(Environment.CurrentDirectory, @"Routines");
        private static volatile bool _updaterStarted, _updaterFinished, _loaded;
        public sealed override CapabilityFlags SupportedCapabilities => CapabilityFlags.All;

        public override float PullRange => 25;

        public override ClassJobType[] Class
        {
            get
            {
                switch (Core.Me.CurrentJob)
                {
                    case ClassJobType.Arcanist:
                    case ClassJobType.Scholar:
                    case ClassJobType.Summoner:
                    case ClassJobType.Archer:
                    case ClassJobType.Bard:
                    case ClassJobType.Thaumaturge:
                    case ClassJobType.BlackMage:
                    case ClassJobType.Conjurer:
                    case ClassJobType.WhiteMage:
                    case ClassJobType.Lancer:
                    case ClassJobType.Dragoon:
                    case ClassJobType.Gladiator:
                    case ClassJobType.Paladin:
                    case ClassJobType.Pugilist:
                    case ClassJobType.Monk:
                    case ClassJobType.Marauder:
                    case ClassJobType.Warrior:
                    case ClassJobType.Rogue:
                    case ClassJobType.Ninja:
                    case ClassJobType.Astrologian:
                    case ClassJobType.Machinist:
                    case ClassJobType.DarkKnight:
                    case ClassJobType.RedMage:
                    case ClassJobType.Samurai:
                    case ClassJobType.Dancer:
                    case ClassJobType.Gunbreaker:
                        return new[] { Core.Me.CurrentJob };
                    default:
                        return new[] { ClassJobType.Adventurer };
                }
            }
        }

        public CombatRoutineLoader()
        {
            if (_updaterStarted) { return; }

            _updaterStarted = true;
            Task.Factory.StartNew(AutoUpdate);
        }

        private static object Product { get; set; }

        private static PropertyInfo CombatProp { get; set; }

        private static PropertyInfo HealProp { get; set; }

        private static PropertyInfo PullProp { get; set; }

        private static PropertyInfo PreCombatProp { get; set; }

        private static PropertyInfo CombatBuffProp { get; set; }

        private static PropertyInfo PullBuffProp { get; set; }

        private static PropertyInfo RestProp { get; set; }

        private static MethodInfo ShutdownFunc { get; set; }

        private static MethodInfo PulseFunc { get; set; }

        private static MethodInfo ButtonFunc { get; set; }

        private static MethodInfo InitFunc { get; set; }

        public override string Name => ProjectName;

        public override void Initialize()
        {
            if (!_loaded && Product == null && _updaterFinished) { LoadProduct(); }
            if (Product != null) { InitFunc.Invoke(Product, null); }
        }

        public override void OnButtonPress()
        {
            if (!_loaded && Product == null && _updaterFinished) { LoadProduct(); }
            if (Product != null) { ButtonFunc.Invoke(Product, null); }
        }

        public override void Pulse()
        {
            if (!_loaded && Product == null && _updaterFinished) { LoadProduct(); }
            if (Product != null) { PulseFunc.Invoke(Product, null); }
        }

        public override void ShutDown()
        {
            if (!_loaded && Product == null && _updaterFinished) { LoadProduct(); }
            if (Product != null) { ShutdownFunc.Invoke(Product, null); }
        }

        public override Composite CombatBehavior
        {
            get
            {
                if (!_loaded && Product == null && _updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)CombatProp?.GetValue(Product, null); }
                return new Action();
            }
        }

        public override Composite HealBehavior
        {
            get
            {
                if (!_loaded && Product == null && _updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)HealProp?.GetValue(Product, null); }
                return new Action();
            }
        }

        public override Composite PullBehavior
        {
            get
            {
                if (!_loaded && Product == null && _updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)PullProp?.GetValue(Product, null); }
                return new Action();
            }
        }

        public override Composite PreCombatBuffBehavior
        {
            get
            {
                if (!_loaded && Product == null && _updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)PreCombatProp?.GetValue(Product, null); }
                return new Action();
            }
        }

        public override Composite CombatBuffBehavior
        {
            get
            {
                if (!_loaded && Product == null && _updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)CombatBuffProp?.GetValue(Product, null); }
                return new Action();
            }

        }

        public override Composite PullBuffBehavior
        {
            get
            {
                if (!_loaded && Product == null && _updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)PullBuffProp?.GetValue(Product, null); }
                return new Action();
            }
        }

        public override Composite RestBehavior
        {
            get
            {
                if (!_loaded && Product == null && _updaterFinished) { LoadProduct(); }
                if (Product != null) { return (Composite)RestProp?.GetValue(Product, null); }
                return new Action();
            }
        }

        public static void RedirectAssembly()
        {
            ResolveEventHandler handler = (sender, args) =>
            {
                string name = Assembly.GetEntryAssembly().GetName().Name;
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != name ? null : Assembly.GetEntryAssembly();
            };

            AppDomain.CurrentDomain.AssemblyResolve += handler;

            ResolveEventHandler greyMagicHandler = (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != "GreyMagic" ? null : Assembly.LoadFrom(GreyMagicAssembly);
            };

            AppDomain.CurrentDomain.AssemblyResolve += greyMagicHandler;
        }

        private static string CompiledAssembliesPath => Path.Combine(Utilities.AssemblyDirectory, "CompiledAssemblies");

        private static Assembly LoadAssembly(string path)
        {
            if (!File.Exists(path)) { return null; }
            if (!Directory.Exists(CompiledAssembliesPath))
            {
                Directory.CreateDirectory(CompiledAssembliesPath);
            }

            var t = DateTime.Now.Ticks;
            var name = $"{Path.GetFileNameWithoutExtension(path)}{t}.{Path.GetExtension(path)}";
            var pdbPath = path.Replace(Path.GetExtension(path), "pdb");
            var pdb = $"{Path.GetFileNameWithoutExtension(path)}{t}.pdb";
            var capath = Path.Combine(CompiledAssembliesPath, name);
            if (File.Exists(capath))
            {
                try
                {
                    File.Delete(capath);
                }
                catch (Exception)
                {
                    //
                }
            }
            if (File.Exists(pdb))
            {
                try
                {
                    File.Delete(pdb);
                }
                catch (Exception)
                {
                    //
                }
            }

            if (!File.Exists(capath))
            {
                File.Copy(path, capath);
            }

            if (!File.Exists(pdb) && File.Exists(pdbPath))
            {
                File.Copy(pdbPath, pdb);
            }


            Assembly assembly = null;
            try { assembly = Assembly.LoadFrom(capath); }
            catch (Exception e) { Logging.WriteException(e); }

            return assembly;
        }

        private static object Load()
        {
            RedirectAssembly();

            var assembly = LoadAssembly(ProjectAssembly);
            if (assembly == null) { return null; }

            Type baseType;
            try { baseType = assembly.GetType(ProjectMainType); }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }

            object bb;
            try { bb = Activator.CreateInstance(baseType); }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }

            if (bb != null) { Log(ProjectName + " was loaded successfully."); }
            else { Log("Could not load " + ProjectName + ". This can be due to a new version of Rebornbuddy being released. An update should be ready soon."); }

            return bb;
        }

        private static void LoadProduct()
        {
            lock (Locker)
            {
                if (Product != null) { return; }
                Product = Load();
                _loaded = true;
                if (Product == null) { return; }

                CombatProp = Product.GetType().GetProperty("CombatBehavior");
                HealProp = Product.GetType().GetProperty("HealBehavior");
                PullProp = Product.GetType().GetProperty("PullBehavior");
                PreCombatProp = Product.GetType().GetProperty("PreCombatBuffBehavior");
                PullBuffProp = Product.GetType().GetProperty("PullBuffBehavior");
                CombatBuffProp = Product.GetType().GetProperty("CombatBuffBehavior");
                RestProp = Product.GetType().GetProperty("RestBehavior");
                PulseFunc = Product.GetType().GetMethod("Pulse");
                InitFunc = Product.GetType().GetMethod("Initialize");
                ShutdownFunc = Product.GetType().GetMethod("Shutdown");
                ButtonFunc = Product.GetType().GetMethod("OnButtonPress");
            }
        }

        private static void Log(string message)
        {
            message = "[Auto-Updater][" + ProjectName + "] " + message;
            Logging.Write(LogColor, message);
        }

        private static string GetLocalVersion()
        {
            if (!File.Exists(VersionPath)) { return null; }
            try
            {
                var version = File.ReadAllText(VersionPath);
                return version;
            }
            catch { return null; }
        }

        private static void AutoUpdate()
        {
            var stopwatch = Stopwatch.StartNew();
            var local = GetLocalVersion();

            var message = new VersionMessage { LocalVersion = local, ProductId = ProjectId };
            var responseMessage = GetLatestVersion(message).Result;

            var latest = responseMessage.LatestVersion;

            if (local == latest || latest == null)
            {
                _updaterFinished = true;
                LoadProduct();
                return;
            }

            Log($"Updating to version {latest}.");
            var bytes = responseMessage.Data;
            if (bytes == null || bytes.Length == 0) { return; }

            if (!Clean(BaseDir))
            {
                Log("Could not clean directory for update.");
                _updaterFinished = true;
                return;
            }

            Log("Extracting new files.");
            if (!Extract(bytes, ProjectTypeFolder))
            {
                Log("Could not extract new files.");
                _updaterFinished = true;
                return;
            }

            if (File.Exists(VersionPath)) { File.Delete(VersionPath); }
            try { File.WriteAllText(VersionPath, latest); }
            catch (Exception e) { Log(e.ToString()); }

            stopwatch.Stop();
            Log($"Update complete in {stopwatch.ElapsedMilliseconds} ms.");
            _updaterFinished = true;
            LoadProduct();
        }

        private static bool Clean(string directory)
        {
            foreach (var file in new DirectoryInfo(directory).GetFiles())
            {
                try { file.Delete(); }
                catch { return false; }
            }

            foreach (var dir in new DirectoryInfo(directory).GetDirectories())
            {
                try { dir.Delete(true); }
                catch { return false; }
            }

            return true;
        }

        private static bool Extract(byte[] files, string directory)
        {
            using (var stream = new MemoryStream(files))
            {
                var zip = new FastZip();

                try { zip.ExtractZip(stream, directory, FastZip.Overwrite.Always, null, null, null, false, true); }
                catch (Exception e)
                {
                    Log(e.ToString());
                    return false;
                }
            }

            return true;
        }

        private static async Task<VersionMessage> GetLatestVersion(VersionMessage message)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://auth.magitek.io");

                var json = JsonConvert.SerializeObject(message);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync("/products/version", content);
                }
                catch (Exception e)
                {
                    Log(e.Message);
                    return null;
                }

                var contents = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<VersionMessage>(contents);
                return responseObject;
            }
        }

        private class VersionMessage
        {
            public int ProductId { get; set; }
            public string LocalVersion { get; set; }
            public string LatestVersion { get; set; }
            public byte[] Data { get; set; } = new byte[0];
        }
    }
}