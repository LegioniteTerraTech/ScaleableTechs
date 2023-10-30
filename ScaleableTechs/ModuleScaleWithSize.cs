using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScaleableTechs
{

    public class ModuleScaleWithSize : Module
    {
        //This is shoehorned into every block to change existing variables of other components.
        //  Only affects:
        //    Wheel Sizes  (rescale stats but level out with size)
        //    Centre of Mass (approx)
        //    Big Tony


        // Variables
        public RescaleableTank rescaleableTank;
        public TankBlock TankBlock;
        private Vector3 OGScale = Vector3.one;
        public bool secondScanFallback = false;

        // Player-Block-defined Variables
        public bool IsPipParticles = false;             // Is this a Pip Particles module?
        public bool IsStrongPipParticles = false;       // Does this override DynamicPipParticles?
        public bool IsDynamicPipParticles = false;      // Do we enable sliders for this?
        public bool MustBeAnchored = false;             // Does this only operate when anchored?
        public float SavedScale = 1f;                   // Set the value for fixed Pip Particles here
        public float maxPermittedSliderScaling = 1f;    // The maximum allowed scale on the slider for when IsDynamicPipParticles is enabled
        public float minPermittedSliderScaling = 1f;    // The minimum allowed scale on the slider for when IsDynamicPipParticles is enabled
        /*
         * "ScaleableTechs.RescaleSystem.ModuleScaleWithSize": 
         * {
         *      "IsPipParticles": true,              // Is this a Pip Particles module?
         *      "SavedScale": 1,                     // Set the value for fixed Pip Particles here
         *      "IsStrongPipParticles": false,       // Does this override DynamicPipParticles?
         *      "IsDynamicPipParticles": false,      // Do we enable sliders for this? - REQUIRES ModuleDynamicScaler to function!
         *      "maxPermittedSliderScaling": 0.4,    // The maximum allowed scale on the slider for when IsDynamicPipParticles is enabled
         *      "minPermittedSliderScaling": 2.5,    // The minimum allowed scale on the slider for when IsDynamicPipParticles is enabled
         *      "MustBeAnchored": false,             // Does this only operate when anchored?
         * }
         */


        //   Universal
        public bool KeepStatsRegardlessofScale = false; // Set this to "true" through your JSON to disable all block patches based on scale
        /*
         * "ScaleableTechs.RescaleSystem.ModuleScaleWithSize": 
         * {
         *     "KeepStatsRegardlessofScale": false, // Set this to "true" through your JSON to disable all block patches based on scale
         * }
         */

        // Collection Variables
        public float wheelSpeedCache = 0.5f;
        public float wheelSpringCache = 0.5f;
        public float wheelRadiusCache = 0.5f;
        public float wheelAccelCache = 0.5f;
        public float wheelTravelCache = 0.5f;
        public bool prevTony = false;
        public bool thisHasShield = false;
        public bool shieldHasBeenRefreshed = false;
        public float shieldCache = 0.5f;
        //public bool shieldTechCache = false;
        public float gunCache = 0.05f;
        public float gunSFXCache = 0.05f;
        public float lastBubbleShield = 1f;
        public float lastScaleReceived = 1f;
        public Vector3 savedShieldScale = Vector3.zero;
        public Vector3 savedColliderScale = Vector3.zero;
        private float distFromCore = 0f;        // how far we offset from ground
                                                //private bool IsControlBlock = false;    // will rescaling cause issues

        public void OnPool()
        {
            OGScale = TankBlock.trans.localScale;
            if (IsDynamicPipParticles || gameObject.name == "_C_BLOCK:584870" || gameObject.name == "_C_BLOCK:584871")
            {
                TankBlock.AttachingEvent.Subscribe(OnAttach);
                TankBlock.DetachedEvent.Subscribe(OnDetach);
                //Debug.Log("ScaleTechs - ModuleScaleWithSize: Subscribed to TankBlock");
            }

            if (gameObject.transform.GetComponent<ModuleWeaponGun>())
            {
                try
                {
                    gunCache = gameObject.GetComponent<ModuleWeaponGun>().m_ShotCooldown;
                    gunSFXCache = gameObject.GetComponent<ModuleWeapon>().m_ShotCooldown;
                    //Debug.Log("ScaleTechs: Saved " + gameObject.GetComponent<ModuleWeaponGun>().m_ShotCooldown + " => " + gunCache);
                }
                catch { }
            }

            if (gameObject.transform.GetComponent<ModuleShieldGenerator>())
            {
                try
                {
                    var shld = gameObject.transform.GetComponent<ModuleShieldGenerator>();
                    shieldCache = shld.m_Radius;
                    if (shld.m_Healing == false)
                        thisHasShield = true;
                    //Debug.Log("ScaleTechs: Saved " + shld.m_Radius + " => " + shieldCache);
                }
                catch
                {
                    //Debug.Log("CRITICAL ERROR!");
                }
            }

            if (gameObject.transform.GetComponent<ModuleWheels>())
            {
                try
                {
                    var whel = gameObject.GetComponent<ModuleWheels>();
                    wheelSpeedCache = whel.m_TorqueParams.torqueCurveMaxRpm;
                    wheelSpringCache = whel.m_WheelParams.suspensionSpring;
                    wheelTravelCache = whel.m_WheelParams.suspensionTravel;
                    wheelAccelCache = whel.m_WheelParams.maxSuspensionAcceleration;
                    wheelRadiusCache = whel.m_WheelParams.radius;
                    //Debug.Log("ScaleTechs: Saved " + gameObject.GetComponent<ModuleScaleWithSize>().wheelSpeedCache + " | " + gameObject.GetComponent<ModuleScaleWithSize>().wheelSpringCache
                    //+ " | " + gameObject.GetComponent<ModuleScaleWithSize>().wheelTravelCache + " | "+ gameObject.GetComponent<ModuleScaleWithSize>().wheelAccelCache + " | " + gameObject.GetComponent<ModuleScaleWithSize>().wheelRadiusCache);
                    //ScaleAccordingly(1f, Vector3.one, false);
                }
                catch
                {
                    //Debug.Log("CRITICAL ERROR!");
                }

            }

        }
        public void OnAttach()
        {
            TankBlock.serializeEvent.Subscribe(new Action<bool, TankPreset.BlockSpec>(OnSerialize));
            //TankBlock.serializeTextEvent.Subscribe(new Action<bool, TankPreset.BlockSpec>(OnSerialize));
        }
        public void OnDetach()
        {
            TankBlock.serializeEvent.Unsubscribe(new Action<bool, TankPreset.BlockSpec>(OnSerialize));
            //TankBlock.serializeTextEvent.Unsubscribe(new Action<bool, TankPreset.BlockSpec>(OnSerialize));
            ResetScale();
        }

        public void ResetScale()
        {
            //RESET SCALES
            gameObject.transform.localScale = OGScale;
        }


        public Collider SeekBulletCollider(Transform host)
        {
            int childCB = host.GetChild(1).transform.childCount;
            for (int vCB = 0; vCB < childCB; ++vCB)
            {
                Transform grabbedGameObjectCB = host.GetChild(1).transform.GetChild(vCB);
                //Debug.Log("ScaleTechs: Searching for collider in " + grabbedGameObjectCB.name);
                var isBubbleShield = grabbedGameObjectCB.gameObject.GetComponent<BubbleShield>();
                if (grabbedGameObjectCB.name == "_bubbleBulletTrigger")
                {
                    return grabbedGameObjectCB.gameObject.GetComponent<CapsuleCollider>();
                }
            }
            Debug.Log("ScaleTechs: Mission failed, we'll get 'em next time.");
            return null; //crash, but ship happens
        }

        public void RecursiveShieldReturn(Transform grabbedGameObject, float input, bool isRefreshed)
        {
            var thisInst = gameObject.GetComponent<ModuleScaleWithSize>();
            int childCB = grabbedGameObject.transform.childCount;
            for (int vCB = 0; vCB < childCB; ++vCB)
            {
                Transform grabbedGameObjectCB = grabbedGameObject.transform.GetChild(vCB);
                //Debug.Log("ScaleTechs: Searching " + grabbedGameObjectCB.name);
                var isBubbleShield = grabbedGameObjectCB.GetComponent<BubbleShield>();
                if (isBubbleShield)
                {
                    if (gameObject.GetComponent<ModuleShieldGenerator>().IsPowered)
                        isBubbleShield.SetTargetScale(shieldCache * input);
                }

                if ((grabbedGameObjectCB.name == "_bubbleBulletTrigger" || grabbedGameObjectCB.name == "_bubbleTechTrigger") && grabbedGameObjectCB.gameObject.GetComponent<SphereCollider>())
                {
                    try
                    {
                        if (savedColliderScale == Vector3.zero)
                            savedColliderScale = grabbedGameObjectCB.localScale;
                        Vector3 toChange = savedColliderScale;
                        toChange = toChange / input;
                        grabbedGameObjectCB.localScale = toChange;
                        //Debug.Log("ScaleTechs: Updated sphereCollider size to " + toChange);
                    }
                    catch
                    {
                        Debug.Log("ScaleTechs: EPIC FAIL " + grabbedGameObjectCB.name + " Could not be re-utilized!");
                    }

                }
                else if (grabbedGameObjectCB.name == "ShieldSphereEdge")
                {

                    try
                    {
                        if (savedShieldScale == Vector3.zero)
                            savedShieldScale = grabbedGameObjectCB.localScale;
                        Vector3 toChange = savedShieldScale;
                        toChange = toChange / input;
                        grabbedGameObjectCB.localScale = toChange;
                        //Debug.Log("ScaleTechs: Updated shield size to " + toChange);
                    }
                    catch
                    {
                        Debug.Log("ScaleTechs: Failiure in setting shield size");
                    }
                }
                if (grabbedGameObjectCB.transform.childCount > 0)
                {
                    //Debug.Log("Performing Recursive Action!");
                    RecursiveShieldReturn(grabbedGameObjectCB, input, isRefreshed);
                }
            }
        }

        public void ScaleAccordingly(float scaleToAimFor, Vector3 COMTarget, bool TonyTime)
        {
            if (KeepStatsRegardlessofScale == true)
                return; //Make no changes beyond this point
            var isGunExists = gameObject.GetComponent<ModuleWeaponGun>();
            if (isGunExists)
            {
                //RecursiveSearchForBulletSpawn(TankBlock.transform, scaleToAimFor);
                /*
                if (TonyTime)// TIME TO KILL
                {
                    Debug.Log("ScaleTechs - ModuleScaleWithSize: TONY WRATH ENABLED");
                    prevTony = true;
                    gameObject.GetComponent<ModuleWeapon>().m_ShotCooldown = 0.05f;
                    isGunExists.m_ShotCooldown = 0.05f;
                }
                else if (prevTony == true)
                {
                    prevTony = false;
                    gameObject.GetComponent<ModuleWeapon>().m_ShotCooldown = gunSFXCache;
                    isGunExists.m_ShotCooldown = gunCache;
                }*/
            }
            var isWheelExists = gameObject.GetComponent<ModuleWheels>();
            if (isWheelExists)
            {
                isWheelExists.m_TorqueParams.torqueCurveMaxRpm = wheelSpeedCache / scaleToAimFor;
                if (wheelTravelCache >= 0.4 || scaleToAimFor >= 1.2f) //If it's smaller than 0.4 then do not bother changing it (break!)
                    isWheelExists.m_WheelParams.suspensionTravel = Mathf.Max(wheelTravelCache * scaleToAimFor, 0.4f);// Prevent UNDERSIZE ISSUE
                else
                    isWheelExists.m_WheelParams.suspensionTravel = wheelTravelCache;

                isWheelExists.m_WheelParams.suspensionSpring = wheelSpringCache * scaleToAimFor;
                isWheelExists.m_WheelParams.maxSuspensionAcceleration = wheelAccelCache * scaleToAimFor;

                if (wheelRadiusCache >= 0.4 || scaleToAimFor >= 1.2f) //If it's smaller than 0.4 then do not bother changing it (break!)
                    isWheelExists.m_WheelParams.radius = Mathf.Max(wheelRadiusCache * scaleToAimFor, 0.4f);// Prevent UNDERSIZE ISSUE
                else
                    isWheelExists.m_WheelParams.radius = wheelRadiusCache;

                if (TonyTime)// TIME TO KILL
                    isWheelExists.m_TorqueParams.torqueCurveMaxRpm = wheelSpeedCache / 1.5f;
                UpdateFreakingWheels.ForceUpdateWHEELS(TankBlock.gameObject);
            }
            var isBubbleExists = gameObject.GetComponent<ModuleShieldGenerator>();
            if (isBubbleExists)
            {
                if (isBubbleExists.m_Healing == false)
                {
                    isBubbleExists.m_Radius = shieldCache * scaleToAimFor;
                    RecursiveShieldReturn(TankBlock.transform, scaleToAimFor, false);
                }
            }
        }

        public float GetInfo()
        {
            string CB = gameObject.name;
            distFromCore = TankBlock.transform.localPosition.y;
            //Debug.Log("Processing " + CB + " | " + distFromCore);
            //Debug.Log("Processing " + gameObject + " " + CB);

            if (IsPipParticles == true)
            {   //Handling of Block Injector blocks
                gameObject.transform.GetComponentInParent<RescaleableTank>().PipReceived(SavedScale, IsDynamicPipParticles, IsStrongPipParticles, MustBeAnchored, minPermittedSliderScaling, maxPermittedSliderScaling); ;
                //Debug.Log("ScaleTechs: Found Pip Particles!  " + gameObject.name + " is a Pip Particle block - sent new Tech LocalAimedScale value of " + SavedScale + " to " + gameObject.transform.parent.name);
            }
            else if (CB == "_C_BLOCK:584866")
            {   //VEN Shrink Pip Particles
                //SavedScale = 0.5f;
                gameObject.transform.GetComponentInParent<RescaleableTank>().PipReceived(0.4f, false, true, false, 1f, 1f);
                //Debug.Log("ScaleTechs: Found Pip Particles!  " + gameObject.name + " is a Dedicated Pip Particle block - sent new Tech LocalAimedScale value of " + SavedScale + " to " + gameObject.transform.parent.name);
            }
            else if (CB == "_C_BLOCK:584867")
            {   //GSO Giant Pip Particles
                //SavedScale = 1.4f;
                gameObject.transform.GetComponentInParent<RescaleableTank>().PipReceived(1.4f, false, false, false, 1f, 1f);
                //Debug.Log("ScaleTechs: Found Pip Particles!  " + gameObject.name + " is a Dedicated Pip Particle block - sent new Tech LocalAimedScale value of " + SavedScale + " to " + gameObject.transform.parent.name);
            }
            else if (CB == "_C_BLOCK:584868")
            {   //Hawkeye Goliath Pip Particles
                //SavedScale = 2.0f;
                gameObject.transform.GetComponentInParent<RescaleableTank>().PipReceived(2.0f, false, false, false, 1f, 1f);
                //Debug.Log("ScaleTechs: Found Pip Particles!  " + gameObject.name + " is a Dedicated Pip Particle block - sent new Tech LocalAimedScale value of " + SavedScale + " to " + gameObject.transform.parent.name);
            }
            else if (CB == "_C_BLOCK:584869")
            {   //GeoCorp Colossus Pip Particles
                //IsPipParticles = true;
                //SavedScale = 3.0f;
                gameObject.transform.GetComponentInParent<RescaleableTank>().PipReceived(3.0f, false, true, false, 1f, 1f);
                //Debug.Log("ScaleTechs: Found Pip Particles!  " + gameObject.name + " is a Dedicated Pip Particle block - sent new Tech LocalAimedScale value of " + SavedScale + " to " + gameObject.transform.parent.name);
            }
            else if (CB == "_C_BLOCK:584870")
            {   //Better Future Inflation Pip Particles
                //IsPipParticles = true;
                //IsDynamicPipParticles = true;
                //minPermittedSliderScaling = 1.0f;
                //maxPermittedSliderScaling = 2.0f;
                //  Will attempt to look for ModuleDynamicScaler in the block it's aiming for or it will break an error and act as a normal one
                gameObject.transform.GetComponentInParent<RescaleableTank>().PipReceived(1f, true, false, false, 1f, 2.0f);
                //Debug.Log("ScaleTechs: Found Pip Particles!  " + gameObject.name + " is a Dedicated Pip Particle block - sent new Tech LocalAimedScale value of " + SavedScale + " to " + gameObject.transform.parent.name);
            }
            else if (CB == "_C_BLOCK:584871")
            {   //Reticle Research Dynomaniac Pip Particles
                //IsPipParticles = true;
                //IsDynamicPipParticles = true;
                //minPermittedSliderScaling = 0.5f;
                //maxPermittedSliderScaling = 1.0f;
                //  Will attempt to look for ModuleDynamicScaler in the block it's aiming for or it will break an error and act as a normal one
                gameObject.transform.GetComponentInParent<RescaleableTank>().PipReceived(1f, true, false, false, 0.5f, 1f);
                //Debug.Log("ScaleTechs: Found Pip Particles!  " + gameObject.name + " is a Dedicated Pip Particle block - sent new Tech LocalAimedScale value of " + SavedScale + " to " + gameObject.transform.parent.name);
            }
            else if (CB == "_C_BLOCK:584873")
            {   //TAC Ant-Tech Pip Particles
                //IsPipParticles = true;
                //IsStrongPipParticles = true;  //Overrides DynamicPipParticles
                //SavedScale = 0.2f;            //YES I know it's dangerous, just deal with it or don't use.
                gameObject.transform.GetComponentInParent<RescaleableTank>().PipReceived(0.2f, false, true, false, 1f, 1f);
                //Debug.Log("ScaleTechs: Found Pip Particles!  " + gameObject.name + " is a Dedicated Pip Particle block - sent new Tech LocalAimedScale value of " + SavedScale + " to " + gameObject.transform.parent.name);
            }
            else if (CB == "_C_BLOCK:584874")
            {   //LK Bastion Pip Particles
                //IsPipParticles = true;
                //IsStrongPipParticles = true;  //Overrides DynamicPipParticles
                //SavedScale = 5.0f;            //BIGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
                gameObject.transform.GetComponentInParent<RescaleableTank>().PipReceived(5.0f, false, true, true, 1f, 1f);
                //Debug.Log("ScaleTechs: Found Pip Particles!  " + gameObject.name + " is a Dedicated Pip Particle block - sent new Tech LocalAimedScale value of " + SavedScale + " to " + gameObject.transform.parent.name);
            }

            //Test for a control block and !!ABORT!! if found
            else if (CB == "_C_BLOCK:1293838" || CB == "_C_BLOCK:129380" || CB == "_C_BLOCK:129381" || CB == "_C_BLOCK:6194710" || CB == "_C_BLOCK:1293834" || CB == "_C_BLOCK:1293837" ||
                CB == "_C_BLOCK:1980325" || CB == "_C_BLOCK:1293835" || CB == "_C_BLOCK:1393838" || CB == "_C_BLOCK:1393837" || CB == "_C_BLOCK:1393836" || CB == "_C_BLOCK:1393835" ||
                CB == "_C_BLOCK:29571436")
            { //EVERY PISTON AND SWIVEL
                Debug.Log("ScaleTechs: CLUSTERBODY CONTROL BLOCK FOUND IN " + gameObject + " REPORTING TO RescaleableTank AND ABORTING ALL OPERATIONS!");
                gameObject.transform.root.GetComponent<RescaleableTank>().TankIsScrewed();
                //IsControlBlock = true;
            }
            return distFromCore;

        }



        [Serializable]
        private new class SerialData : Module.SerialData<ModuleScaleWithSize.SerialData>
        {
            public float savedScale;
        }

        private void OnSerialize(bool saving, TankPreset.BlockSpec blockSpec)
        {

            if (KickStart.unInstallTrue)
                return;// no operations here for uninstall
            if (IsDynamicPipParticles || gameObject.name == "_C_BLOCK:584870")
            {   //only IF this is dynamic particles as the other WON'T Save!
                if (saving)
                {   //Save to snap
                    if (MustBeAnchored)
                    {
                        ModuleScaleWithSize.SerialData serialData = new ModuleScaleWithSize.SerialData()
                        {
                            savedScale = TankBlock.transform.root.GetComponent<RescaleableTank>().DynamicScaleA
                        };
                        serialData.Store(blockSpec.saveState);
                        //Debug.Log("ScaleTechs: Saved " + SavedScale + " in gameObject " + gameObject.name);
                    }
                    else
                    {
                        ModuleScaleWithSize.SerialData serialData = new ModuleScaleWithSize.SerialData()
                        {
                            savedScale = TankBlock.transform.root.GetComponent<RescaleableTank>().DynamicScale
                        };
                        serialData.Store(blockSpec.saveState);
                        //Debug.Log("ScaleTechs: Saved " + SavedScale + " in gameObject " + gameObject.name);
                    }
                }
                else
                {   //Load from snap
                    ModuleScaleWithSize.SerialData serialData2 = Module.SerialData<ModuleScaleWithSize.SerialData>.Retrieve(blockSpec.saveState);
                    if (serialData2 != null)
                    {
                        SetScale(serialData2.savedScale);
                        Debug.Log("ScaleTechs: Loaded " + SavedScale + " from gameObject " + gameObject.name);
                    }
                }
            }
        }
        public void SetScale(float scale)
        {
            SavedScale = scale;
            UpdateTank();
        }

        public void UpdateTank()
        {
            try
            {
                if (MustBeAnchored)
                    TankBlock.transform.root.GetComponent<RescaleableTank>().DynamicScaleA = SavedScale;
                else
                    TankBlock.transform.root.GetComponent<RescaleableTank>().DynamicScale = SavedScale;
            }
            catch
            {
                Debug.Log("ScaleTechs: ModuleScaleWithSize error - tank no longer exists");
            }
        }
    }
}
