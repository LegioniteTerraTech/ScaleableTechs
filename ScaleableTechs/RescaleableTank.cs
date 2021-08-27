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

        public bool isThisTony = false;

        // Pip Particles variables
        // - Basics
        public bool HasPip = false;
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
        private float CurrentScale = 1f;//What we automatically try to rescale to while RescaleUpdate() is active
        private float AimedScale = 1f;
        private bool ControlBlockBotched = false; // Let the player know 
        private Vector3 COM;
        private Vector3 inertiaTensorSav;

        private float randomScale = 0f;
        private float lastBlockCount = 1f;
        private float lastGlobalAimedScale;
        private bool lastAnchorState;


        // Update handling variables
        const float scaleRate = 0.5f;
        private float scalingSpeed { get { return Time.fixedDeltaTime * scaleRate; } }
        private float snapThreshold { get { return scalingSpeed; } }
        private float COMDragValue = 1;
        //private static float trueHeightValue = 0f;
        private float lastBestValue = 0f;


        public void Subscribe(Tank tank)
        {
            this.tank = tank;
            tank.AttachEvent.Subscribe(OnAttach);
            tank.DetachEvent.Subscribe(OnDetach);
            //tank.AnchorEvent.Subscribe(OnAnchor);
            //tank.PostSpawnEvent.Subscribe(OnSpawn);
            tank.TankRecycledEvent.Subscribe(OnRecycle);
        }
        public void OnAttach(TankBlock tankblock, Tank tank)
        {
            if (tank == this.tank)
            {
                tankblock.GetComponent<ModuleScaleWithSize>().rescaleableTank = this;
                UpdateAllModuleScaleWithSize(true);
            }
        }
        public void OnDetach(TankBlock tankblock, Tank tank)
        {
            if (tank == this.tank)
            {
                tankblock.GetComponent<ModuleScaleWithSize>().rescaleableTank = null;
            }
        }
        public void OnAnchor(ModuleAnchor moduleAnchor, bool unUsed, bool unUsed2)
        {
            var ThisInst = tank.GetComponent<RescaleableTank>();
            lastAnchorState = true;
            //Debug.Log("AnchorUpdate");
        }
        public void OnSpawn()
        {   //RESET ALL 
            RescaleEntireTech();
            
            AimedScale = 1f;
            HasPip = false;
            isPipDynamic = false;
            isPipDynamicA = false;
            isPipLocked = false;
            isPipLockedA = false;
            DynamicScale = 1f;
            DynamicScaleA = 1f;
            maxPipRange = 1f;
            minPipRange = 1f;
            maxPipRangeA = 1f;
            minPipRangeA = 1f;
            randomScale = 0f;
            ControlBlockBotched = false;
        }
        public void OnRecycle(Tank tank)
        {   //RESET ALL
            OnSpawn();
            //Debug.Log("OnRecycle - " + tank.name);
        }

        public void TankIsScrewed()
        {   //oh, did I mention that fixing oddly-scaled Control Blocks clusterbodies is near impossible?
            //if (KickStart.AttemptRecovery == false)
            //    RescaleEntireTech(1f);//GET BACK TO NORMAL ASAP
            ControlBlockBotched = true;
            RescaleManager.CriticalError = true;
        }

        public void PipReceived(float valueIn, bool isDynamic, bool isStrong, bool isAnchored, float newMin, float newMax)
        {   //When a Pip is thrown on the tech
            HasPip = true;
            if (isAnchored)
            {
                if (isStrong)
                {
                    isPipLockedA = true;
                    savedPipValueA = valueIn;
                    //Debug.Log("ScaleTechs: Logged ANCHORED Strong savedPipValue " + savedPipValue);
                }
                else if (isDynamic && isPipLockedA == false)
                {
                    isPipDynamicA = isDynamic;
                    if (maxPipRangeA <= newMax)
                        maxPipRangeA = newMax;
                    if (minPipRangeA >= newMin)
                        minPipRangeA = newMin;
                }
                else if (isPipDynamicA == false && isPipLockedA == false)
                {
                    savedPipValueA = valueIn;
                    //Debug.Log("ScaleTechs: Logged ANCHORED savedPipValue " + savedPipValue);
                }
            }
            else
            {
                if (isStrong)
                {
                    isPipLocked = true;
                    savedPipValue = valueIn;
                    //Debug.Log("ScaleTechs: Logged Strong savedPipValue " + savedPipValue);
                }
                else if (isDynamic && isPipLocked == false)
                {
                    isPipDynamic = true;
                    if (maxPipRange <= newMax)
                        maxPipRange = newMax;
                    if (minPipRange >= newMin)
                        minPipRange = newMin;
                }
                else if (isPipDynamic == false && isPipLocked == false)
                {
                    savedPipValue = valueIn;
                    //Debug.Log("ScaleTechs: Logged savedPipValue " + savedPipValue);

                }
            }
        }

        public void SetDynamicScale(float set, bool anchored)
        {   // Save/set the dynamic scale in the current instance of the tank
            if (anchored)
                DynamicScaleA = set;
            else
                DynamicScale = set;
        }


        private void COMReCalc()
        {
            // Update this on each block addition
            //   Scale to the default to get tailored COM
            if (!(bool)tank.rbody)
                return;

            float localVariable = tank.transform.localScale.x;
            //Debug.Log("ScaleTechs: Current COM is " + tank.transform.GetComponent<Rigidbody>().centerOfMass + " on " + tank.name);
            //Debug.Log("ScaleTechs: Current inertia tensor is " + tank.transform.GetComponent<Rigidbody>().inertiaTensor + " on " + tank.name);
            RescaleEntireTech(1f);

            //FETCH CENTER OF MASS
            tank.ResetPhysics();
            COM = tank.CenterOfMass;
            //Debug.Log("ScaleTechs: Grabbed natural COM " + COM + " on " + tank.name);
            inertiaTensorSav = tank.rbody.inertiaTensor;
            //Debug.Log("ScaleTechs: Grabbed natural inertia tensor " + inertiaTensorSav + " on " + tank.name);


            COMDragValue = tank.rbody.drag;
            //now actually SCAAAAALE BAAAACK 
            RescaleEntireTech(localVariable);
        }

        private void AttemptCOMRebalance()
        {
            //Fairly demanding - Update this on each block addition
            //Scale to the default to get tailored COM
            if (!(bool)tank.rbody)
                return;

            float localVariable = tank.transform.localScale.x;
            Vector3 ReCalcCOM;
            ReCalcCOM = COM * localVariable;
            tank.rbody.centerOfMass = ReCalcCOM;

            //Debug.Log("ScaleTechs: Force synced COM on " + tank.name + " to " + COM + " Yielding " + tank.transform.GetComponent<Rigidbody>().centerOfMass);
            ReCalcCOM = inertiaTensorSav * localVariable;
            tank.rbody.inertiaTensor = ReCalcCOM;
            //Debug.Log("ScaleTechs: Force synced COM on " + tank.name + " to " + inertiaTensorSav + " Yielding " + tank.transform.GetComponent<Rigidbody>().inertiaTensor);

            tank.rbody.drag = COMDragValue * (localVariable / 2 + 0.5f);
        }

        public void RescaleEntireTech(float localVariable = 1)
        {
            //now actually SCAAAAALE
            tank.transform.localScale = Vector3.one * localVariable;
            //LOL THIS WORKS
        }



        public void RequestUpdate()
        {
            RescaleManager.QueueUpdater(tank);
        }
        public bool GetNeedsUpdate()
        {
            //Update if anything changes
            CurrentScale = tank.transform.localScale.x;

            if (lastBlockCount != tank.blockman.IterateBlocks().Count() || KickStart.GlobalAimedScale != lastGlobalAimedScale || tank.IsAnchored  != lastAnchorState)
            {
                Debug.Log("ScaleTechs: Update requested - " + KickStart.GlobalAimedScale + "|" + lastGlobalAimedScale + " || " + tank.blockman.blockCount + "|" + lastBlockCount + " || " + lastAnchorState);
                lastBlockCount = tank.blockman.blockCount;
                lastGlobalAimedScale = KickStart.GlobalAimedScale;
                lastAnchorState = tank.IsAnchored;
                NeedsUpdate = true;
                return true;
            }

            return false;
        }
        public void GetAimedScale()
        {
            // Get the scale to reach for
            if (KickStart.ResetPlayerScale && Singleton.playerTank == tank && tank.beam.IsActive)
                AimedScale = 1;
            else if (HasPip)
            {
                if (isPipDynamicA && tank.IsAnchored && isPipLockedA == false)
                {   //Is the tech anchored and have an anchored-only dynamic pip?
                    AimedScale = DynamicScaleA;
                }
                else if (isPipDynamic && isPipLocked == false)
                {   //Is the tech anchored and have a dynamic pip?
                    AimedScale = DynamicScale;
                }
                else if (savedPipValueA != 1f && tank.IsAnchored)
                {   //Is the tech anchored and have an anchored-only pip?
                    AimedScale = savedPipValueA;
                }
                else
                {   //Does this tank have a valid pip?
                    AimedScale = savedPipValue;
                }
            }
            else
                AimedScale = KickStart.GlobalAimedScale;


        }
        public bool UpdateTank()
        {
            //  Big or small, does them all!

            //Run the numbers
            CurrentScale = tank.transform.localScale.x;

            GetAimedScale();

            // Now handle the important stuff
            //GetAllBlocksInfo();

            if (tank.name == "Big Tony" && tank.IsEnemy())
            {
                AimedScale = 2f;
                if (isThisTony == false)
                {
                    isThisTony = true;
                    //Debug.Log("ScaleTechs: Tony detected! Upgrading!");
                    RescaleEntireTech();
                }
            }
            else
            {
                if (ControlBlockBotched == true && KickStart.AttemptRecovery == false)
                {
                    RescaleEntireTech(1f);
                    AimedScale = 1f;
                    if (RescaleManager.ControlBlockWarningIssued == false)
                    {
                        UpdateAllModuleScaleWithSize(true);
                        RescaleManager.CriticalError = true;
                        RescaleManager.ControlBlockWarningIssued = true;
                        Debug.Log("\n-------------------------------------------------------------------------------");
                        Debug.Log("\n!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                        Debug.Log("|!| ScaleTechs: LOCKDOWN ON " + tank.name + " AS IT CONTAINS CONTROL BLOCKS |!|");
                        Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                        Debug.Log("\n-------------------------------------------------------------------------------\n");
                    }
                }
                else if (HasPip)
                {   //Is the tech anchored and have an anchored-only dynamic pip?
                    if (tank.FirstUpdateAfterSpawn)
                    {
                        //TECH WAS LIKELY IN THE WORLD AND MUST MAINTAIN SCALE IMMEDEATELY TO PREVENT ISSUES
                        RescaleEntireTech(AimedScale);
                        UpdateAllModuleScaleWithSize(true);
                        //Debug.Log("ScaleTechs: First ANCHORED DYNAMIC Pip Particles update after spawn for " + tank.name);
                    }
                }
                else
                {   // Else if this is a tech we check to rescale it to the global value
                    //Debug.Log("ScaleTechs: Synced scaling to GLOBAL Scale " + KickStart.GlobalAimedScale + " on " + tank.name);
                    if (KickStart.RandomEnemyScales && tank.IsEnemy())
                    {
                        if (tank.FirstUpdateAfterSpawn)
                        {
                            //It's likely a fresh new tech or newly loaded in - reset scaling to cycle scale
                            randomScale = 0f;
                            RescaleEntireTech();
                            UpdateAllModuleScaleWithSize(true);
                            //Debug.Log("ScaleTechs: First update after spawn for " + tank.name);
                        }
                        if (randomScale == 0f)
                        {
                            randomScale = UnityEngine.Random.Range(0.6f, 1.8f);
                            //Debug.Log("ScaleTechs: Tank " + tank.name + " has been rescaled to " + randomScale);
                        }
                        AimedScale = randomScale;
                    }
                    else if (tank.FirstUpdateAfterSpawn)
                    {
                        //It's likely a fresh new tech or newly loaded in - reset scaling to cycle scale
                        RescaleEntireTech();
                        UpdateAllModuleScaleWithSize(true);
                        //Debug.Log("ScaleTechs: First update after spawn for " + tank.name);
                    }
                }
            }

            //Do we want to only change player Tech?
            if (KickStart.dontPreventLogSpam == false && tank.PlayerFocused == false)
                AimedScale = 1f;


            //Check if the Tech contains a Pip Particles module, if so the rescale based on saved value in TankBlock (retrived from community of Pip Blocks)
            //  If the state is not teh same as laststate
            if (AimedScale != CurrentScale)
            {
                if (!RescaleUpdate())
                {
                    NeedsUpdate = false;
                    return true;
                }
            }
            else
            {
                NeedsUpdate = false;
                return true;
            }

            return false;
        }


        private bool RescaleUpdate()
        {
            bool updateGO = false;
            bool pendingUpdate = false;

            float ScaleChange = CurrentScale;

            /*
            if (((COMUpdate - COMUpdateThreshold) < CurrentScale && CurrentScale < (COMUpdate + COMUpdateThreshold)) && CurrentScale != COMUpdate)
            {//UPDATE COM
                COMUpdate = CurrentScale;
                COMReCalc();
                AttemptCOMRebalance();
                UpdateAllModuleScaleWithSize(false, CurrentScale);
                Debug.Log("ScaleTechs: COM Update for " + tank.name);
                updateGO = true;
            }*/
            if ((AimedScale - snapThreshold) < CurrentScale && CurrentScale < (AimedScale + snapThreshold) && CurrentScale != AimedScale)
            {//SNAP TO EXACT

                RescaleEntireTech(AimedScale);
                COMReCalc();
                AttemptCOMRebalance();
                UpdateAllModuleScaleWithSize(true);

                //Offset off the ground accordingly
                TryFixupAnchors();

                Debug.Log("ScaleTechs: Finalizing updates for " + tank.name);
                pendingUpdate = false;
            }
            else if (CurrentScale < AimedScale)//GROW
            {
                //LERP IN SIZE
                ScaleChange = CurrentScale + (scalingSpeed * CurrentScale);
                updateGO = true;

                //Offset off the ground accordingly
                TryFixupAnchors();
                pendingUpdate = true;
            }
            else if (CurrentScale > AimedScale)//SHRINK
            {
                //LERP IN SIZE
                ScaleChange = CurrentScale - (scalingSpeed * CurrentScale);
                updateGO = true;

                //Offset off the ground accordingly
                TryFixupAnchors();
                pendingUpdate = true;
            }
            else //We are at EXACTLY ideal scale
            {
                Debug.Log("ScaleTechs: Extra updates for " + tank.name);
                if (pendingUpdate == true)
                {
                    COMReCalc();
                    AttemptCOMRebalance();
                    UpdateAllModuleScaleWithSize(true);
                    pendingUpdate = false;
                }
            }
            if (updateGO)
                RescaleEntireTech(ScaleChange);
            return pendingUpdate;
        }
        private void TryFixupAnchors()
        {
            TechAnchors anchors = tank.Anchors;
            if (anchors)
            {
                if (anchors.NumIsAnchored > 0)
                {
                    tank.visible.Teleport(tank.boundsCentreWorldNoCheck, tank.visible.trans.rotation);
                    if (tank.Anchors.NumAnchored < 1)
                    {
                        tank.FixupAnchors();
                        if (!tank.IsAnchored)
                            tank.TryToggleTechAnchor();
                    }
                }
            }
        }

        private void GetAllBlocksInfo()
        {
            //Debug.Log("ScaleTechs: Updating Modules...");
            ControlBlockBotched = false;
            try
            {
                float bestValue = 0;
                foreach (TankBlock block in tank.blockman.IterateBlocks())
                {
                    var check = block.GetComponent<ModuleScaleWithSize>();
                    if (check)
                    {
                        bestValue = check.GetInfo();
                        if (lastBestValue > bestValue && lastBestValue >= -64)
                        {
                            lastBestValue = bestValue;
                            //Debug.Log("ScaleTechs: new BestValue is " + lastBestValue + "!");
                        }
                    }
                }
            }
            catch
            {
                Debug.Log("ScaleTechs: GetAllBlocksInfo - Failiure on " + tank.name + "!");
            }
            //Debug.Log("ScaleTechs: lastBestValue is " + lastBestValue + "!");
        }

        private void UpdateAllModuleScaleWithSize(bool resetScales, float scale = 1)
        {
            //GET CURRENT VALUES
            UpdateFreakingWheels.GrabCurrent();
            try
            {
                float toSet;
                if (scale == 1)
                    toSet = AimedScale;
                else
                    toSet = scale;
                foreach (TankBlock block in tank.blockman.IterateBlocks())
                {
                    var check = block.GetComponent<ModuleScaleWithSize>();
                    if (check)
                    {
                        try
                        {
                            check.ScaleAccordingly(AimedScale, COM, isThisTony);
                            if (resetScales)
                                check.ResetScale();
                        }
                        catch
                        {
                            Debug.Log("ScaleTechs: " + block.name + " has not been imprinted properly!?");
                        }
                    }
                }
            }
            catch
            {
                Debug.Log("ScaleTechs: UpdateAllModuleScaleWithSize - Failiure on " + tank.name + "!");
            }
        }

    }
}
