using System;
using System.Collections.Generic;
using System.Linq;
using BBI.Unity.Game;
using HarmonyLib;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TestProj
{
    [HarmonyPatch(typeof(ScriptableObject), MethodType.Constructor)]
    public class ScriptableObject_ScriptableObject
    {
        public static Dictionary<Type, List<UnityEngine.ScriptableObject>> ScriptableObjectsMapping = new Dictionary<Type, List<UnityEngine.ScriptableObject>>();

        [HarmonyPrefix]
        public static void Prefix(ScriptableObject __instance)
        {
            var instanceType = __instance.GetType();
            if (!ScriptableObjectsMapping.ContainsKey(instanceType)) ScriptableObjectsMapping.Add(instanceType, new List<UnityEngine.ScriptableObject>());
            ScriptableObjectsMapping[instanceType].Add(__instance);
        }
    }

    [HarmonyPatch(typeof(Module), "CreateAndLoadAsync")]
    public class Module_CreateAndLoadAsync
    {
        public static Dictionary<string, GameObject> LoadedGameObjects = new Dictionary<string, GameObject>();
        public static Dictionary<string, string> LoadedIDs = new Dictionary<string, string>();
        public static Dictionary<string, AssetReferenceGameObject> LoadedRefs = new Dictionary<string, AssetReferenceGameObject>();

        [HarmonyPrefix]
        public static void Prefix(ShipSpawnParams spawnParams, AssetReferenceGameObject modulePrefabRef, RigidTransform rigidTransform, string memberName, string fileName, int sourceLineNumber)
        {
            Addressables.LoadAssetAsync<GameObject>(modulePrefabRef).Completed += (obj) =>
            {
                // Console.WriteLine($"{obj.Result.name}");
                LoadedGameObjects[obj.Result.name] = obj.Result;
                LoadedIDs[modulePrefabRef.AssetGUID] = obj.Result.name;
                LoadedRefs[obj.Result.name] = modulePrefabRef;
            };
        }

        public static object addressable;
        public static void GetAddressable<T>(string key)
        {
            Addressables.LoadAssetAsync<T>(new AssetReference(key)).Completed += (obj) =>
            {
                Console.WriteLine($"Loading {key}: {obj.IsValid()}");
                addressable = obj.Result;
            };
        }
    }

    [HarmonyPatch(typeof(Addressables), "InstantiateAsync", new Type[] { typeof(object), typeof(Vector3), typeof(Quaternion), typeof(Transform), typeof(bool) })]
    public class Addressables_InstantiateAsync
    {
        static AsyncOperationHandle<GameObject> GameObjectReady(AsyncOperationHandle<GameObject> arg)
        {
            var result = InstantiateChildren(arg.Result);

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

                Console.WriteLine($"Loading GO ref {assetGUID}");

                Addressables.ResourceManager.CreateChainOperation<GameObject, GameObject>(
                    Addressables.LoadAssetAsync<GameObject>(new AssetReferenceGameObject(assetGUID)), arg =>
                    {
                        GameObject result = arg.Result;
                        if(!string.IsNullOrEmpty(childPath))
                        {
                            result = result.transform.Find(childPath)?.gameObject;
                        }

                        if(result == null)
                        {
                            Console.WriteLine($"ERROR: Can't find {childPath} in {assetGUID}");
                            result = arg.Result;
                        }

                        result = InstantiateChildren(GameObject.Instantiate(result, addressableGO.transform));

                        if (!string.IsNullOrEmpty(childPath))
                        {
                            result.transform.localPosition = Vector3.zero;
                        }

                        foreach(var child in disabledChildren)
                        {
                            var foundChild = result.transform.Find(child)?.gameObject;
                            if (foundChild != null)
                            {
                                GameObject.Destroy(foundChild);
                                Console.WriteLine($"Removing {child} from {assetGUID}");
                            }
                            else
                            {
                                Console.WriteLine($"ERROR: Can't find {child} from {assetGUID}");
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
                Console.WriteLine("SO loading exception!");
                Console.WriteLine(ex.Message);
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
                Console.WriteLine($"Loading SO ref {refs[i]}");

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
            var newResult = Addressables.ResourceManager.CreateChainOperation<UnityEngine.Object, UnityEngine.Object>(__result, arg =>
            {
                try
                {
                    GameObject result = (GameObject)arg.Result;

                    var addressableSOType = System.Reflection.Assembly.GetAssembly(typeof(BBI.Unity.Game.SecuringObjectRemovedEvent)).GetType("BBI.Unity.Game.AddressableSOLoader");

                    // Console.WriteLine("Loading references");

                    foreach (var addressableSO in result.GetComponentsInChildren(addressableSOType))
                    // if (arg.Result.TryGetComponent(addressableSOType, out var addressableSO))
                    {
                        Addressables_InstantiateAsync.LoadScriptableObjectReferences((MonoBehaviour)addressableSO);
                    }

                }
                catch (InvalidCastException ex)
                {
                    // Do nothing, expected
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SO loading exception!");
                    Console.WriteLine(ex.Message);
                }

                // newResultValid = true;
                return Addressables.ResourceManager.CreateCompletedOperation(arg.Result, string.Empty);
            });

            if (newResultValid)
                __result = newResult;
        }
    }

    [HarmonyPatch(typeof(ShipRandomizationHelper), "ConstructModuleGroup")]
    public class ShipRandomizationHelper_ConstructModuleGroup
    {
        [HarmonyPrefix]
        public static void Prefix(ShipPreview shipPreview, ShipSpawnParams spawnParams, AddressableCache addressableCache)
        {
            Console.WriteLine($"swap started");

            System.Reflection.FieldInfo fi = typeof(ModuleConstructionAsset.ModuleConstructionData).GetField("m_RootModuleRef", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // fi.SetValue(conAsset.Data, Module_CreateAndLoadAsync.LoadedRefs["PRF_CargoFloor10_CanCanCan"]);

            AssetReferenceGameObject newValue;
            if (Settings.settings.assetReferenceGameObject != null && Settings.settings.assetReferenceGameObject != "")
            {
                newValue = new AssetReferenceGameObject(Settings.settings.assetReferenceGameObject);
            }
            else
            {
                // Custom:
                // newValue = new AssetReferenceGameObject("Assets/CustomRootModule.prefab");
                // newValue = new AssetReferenceGameObject("41ec03b2a742f9d43bf0a13bb5ba8fe2"); // CuttableDemo
                // newValue = new AssetReferenceGameObject("985e889cb1a325c4ba92855918db3bcf"); // Assets/CustomCrate.prefab
                // newValue = new AssetReferenceGameObject("ef1e539d57a75124f92c7ba11c2892e7"); // Assets/TestBeams.prefab
                // newValue = new AssetReferenceGameObject("245edd5faef68494097595f84d147652"); // Assets/PRF_Crate_Hard(Clone).prefab
                // newValue = new AssetReferenceGameObject("a6c97fb2efb3bb14faac21c54b561293"); // Assets/CustomOperation/MackNoseRefs.prefab
                // newValue = new AssetReferenceGameObject("102453e3bd4709c41b25e2fc6f6fe3f7"); // Assets/CustomOperation/AirlockRef.prefab
                // newValue = new AssetReferenceGameObject("f2ade62975d33c5408fc695ba5be1d27"); // Assets/CustomOperation/BoxRef.prefab
                // newValue = new AssetReferenceGameObject("41de0a0595534cc4eb29ba7c4baf3a46"); // Assets/CustomOperation/BoxRefAirlock.prefab
                newValue = new AssetReferenceGameObject("e723eba8ae422e84190e2971c9c374f5");    // Assets/CustomOperation/FirstShip.prefab
                // newValue = new AssetReferenceGameObject("792652162aeed3342810ad6a261da3d2"); // Assets/CustomOperation/FirstShipSpawner.prefab

                // Standard:
                // newValue = new AssetReferenceGameObject("Assets/Content/Prefabs/Objects/Structural/PRF_Cutpoint_Generic_1xBxB.prefab");
                // newValue = new AssetReferenceGameObject("e90d482ebb5084143872d4e498c6462b");
                // newValue = new AssetReferenceGameObject("3e84870b4ad837d458773aeaae90a447"); // Hard crate
                // newValue = new AssetReferenceGameObject("Assets/Content/Prefabs/Objects/Storage/PRF_Crate_Hard.prefab"); // Hard crate
                // newValue = new AssetReferenceGameObject("fd038d23f35b59747a22dec2f214b11f");

                // newValue = new AssetReferenceGameObject("8a1c2cd36ea6fb14b8ce4f6e29bbafb8"); // PRF_CargoFloor10_CanCanCan
                // newValue = new AssetReferenceGameObject("fd038d23f35b59747a22dec2f214b11f"); // Ship Kit/Nodes/Mackerel/Core Segments/PRF_Mackerel_Airlock.prefab
                // newValue = new AssetReferenceGameObject("c205dbbc6d144134a872ec34bf213277"); // Assets/Content/Prefabs/Objects/_Object Groups/Hardpoint Group/Crew Bed Bunks/PRF_CrewBedBunksDouble01.prefab
                // newValue = new AssetReferenceGameObject("8d8c20336138f10499ab31e0b2dd8bb4"); // Working airlock - PRF_Mackerel_Airlock_Layout_AirlockPortWallStarboard(Clone)
            }

            fi.SetValue(shipPreview.ConstructionAsset.Data, newValue);

            Console.WriteLine($"swap done");
        }
    }

    [HarmonyPatch(typeof(Carbon.Core.Log), "LogMessage")]
    public class Log_LogMessage
    {
        [HarmonyPrefix]
        public static void Prefix(object sender, Carbon.Core.Log.Severity sev, Carbon.Core.Log.Channel channel, string format, params object[] args)
        {
            string message = $"{sev.ToString()}: {((args != null && args.Length != 0) ? string.Format(format, args) : format)}";
            Console.WriteLine(message);

            if (sev == Carbon.Core.Log.Severity.Error)
            {
                try
                {
                    var stackTrace = new System.Diagnostics.StackTrace(2, true);
                    System.IO.File.WriteAllText("D:\\exception.txt", stackTrace.GetFrame(0).GetType() + "\r\n" + stackTrace.ToString());
                }
                catch { }
            }
        }
    }

    [HarmonyPatch(typeof(PlayableArea), "GetPlayableAreaState")]
    public class PlayableAreaState_PlayableAreaState
    {
        public static bool Prefix(ref PlayableArea.PlayableAreaState __result)
        {
            __result = PlayableArea.PlayableAreaState.Safe;
            return false;
        }
    }
}