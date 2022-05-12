using Newtonsoft.Json;
using InControl;
using Modding.Converters;
using System.Collections.Generic;

namespace HideModList;
public class GlobalSettings
{
    public bool modListHidden = true;
    public string placeHolder = "?";
    public List<string> placeHolderOptions = new() { "?", ".", " ", "Unknown" };
    public bool HideOrShowWithPlayModeMenu = false;
        
    [JsonConverter(typeof(PlayerActionSetConverter))]
    public KeyBinds keybinds = new KeyBinds();
}
    
public class KeyBinds : PlayerActionSet
{
    public PlayerAction keyHideModList;

    public KeyBinds()
    {
        keyHideModList = CreatePlayerAction("keyRandomTeleport");
    }

    public bool wasPressed() => keyHideModList.WasPressed;
}