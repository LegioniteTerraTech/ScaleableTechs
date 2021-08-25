using UnityEngine;

namespace ScaleableTechs
{
    public class GlobalScaleGUIController : MonoBehaviour
    {
        //We handle the GUI for the Pip Particles block here

        static private bool GUIIsActive = false;
        static private Rect BlockWindow = new Rect(200, 0, 220, 110);   // normie screen
        static private Rect BlockWindow2 = new Rect(200, 0, 360, 110);  // CRITICAL ERROR!!!
        static public GameObject GUIWindow;

        static private void GUIHandler(int ID)
        {
            //Control the scale of Techs worldwide
            KickStart.GlobalAimedScale = GUI.HorizontalSlider(new Rect(20, 80, 160, 15), Mathf.Round(KickStart.GlobalAimedScale * 10) / 10, 0.5f, 2.0f);
            int sizeMatters = (int)(KickStart.GlobalAimedScale * 100);
            GUI.Label(new Rect(20, 60, 160, 20), "GLOBAL TECH SIZE: " + sizeMatters + "%");
            if (RescaleSystem.CriticalError == true)
            {
                GUI.Label(new Rect(20, 30, 330, 20), "The Mod \"Control Blocks\" is unsupported!");
                GUI.Label(new Rect(20, 45, 330, 20), "   ������                                          ATTEMPT RECOVERY");
                KickStart.AttemptRecovery = GUI.Toggle(new Rect(190, 70, 60, 20), KickStart.AttemptRecovery, "Force Anyways");
            }
            else
            {
                if (KickStart.GlobalAimedScale != 1f)
                    GUI.Label(new Rect(20, 30, 185, 20), "Avoid changes at this scale!");
                else
                    GUI.Label(new Rect(20, 30, 185, 20), "----------------------------------");
                if (KickStart.dontPreventLogSpam == false)
                    GUI.Label(new Rect(20, 45, 160, 20), "Affects ALL Techs");
            }
            GUI.DragWindow();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KickStart.hotKey))
            {
                GUIIsActive = !GUIIsActive;
                GUIWindow.SetActive(GUIIsActive);
                if (!GUIIsActive)
                {
                    Debug.Log("\nScaleTech - GlobalGUI: Writing to Config...");
                    KickStart._thisModConfig.WriteConfigJsonFile();
                }
            }
        }

        public static void Save()
        {
            Debug.Log("\nScaleTechs: Writing to Config...");
            KickStart._thisModConfig.WriteConfigJsonFile();
        }

        public static void Initiate()
        {
            new GameObject("GlobalScaleGUIController").AddComponent<GlobalScaleGUIController>();
            GUIWindow = new GameObject();
            GUIWindow.AddComponent<GUIDisplay>();
            GUIWindow.SetActive(false);
            Debug.Log("ScaleTechs - GlobalGUI: Now Exists");
        }
        internal class GUIDisplay : MonoBehaviour
        {
            private void OnGUI()
            {
                if (RescaleSystem.CriticalError == true && GUIIsActive)
                {
                    BlockWindow2 = GUI.Window(1337, BlockWindow2, GUIHandler, "ScaleTech Has Encountered a Serious Error!");
                }
                else if (GUIIsActive)
                {
                    BlockWindow = GUI.Window(1337, BlockWindow, GUIHandler, "Global Tech Scales");
                }
            }
        }
    }
}
