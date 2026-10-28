using HarmonyLib;
using NewHorizons;
using OWML.Common;
using OWML.ModHelper;
using System.Collections;
using System.Reflection;
using UnityEngine;
using static LoadManager;

namespace TSTAButGood
{
    public class TSTAButGood : ModBehaviour
    {
        public static TSTAButGood Instance;
        public INewHorizons NewHorizons;

        private static Text loadingText; //mm loading

        public void Awake()
        {
            Instance = this;
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
        }

        public void Start()
        {
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine("Made TSTA good! 16", MessageType.Success);

            new Harmony("Hamstry.TSTAButGood").PatchAll(Assembly.GetExecutingAssembly());

            // Get the New Horizons API and load configs
            NewHorizons = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
            NewHorizons.LoadConfigs(this);
        }
    }

    [HarmonyPatch]
    public class LoadStopper : MonoBehaviour
    {
        private static Text FindLoadingText()
        {
            foreach (SubmitActionLoadScene action in Object.FindObjectsOfType<SubmitActionLoadScene>())
            {
                Traverse submit = Traverse.Create(action);
                if (submit.Field("_receivedSubmitAction").GetValue<bool>())
                {
                    return submit.Field("_loadingText").GetValue<Text>();
                }
            }
        
            return null;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(LoadManager), (nameof(LoadManager.FinalizeAsyncSceneLoad)))]
        public static bool DontFinalizeAsyncLoading(LoadManager __instance)
        {
            TSTAButGood.Instance.ModHelper.Console.WriteLine("Unfinalizing the asyncloading?");
            s_previousScene = s_currentScene;
            s_currentScene = s_loadingScene;
            s_loadingScene = OWScene.None;
            StreamingManager.ForcePhysicsBakeJobsComplete();
            if (!s_skipVsyncChange && QualitySettings.vSyncCount > 0 && s_lastVSyncCount < 0)
            {
                s_lastVSyncCount = QualitySettings.vSyncCount;
                QualitySettings.vSyncCount = 0;
            }
            SpinnerUI.Show();
            TSTAButGood.Instance.StartCoroutine(LoadingLogs());
            return false;
        }

        static IEnumerator LoadingLogs()
        {
            float delay = 0f;
            int loadedAssets = 0;
            int assetsToLoad = 128;
            while (true)
            {
                yield return new WaitForSecondsRealtime(delay);
                loadedAssets += (int)Mathf.Round(delay*UnityEngine.Random.Range(0.5f, 10f));
                if (loadedAssets > assetsToLoad)
                {
                    assetsToLoad = loadedAssets;
                    assetsToLoad += (int)Mathf.Round(UnityEngine.Random.Range(10f, 50f));
                }
                else
                {
                    if (UnityEngine.Random.Range(1f, 5f) <= 1f)
                    {
                        assetsToLoad += (int)Mathf.Round(UnityEngine.Random.Range(10f, 50f));
                    }
                }
                if (loadingText == null)
                {
                    loadingText = FindLoadingText();
                }
                
                if (loadingText != null)
                {
                    loadingText.text = $"Loading assets... [{loadedAssets}/{assetsToLoad}]";
                }
                TSTAButGood.Instance.ModHelper.Console.WriteLine($"Loading assets... [{loadedAssets}/{assetsToLoad}]");
                delay = UnityEngine.Random.Range(0.5f, 5f);
            }
        }
    }

}
