using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScaleableTechs
{
    public class RescaleManager : MonoBehaviour
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
        public static RescaleManager inst;

        public static bool LastPlayerBeamState = false; 
        public static bool CriticalError = false;           // IS A CRASH LIKELY
        public static bool Overburdened = false;            // is there well over 64 techs for unknown reasons?
        public static bool ForceUpdateManWheels = false;    // do we need to update all wheels 
        public static int DelayedUpdateTimer = 0;               

        internal static bool ControlBlockWarningIssued = false; // Issue the warning once

        public static List<RescaleableTank> ScaleQueued = new List<RescaleableTank>();

        public static void Initiate()
        {
            inst = new GameObject("RescaleManager").AddComponent<RescaleManager>();
            Singleton.Manager<ManTechs>.inst.TankNameChangedEvent.Subscribe(QueueUpdater);
            Singleton.Manager<ManTechs>.inst.TankTeamChangedEvent.Subscribe(QueueUpdater);
            Singleton.Manager<ManTechs>.inst.TankPostSpawnEvent.Subscribe(QueueUpdater);
        }

        public static void AddToQueue(RescaleableTank resTank)
        {
            if (resTank)
            {
                if (!ScaleQueued.Contains(resTank))
                {
                    Debug.Log("ScaleTechs: Update requested - Tech " + resTank.tank.name);
                    resTank.NeedsUpdate = true;
                    ScaleQueued.Add(resTank);
                }
            }
        }
        public static void QueueUpdater(Tank tech)
        {
            var resTank = tech.GetComponent<RescaleableTank>();
            AddToQueue(resTank);
        }
        public static void QueueUpdater(Tank tech, TrackedVisible TV)
        {
            var resTank = tech.GetComponent<RescaleableTank>();
            AddToQueue(resTank);
        }

        public static void QueueUpdater(Tank tech, ManTechs.TeamChangeInfo info)
        {
            QueueUpdater(tech);
        }

        public static void UpdateAllEnemies()
        {
            foreach (Tank tech in Singleton.Manager<ManTechs>.inst.CurrentTechs)
            {
                if (tech.IsEnemy())
                {
                    var resTank = tech.GetComponent<RescaleableTank>();
                    AddToQueue(resTank);
                }
            }
        }
        public static void UpdateAll()
        {
            foreach (Tank tech in Singleton.Manager<ManTechs>.inst.CurrentTechs)
            {
                var resTank = tech.GetComponent<RescaleableTank>();
                AddToQueue(resTank);
            }
        }

        public static void FetchAllNeedsUpdate()
        {
            foreach (Tank tech in Singleton.Manager<ManTechs>.inst.CurrentTechs)
            {
                var resTank = tech.GetComponent<RescaleableTank>();
                if (resTank)
                {
                    if (resTank.GetNeedsUpdate() || resTank.NeedsUpdate)
                        AddToQueue(resTank);
                }
            }
        }
        public static void ClearDone()
        {
            int fireTimes = ScaleQueued.Count;
            for (int step = 0; step < fireTimes; step++)
            {
                RescaleableTank resTank = ScaleQueued.ElementAt(step);
                if (resTank)
                {
                    if (resTank.NeedsUpdate)
                        continue;
                }
                ScaleQueued.RemoveAt(step);
                fireTimes--;
                step--;
            }
        }

        public void Update()
        {
            // Check if all techs were updated
            if (KickStart.ResetPlayerScale && Singleton.playerTank)
            {   //Handle player updates for autoscaling
                if (Singleton.playerTank.beam.IsActive != LastPlayerBeamState)
                {
                    QueueUpdater(Singleton.playerTank);
                }
            }

            if (!ManPauseGame.inst.IsPaused)
            {
                int fireTimes = ScaleQueued.Count;
                if (fireTimes > 0)
                {
                    for (int step = 0; step < fireTimes; step++)
                    {
                        RescaleableTank resTank = ScaleQueued.ElementAt(step);
                        if (resTank.UpdateTank())
                        {
                            ScaleQueued.RemoveAt(step);
                            fireTimes--;
                            step--;
                        }
                    }
                }
            }
            DelayedUpdateTimer++;
            if (DelayedUpdateTimer > 30)
            {
                DelayedUpdate();
                DelayedUpdateTimer = 0;
            }
        }
        public void DelayedUpdate()
        {
            FetchAllNeedsUpdate();
            GlobalScaleGUIController.isSaving = false;
        }
    }
}