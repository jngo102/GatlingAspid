using HutongGames.PlayMaker.Actions;
using Modding;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Vasi;
using UObject = UnityEngine.Object;

namespace GatlingAspid
{
    public class GatlingAspid : Mod, IGlobalSettings<GlobalSettings>
    {
        public static Dictionary<string, AudioClip> AudioClips = new();
        public static Dictionary<string, GameObject> GameObjects = new();

        private GlobalSettings _globalSettings = new();

        public GlobalSettings GlobalSettings => _globalSettings;

        internal static GatlingAspid Instance;

        public override string GetVersion() => "1.0.0.1";

        public GatlingAspid() : base("Gatling Aspid") { }

        public override List<(string, string)> GetPreloadNames()
        {
            List<(string, string)> preloads = new();

            if (_globalSettings.Crystals)
            {
                preloads.Add(("Mines_07", "Crystal Flyer"));
            }

            if (_globalSettings.Grenades)
            {
                preloads.Add(("Fungus3_02", "Jellyfish"));
            }

            return preloads;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            if (_globalSettings.Crystals)
            {
                var flyer = preloadedObjects["Mines_07"]["Crystal Flyer"].LocateMyFSM("Crystal Flyer");
                var crystal = flyer.GetAction<SpawnObjectFromGlobalPool>("Fire").gameObject.Value;
                GameObjects.Add("Crystal", crystal);
            }
            
            if (_globalSettings.Grenades)
            {
                var jellyDeath = preloadedObjects["Fungus3_02"]["Jellyfish"].GetComponent<EnemyDeathEffects>();
                var corpse = ReflectionHelper.GetField<EnemyDeathEffects, GameObject>(jellyDeath, "corpsePrefab");
                PlayMakerFSM corpseFSM = corpse.LocateMyFSM("corpse");
                GameObject jelly = corpseFSM.GetAction<CreateObject>("Explode", 3).gameObject.Value;
                GameObjects.Add("Jelly", jelly);
            }

            Instance = this;

            LoadAssets();

            ModHooks.LanguageGetHook += OnLanguageGet;
            On.PlayMakerFSM.Start += OnPFSMStart;
        }

        public void OnLoadGlobal(GlobalSettings globalSettings)
        {
            _globalSettings = globalSettings;
        }

        public GlobalSettings OnSaveGlobal()
        {
            return _globalSettings;
        }

        private void LoadAssets()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.Contains("gatlingaspid")) continue;
                
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) continue;
                    
                    var bundle = AssetBundle.LoadFromStream(stream);
                    AudioClips["Firing"] = bundle.LoadAsset<AudioClip>("Firing");
                    AudioClips["Grenade"] = bundle.LoadAsset<AudioClip>("Grenade");
                    GameObjects["Anim"] = bundle.LoadAsset<GameObject>("GatlingAnim");
                    GameObjects["Cln"] = bundle.LoadAsset<GameObject>("GatlingCln");
                    
                    stream.Dispose();
                }
            }

        }
        private string OnLanguageGet(string key, string sheetTitle, string orig)
        {
            switch (key)
            {
                case "NAME_SUPER_SPITTER":
                    return "Gatling Aspid";
                case "DESC_SUPER_SPITTER":
                    return "Ancient, well-armed form of the Aspid. Once thought extinct, they have reappeared at the edges of the world.";
                case "NOTE_SUPER_SPITTER":
                    return "holy shit what is that";
                default:
                    return orig;
            }
        }

        private void OnPFSMStart(On.PlayMakerFSM.orig_Start orig, PlayMakerFSM self)
        {
            if (self.FsmName == "spitter" && self.gameObject.name.Contains("Super Spitter"))
            {
                self.gameObject.AddComponent<Aspid>();
            }
        }
    }
}