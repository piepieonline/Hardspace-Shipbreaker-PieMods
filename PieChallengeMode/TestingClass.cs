using BBI.Unity.Game;
using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PieChallengeMode
{
    [BepInPlugin("com.piepieonline.testingplugin", "pieTesting", PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shipbreaker.exe")]
    internal class TestingClass : BaseUnityPlugin
    {
        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.M))
            {
                Logger.LogInfo($"M pressed");
                // BBI.Unity.Game.WorkOrderService.Instance.CreateSingleTutorialWorkOrder(Main.Instance.MainSettings.TutorialSettings.CampaignObjectivesList[0]);

                /*
                int indexToChange = 96;
                if(indexToChange >= 0)
                {
                    var asset = (UpgradeAsset)ScriptableObject_ScriptableObject.ScriptableObjectsMapping[typeof(UpgradeAsset)][indexToChange];
                    Logger.LogInfo(asset.name);
                }
                */

                float changeAmount = 2;
                foreach(UpgradeAsset asset in ScriptableObject_ScriptableObject.ScriptableObjectsMapping[typeof(UpgradeAsset)])
                {
                    foreach(var price in asset.Price)
                    {
                        FieldInfo amountField = typeof(UpgradePrice).GetField("m_Amount", BindingFlags.NonPublic | BindingFlags.Instance);
                        Logger.LogInfo($"Price for {asset.name} before: {price.Amount}");
                        amountField.SetValue(price, (int)(price.Amount * changeAmount));
                        Logger.LogInfo($"Price for {asset.name} after: {price.Amount}");
                    }
                }
                Logger.LogInfo($"Costs changed");
            }

            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.N))
            {
                Logger.LogInfo($"N pressed");
                var orCreateSystem = Unity.Entities.World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<UpdateActiveObjectiveGroupSystem>();

                FieldInfo fi = typeof(UpdateActiveObjectiveGroupSystem).GetField("mQueuedObjectiveGroups", BindingFlags.NonPublic | BindingFlags.Instance);
                var nextObj = ((List<ObjectiveGroupEntry>)fi.GetValue(orCreateSystem))[orCreateSystem.GetCurrentObjectiveIndex() + 1];
                WorkOrderService.Instance.ActivateNewObjectiveGroup(nextObj, out var _);
                //orCreateSystem.SetActiveObjectiveGroup(nextObj);
            }
        }
    }
}
