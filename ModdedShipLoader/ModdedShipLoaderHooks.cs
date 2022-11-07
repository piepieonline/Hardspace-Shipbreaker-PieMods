using BBI.Unity.Game;
using HarmonyLib;
using System;
using System.Collections;
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
        public static Type addressableType;
        public static Type addressableComponentLoaderType;
        public static Type addressableSOType;
        public static Type addressableComponentValueType;

        static void Log(string message, bool detailed = false)
        {
            if ((Settings.settings.debugLog && !detailed) || (Settings.settings.debugLogDetailed && detailed))
            {
                ModdedShipLoader.LoggerInstance.LogInfo(message);
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
                        if (shaderCache.ContainsKey(shaderName))
                        {
                            renderer.sharedMaterial.shader = shaderCache[shaderName];
                        }
                        else
                        {
                            UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Shader>(new UnityEngine.AddressableAssets.AssetReference(shaderToReference[shaderName])).Completed += res =>
                            {
                                renderer.sharedMaterial.shader = res.Result;
                                // Async, so it might already contain it by the time we call this
                                if (!shaderCache.ContainsKey(shaderName))
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
                return Addressables.ResourceManager.CreateChainOperation<GameObject, GameObject>(InstantiateChildren(arg.Result), result =>
                {
                    ReplaceShaders(result.Result);
                    return result;
                });
            }

            static AsyncOperationHandle<GameObject> InstantiateChildren(GameObject parent)
            {
                List<AsyncOperationHandle> handles = new List<AsyncOperationHandle>();

                foreach (var addressableGO in parent.GetComponentsInChildren(addressableType))
                // if (arg.Result.TryGetComponent(addressableType, out var loader))
                {
                    string assetGUID = (string)addressableType.GetField("assetGUID").GetValue(addressableGO);
                    string childPath = (string)addressableType.GetField("childPath").GetValue(addressableGO);
                    List<string> disabledChildren = (List<string>)addressableType.GetField("disabledChildren").GetValue(addressableGO);
                    List<Component> componentsOnChildren = (List<Component>)addressableType.GetField("componentsOnChildren").GetValue(addressableGO);
                    List<string> componentsOnChildrenPaths = (List<string>)addressableType.GetField("componentsOnChildrenPaths").GetValue(addressableGO);


                    Log($"Loading GO ref {assetGUID}", true);

                    handles.Add(Addressables.ResourceManager.CreateChainOperation<GameObject, GameObject>(
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

                            if(!result.TryGetComponent<ModuleDefinition>(out _))
                            {
                                result.AddComponent<ModuleDefinition>();
                            }

                            for (int i = 0; i < componentsOnChildren.Count; i++)
                            {
                                Log($"Trying to clone {componentsOnChildren[i]}");
                                var componentParent = (componentsOnChildrenPaths.Count - 1 < i || componentsOnChildrenPaths[i] == "") ? result : result.transform.Find(childPath).gameObject;
                                CloneComponent(componentParent.AddComponent(componentsOnChildren[i].GetType()), componentsOnChildren[i]);
                                Log($"Cloned {componentsOnChildren[i].GetType().ToString()}");
                            }

                            return Addressables.ResourceManager.CreateChainOperation<GameObject, GameObject>(InstantiateChildren(GameObject.Instantiate(result, addressableGO.transform)), handle =>
                            {
                                var handleResult = handle.Result;
                                ReplaceShaders(handleResult);

                                if (!string.IsNullOrEmpty(childPath))
                                {
                                    handleResult.transform.localPosition = Vector3.zero;
                                }

                                foreach (var child in disabledChildren)
                                {
                                    var foundChild = handleResult.transform.Find(child)?.gameObject;
                                    if (foundChild != null)
                                    {
                                        foundChild.transform.parent = null;
                                        GameObject.Destroy(foundChild);
                                        Log($"Removing {child} from {assetGUID}", true);
                                    }
                                    else
                                    {
                                        Log($"ERROR: Can't find {child} from {assetGUID}");
                                    }
                                }

                                return Addressables.ResourceManager.CreateCompletedOperation(handleResult, null);
                            });
                        })
                    );
                }

                return Addressables.ResourceManager.CreateChainOperation(
                    Addressables.ResourceManager.CreateGenericGroupOperation(handles),
                    handle =>
                    {
                        List<AsyncOperationHandle> soHandles = new List<AsyncOperationHandle>();

                        try
                        {
                            foreach (var addressableSO in parent.GetComponentsInChildren(addressableSOType))
                            {
                                soHandles.Add(LoadScriptableObjectReferences(addressableSO));
                            }

                            foreach (var addressableComponentLoader in parent.GetComponentsInChildren(addressableComponentLoaderType))
                            {
                                soHandles.Add(LoadScriptableObjectReferences(addressableComponentLoader));
                            }
                        }
                        catch (Exception ex)
                        {
                            Log("SO loading exception!");
                            Log(ex.Message);
                        }

                        if(soHandles.Count > 0)
                        {
                            return Addressables.ResourceManager.CreateChainOperation(
                                Addressables.ResourceManager.CreateGenericGroupOperation(handles), handle =>
                                    {
                                        return Addressables.ResourceManager.CreateCompletedOperation(parent, null);
                                    });
                        }
                        else
                        {
                            return Addressables.ResourceManager.CreateCompletedOperation(parent, null);
                        }
                    }
                );
            }

            [HarmonyPostfix]
            public static void HarmonyPostfix(ref UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> __result, object key, Vector3 position, Quaternion rotation, Transform parent, bool trackHandle)
            {
                __result = Addressables.ResourceManager.CreateChainOperation<GameObject, GameObject>(__result, GameObjectReady);
            }

            public static AsyncOperationHandle LoadScriptableObjectReferences(Component addressable)
            {
                List<AsyncOperationHandle> soHandles = new List<AsyncOperationHandle>();

                // Old style
                if(addressable.GetType() == addressableSOType)
                {
                    List<string> onChildren = (List<string>)addressableSOType.GetField("onChild").GetValue(addressable);
                    List<string> comps = (List<string>)addressableSOType.GetField("comp").GetValue(addressable);
                    List<string> fields = (List<string>)addressableSOType.GetField("field").GetValue(addressable);
                    List<string> refs = (List<string>)addressableSOType.GetField("refs").GetValue(addressable);

                    Log($"WARN: {addressable.name} is using the old AddressableSOLoader. Consider migrating to the AddressableComponentLoader!");

                    for (int i = 0; i < refs.Count; i++)
                    {
                        Log($"Loading SO ref {refs[i]}", true);

                        var parent = (onChildren.Count > 0 && onChildren[i] != "") ? addressable.transform.Find(onChildren[i]) : addressable.transform;
                        var comp = parent.GetComponents<Component>().Where(comp => comp.GetType().ToString() == comps[i]).FirstOrDefault();

                        if (comp == null)
                        {
                            Log($"ERROR: Couldn't load {comps[i]} from {parent.name} ({refs[i]}). Ship will not load!");
                            continue;
                        }

                        if (fields[i] == null || fields[i] == "")
                        {
                            Log($"ERROR: Couldn't load {fields[i]} from {parent.name} ({refs[i]}). Ship will not load!");
                            continue;
                        }

                        if (refs[i] == null || refs[i] == "")
                        {
                            Log($"ERROR: Couldn't load {fields[i]} from {parent.name} ({refs[i]}). Ship will not load!");
                            continue;
                        }

                        soHandles.Add(LoadScriptableObjectReferences(comp, fields[i], refs[i]));
                    }
                }
                // New style
                else if (addressable.GetType() == addressableComponentLoaderType)
                {
                    List<Component> components = (List<Component>)addressableComponentLoaderType.GetField("components").GetValue(addressable);
                    List<string> fields = (List<string>)addressableComponentLoaderType.GetField("fields").GetValue(addressable);
                    List<string> addresses = (List<string>)addressableComponentLoaderType.GetField("addresses").GetValue(addressable);

                    for (int i = 0; i < addresses.Count; i++)
                    {
                        Log($"Loading AddressableComponent ref {addresses[i]}", true);
                        soHandles.Add(LoadScriptableObjectReferences(components[i], fields[i], addresses[i]));
                    }
                }
                

                if (soHandles.Count > 0)
                {
                    return Addressables.ResourceManager.CreateChainOperation(
                        Addressables.ResourceManager.CreateGenericGroupOperation(soHandles), handle =>
                        {
                            return Addressables.ResourceManager.CreateCompletedOperation(addressable.gameObject, null);
                        });
                }
                else
                {
                    return Addressables.ResourceManager.CreateCompletedOperation(addressable.gameObject, null);
                }
            }

            public static AsyncOperationHandle LoadScriptableObjectReferences(Component componentToModify, string fieldName, string addressToLoad)
            {
                System.Reflection.FieldInfo fi = componentToModify.GetType().GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                return Addressables.ResourceManager.CreateChainOperation<ScriptableObject, ScriptableObject>(
                    Addressables.LoadAssetAsync<ScriptableObject>(new AssetReferenceScriptableObject(addressToLoad)),
                    res =>
                    {
                        if (res.IsValid())
                        {
                            fi.SetValue(componentToModify, res.Result);
                        }
                        return Addressables.ResourceManager.CreateCompletedOperation(res.Result, null);
                    }
                );
            }
        }

        [HarmonyPatch()]
        public class Addressables_LoadAssetAsync
        {
            public static string[] assetKeys;

            static System.Reflection.MethodInfo TargetMethod()
            {
                return typeof(Addressables).GetMethod("LoadAssetAsync", new Type[] { typeof(object) }).MakeGenericMethod(typeof(UnityEngine.Object));
            }

            [HarmonyPostfix]
            public static void HarmonyPostfix(ref AsyncOperationHandle<UnityEngine.Object> __result, object key)
            {
                bool newResultValid = assetKeys != null && assetKeys.Contains(key.ToString());

                var newResult = Addressables.ResourceManager.CreateChainOperation<UnityEngine.Object, UnityEngine.Object>(__result, overrideResult =>
                {
                    if (overrideResult.Result is GameObject)
                    {
                        GameObject result = (GameObject)overrideResult.Result; 
                        // Console.WriteLine("Loading references");

                        foreach (var addressableSO in result.GetComponentsInChildren(addressableSOType))
                        // if (arg.Result.TryGetComponent(addressableSOType, out var addressableSO))
                        {
                            if (addressableSO.GetComponent(addressableType) == null)
                                Addressables_InstantiateAsync.LoadScriptableObjectReferences((MonoBehaviour)addressableSO);
                        }

                        foreach (var addressableComponents in result.GetComponentsInChildren(addressableComponentLoaderType))
                        {
                            // TODO: can't remember why this is an if?
                            if (addressableComponents.GetComponent(addressableType) == null)
                                Addressables_InstantiateAsync.LoadScriptableObjectReferences((MonoBehaviour)addressableComponents);
                        }
                    }

                    if (overrideResult.Result is ScriptableObject)
                    {
                        string AssetCloneRef = (string)overrideResult.Result.GetType().GetField("AssetCloneRef", (System.Reflection.BindingFlags)(-1))?.GetValue(overrideResult.Result);
                        string AssetBasis = (string)overrideResult.Result.GetType().GetField("AssetBasis", (System.Reflection.BindingFlags)(-1))?.GetValue(overrideResult.Result);

                        if(AssetCloneRef != null && AssetCloneRef != "")
                        {
                            Log($"Clone Start: {overrideResult.Result.name} is a clone reference of {AssetCloneRef}");
                            return Addressables.LoadAssetAsync<UnityEngine.Object>(new AssetReferenceScriptableObject(AssetCloneRef));
                        }

                        if (AssetBasis != null && AssetBasis != "")
                        {
                            Log($"Basis Start: {overrideResult.Result.name} is based on {AssetBasis}");

                            return Addressables.ResourceManager.CreateChainOperation<UnityEngine.Object, UnityEngine.Object>(
                                Addressables.LoadAssetAsync<UnityEngine.Object>(new AssetReferenceScriptableObject(AssetBasis)), basisResult =>
                                {
                                    ScriptableObject newSO = (ScriptableObject)UnityEngine.Object.Instantiate(basisResult.Result);

                                    var dataField = overrideResult.Result.GetType().GetField("Data");

                                    var dataGot = dataField.GetValue(overrideResult.Result);
                                    // dataField.SetValue(overrideResult.Result, dataField.GetValue(basisResult.Result));

                                    Log($"Member Start", true);
                                    foreach (var member in dataField.FieldType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                                    {
                                        Log($"Member: {member.Name}", true);

                                        var baseVal = member.GetValue(dataField.GetValue(basisResult.Result));
                                        var overVal = member.GetValue(dataGot);

                                        // Must use equals to compare objects, not references

                                        Log($"Values: {baseVal} < {overVal}", true);
                                        Log($"Equal: {baseVal?.Equals(overVal)}", true);

                                        if (overVal == null) continue;
                                        // if (overVal == baseVal) continue;
                                        if (overVal.Equals(baseVal)) continue;
                                        if (typeof(IList).IsAssignableFrom(member.FieldType) && ((IList)overVal).Count == 0) continue;

                                        var assetGUIDField = overVal.GetType().GetProperty("AssetGUID", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);

                                        if (assetGUIDField != null)
                                        {
                                            var assetGUID = (string)assetGUIDField.GetValue(overVal);

                                            if (assetGUID != null && assetGUID == "") continue;
                                        }

                                        // if (member.Name != "m_RootModuleRef") continue;

                                        Log($"Setting member {member.Name} to {overVal} from {baseVal}", true);
                                        member.SetValue(dataField.GetValue(newSO), overVal);
                                    }
                                    Log($"Member done", true);

                                    return Addressables.ResourceManager.CreateCompletedOperation((UnityEngine.Object)newSO, string.Empty);
                                });
                        }
                    }

                    return Addressables.ResourceManager.CreateCompletedOperation(overrideResult.Result, string.Empty);
                });

                if (newResultValid)
                    __result = newResult;
            }
        }

        // TODO: Move to it's own util file?
        public static T CloneComponent<T>(Component comp, T other) where T : Component
        {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.DeclaredOnly;
            System.Reflection.PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            System.Reflection.FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }

        // Helpers

        // Hide test ships
        [HarmonyPatch(typeof(LevelSelectController), "LevelAssetLoadComplete")]
        public class LevelSelectController_LevelAssetLoadComplete
        {
            public static void Prefix(ref AsyncOperationHandle<IList<LevelAsset>> levelHandle)
            {
                if (Settings.settings.enableDeveloperShips)
                    return;

                for(int i = levelHandle.Result.Count - 1; i >= 0; i--)
                {
                    if(levelHandle.Result[i].Data.IsDeveloperLevel)
                        levelHandle.Result.RemoveAt(i);
                }
            }
        }

        // Debug Menu
        [HarmonyPatch(typeof(GameSession), "InitializeServices")]
        public class GameSession_InitializeServices
        {
            public static void Prefix(ref Carbon.Core.Services.ServiceContext ___mSessionServices)
            {
                ___mSessionServices.AddService<DebugMenu>(new DebugMenu(), true);
            }
        }

        // Show in campaign menu
        [HarmonyPatch(typeof(ShipClassAsset), "GeneratableShips", MethodType.Getter)]
        public class ShipClassAsset_GeneratableShips
        {
            public static Dictionary<string, ShipArchetypeAsset> moduleToArchetype = new Dictionary<string, ShipArchetypeAsset>();
            public static Dictionary<string, int> moduleToMinLevel = new Dictionary<string, int>();
            
            
            private static Dictionary<string, int> overrideRefToLevel = new Dictionary<string, int>()
            {
                { "47acc2fd684b23b4fbb012d6d946b4cd", 0 },
                { "b9e1e972902114c40a2d4757ac015c09", 1 },
                { "389ff83b79255d54682f27d3f4f093b9", 2 },
                { "4246848384a0fef4fb3d5dac7d2575ac", 3 },
                { "0d27abce2f298fb409698a0969036eb3", 3 },
                { "80525e0693e537b4ba9dfb89c2475d3b", 4 },
                { "9f3d5012d0385694a8e3208d45262f12", 4 },
                { "11694b7aa96123f449c66f1907248d84", 5 },
                { "2a8b10e2c04c4e247b7725a46a177119", 5 },
                { "39107a60851420e4fb61ae12c26923ce", 5 },
                { "534f845a37e08e04c93cc6608728a2d6", 5 },
                { "02b7babd28b63244f98e62ca94ffddf7", 6 },
                { "6bd1a6426338e094992169b4c4af18d0", 6 },
                { "4caad3e752972334eb2b34646b43fc81", 6 },
                { "43f493ab9c63cbb49a3c6cc01ba654b8", 7 },
                { "b36c98863c5d26e4a98d246444a4c60d", 7 },
                { "dc60f136d48327b4e872352efec7780d", 7 },
                { "f4f482461faa29e4e851ac22e9ce0ce3", 7 },
                { "1529837472d18fa4fb7800d1237ad1a9", 8 },
                { "5104c2b3a4f281743b94f6b624a35335", 8 },
                { "fa9077bcfa9d0d84f88f887d85efeb1f", 8 },
                { "af318959337b4e94e9d8b47db3902237", 9 },
                { "27290745cafacfb439cdd9aada509105", 10 }
            };

            public static bool Prefix(ref ShipClassAsset.GeneratableShipOverridePair[] __result, ShipClassAsset.GeneratableShipOverridePair[] ___m_GeneratableShips)
            {
                var ships = ___m_GeneratableShips.ToList();

                foreach (var moduleConstructionRef in moduleToArchetype.Keys)
                {
                    if(moduleToMinLevel[moduleConstructionRef] <= overrideRefToLevel[ships[0].OverrideRef.AssetGUID])
                    {
                        ships.Add(new ShipClassAsset.GeneratableShipOverridePair()
                        {
                            Guaranteed = false,
                            OverrideRef = ships[0].OverrideRef,
                            ShipArchetype = moduleToArchetype[moduleConstructionRef],
                            ShipRef = new AssetReferenceModuleConstructionAsset(moduleConstructionRef)
                        });
                    }
                }

                __result = ships.ToArray();

                return false;
            }
        }

        // Configure the number of ships in the campaign menu
        [HarmonyPatch(typeof(ShipClassAsset), "ShipsToGenerateInJobBoard", MethodType.Getter)]
        public class ShipClassAsset_ShipsToGenerateInJobBoard
        {
            public static bool Prefix(ref int __result, int ___m_ShipsToGenerateInJobBoard)
            {
                __result = ___m_ShipsToGenerateInJobBoard + Settings.settings.numberOfExtraShipsInCareerCatalog;

                return false;
            }
        }
        
        // Disable mass and cost, as they are wrong
        // TODO: Fix this calculation? Might just not be possible :(
        [HarmonyPatch(typeof(JobBoardScreenController), "DisplayShipInfoSalvageIndicator")]
        public class JobBoardScreenController_DisplayShipInfoSalvageIndicator
        {
            public static void Postfix(ShipCardController ___mCurrentlySelectedShip, TMPro.TextMeshProUGUI ___m_TotalMassField, TMPro.TextMeshProUGUI ___m_TotalValueField)
            {
                if(ShipClassAsset_GeneratableShips.moduleToArchetype.ContainsKey(___mCurrentlySelectedShip.ShipPreview.ConstructionAssetRef.AssetGUID))
                {
                    ___m_TotalMassField.SetText("Unknown (Modded)", true);
                    ___m_TotalValueField.SetText("Unknown (Modded)", true);
                }
            }
        }
    }
}
