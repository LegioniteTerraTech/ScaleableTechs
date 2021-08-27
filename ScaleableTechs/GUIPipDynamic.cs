using UnityEngine;

namespace ScaleableTechs
{
    class LocalDisplayController : MonoBehaviour
    {
        public LocalDisplayController()
        {
            inst = this;
        }
        public static LocalDisplayController inst;
        public static void CheckValid()
        {
            inst.gameObject.SetActive(GUIPipDynamic.inst.ReturnGUI());
        }
        public void OnGUI()
        {
            GUIPipDynamic.inst.LaunchGUI();
        }
    }


    internal class GUIPipDynamic : MonoBehaviour
    {
        public static GUIPipDynamic inst;
        private static ModuleScaleWithSize thisModule;
        private static bool cTargetVisible = false;
        private static bool isAnchored = false;
        private static float tempScale;
        //private string tempString;
        private readonly int ID = 3099;

        private float minScaleTank = 1f;
        private float maxScaleTank = 1f;

        private static Rect localScaleGUI;

        public GUIPipDynamic()
        {
            inst = this;
        }

        private void Initiate()
        {
            Singleton.Manager<ManPointer>.inst.MouseEvent.Subscribe(MouseUpdate);
        }

        private static void MouseUpdate(ManPointer.Event click, bool y1, bool y2)
        {
            //Debug.Log("ScaleTechs - LocalGUI: RUNNING!");
            if (!Singleton.Manager<ManPointer>.inst.DraggingItem && click == ManPointer.Event.RMB)
            {
                localScaleGUI = new Rect(Input.mousePosition.x, Screen.height - Input.mousePosition.y - 75f, 250f, 120f);
                try
                {
                    thisModule = Singleton.Manager<ManPointer>.inst.targetVisible.block.GetComponent<ModuleScaleWithSize>();
                    //Debug.Log("ScaleTechs - LocalGUI: GrabSuccess");
                }
                catch
                {
                    thisModule = null;
                    //Debug.Log("ScaleTechs - LocalGUI: GrabFail");
                }

                RescaleableTank fetch = thisModule.transform.root.GetComponent<RescaleableTank>();
                if (!(bool)fetch)
                    return;

                cTargetVisible = thisModule;
                if (cTargetVisible && ((thisModule.IsPipParticles && thisModule.IsDynamicPipParticles && fetch.isPipLocked == false)
                    || thisModule.gameObject.name == "_C_BLOCK:584870"))
                {   //We carry onwards and make this work
                    tempScale = thisModule.SavedScale;
                    //tempString = tempScale.ToString();
                }
            }
        }

        public bool ReturnGUI()
        {
            return cTargetVisible && thisModule;
        }

        public void LaunchGUI()
        {
            if (!cTargetVisible || !thisModule)
            {
                return;
            }
            string name = thisModule.gameObject.name;
            if (thisModule.IsDynamicPipParticles || name == "_C_BLOCK:584870" || name == "_C_BLOCK:584871")
            {   //MAKE SURE THIS IS DYNAMIC PIP PARTICLES
                try
                {
                    if (thisModule.gameObject.name == "_C_BLOCK:584870")
                        localScaleGUI = GUI.Window(ID, localScaleGUI, new GUI.WindowFunction(LaunchWindows), "Better Future Inflate Pip");
                    else if (thisModule.gameObject.name == "_C_BLOCK:584871")
                        localScaleGUI = GUI.Window(ID, localScaleGUI, new GUI.WindowFunction(LaunchWindows), "RR Dynomaniac Pip");
                    else //Handle for custom Pips
                        localScaleGUI = GUI.Window(ID, localScaleGUI, new GUI.WindowFunction(LaunchWindows), thisModule.gameObject.GetComponent<TankBlock>().name);
                }
                catch { }
            }
        }

        private void LaunchWindows(int id)
        {
            if (thisModule == null)
            {
                cTargetVisible = false;
                return;
            }
            try
            {
                isAnchored = thisModule.transform.root.GetComponent<Tank>().IsAnchored;
                if (thisModule.gameObject.name == "_C_BLOCK:584870")
                {
                    minScaleTank = 1f;
                    maxScaleTank = 2.0f;
                }
                else if (thisModule.gameObject.name == "_C_BLOCK:584871")
                {
                    minScaleTank = 0.5f;
                    maxScaleTank = 1f;
                }
                else if (isAnchored)
                {   //Grab Anchored Values
                    GUILayout.Label("Anchored Scale Range: " + minScaleTank.ToString() + " to " + maxScaleTank.ToString());
                    minScaleTank = thisModule.transform.root.GetComponent<RescaleableTank>().minPipRangeA;
                    maxScaleTank = thisModule.transform.root.GetComponent<RescaleableTank>().maxPipRangeA;
                }
                else
                {   //Grab Unanchored Values
                    GUILayout.Label("Scale Range: " + minScaleTank.ToString() + " to " + maxScaleTank.ToString());
                    minScaleTank = thisModule.transform.root.GetComponent<RescaleableTank>().minPipRange;
                    maxScaleTank = thisModule.transform.root.GetComponent<RescaleableTank>().maxPipRange;
                }

                /*
                // text field addition - removed for various rounding-counteractive reasons
                GUI.changed = false;
                tempString = GUILayout.TextField(tempString);
                if (GUI.changed && float.TryParse(tempString, out float stringMass))
                {
                    tempScale = (Mathf.Clamp(stringMass, minScaleTank, maxScaleTank) * 10) / 10f;
                    thisModule.SavedScale = tempScale;
                    thisModule.transform.root.GetComponent<RescaleableTank>().SetDynamicScale(tempScale, isAnchored);
                }
                */

                GUI.changed = false;
                tempScale = (int)(GUILayout.HorizontalSlider(thisModule.SavedScale, minScaleTank, maxScaleTank) * 10) / 10f;
                if (GUI.changed)
                {
                    //tempString = tempScale.ToString();
                    thisModule.SavedScale = tempScale;
                    thisModule.transform.root.GetComponent<RescaleableTank>().SetDynamicScale(tempScale, isAnchored);
                }
                if (thisModule.transform.root.GetComponent<RescaleableTank>().DynamicScale != 1f)
                    GUILayout.Label("Current Scale: " + thisModule.transform.root.GetComponent<RescaleableTank>().DynamicScale.ToString());
                else
                    GUILayout.Label("Current Scale: Global Settings");

                if (isAnchored)
                    GUILayout.Label("Overpowered by Stronger Pip: " + thisModule.transform.root.GetComponent<RescaleableTank>().isPipLockedA);
                else
                    GUILayout.Label("Overpowered by Stronger Pip: " + thisModule.transform.root.GetComponent<RescaleableTank>().isPipLocked);
                
                if (GUILayout.Button("Close"))
                {
                    cTargetVisible = false;
                    thisModule = null;
                }
                GUI.DragWindow();
            }
            catch
            {
                Debug.Log("ScaleTechs - LocalGUI: !CRITICAL ERROR! Could not find Tank!");
            }
        }
    }
}
