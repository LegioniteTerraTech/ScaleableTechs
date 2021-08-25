using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScaleableTechs
{
    public class RescaleSystem : MonoBehaviour
    {
        /// <summary>
        ///   Probably the BIGGEST of messes out there, the Rescalesystem is home to a fairly quirky thingamabob
        ///     that messes with the very core mechanic of the game - Techs to the bone.
        ///    
        ///    It's based off of the concept of Exund's Advanced Building where you can rescale and shift blocks
        ///     BUT it instead does the whole tech, allowing for it to also adversely impact the enemy populice
        /// </summary>

        //KickStart Variables
        //public static bool ResetPlayerScale = false;      // Automatically downscale Tech when building?
        //public static bool PreventLogSpam = false;        // Disable non-player-controlled-Tech to disable log spamming

        //World Variables
        private static int handoffUpdate = 0;               //How many Techs are still updating
        private static int handoffUpdateLoopChecker = 0;    //How many Techs are still updating
        private static int handoffUpdateLoopLast = 0;       //How many Techs are still updating

        public static bool CriticalError = false;           // IS A CRASH LIKELY
        public static bool Overburdened = false;            // is there well over 64 techs for unknown reasons?
        public static bool ForceUpdateManWheels = false;    // do we need to update all wheels 
        private static int enemyCount = 0;                  // Keep track of enemies
        public static int lastEnemyCount = 0;               // how many enemies were there last

        private static bool ControlBlockWarningIssued = false; // Issue the warning once


        public static void Initiate()
        {
        }
        public void Update()
        {
            // Check if all techs were updated
            if (handoffUpdateLoopChecker >= ManTechs.inst.Count)
            {
                lastEnemyCount = enemyCount;
                enemyCount = 0;

                // Loop this again as we are not finished yet
                if (handoffUpdateLoopLast != ManTechs.inst.Count)
                {
                    handoffUpdateLoopLast = ManTechs.inst.Count;
                    Debug.Log("ScaleTechs: Tech count changed! performing additional loop!");
                    handoffUpdate = ManTechs.inst.Count;
                }
                else if (handoffUpdate > 0 && Overburdened == true)
                {
                    Debug.Log("ScaleTechs: Handoff loop finished with " + handoffUpdate + " tasks remaining for " + ManTechs.inst.Count + " Techs, looping again...");
                    handoffUpdate = ManTechs.inst.Count;
                }
                //else
                //Debug.Log("ScaleTechs: All rescaling tasks complete.");
                handoffUpdateLoopChecker = 0;
            }


            //onto the next tech
            handoffUpdateLoopChecker++;//Indicate that a tech has been cycled
        }
    }
}