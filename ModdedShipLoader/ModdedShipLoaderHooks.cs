using BBI.Unity.Game;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ModdedShipLoader
{
    internal class ModdedShipLoaderHooks
    {
        static void Log(string message, bool detailed = false)
        {
            if((Settings.settings.debugLog && !detailed) || (Settings.settings.debugLogDetailed && detailed))
            {
                ModdedShipLoader.LoggerInstance.LogInfo(message);
            }
        }

        static string guidToSwapTo = null;

        [HarmonyPatch(typeof(LevelSelectController), "LevelAssetLoadComplete")]
        public class LevelSelectController_LevelAssetLoadComplete
        {
            [HarmonyPrefix]
            public static bool Prefix(
                ref AsyncOperationHandle<IList<LevelAsset>> levelHandle,
                LevelSelectController __instance,
                ref int ___mTotalVisibleButtons,
                ref float ___m_ButtonSpacing,
                RectTransform ___m_ButtonParent,
                UnityEngine.GameObject ___m_MissionSelectButtonPrefab,
                ref List<UnityEngine.GameObject> ___mLevelSelectButtonList
            )
            {
                switch (levelHandle.Status)
                {
                    case AsyncOperationStatus.Succeeded:
                        {
                            bool flag = false;
                            int num = 0;
                            float num2 = 0f;
                            List<LevelAsset> list = new List<LevelAsset>(levelHandle.Result);
                            list.Sort(delegate (LevelAsset a, LevelAsset b)
                            {
                                if (a == null && b == null)
                                {
                                    return 0;
                                }
                                if (a == null)
                                {
                                    return -1;
                                }
                                if (b == null)
                                {
                                    return 1;
                                }
                                return a.Data.SortOrder.CompareTo(b.Data.SortOrder);
                            });

                            // Console.WriteLine($"Adding extra ships");
                            // LevelAsset newLevel = new LevelAsset();
                            // newLevel.Data = list[0].Data;

                            // newLevel.Data.StartingShipRef = list.Last().Data.StartingShipRef; //  new AssetReferenceModuleConstructionAsset("e723eba8ae422e84190e2971c9c374f5"); // Assets/CustomOperation/FirstShip.prefab

                            // newLevel.Data.

                            /*
                            System.Reflection.FieldInfo fi = typeof(ModuleConstructionAsset.ModuleConstructionData).GetField("m_RootModuleRef", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            UnityEngine.AddressableAssets.AssetReferenceGameObject newValue;
                            newValue = new UnityEngine.AddressableAssets.AssetReferenceGameObject("e723eba8ae422e84190e2971c9c374f5");    // Assets/CustomOperation/FirstShip.prefab
                            fi.SetValue(newLevel.Data, newValue);
                            */

                            // list.Add(newLevel);

                            //Console.WriteLine($"Done adding extra ship levels");

                            ___mTotalVisibleButtons = 0;
                            foreach (LevelAsset item in list)
                            {
                                GameObject gameObject = UnityEngine.Object.Instantiate(___m_MissionSelectButtonPrefab, ___m_ButtonParent.transform, false);
                                ___mLevelSelectButtonList.Add(gameObject);
                                LevelSelectButton componentInChildren = gameObject.GetComponentInChildren<LevelSelectButton>();
                                var uiButton = gameObject.GetComponentInChildren<UnityEngine.UI.Button>();
                                RectTransform component = gameObject.GetComponent<RectTransform>();
                                if (num2 > 0f)
                                {
                                    num2 += ___m_ButtonSpacing;
                                }
                                num2 += component.rect.width;

                                var dynGetRandomPropertyOverrideForLevel = __instance.GetType().GetMethod("GetRandomPropertyOverrideForLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                AssetReferencePropertyContainerAsset randomPropertyOverrideForLevel = (AssetReferencePropertyContainerAsset)dynGetRandomPropertyOverrideForLevel.Invoke(__instance, new object[] { item });

                                // AssetReferencePropertyContainerAsset randomPropertyOverrideForLevel = __instance.GetRandomPropertyOverrideForLevel(item);

                                if (item.Data.StartingUpgrades == null)
                                {
                                    Log("No upgrades");
                                    item.Data.StartingUpgrades = Resources.FindObjectsOfTypeAll<UpgradeListAsset>().Where(asset => asset.name == "FreeMode_UpgradeListAsset").First();
                                }

                                componentInChildren.InitButton(item, randomPropertyOverrideForLevel, __instance, num++, delegate (bool shouldSelect)
                                {
                                    Main.EventSystem.Post(UIAsyncLoadingEvent.GetEvent(false, UIAsyncLoadingController.UISectionName.FREEPLAY_LEVEL_SELECT_LIST));
                                    if (___m_ButtonParent != null)
                                    {
                                        ___m_ButtonParent.gameObject.SetActive(true);
                                    }
                                    if (shouldSelect)
                                    {
                                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(uiButton.gameObject);
                                    }

                                    // If it's a custom ship, replace the name
                                    if (item.Data.LevelDescriptionFull.StartsWith("CUSTOM"))
                                    {
                                        ((LocalizedTextMeshProUGUI)(typeof(LevelSelectButton).GetField("m_ShipTypeName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)).GetValue(componentInChildren))
                                            .ChangeFieldByStringContent(item.Data.LevelDisplayName);
                                        ((LocalizedTextMeshProUGUI)(typeof(LevelSelectButton).GetField("m_ShipRoleName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)).GetValue(componentInChildren))
                                            .ChangeFieldByStringContent(item.Data.LevelDescriptionShort);
                                    }
                                });
                                uiButton.interactable = (item.Data.IsUnlocked | flag);
                                ___mTotalVisibleButtons++;
                            }
                            ___m_ButtonParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num2);
                            break;
                        }
                    case AsyncOperationStatus.Failed:
                        Main.EventSystem.Post(UIAsyncLoadingEvent.GetEvent(false, UIAsyncLoadingController.UISectionName.FREEPLAY_LEVEL_SELECT_LIST));
                        break;
                }

                return false;
            }
        }
       
        [HarmonyPatch(typeof(LevelSelectButton), "LoadLevelAsync")]
        public class LevelSelectButton_LoadLevelAsync
        {
            [HarmonyPrefix]
            public static bool Prefix(
                LevelAsset.LevelData ___mLevelToLoad
            )
            {
                // If we don't have a description, it's definitely not a custom level
                if (___mLevelToLoad.LevelDescriptionFull == null)
                    return true;

                guidToSwapTo = ___mLevelToLoad.LevelDescriptionFull.StartsWith("CUSTOM") ? ___mLevelToLoad.LevelDescriptionFull.Split(':')[1].Trim() : null;
                Log($"Loading: {___mLevelToLoad.LevelDisplayName}");
                Log($"Loading: {___mLevelToLoad.LevelDescriptionFull}");
                return true;
            }
        }

        // Swap on load

        [HarmonyPatch(typeof(ShipRandomizationHelper), "ConstructModuleGroup")]
        public class ShipRandomizationHelper_ConstructModuleGroup
        {
            [HarmonyPrefix]
            public static void Prefix(ShipPreview shipPreview, ShipSpawnParams spawnParams, AddressableCache addressableCache)
            {
                Log($"Checking if we need to swap: {guidToSwapTo}");
                
                if(guidToSwapTo != null)
                {
                    Log($"Swap started", true);

                    System.Reflection.FieldInfo fi = typeof(ModuleConstructionAsset.ModuleConstructionData).GetField("m_RootModuleRef", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    // fi.SetValue(shipPreview.ConstructionAsset.Data, new AssetReferenceGameObject(guidToSwapTo));

                    Log($"Swap done", true);
                }
            }
        }

        // Recursive loading hooks

        [HarmonyPatch(typeof(Addressables), "InstantiateAsync", new Type[] { typeof(object), typeof(Vector3), typeof(Quaternion), typeof(Transform), typeof(bool) })]
        public class Addressables_InstantiateAsync
        {
            static Dictionary<string, Shader> shaderCache = new Dictionary<string, Shader>();
            static Dictionary<string, string> shaderToReference = new Dictionary<string, string>() 
            {
                { "Fake/_Lynx/Surface/HDRP/Lit", "2ff41ba12704fae4fbdb6d3886c89479" }
            };

            static void ReplaceShaders(GameObject parent)
            {
                foreach (var renderer in parent.GetComponentsInChildren<MeshRenderer>())
                {
                    var shaderName = renderer.sharedMaterial.shader.name;
                    if (shaderToReference.ContainsKey(shaderName))
                    {
                        if(shaderCache.ContainsKey(shaderName))
                        {
                            renderer.sharedMaterial.shader = shaderCache[shaderName];
                        }
                        else
                        {
                            UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Shader>(new UnityEngine.AddressableAssets.AssetReference(shaderToReference[shaderName])).Completed += res =>
                            {
                                renderer.sharedMaterial.shader = res.Result;
                                // Async, so it might already contain it by the time we call this
                                if(!shaderCache.ContainsKey(shaderName))
                                {
                                    shaderCache.Add(shaderName, res.Result);
                                }
                            };
                        }
                    }
                }
            }

            static AsyncOperationHandle<GameObject> GameObjectReady(AsyncOperationHandle<GameObject> arg)
            {
                var result = InstantiateChildren(arg.Result);

                ReplaceShaders(result);

                return Addressables.ResourceManager.CreateCompletedOperation(result, string.Empty);
            }

            static GameObject InstantiateChildren(GameObject parent)
            {
                var addressableType = System.Reflection.Assembly.GetAssembly(typeof(BBI.Unity.Game.SecuringObjectRemovedEvent)).GetType("BBI.Unity.Game.AddressableLoader");

                foreach (var addressableGO in parent.GetComponentsInChildren(addressableType))
                // if (arg.Result.TryGetComponent(addressableType, out var loader))
                {
                    string assetGUID = (string)addressableType.GetField("assetGUID").GetValue(addressableGO);
                    string childPath = (string)addressableType.GetField("childPath").GetValue(addressableGO);
                    List<string> disabledChildren = (List<string>)addressableType.GetField("disabledChildren").GetValue(addressableGO);

                    Log($"Loading GO ref {assetGUID}", true);

                    Addressables.ResourceManager.CreateChainOperation<GameObject, GameObject>(
                        Addressables.LoadAssetAsync<GameObject>(new AssetReferenceGameObject(assetGUID)), arg =>
                        {
                            GameObject result = arg.Result;
                            if (!string.IsNullOrEmpty(childPath))
                            {
                                result = result.transform.Find(childPath)?.gameObject;
                            }

                            if (result == null)
                            {
                                Log($"ERROR: Can't find {childPath} in {assetGUID}");
                                result = arg.Result;
                            }

                            result = InstantiateChildren(GameObject.Instantiate(result, addressableGO.transform));

                            ReplaceShaders(result);

                            if (!string.IsNullOrEmpty(childPath))
                            {
                                result.transform.localPosition = Vector3.zero;
                            }

                            foreach (var child in disabledChildren)
                            {
                                var foundChild = result.transform.Find(child)?.gameObject;
                                if (foundChild != null)
                                {
                                    GameObject.Destroy(foundChild);
                                    Log($"Removing {child} from {assetGUID}", true);
                                }
                                else
                                {
                                    Log($"ERROR: Can't find {child} from {assetGUID}");
                                }
                            }

                            return Addressables.ResourceManager.CreateCompletedOperation(result, string.Empty);
                        })
                    ;
                }

                try
                {
                    var addressableSOType = System.Reflection.Assembly.GetAssembly(typeof(BBI.Unity.Game.SecuringObjectRemovedEvent)).GetType("BBI.Unity.Game.AddressableSOLoader");

                    foreach (var addressableSO in parent.GetComponentsInChildren(addressableSOType))
                    // if (arg.Result.TryGetComponent(addressableSOType, out var addressableSO))
                    {
                        LoadScriptableObjectReferences(addressableSO);
                    }

                }
                catch (Exception ex)
                {
                    Log("SO loading exception!");
                    Log(ex.Message);
                }

                return parent;
            }

            [HarmonyPostfix]
            public static void HarmonyPostfix(ref UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> __result, object key, Vector3 position, Quaternion rotation, Transform parent, bool trackHandle)
            {
                __result = Addressables.ResourceManager.CreateChainOperation<GameObject, GameObject>(__result, GameObjectReady);
            }

            public static void LoadScriptableObjectReferences(Component addressableSO)
            {
                var addressableSOType = System.Reflection.Assembly.GetAssembly(typeof(BBI.Unity.Game.SecuringObjectRemovedEvent)).GetType("BBI.Unity.Game.AddressableSOLoader");

                List<string> comps = (List<string>)addressableSOType.GetField("comp").GetValue(addressableSO);
                List<string> fields = (List<string>)addressableSOType.GetField("field").GetValue(addressableSO);
                List<string> refs = (List<string>)addressableSOType.GetField("refs").GetValue(addressableSO);

                for (int i = 0; i < refs.Count; i++)
                {
                    Log($"Loading SO ref {refs[i]}", true);

                    var comp = addressableSO.GetComponents<Component>().Where(comp => comp.GetType().ToString() == comps[i]).First();
                    System.Reflection.FieldInfo fi = comp.GetType().GetField(fields[i], System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    Addressables.LoadAssetAsync<ScriptableObject>(new AssetReferenceScriptableObject(refs[i])).Completed += res =>
                    {
                        if (res.IsValid())
                        {
                            fi.SetValue(comp, res.Result);
                        }
                    };
                }
            }
        }


        [HarmonyPatch()]
        public class Addressables_LoadAssetAsync
        {
            public static Dictionary<string, string> assetReferences = new Dictionary<string, string>();

            static System.Reflection.MethodInfo TargetMethod()
            {
                return typeof(Addressables).GetMethod("LoadAssetAsync", new Type[] { typeof(object) }).MakeGenericMethod(typeof(UnityEngine.Object));
            }

            [HarmonyPostfix]
            public static void HarmonyPostfix(ref AsyncOperationHandle<UnityEngine.Object> __result, object key)
            {
                bool newResultValid = false;

                var newResult = Addressables.ResourceManager.CreateChainOperation<UnityEngine.Object, UnityEngine.Object>(__result, overrideResult =>
                {
                    if(overrideResult.Result is GameObject)
                    {
                        GameObject result = (GameObject)overrideResult.Result;

                        var addressableSOType = System.Reflection.Assembly.GetAssembly(typeof(BBI.Unity.Game.SecuringObjectRemovedEvent)).GetType("BBI.Unity.Game.AddressableSOLoader");

                        // Console.WriteLine("Loading references");

                        foreach (var addressableSO in result.GetComponentsInChildren(addressableSOType))
                        // if (arg.Result.TryGetComponent(addressableSOType, out var addressableSO))
                        {
                            Addressables_InstantiateAsync.LoadScriptableObjectReferences((MonoBehaviour)addressableSO);
                        }
                    }    

                    if (overrideResult.Result is TypeAsset)
                    {
                        // var res = (ModuleConstructionAsset)overrideResult.Result;
                        // Log($"Key: {key}");
                        // Log($"Found: {res.name}");
                        // 64a1a1f3e77574f468b15c16dfe0229f

                        string AssetBasis = (string)typeof(TypeAsset).GetField("AssetBasis")?.GetValue(overrideResult.Result);

                        if (AssetBasis != "" && AssetBasis != null)
                        {
                            Log($"Basis Start: {overrideResult.Result.name} is based on {AssetBasis}");

                            newResultValid = false;
                            return Addressables.ResourceManager.CreateChainOperation<UnityEngine.Object, UnityEngine.Object>(
                                Addressables.LoadAssetAsync<UnityEngine.Object>(new AssetReferenceScriptableObject(AssetBasis)), basisResult =>
                                {
                                    // ScriptableObject newSO = (ScriptableObject)UnityEngine.Object.Instantiate(res.Result);

                                    var dataField = overrideResult.Result.GetType().GetField("Data");

                                    var dataGot = dataField.GetValue(overrideResult.Result);
                                    dataField.SetValue(overrideResult.Result, dataField.GetValue(basisResult.Result));

                                    Log($"Member Start");
                                    foreach (var member in dataField.FieldType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                                    {
                                        Log($"Member: {member.Name}");

                                        var baseVal = member.GetValue(dataField.GetValue(basisResult.Result));
                                        var overVal = member.GetValue(dataGot);

                                        Log($"Values: {baseVal} < {overVal}");
                                        Log($"Equal: {baseVal == overVal}");

                                        if (overVal == null) continue;
                                        if (overVal == baseVal) continue;

                                        var assetGUIDField = overVal.GetType().GetProperty("AssetGUID", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);

                                        if(assetGUIDField != null)
                                        {
                                            var assetGUID = (string)assetGUIDField.GetValue(overVal);

                                            if (assetGUID != null && assetGUID == "") continue;
                                        }

                                        if (member.Name != "m_RootModuleRef") continue;

                                        Log($"Setting member {member.Name} to {overVal} from {baseVal}");
                                        member.SetValue(dataField.GetValue(overrideResult.Result), overVal);
                                    }
                                    Log($"Member done");

                                    return Addressables.ResourceManager.CreateCompletedOperation((UnityEngine.Object)overrideResult.Result, string.Empty);
                                });
                        }
                    }
                

                    // newResultValid = true;
                    return Addressables.ResourceManager.CreateCompletedOperation(overrideResult.Result, string.Empty);
                });

                if (newResultValid)
                    __result = newResult;
            }
        }
    }
}
