using UnityEngine;

namespace MagicVillageDash.UI
{
    public class MainMenu : MonoBehaviour
    {
        public GameObject MainMenuRoot;
        public GameObject SettingsMenuRoot;
        
        void Start()
        {
            if (MainMenuRoot != null) MainMenuRoot.SetActive(true);
            if (SettingsMenuRoot != null) SettingsMenuRoot.SetActive(false);
        }

        public void ShowMainMenu()
        {
            if (MainMenuRoot != null) MainMenuRoot.SetActive(true);
            if (SettingsMenuRoot != null) SettingsMenuRoot.SetActive(false);
        }
        public void ShowSettingsMenu()
        {
            if (MainMenuRoot != null) MainMenuRoot.SetActive(false);
            if (SettingsMenuRoot != null) SettingsMenuRoot.SetActive(true);
        }
    }
}
