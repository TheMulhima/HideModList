using Logger = Modding.Logger;

namespace HideModList;
public class KeyAndTextMonoBehaviour : MonoBehaviour
{
    public void OnGUI()
    {
        if (GameManager.instance.GetSceneNameString() == Constants.MENU_SCENE)
        {
            if (!HideModList.settings.modListHidden) return;
            if (UIManager.instance.uiState is not(UIState.MAIN_MENU_HOME or UIState.PAUSED)) return;
            var oldBackgroundColor = GUI.backgroundColor;
            var oldContentColor = GUI.contentColor;
            var oldColor = GUI.color;
            var oldMatrix = GUI.matrix;

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.identity,
                new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1f)
            );

            GUI.Label(
                new Rect(20f, Screen.height - 30f, 200f, 200f),
                "HideModList Mod Enabled",
                new GUIStyle
                {
                    fontSize = 30,
                    normal = new GUIStyleState
                    {
                        textColor = Color.white,
                    }
                }
            );

            GUI.backgroundColor = oldBackgroundColor;
            GUI.contentColor = oldContentColor;
            GUI.color = oldColor;
            GUI.matrix = oldMatrix;
        }
    }

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