using GlobalEnums;
using UnityEngine;

namespace HideModList;
public class KeypressMonoBehaviour : MonoBehaviour
{
    public void Update()
    {
        if (UIManager.instance == null)
        {
            return;
        }

        //conditions for when ModList is normally shown
        if (UIManager.instance.uiState is UIState.MAIN_MENU_HOME or UIState.PAUSED)
        {
            if (HideModList.settings.keybinds.wasPressed())
            {
                HideModList.settings.modListHidden = !HideModList.settings.modListHidden;
                
                if (HideModList.settings.modListHidden) HideModList.Instance.CreateILHook();
                else HideModList.Instance.RemoveILHook();

                HideModList.Instance.HideModListToggle.SetOptionTo(HideModList.settings.modListHidden ? 0 : 1);
            }
        }
    }
}