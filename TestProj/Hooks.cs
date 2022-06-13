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
    [HarmonyPatch(typeof(ShipPreview), "GatherModuleGroupData")]
    public class ShipPreview_GatherModuleGroupData
    {
        public static Dictionary<ShipPreview, ModuleGroupSummary> ModuleGroupSummaries = new Dictionary<ShipPreview, ModuleGroupSummary>();

        [HarmonyPrefix]
        public static void Prefix(ShipPreview __instance, ModuleGroupSummary moduleGroupSummary, ShipPreview.OptionalCreateParams optionalParams)
        {
            Console.WriteLine("ShipPreview_GatherModuleGroupData called");
            ModuleGroupSummaries[__instance] = moduleGroupSummary;
        }
    }

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
        static Dictionary<System.Reflection.FieldInfo, UnityEngine.Component> fieldToIndex = new Dictionary<System.Reflection.FieldInfo, UnityEngine.Component>();

        [HarmonyPrefix]
        public static bool HarmonyPrefix(ref UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> __result, object key, Vector3 position, Quaternion rotation, Transform parent, bool trackHandle)
        {
            Console.WriteLine("prefix!");
            return true;

            if (key.ToString() == "[b0a23fd93bc49804b982682709171202]")
            {
                __result = Addressables.InstantiateAsync(new AssetReferenceGameObject("2938e77a1793e5741af97e5417386f30"), position, rotation, parent, trackHandle);
                return false;
            }

            Console.WriteLine(key.GetType());
            Console.WriteLine(key.ToString());
            return true;

            // "PRF_MACKEREL Root Module"
            // PRF_CargoFloor10_CanCanCan
            // 3d5e96aef3a7b6048a24f7c0e902fdb9
            // if(key.ToString() == "[Assets/CustomRootModule.prefab]")
        }

        static AsyncOperationHandle<GameObject> GameObjectReady(AsyncOperationHandle<GameObject> arg)
        {
            var addressableType = System.Reflection.Assembly.GetAssembly(typeof(BBI.Unity.Game.SecuringObjectRemovedEvent)).GetType("BBI.Unity.Game.AddressableLoader");

            if (arg.Result.TryGetComponent(addressableType, out var loader))
            {
                List<string> refs = (List<string>)addressableType.GetField("refs").GetValue(loader);
                for (int i = 0; i < refs.Count; i++)
                {
                    Console.WriteLine($"Loading GO ref {refs[i]}");

                    Addressables.ResourceManager.CreateChainOperation<GameObject, GameObject>(
                        Addressables.InstantiateAsync(new AssetReferenceGameObject(refs[i]), arg.Result.transform.GetChild(i)), GameObjectReady)
                    ;

                }
            }

            try
            {
                var addressableSOType = System.Reflection.Assembly.GetAssembly(typeof(BBI.Unity.Game.SecuringObjectRemovedEvent)).GetType("BBI.Unity.Game.AddressableSOLoader");

                foreach(var addressableSO in arg.Result.GetComponentsInChildren(addressableSOType))
                // if (arg.Result.TryGetComponent(addressableSOType, out var addressableSO))
                {
                    List<string> comps = (List<string>)addressableSOType.GetField("comp").GetValue(addressableSO);
                    List<string> fields = (List<string>)addressableSOType.GetField("field").GetValue(addressableSO);
                    List<string> refs = (List<string>)addressableSOType.GetField("refs").GetValue(addressableSO);

                    for (int i = 0; i < refs.Count; i++)
                    {
                        Console.WriteLine($"Loading SO ref {refs[i]}");

                        var comp = arg.Result.GetComponents<Component>().Where(comp => comp.GetType().ToString() == comps[i]).First();
                        System.Reflection.FieldInfo fi = comp.GetType().GetField(fields[i], System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        fieldToIndex[fi] = comp;

                        Addressables.LoadAssetAsync<ScriptableObject>(new AssetReferenceScriptableObject(refs[i])).Completed += res =>
                        {
                            if (res.IsValid())
                            {
                                fi.SetValue(fieldToIndex[fi], res.Result);
                            }
                            fieldToIndex.Remove(fi);
                        };
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("SO loading exception!");
                Console.WriteLine(ex.Message);
            }

            return Addressables.ResourceManager.CreateCompletedOperation(arg.Result, string.Empty);
        }

        [HarmonyPostfix]
        public static void HarmonyPostfix(ref UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> __result, object key, Vector3 position, Quaternion rotation, Transform parent, bool trackHandle)
        {
            Console.WriteLine("starting post");
            __result = Addressables.ResourceManager.CreateChainOperation<GameObject, GameObject>(__result, GameObjectReady);

            // "PRF_MACKEREL Root Module"
            // PRF_CargoFloor10_CanCanCan
            // 3d5e96aef3a7b6048a24f7c0e902fdb9
            // if(key.ToString() == "[Assets/CustomRootModule.prefab]")
        }
    }

    /*
    [HarmonyPatch(typeof(Addressables), "LoadAssetAsync", new Type[] { typeof(object) })]
    public class Addressables_LoadAssetAsync
    {
        public static Dictionary<string, string> assetReferences = new Dictionary<string, string>();

        [HarmonyPostfix]
        public static void HarmonyPostfix(ref UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle __result, object key)
        {
            __result.Completed += res =>
            {
                Console.WriteLine((res.Result).ToString() + " " + key.ToString());
                assetReferences[(res.Result).ToString()] = key.ToString();
            };
        }
    }
    */

    /*
    [HarmonyPatch(typeof(HardPoint), "Awake")]
    public class HardPoint_Awake
    {
        [HarmonyPrefix]
        public static void Prefix(HardPoint __instance)
        {
            Console.WriteLine($"HardPoint {__instance.name} awake");
        }
    }
    */

    [HarmonyPatch(typeof(ShipRandomizationHelper), "ConstructModuleGroup")]
    public class ShipRandomizationHelper_ConstructModuleGroup
    {
        [HarmonyPrefix]
        public static void Prefix(ShipPreview shipPreview, ShipSpawnParams spawnParams, AddressableCache addressableCache)
        {
            Console.WriteLine($"swap started");

            System.Reflection.FieldInfo fi = typeof(ModuleConstructionAsset.ModuleConstructionData).GetField("m_RootModuleRef", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // fi.SetValue(conAsset.Data, Module_CreateAndLoadAsync.LoadedRefs["PRF_CargoFloor10_CanCanCan"]);

            // Custom:
            // var newValue = new AssetReferenceGameObject("Assets/CustomRootModule.prefab");
            // var newValue = new AssetReferenceGameObject("41ec03b2a742f9d43bf0a13bb5ba8fe2"); // CuttableDemo
            // var newValue = new AssetReferenceGameObject("985e889cb1a325c4ba92855918db3bcf"); // Assets/CustomCrate.prefab
            // var newValue = new AssetReferenceGameObject("ef1e539d57a75124f92c7ba11c2892e7"); // Assets/TestBeams.prefab
            // var newValue = new AssetReferenceGameObject("245edd5faef68494097595f84d147652"); // Assets/PRF_Crate_Hard(Clone).prefab
            // var newValue = new AssetReferenceGameObject("a6c97fb2efb3bb14faac21c54b561293"); // Assets/CustomOperation/MackNoseRefs.prefab
            // var newValue = new AssetReferenceGameObject("102453e3bd4709c41b25e2fc6f6fe3f7"); // Assets/CustomOperation/AirlockRef.prefab
            // var newValue = new AssetReferenceGameObject("f2ade62975d33c5408fc695ba5be1d27"); // Assets/CustomOperation/BoxRef.prefab
            var newValue = new AssetReferenceGameObject("41de0a0595534cc4eb29ba7c4baf3a46"); // Assets/CustomOperation/BoxRefAirlock.prefab

            // Standard:
            // var newValue = new AssetReferenceGameObject("Assets/Content/Prefabs/Objects/Structural/PRF_Cutpoint_Generic_1xBxB.prefab");
            // var newValue = new AssetReferenceGameObject("e90d482ebb5084143872d4e498c6462b");
            // var newValue = new AssetReferenceGameObject("3e84870b4ad837d458773aeaae90a447"); // Hard crate
            // var newValue = new AssetReferenceGameObject("Assets/Content/Prefabs/Objects/Storage/PRF_Crate_Hard.prefab"); // Hard crate
            //var newValue = new AssetReferenceGameObject("fd038d23f35b59747a22dec2f214b11f");

            // var newValue = new AssetReferenceGameObject("8a1c2cd36ea6fb14b8ce4f6e29bbafb8"); // PRF_CargoFloor10_CanCanCan
            // var newValue = new AssetReferenceGameObject("fd038d23f35b59747a22dec2f214b11f"); // Ship Kit/Nodes/Mackerel/Core Segments/PRF_Mackerel_Airlock.prefab
            // var newValue = new AssetReferenceGameObject("c205dbbc6d144134a872ec34bf213277"); // Assets/Content/Prefabs/Objects/_Object Groups/Hardpoint Group/Crew Bed Bunks/PRF_CrewBedBunksDouble01.prefab
            // var newValue = new AssetReferenceGameObject("8d8c20336138f10499ab31e0b2dd8bb4"); // Working airlock - PRF_Mackerel_Airlock_Layout_AirlockPortWallStarboard(Clone)



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
                var stackTrace = new System.Diagnostics.StackTrace(2, true);
                System.IO.File.WriteAllText("D:\\exception.txt", stackTrace.GetFrame(0).GetType() + "\r\n" + stackTrace.ToString());
            }
        }
    }
}