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
            public static string[] assetKeys;

            static System.Reflection.MethodInfo TargetMethod()
            {
                return typeof(Addressables).GetMethod("LoadAssetAsync", new Type[] { typeof(object) }).MakeGenericMethod(typeof(UnityEngine.Object));
            }

            [HarmonyPostfix]
            public static void HarmonyPostfix(ref AsyncOperationHandle<UnityEngine.Object> __result, object key)
            {
                bool newResultValid = assetKeys.Contains(key.ToString());

                var newResult = Addressables.ResourceManager.CreateChainOperation<UnityEngine.Object, UnityEngine.Object>(__result, overrideResult =>
                {
                    if (overrideResult.Result is GameObject)
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
                        string AssetBasis = (string)typeof(TypeAsset).GetField("AssetBasis")?.GetValue(overrideResult.Result);

                        if (AssetBasis != "" && AssetBasis != null)
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

                                        Log($"Values: {baseVal} < {overVal}", true);
                                        Log($"Equal: {baseVal == overVal}", true);

                                        if (overVal == null) continue;
                                        if (overVal == baseVal) continue;

                                        var assetGUIDField = overVal.GetType().GetProperty("AssetGUID", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);

                                        if (assetGUIDField != null)
                                        {
                                            var assetGUID = (string)assetGUIDField.GetValue(overVal);

                                            if (assetGUID != null && assetGUID == "") continue;
                                        }

                                        if (member.Name != "m_RootModuleRef") continue;

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

        [HarmonyPatch(typeof(GameSession), "InitializeServices")]
        public class GameSession_InitializeServices
        {
            public static void Prefix(ref Carbon.Core.Services.ServiceContext ___mSessionServices)
            {
                ___mSessionServices.AddService<DebugMenu>(new DebugMenu(), true);
            }
        }
    }
}
