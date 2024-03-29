﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using ModHelper;
using Nuterra.NativeOptions;

namespace ScaleableTechs
{
    //A Mod that allows rescalable Techs.
    //  Due to the way Control Blocks handles Clusterbodies, compatability with it is all but impossible. 
    //  This mod will self-override and disable on a Tech that has any Control Blocks unless overridden

    public class KickStart
    {
        //
        public const string ModName = "ScaleTechs";

        internal static bool launched = false;
        internal static bool isConfigHelperPresent = false;
        internal static bool isNativeOptionsPresent = false;

        public static int keyInt = 91;  //default to be [
        public static KeyCode hotKey;

        //Variables
        public static bool GlobalGUIActive = false;     // Is the display up
        public static float GlobalAimedScale = 1f;      // The scale that every non-pip tech tries to aim for
        public static bool AttemptRecovery = false;     // ATTEMPT RECOVERY OF MOD
        public static bool ResetPlayerScale = true;     // Automatically rescale Tech to default when build-beam?
        public static bool dontPreventLogSpam = true;  // Enable non-player-controlled-Tech and unleash your inner BoundsIntersectSphere
        // wait I fixed that, thank crafty
                                                        //   - BoundsIntersectSphere is the main controller for the Tech's aiming range. 
                                                        //     Makes sense the smaller you get the less range you would have because tiny radar
                                                        //  WAIT i fixed it let's just make this the player tech only option

        public static bool RandomEnemyScales = true;   // Randomized Scale for each enemy?
        public static bool unInstallTrue = false;   // Randomized Scale for each enemy?

        public static bool isBuffBlocksPresent = false;

        public static void Main()
        {
            //Where the fun begins

            //Initiate the madness
            Harmony harmonyInstance = new Harmony("legionite.scaletechs.core");
            try
            {
                harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                Debug.Log("ScaleTechs: Eradicated BoundsIntersectSphere warning.");
            }
            catch (Exception e)
            {
                Debug.Log("ScaleTechs: WARNING: FAILIURE TO SUPPRESS \"BoundsIntersectSphere\"!");
                Debug.Log(e);
            }
            ManTankRescaler.Initiate();
            GlobalScaleGUIController.Initiate();

            //Make the Localized Pip GUI
            var GamerObject = new GameObject("PipGUIController");
            GamerObject.AddComponent<LocalDisplayController>();
            GamerObject.AddComponent<GUIPipDynamic>();
            Debug.Log("ScaleTechs - LocalGUI: Now Exists");


            InitSettings();
        }
        public static void InitSettings()
        {
            if (launched)
                return;
            launched = true;
            if (isNativeOptionsPresent && isConfigHelperPresent)
            {
                try
                {
                    KickStartConfigHelper.PushExtModConfigHandling();
                }
                catch (Exception e)
                {
                    Debug.Log("ScaleTechs: Error on Option & Config setup");
                    Debug.Log(e);
                }
            }
            else if (isNativeOptionsPresent)
            {
                try
                {
                    KickStartNativeOptions.PushExtModOptionsHandling();
                }
                catch (Exception e)
                {
                    Debug.Log("ScaleTechs: Error on NativeOptions setup");
                    Debug.Log(e);
                }
            }
            else if (isConfigHelperPresent)
            {
                try
                {
                    KickStartConfigHelper.PushExtModConfigHandlingConfigOnly();
                }
                catch (Exception e)
                {
                    Debug.Log("ScaleTechs: Error on ConfigHelper setup");
                    Debug.Log(e);
                }
            }

        }
        public static void TrySaveConfig()
        {
            try
            {

            }
            catch { }
        }
        public static bool LookForMod(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith(name))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class KickStartConfigHelper
    {
        internal static void PushExtModConfigHandlingConfigOnly()
        {
            PushExtModConfigSetup();
        }
        internal static ModConfig PushExtModConfigSetup()
        {
            Debug.Log("\nScaleTechs: Config Loading...");
            ModConfig thisModConfig = new ModConfig();
            Debug.Log("ScaleTechs: Config Loaded.");

            thisModConfig.BindConfig<KickStart>(null, "keyInt");
            KickStart.hotKey = (KeyCode)KickStart.keyInt;
            thisModConfig.BindConfig<KickStart>(null, "dontPreventLogSpam");
            thisModConfig.BindConfig<KickStart>(null, "RandomEnemyScales");
            thisModConfig.BindConfig<KickStart>(null, "ResetPlayerScale");
            thisModConfig.BindConfig<KickStart>(null, "GlobalAimedScale");
            thisModConfig.BindConfig<KickStart>(null, "unInstallTrue");
            return thisModConfig;
        }
        internal static void PushExtModConfigHandling()
        {
            var thisModConfig = PushExtModConfigSetup();
            try
            {
                KickStartNativeOptions.PushExtModOptionsHandling(thisModConfig);
            }
            catch (Exception e)
            {
                throw new Exception("PushExtModOptionsHandling within PushExtModConfigHandling hit exception - ", e);
            }
        }
    }

    public class KickStartNativeOptions
    {
        public static string ModName => KickStart.ModName;

        // NativeOptions Parameters
        public static OptionKey GUIMenuHotKey;
        public static OptionToggle resetPlayerScaleWhenBuilding;
        public static OptionToggle randomEnemyScale;
        public static OptionRange allTechsScale;
        public static OptionToggle noPreventLogsplosion;
        public static OptionToggle unInstall;



        internal static void PushExtModOptionsHandling()
        {
            //NativeOptions
            var TechScaleProperties = ModName + " - Tech Scale Settings";
            GUIMenuHotKey = new OptionKey("GUI Menu Button", TechScaleProperties, KickStart.hotKey);
            GUIMenuHotKey.onValueSaved.AddListener(() => { KickStart.keyInt = (int)(KickStart.hotKey = GUIMenuHotKey.SavedValue); GlobalScaleGUIController.Save(); });

            noPreventLogsplosion = new OptionToggle("Enable All Techs Rescaling", TechScaleProperties, KickStart.dontPreventLogSpam);
            noPreventLogsplosion.onValueSaved.AddListener(() => { KickStart.dontPreventLogSpam = noPreventLogsplosion.SavedValue; ManTankRescaler.UpdateAll(); GlobalScaleGUIController.Save(); });
            randomEnemyScale = new OptionToggle("Random Enemy Scaling", TechScaleProperties, KickStart.RandomEnemyScales);
            randomEnemyScale.onValueSaved.AddListener(() => { KickStart.RandomEnemyScales = randomEnemyScale.SavedValue; ManTankRescaler.UpdateAllEnemies(); GlobalScaleGUIController.Save(); });

            resetPlayerScaleWhenBuilding = new OptionToggle("Scale to 1 in Build Beam", TechScaleProperties, KickStart.ResetPlayerScale);
            resetPlayerScaleWhenBuilding.onValueSaved.AddListener(() => { KickStart.ResetPlayerScale = resetPlayerScaleWhenBuilding.SavedValue; GlobalScaleGUIController.Save(); });

            allTechsScale = new OptionRange("Global Tech Scale", TechScaleProperties, KickStart.GlobalAimedScale, 0.5f, 2.0f, 0.1f);
            allTechsScale.onValueSaved.AddListener(() => { 
                KickStart.GlobalAimedScale = allTechsScale.SavedValue; 
                GlobalScaleGUIController.Save(); 
            });
            unInstall = new OptionToggle("\n<b>Prepare for Uninstall</b>  \n(Toggle this OFF and Save your Techs & Worlds to keep!)", TechScaleProperties, KickStart.unInstallTrue);
            unInstall.onValueSaved.AddListener(() => { KickStart.unInstallTrue = unInstall.SavedValue; ManTankRescaler.UpdateAll(); GlobalScaleGUIController.Save(); });
        }

        internal static void PushExtModOptionsHandling(ModConfig thisModConfig)
        {
            PushExtModOptionsHandling();
            if (thisModConfig != null)
                NativeOptionsMod.onOptionsSaved.AddListener(() => { thisModConfig.WriteConfigJsonFile(); });
        }
    }
    internal class Patches
    {
        //SHOVE IN ModuleRescale in EVERYTHING!
        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("OnPool")]//On Block Creation
        private class PatchBlock
        {
            private static void Postfix(TankBlock __instance)
            {
                var ModuleAdd = __instance.gameObject.AddComponent<ModuleScaleWithSize>();
                ModuleAdd.TankBlock = __instance;
                ModuleAdd.OnPool();
            }
        }



        [HarmonyPatch(typeof(Tank))]
        [HarmonyPatch("OnPool")]
        private class PatchTank
        {
            private static void Postfix(Tank __instance)
            {
                var TankChange = __instance.gameObject.AddComponent<RescaleableTank>();
                TankChange.Subscribe(__instance);
            }
        }

        [HarmonyPatch(typeof(Tank))]
        [HarmonyPatch("BoundsIntersectSphere")]
        private class ShutUpBoundsIntersectSphere
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {

                int startIndex = -1;
               //Debug.Log("ScaleTechs: LAUNCHED TRANSPILER!");
                //codes = codes.ToList();
                var codes = new List<CodeInstruction>(instructions);
                //Debug.Log("ScaleTechs: LINE COUNT" + codes.Count);
                for (int i = 0; i < codes.Count; i++)
                {
                    //Debug.Log("ScaleTechs: LINE " + i);
                    //let's just do it the bloody lazy way
                    if (i == 2)
                    {
                        //Debug.Log("ScaleTechs: STARTING TRANSPILER AT " + i);
                        startIndex = i;
                    }
                    if (i == 7)
                    {
                        //
                       // Debug.Log("ScaleTechs: AQUIRED \"BoundsIntersectSphere\" MESSAGE ORIGIN" + i);
                    }
                    if (i == 3)
                    {
                        //Debug.Log("ScaleTechs: PERFORMING SWAP OPERATION WITH TRANSPILER AT " + i);
                        int endIndex = i;
                        codes[endIndex].opcode = codes[startIndex].opcode;
                    }
                }
                codes.RemoveRange(6, 20);
                return codes.AsEnumerable();
            }
        }

        //(This is from BuffBlocks, needed to change the radius, speed, and suspension (bounce bug) of wheels to prevent huge Techs from becoming stranded)
        [HarmonyPatch(typeof(ManWheels.Wheel), "UpdateAttachData")] // Thanks Aceba && FireFlyWater!
        private static class FixUpdateAttachData
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                //Debug.Log("ScaleTechs: Checked lines of code, " + codes.Count + " confirmed");

                if (KickStart.LookForMod("FFW_TT_BuffBlock"))
                {
                    Debug.Log("Found Buff Blocks and disengaging modifier!  Will piggyback off of other mod changes!");
                    KickStart.isBuffBlocksPresent = true;
                    codes = codes.ToList();

                }
                else
                    codes = codes.Skip(2).ToList();
                return codes;
            }
        }
    }

    internal class UpdateFreakingWheels
    {
        // I take absolutely no credit for this part, just needed to update the radius of the freaking wheels
        //   >>> All credit for this code goes to Aceba1(Whitepaw) and FireFlyWater <<<
        private static FieldInfo field_Wheels;
        private static FieldInfo field_WheelParams;
        private static FieldInfo field_AttachedId;
        private static FieldInfo field_WheelState;

        public static void GrabCurrent()
        {
            // why does wheels be so complicated
            field_Wheels = typeof(ModuleWheels)
                .GetField("m_Wheels", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            field_WheelParams = typeof(ModuleWheels)
                .GetField("m_WheelParams", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            field_AttachedId = typeof(ManWheels.Wheel)
                .GetField("attachedID", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            field_WheelState = typeof(ManWheels)
                .GetField("m_WheelState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
        
        public static void ForceUpdateWHEELS(GameObject Target)
        {
            //Step
            ModuleWheels wheels = Target.GetComponent<ModuleWheels>();
            List<ManWheels.Wheel> value_Wheels = (List<ManWheels.Wheel>)field_Wheels.GetValue(wheels);
            ManWheels.WheelParams wheelparams = (ManWheels.WheelParams)field_WheelParams.GetValue(wheels); // Read active WheelParams... 
            foreach (ManWheels.Wheel wheel in value_Wheels)
            {
                //Step
                Array value_WheelState = (Array)field_WheelState.GetValue(Singleton.Manager<ManWheels>.inst);

                int value_WheelAttachedId = (int)field_AttachedId.GetValue(wheel); // Important, determines what AttachedWheelState is associated
                if (value_WheelAttachedId > -1) // value is -1 if it's unloaded, I think...
                {

                    object value_AttachedWheelState = value_WheelState.GetValue(value_WheelAttachedId); // AttachedWheelState is a PRIVATE STRUCT, `object` neccessary

                    // Only need the WheelParams to be updated for this case as that contains the Radius
                    FieldInfo field_p_WheelParams = value_AttachedWheelState.GetType()
                        .GetField("wheelParams", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    field_p_WheelParams.SetValue(value_AttachedWheelState, wheelparams); // Apply new WheelParams...

                    // Final Preparations
                    FieldInfo field_p_Inertia = value_AttachedWheelState.GetType()
                        .GetField("inertia", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    ModuleWheels.AttachData moduleData = new ModuleWheels.AttachData(wheels, (float)field_p_Inertia.GetValue(value_AttachedWheelState), value_Wheels.Count);
                    wheel.UpdateAttachData(moduleData); // Update it! Live! Do it! Let the dreams be real!
                }
            }
        }
    }
}
