using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScaleableTechs
{

    public class RescaleableTank : MonoBehaviour
    {
        //Attach this to sync all collider removals on a Tech
        public Tank tank;

        public bool NeedsUpdate = false;

        public bool isAnchored = false;
        public bool isThisTony = false;
        public bool lastExternalRequestState = false;

        // Pip Particles variables
        // - Basics
        public bool isPipDynamic = false;
        public float DynamicScale = 1f;
        public bool isPipLocked = false;
        public float savedPipValue = 1f;
        public float maxPipRange = 1f;
        public float minPipRange = 1f;

        // - Anchored
        public bool isPipDynamicA = false;
        public float DynamicScaleA = 1f;
        public bool isPipLockedA = false;
        public float savedPipValueA = 1f;
        public float maxPipRangeA = 1f;
        public float minPipRangeA = 1f;



        // Self-updating variables
        private float AimedScale;//What we automatically try to rescale to while RescaleUpdate() is active
        private float LocalAimedScale = 1f;
        private bool ControlBlockBotched = false; // Let the player know 
        private Vector3 COM;
        private Vector3 inertiaTensorSav;

        private float randomScale = 0f;
        private float lastBlockCount = 1f;
        private bool pendingUpdate = false;
        private float lastGlobalAimedScale;
        private bool lastAnchorState;


        //RescaleUpdate Variables
        private static float snapThreshold = scalingSpeed + 0.001f;
        private static float COMUpdateThreshold = 0.1f;
        private static float COMUpdate = 0.0f;
        private static float COMDragValue = 0f;
        //private static float trueHeightValue = 0f;
        private string lastName = "null";
        private float lastBestValue = 0f;
        private int lastTeam = 0;//how fast we rescale


        // Update handling variables
        public static float scalingSpeed = 0.005f;//how fast we rescale
                                                  //private static float redetermineThreshold = 0.05f;//When do we want to update all blocks on Tech again?
        public static int TechQueue = 0;//how fast we rescale


        public void Subscribe(Tank tank)
        {
            this.tank = tank;
            tank.AttachEvent.Subscribe(AddBlock);
            tank.DetachEvent.Subscribe(RemoveBlock);
            tank.AnchorEvent.Subscribe(OnAnchor);
            tank.PostSpawnEvent.Subscribe(OnSwitch);
            tank.TankRecycledEvent.Subscribe(OnRecycle);
        }
        public void AddBlock(TankBlock tankblock, Tank tank)
        {
            tankblock.GetComponent<ModuleScaleWithSize>().rescaleableTank = this;
            UpdateAllModuleScaleWithSize(true);
        }

        public void RemoveBlock(TankBlock tankblock, Tank tank)
        {
            tankblock.GetComponent<ModuleScaleWithSize>().rescaleableTank = null;
        }

        public void OnAnchor(ModuleAnchor moduleAnchor, bool unUsed, bool unUsed2)
        {
            var ThisInst = tank.GetComponent<RescaleableTank>();
            ThisInst.lastAnchorState = true;
            //Debug.Log("AnchorUpdate");
        }


        public void OnSwitch()
        {   //RESET ALL 
            var ThisInst = tank.GetComponent<RescaleableTank>();
            RescaleEntireTech(1f);
            ThisInst.AimedScale = 1f;
            ThisInst.LocalAimedScale = 1f;
            ThisInst.isPipDynamic = false;
            ThisInst.isPipDynamicA = false;
            ThisInst.isPipLocked = false;
            ThisInst.isPipLockedA = false;
            ThisInst.DynamicScale = 1f;
            ThisInst.DynamicScaleA = 1f;
            ThisInst.maxPipRange = 1f;
            ThisInst.minPipRange = 1f;
            ThisInst.maxPipRangeA = 1f;
            ThisInst.minPipRangeA = 1f;
            ThisInst.randomScale = 0f;
            ThisInst.ControlBlockBotched = false;
        }
        public void OnRecycle(Tank tank)
        {   //RESET ALL
            OnSwitch();
            Debug.Log("OnRecycle - " + tank.name);
        }

        public void TankIsScrewed()
        {   //oh, did I mention that fixing oddly-scaled Control Blocks clusterbodies is near impossible?
            //if (KickStart.AttemptRecovery == false)
            //    RescaleEntireTech(1f);//GET BACK TO NORMAL ASAP
            var ThisInst = tank.GetComponent<RescaleableTank>();
            ThisInst.ControlBlockBotched = true;
            CriticalError = true;
        }

        public void PipReceived(float valueIn, bool isDynamic, bool isStrong, bool isAnchored, float newMin, float newMax)
        {   //When a Pip is thrown on the tech
            var ThisInst = tank.GetComponent<RescaleableTank>();
            if (isAnchored)
            {
                if (isStrong)
                {
                    ThisInst.isPipLockedA = true;
                    ThisInst.savedPipValueA = valueIn;
                    //Debug.Log("ScaleTechs: Logged ANCHORED Strong savedPipValue " + savedPipValue);
                }
                else if (isDynamic && ThisInst.isPipLockedA == false)
                {
                    ThisInst.isPipDynamicA = isDynamic;
                    if (ThisInst.maxPipRangeA <= newMax)
                        ThisInst.maxPipRangeA = newMax;
                    if (ThisInst.minPipRangeA >= newMin)
                        ThisInst.minPipRangeA = newMin;
                }
                else if (ThisInst.isPipDynamicA == false && ThisInst.isPipLockedA == false)
                {
                    ThisInst.savedPipValueA = valueIn;
                    //Debug.Log("ScaleTechs: Logged ANCHORED savedPipValue " + savedPipValue);
                }
            }
            else
            {
                if (isStrong)
                {
                    ThisInst.isPipLocked = true;
                    ThisInst.savedPipValue = valueIn;
                    //Debug.Log("ScaleTechs: Logged Strong savedPipValue " + savedPipValue);
                }
                else if (isDynamic && ThisInst.isPipLocked == false)
                {
                    ThisInst.isPipDynamic = true;
                    if (ThisInst.maxPipRange <= newMax)
                        ThisInst.maxPipRange = newMax;
                    if (ThisInst.minPipRange >= newMin)
                        ThisInst.minPipRange = newMin;
                }
                else if (ThisInst.isPipDynamic == false && ThisInst.isPipLocked == false)
                {
                    ThisInst.savedPipValue = valueIn;
                    //Debug.Log("ScaleTechs: Logged savedPipValue " + savedPipValue);

                }
            }
        }

        public void SetDynamicScale(float set, bool anchored)
        {   // Save/set the dynamic scale in the current instance of the tank
            var ThisInst = tank.GetComponent<RescaleableTank>();
            if (anchored)
                ThisInst.DynamicScaleA = set;
            else
                ThisInst.DynamicScale = set;
            ThisInst.lastExternalRequestState = true;
        }


        private void COMReCalc()
        {
            // Update this on each block addition
            //   Scale to the default to get tailored COM

            float localVariable = tank.transform.localScale.x;
            //Debug.Log("ScaleTechs: Current COM is " + tank.transform.GetComponent<Rigidbody>().centerOfMass + " on " + tank.name);
            //Debug.Log("ScaleTechs: Current inertia tensor is " + tank.transform.GetComponent<Rigidbody>().inertiaTensor + " on " + tank.name);
            RescaleEntireTech(1f);

            //FETCH CENTER OF MASS
            tank.ResetPhysics();
            COM = tank.CenterOfMass;
            //Debug.Log("ScaleTechs: Grabbed natural COM " + COM + " on " + tank.name);
            inertiaTensorSav = gameObject.transform.GetComponent<Rigidbody>().inertiaTensor;
            //Debug.Log("ScaleTechs: Grabbed natural inertia tensor " + inertiaTensorSav + " on " + tank.name);


            COMDragValue = tank.transform.GetComponent<Rigidbody>().drag;
            //now actually SCAAAAALE BAAAACK 
            RescaleEntireTech(localVariable);
        }

        private void AttemptCOMRebalance()
        {
            //Fairly demanding - Update this on each block addition
            //Scale to the default to get tailored COM

            float localVariable = tank.transform.localScale.x;
            Vector3 ReCalcCOM;
            ReCalcCOM = COM * localVariable;
            tank.transform.GetComponent<Rigidbody>().centerOfMass = ReCalcCOM;

            //Debug.Log("ScaleTechs: Force synced COM on " + tank.name + " to " + COM + " Yielding " + tank.transform.GetComponent<Rigidbody>().centerOfMass);
            ReCalcCOM = inertiaTensorSav * localVariable;
            tank.transform.GetComponent<Rigidbody>().inertiaTensor = ReCalcCOM;
            //Debug.Log("ScaleTechs: Force synced COM on " + tank.name + " to " + inertiaTensorSav + " Yielding " + tank.transform.GetComponent<Rigidbody>().inertiaTensor);

            tank.transform.GetComponent<Rigidbody>().drag = COMDragValue * (localVariable / 2 + 0.5f);
        }

        public void RescaleEntireTech(float localVariable)
        {
            //now actually SCAAAAALE
            Vector3 reScale = tank.transform.localScale;
            reScale = Vector3.one * localVariable;
            tank.transform.localScale = reScale;
            //LOL THIS WORKS
        }


        private static bool updateScaleRequestBool = false;
        private static float updateScaleRequest = 0;
        private static float updateScaleRequestPhy = 0;

        public void FixedUpdate()
        {   //Speed up animation if nesseary according the ingame physics
            if (updateScaleRequestBool)
            {
                updateScaleRequestPhy++;
            }
            if (updateScaleRequestPhy >= 25)
            {
                scalingSpeed = 15625 / (updateScaleRequest * updateScaleRequest * updateScaleRequest) * 0.01f;// Speed up if nesseary
                snapThreshold = scalingSpeed + 0.0025f;
                COMUpdateThreshold = scalingSpeed * 10;
                //Debug.Log("ScaleTechs: changed scalingSpeed to " + scalingSpeed);
                updateScaleRequestBool = false;
                updateScaleRequest = 0;
                updateScaleRequestPhy = 0;
            }
        }
        public void UpdateTank()
        {
            //  Big or small, does them all!
            //Get the current instance
            var ThisInst = tank.GetComponent<RescaleableTank>();
            ThisInst.isAnchored = tank.IsAnchored;

            //Run the numbers
            float localVariable = tank.transform.localScale.x;
            ThisInst.AimedScale = KickStart.GlobalAimedScale;

            //update checking
            if (updateScaleRequestBool)
                updateScaleRequest++;


            //Update if anything changes
            if (ManPauseGame.inst.IsPauseMenuShowing() == false)
            {
                if (tank.FirstUpdateAfterSpawn || ThisInst.lastBlockCount != tank.blockman.blockCount || KickStart.GlobalAimedScale != ThisInst.lastGlobalAimedScale || ThisInst.lastAnchorState || handoffUpdateLoopLast != ManTechs.inst.Count || lastExternalRequestState || tank.Team != ThisInst.lastTeam || ThisInst.lastName != tank.name)
                {
                    Debug.Log("ScaleTechs: Update requested - " + KickStart.GlobalAimedScale + lastGlobalAimedScale + " || " + tank.blockman.blockCount + ThisInst.lastBlockCount + " || " + ThisInst.lastAnchorState);
                    if (handoffUpdateLoopLast != ManTechs.inst.Count)
                        handoffUpdate = ManTechs.inst.Count;
                    ThisInst.lastBlockCount = tank.blockman.blockCount;
                    ThisInst.lastGlobalAimedScale = KickStart.GlobalAimedScale;
                    ThisInst.lastTeam = tank.Team;
                    ThisInst.lastAnchorState = false;
                    ThisInst.pendingUpdate = true;
                    if (ThisInst.lastName != tank.name)
                        OnSwitch(); //Because none of the subscribeables trigger on tech swap
                    ThisInst.lastName = tank.name;
                }


                //Handle player updates for autoscaling
                if (KickStart.ResetPlayerScale && tank.PlayerFocused == true)
                {
                    if (tank.beam.IsActive)
                    {
                        //Debug.Log("ScaleTechs: BUILD TIMER " + lastBuildTime);
                        if (ThisInst.AimedScale != localVariable)
                        {
                            RescaleUpdate(localVariable);
                        }
                    }
                    else
                    {
                        var gamName = ManGameMode.inst.GetCurrentGameMode();
                        if (gamName == "ModeMain" || gamName == "ModeMisc")
                            ThisInst.pendingUpdate = true;
                    }
                }
                ThisInst.lastExternalRequestState = false;

                if (handoffUpdate > 0 || ThisInst.pendingUpdate)
                {

                    // Now handle the important stuff
                    GetAllBlocksInfo();

                    if (ThisInst.ControlBlockBotched == true && KickStart.AttemptRecovery == false)
                    {
                        localVariable = 1f;
                        RescaleEntireTech(1f);
                        ThisInst.AimedScale = 1f;
                        if (ControlBlockWarningIssued == false)
                        {
                            UpdateAllModuleScaleWithSize(true);
                            CriticalError = true;
                            ControlBlockWarningIssued = true;
                            Debug.Log("\n-------------------------------------------------------------------------------");
                            Debug.Log("\n!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                            Debug.Log("|!| ScaleTechs: LOCKDOWN ON " + tank.name + " AS IT CONTAINS CONTROL BLOCKS |!|");
                            Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                            Debug.Log("\n-------------------------------------------------------------------------------\n");
                        }
                    }
                    else if (ThisInst.DynamicScaleA != 1f && ThisInst.isPipDynamicA && tank.IsAnchored && ThisInst.isPipLockedA == false)
                    {   //Is the tech anchored and have an anchored-only dynamic pip?
                        ThisInst.LocalAimedScale = ThisInst.DynamicScaleA;

                        //Apply sizes
                        ThisInst.AimedScale = ThisInst.LocalAimedScale;
                        //Debug.Log("ScaleTechs: Synced ANCHORED DYNAMIC Pip scaling to Local Scale " + LocalAimedScale + " on " + tank.name);
                        if (tank.FirstUpdateAfterSpawn)
                        {
                            //TECH WAS LIKELY IN THE WORLD AND MUST MAINTAIN SCALE IMMEDEATELY TO PREVENT ISSUES
                            localVariable = ThisInst.LocalAimedScale;
                            RescaleEntireTech(localVariable);
                            UpdateAllModuleScaleWithSize(true);
                            Debug.Log("ScaleTechs: First ANCHORED DYNAMIC Pip Particles update after spawn for " + tank.name);
                        }
                    }
                    else if (ThisInst.DynamicScale != 1f && ThisInst.isPipDynamic && ThisInst.isPipLocked == false)
                    {   //Is the tech anchored and have a dynamic pip?
                        ThisInst.LocalAimedScale = ThisInst.DynamicScale;

                        //Apply sizes
                        ThisInst.AimedScale = ThisInst.LocalAimedScale;
                        //Debug.Log("ScaleTechs: Synced DYNAMIC Pip scaling to Local Scale " + LocalAimedScale + " on " + tank.name);
                        if (tank.FirstUpdateAfterSpawn)
                        {
                            //TECH WAS LIKELY IN THE WORLD AND MUST MAINTAIN SCALE IMMEDEATELY TO PREVENT ISSUES
                            localVariable = ThisInst.LocalAimedScale;
                            RescaleEntireTech(localVariable);
                            UpdateAllModuleScaleWithSize(true);
                            Debug.Log("ScaleTechs: First DYNAMIC Pip Particles update after spawn for " + tank.name);
                        }
                    }
                    else if (ThisInst.savedPipValueA != 1f && tank.IsAnchored)
                    {   //Is the tech anchored and have an anchored-only pip?
                        ThisInst.LocalAimedScale = ThisInst.savedPipValueA;

                        //Apply sizes
                        ThisInst.AimedScale = ThisInst.LocalAimedScale;
                        //Debug.Log("ScaleTechs: Synced ANCHORED Pip scaling to Local Scale " + LocalAimedScale + " on " + tank.name);
                        if (tank.FirstUpdateAfterSpawn)
                        {
                            //TECH WAS LIKELY IN THE WORLD AND MUST MAINTAIN SCALE IMMEDEATELY TO PREVENT ISSUES
                            localVariable = ThisInst.LocalAimedScale;
                            RescaleEntireTech(localVariable);
                            UpdateAllModuleScaleWithSize(true);
                            Debug.Log("ScaleTechs: First ANCHORED Pip Particles update after spawn for " + tank.name);
                        }
                    }
                    else if (ThisInst.savedPipValue != 1f)
                    {   //Does this tank have a valid pip?
                        ThisInst.LocalAimedScale = ThisInst.savedPipValue;

                        //Apply sizes
                        ThisInst.AimedScale = ThisInst.LocalAimedScale;
                        //Debug.Log("ScaleTechs: Synced Pip scaling to Local Scale " + LocalAimedScale + " on " + tank.name);
                        if (tank.FirstUpdateAfterSpawn)
                        {
                            //TECH WAS LIKELY IN THE WORLD AND MUST MAINTAIN SCALE IMMEDEATELY TO PREVENT ISSUES
                            localVariable = ThisInst.LocalAimedScale;
                            RescaleEntireTech(localVariable);
                            UpdateAllModuleScaleWithSize(true);
                            Debug.Log("ScaleTechs: First Pip Particles update after spawn for " + tank.name);
                        }
                    }
                    else
                    {
                        // Else if this is a tech we check to rescale it to the global value
                        //Debug.Log("ScaleTechs: Synced scaling to GLOBAL Scale " + KickStart.GlobalAimedScale + " on " + tank.name);
                        if (KickStart.RandomEnemyScales && tank.IsEnemy())
                        {
                            if (tank.FirstUpdateAfterSpawn)
                            {
                                //It's likely a fresh new tech or newly loaded in - reset scaling to cycle scale
                                localVariable = 1f;
                                ThisInst.randomScale = 0f;
                                RescaleEntireTech(localVariable);
                                UpdateAllModuleScaleWithSize(true);
                                Debug.Log("ScaleTechs: First update after spawn for " + tank.name);
                            }
                            if (ThisInst.randomScale == 0f)
                            {
                                ThisInst.randomScale = UnityEngine.Random.Range(0.6f, 1.8f);
                                Debug.Log("ScaleTechs: Tank " + tank.name + " has been rescaled to " + ThisInst.randomScale);
                            }
                            ThisInst.AimedScale = ThisInst.randomScale;
                            enemyCount++;
                        }
                        else if (tank.FirstUpdateAfterSpawn)
                        {
                            //It's likely a fresh new tech or newly loaded in - reset scaling to cycle scale
                            localVariable = 1f;
                            RescaleEntireTech(localVariable);
                            UpdateAllModuleScaleWithSize(true);
                            Debug.Log("ScaleTechs: First update after spawn for " + tank.name);
                        }
                    }
                    if (tank.name == "Big Tony" && tank.IsEnemy())
                    {
                        ThisInst.AimedScale = 2f;
                        if (ThisInst.isThisTony == false)
                        {
                            ThisInst.isThisTony = true;
                            Debug.Log("ScaleTechs: Tony detected! Upgrading!");
                            handoffUpdate = ManTechs.inst.Count;
                            localVariable = 1f;
                            RescaleEntireTech(localVariable);
                        }
                    }
                    else
                        ThisInst.isThisTony = false;

                    //Do we want to only change player Tech?
                    if (KickStart.dontPreventLogSpam == false && tank.PlayerFocused == false)
                        ThisInst.AimedScale = 1f;


                    //Check if the Tech contains a Pip Particles module, if so the rescale based on saved value in TankBlock (retrived from community of Pip Blocks)
                    //  If the state is not teh same as laststate
                    if (ThisInst.AimedScale != localVariable)
                        RescaleUpdate(localVariable);
                    else
                    {
                        handoffUpdate--;//Let the system know we are done updating
                        if (ThisInst.pendingUpdate == true)
                        {
                            COMReCalc();
                            AttemptCOMRebalance();
                            UpdateAllModuleScaleWithSize(true);
                            ThisInst.pendingUpdate = false;
                        }
                    }
                }
            }
            else
            {
                //Debug.Log("ScaleTechs: GAME COUNTS AS PAUSED!");
            }

            // Check if all techs were updated
            if (handoffUpdateLoopChecker >= ManTechs.inst.Count)
            {
                updateScaleRequestBool = true;
                lastEnemyCount = enemyCount;
                enemyCount = 0;
                TechQueue = 0;

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
            TechQueue++;
        }


        private void RescaleUpdate(float localVariable)
        {
            var ThisInst = tank.GetComponent<RescaleableTank>();
            if (handoffUpdate > 0)
                handoffUpdate--;//Let the system know we have stepped an update
            Vector3 ReCalcHeight;
            bool updateGO = false;
            if (((COMUpdate - COMUpdateThreshold) < localVariable && localVariable < (COMUpdate + COMUpdateThreshold)) && localVariable != COMUpdate)
            {//UPDATE COM
                COMUpdate = localVariable;
                COMReCalc();
                AttemptCOMRebalance();
                UpdateAllModuleScaleWithSize(false, localVariable);
                Debug.Log("ScaleTechs: COM Update for " + tank.name);
                updateGO = true;
            }
            if ((AimedScale - snapThreshold) < localVariable && localVariable < (AimedScale + snapThreshold) && localVariable != AimedScale)
            {//SNAP TO EXACT
                COMUpdate = localVariable;
                float snapDist = AimedScale - localVariable;
                localVariable = AimedScale;
                COMReCalc();
                AttemptCOMRebalance();
                UpdateAllModuleScaleWithSize(true);
                updateGO = true;

                //Offset off the ground accordingly
                ReCalcHeight = tank.transform.GetComponent<Rigidbody>().position;
                Debug.Log("ScaleTechs: InitialPosition of " + tank.name + " was " + ReCalcHeight.y);
                if (snapDist > 0)
                    ReCalcHeight.y = Mathf.Round((ReCalcHeight.y + (-lastBestValue * (snapDist * localVariable))) * 4) / 4;//We snap to prevent odd floating-point errors
                else if (snapDist < 0)
                    ReCalcHeight.y = Mathf.Round((ReCalcHeight.y - (-lastBestValue * (-snapDist * localVariable))) * 4) / 4;//We snap to prevent odd floating-point errors
                tank.transform.GetComponent<Rigidbody>().position = ReCalcHeight;
                Debug.Log("ScaleTechs: FinalPosition of " + tank.name + " is " + ReCalcHeight.y);


                Debug.Log("ScaleTechs: Finalizing updates for " + tank.name);
                ThisInst.pendingUpdate = false;
            }
            else if (localVariable < AimedScale)//GROW
            {
                //LERP IN SIZE
                localVariable = localVariable + (scalingSpeed * localVariable);
                updateGO = true;

                //Offset off the ground accordingly
                ReCalcHeight = tank.transform.GetComponent<Rigidbody>().position;
                ReCalcHeight.y = ReCalcHeight.y + (-lastBestValue * (scalingSpeed * localVariable));
                tank.transform.GetComponent<Rigidbody>().position = ReCalcHeight;
                ThisInst.pendingUpdate = true;
            }
            else if (localVariable > AimedScale)//SHRINK
            {
                //LERP IN SIZE
                localVariable = localVariable - (scalingSpeed * localVariable);
                updateGO = true;

                //Offset off the ground accordingly
                ReCalcHeight = tank.transform.GetComponent<Rigidbody>().position;
                ReCalcHeight.y = ReCalcHeight.y - (-lastBestValue * (scalingSpeed * localVariable));
                tank.transform.GetComponent<Rigidbody>().position = ReCalcHeight;
                ThisInst.pendingUpdate = true;
            }
            else //We are at EXACTLY ideal scale
            {
                Debug.Log("ScaleTechs: Extra updates for " + tank.name);
                if (ThisInst.pendingUpdate == true)
                {
                    COMReCalc();
                    AttemptCOMRebalance();
                    UpdateAllModuleScaleWithSize(true);
                    ThisInst.pendingUpdate = false;
                }
            }
            if (updateGO)
                RescaleEntireTech(localVariable);
        }

        private void GetAllBlocksInfo()
        {
            //Debug.Log("ScaleTechs: Updating Modules...");
            ControlBlockBotched = false;
            try
            {
                float bestValue = 0;
                int child = tank.transform.childCount;
                for (int v = 0; v < child; ++v)
                {
                    Transform grabbedGameObject = tank.transform.GetChild(v);
                    try
                    {
                        bestValue = grabbedGameObject.GetComponent<ModuleScaleWithSize>().GetInfo();
                        if (lastBestValue > bestValue && lastBestValue >= -64)
                        {
                            lastBestValue = bestValue;
                            //Debug.Log("ScaleTechs: new BestValue is " + lastBestValue + "!");
                        }
                    }
                    catch
                    {
                        //Debug.Log("ScaleTechs: FetchFailiure on " + grabbedGameObject.name + "!");
                    }
                }
            }
            catch
            {
                Debug.Log("ScaleTechs: FetchFailiure on " + tank.name + "!");
            }
            //Debug.Log("ScaleTechs: lastBestValue is " + lastBestValue + "!");
        }

        private void UpdateAllModuleScaleWithSize(bool resetScales)
        {
            //GET CURRENT VALUES
            UpdateFreakingWheels.GrabCurrent();
            try
            {
                int child = tank.transform.childCount;
                for (int v = 0; v < child; ++v)
                {
                    Transform grabbedGameObject = tank.transform.GetChild(v);
                    try
                    {
                        grabbedGameObject.GetComponent<ModuleScaleWithSize>().ScaleAccordingly(AimedScale, COM, isThisTony);
                        if (resetScales)
                            grabbedGameObject.GetComponent<ModuleScaleWithSize>().ResetScale();
                    }
                    catch
                    {
                        //Debug.Log(grabbedGameObject + " is not a block");
                        if (resetScales && grabbedGameObject.name == "")
                        {
                            grabbedGameObject.transform.localScale = Vector3.one;
                        }
                    }
                }
                //Debug.Log("ScaleTechs: Rescaled components of " + tank.name + "!");
            }
            catch
            {
                Debug.Log("ScaleTechs: FetchFailiure on " + tank.name + "!");
            }
        }
        private void UpdateAllModuleScaleWithSize(bool resetScales, float input)
        {
            //GET CURRENT VALUES
            UpdateFreakingWheels.GrabCurrent();
            try
            {
                int child = tank.transform.childCount;
                for (int v = 0; v < child; ++v)
                {
                    Transform grabbedGameObject = tank.transform.GetChild(v);
                    try
                    {
                        grabbedGameObject.GetComponent<ModuleScaleWithSize>().ScaleAccordingly(input, COM, isThisTony);
                        if (resetScales)
                            grabbedGameObject.GetComponent<ModuleScaleWithSize>().ResetScale();
                    }
                    catch
                    {
                        //Debug.Log(grabbedGameObject + " is not a block");
                        if (resetScales && grabbedGameObject.name == "")
                        {
                            grabbedGameObject.transform.localScale = Vector3.one;
                        }
                    }
                }
                //Debug.Log("ScaleTechs: Rescaled components of " + tank.name + "!");
            }
            catch
            {
                Debug.Log("ScaleTechs: FetchFailiure on " + tank.name + "!");
            }
        }
    }
}
