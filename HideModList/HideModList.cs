global using GlobalEnums;
global using Modding;
global using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Modding.Menu;
using Modding.Menu.Config;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.ModInterop;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using UnityEngine.UI;

namespace HideModList;

public class HideModList : Mod, ICustomMenuMod, IGlobalSettings<GlobalSettings>
{
    private static ILHook updateModTextHook = null;
    private static Type ModLoaderType = Type.GetType("Modding.ModLoader, Assembly-CSharp");
    private static MethodInfo updateModTextMethodInfo= ModLoaderType.GetMethod("UpdateModText", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
    private static FastReflectionDelegate updateModTextFunction = updateModTextMethodInfo.GetFastDelegate();

    internal static HideModList Instance;
    internal MenuOptionHorizontal HideModListToggle;

    public bool ToggleButtonInsideMenu { get; set; }
    internal static GlobalSettings settings { get; private set; } = new GlobalSettings();
    public void OnLoadGlobal(GlobalSettings s) => settings = s;
    public GlobalSettings OnSaveGlobal() => settings;

    private void CallUpdateModText() => updateModTextFunction.Invoke(null, null);
    public override string GetVersion() => "2.3.0";

    private static bool UsePlaceHolder = true;

    public HideModList()
    {
        // Register exports early so other mods can use them when initializing
        typeof(HideModListExport).ModInterop();
    }

    public override void Initialize()
    {
        Instance ??= this;
        
        GameObject HideModListGo = new GameObject("HideModListGo", typeof(KeyAndTextMonoBehaviour));
        UnityEngine.Object.DontDestroyOnLoad(HideModListGo);
        
        ModHooks.FinishedLoadingModsHook += () =>
        {
            // we do this in finished mod load hook to make sure the initial logging of mods list doesnt get messed with
            if (settings.modListHidden) CreateILHook();
        };
        
        // Removes the modlist for menu changer menus. Mostly so it doesn't come in the way of rando settings
        On.UIManager.SetMenuState += HideOnMenuChangerMenu;
    }
    
    /// <summary>
    /// Forces list of mods to be shown
    /// </summary>
    [PublicAPI]
    public static void ShowList()
    {
        settings.modListHidden = false;
        Instance.RemoveILHook();
        Instance.HideModListToggle.SetOptionTo(settings.modListHidden ? 0 : 1);
    }
    
    /// <summary>
    /// Forces list of mods to be hidden
    /// <param name="usePlaceHolder">Should the mod text be replaced with a smaller placeholder</param>
    /// </summary>
    [PublicAPI]
    public static void HideList(bool usePlaceHolder = true)
    {
        settings.modListHidden = true;
        UsePlaceHolder = usePlaceHolder;
        Instance.CreateILHook();
        Instance.HideModListToggle.SetOptionTo(settings.modListHidden ? 0 : 1);
    }
    
    /// <summary>
    /// Changes the state of the list of mods to hidden or shown depending on the the value of <paramref name="isHidden"/>
    /// <param name="isHidden">Should the list be hidden</param>
    /// <param name="usePlaceHolder">Should the mod text be replaced with a smaller placeholder. Will be ignored if <paramref name="isHidden"/> is set to false</param>
    /// </summary>
    [PublicAPI]
    public static void UpdateListState(bool isHidden, bool usePlaceHolder = true)
    {
        if (isHidden)
        {
            HideList(usePlaceHolder);
        }
        else
        {
            ShowList();
        }
    }

    private void HideOnMenuChangerMenu(On.UIManager.orig_SetMenuState orig, UIManager self, MainMenuState newstate)
    {
        if (settings.HideOrShowWithPlayModeMenu)
        {
            UpdateListState(isHidden: newstate == MainMenuState.PLAY_MODE_MENU);
        }

        orig(self, newstate);
    }

    internal void CreateILHook()
    {
        updateModTextHook ??= new ILHook(updateModTextMethodInfo, NewUpdateModText);

        CallUpdateModText();
    }

    internal void RemoveILHook()
    {
        updateModTextHook?.Dispose();
        updateModTextHook = null;
        CallUpdateModText();
    }

    private void NewUpdateModText(ILContext il)
    {
        ILCursor cursor = new ILCursor(il).Goto(0);

        // for src verification reasons not going to publicly explain what is happening here
        if (cursor.TryGotoNext(i => i.MatchLdstr(" : ")))
        {
            cursor.Emit(OpCodes.Pop);

            int remove = int.Parse(ModHooks.ModVersion.Split('-')[1]) >= 74 ? 6 : 5;

            for (int i = 0; i < remove; i++) cursor.Remove();
            cursor.EmitDelegate(() => UsePlaceHolder ? settings.placeHolder : "");
        }

        cursor.Goto(0);
        if (cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld<ModHooks>("ModVersion")))
        {
            cursor.EmitDelegate<Func<string, string>>(orig => 
                orig + $"\nWith {ModHooks.GetAllMods(false, true).Count().ToString()} Mods");
        }
    }

    public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
    {
        var Menu = MenuUtils.CreateMenuBuilderWithBackButton("Hide Mod List", modListMenu, out _);
        Menu.AddContent(
            RegularGridLayout.CreateVerticalLayout(105f),
            c =>
            {
                c.AddHorizontalOption("Hide Mod List", new HorizontalOptionConfig()
                {
                    Label = "Hide Mod List",
                    Description = new DescriptionInfo
                    {
                        Text = "Toggle whether modList shows the modname or a placeholder",
                    },

                    Options = new[] { "True", "False" },
                    ApplySetting = (_, s) =>
                    {
                        settings.modListHidden = s == 0;

                        if (settings.modListHidden) CreateILHook();
                        else RemoveILHook();
                    },
                    RefreshSetting = (s, _) => s.optionList.SetOptionTo(settings.modListHidden ? 0 : 1),
                }, out HideModListToggle);

                c.AddHorizontalOption("Placeholder Text", new HorizontalOptionConfig()
                {
                    Label = "Placeholder Text",
                    Description = new DescriptionInfo
                    {
                        Text = "Toggle what should the placeholder for the modname is"
                    },

                    Options = settings.placeHolderOptions.ToArray(),
                    ApplySetting = (_, s) =>
                    {
                        settings.placeHolder = settings.placeHolderOptions[s];
                        if (settings.modListHidden) CreateILHook();
                        else RemoveILHook();
                    },
                    RefreshSetting = (s, _) =>
                    {
                        int index = settings.placeHolderOptions.IndexOf(settings.placeHolder);
                        index = index == -1 ? 0 : index;
                        s.optionList.SetOptionTo(index);
                    },
                });
                c.AddHorizontalOption("MenuChanger Integration", new HorizontalOptionConfig()
                {
                    Label = "MenuChanger Integration",
                    Description = new DescriptionInfo
                    {
                        Text = "Hide/Show the modlist with play mode menu"
                    },

                    Options = new[] { "True", "False" },
                    ApplySetting = (_, s) => settings.HideOrShowWithPlayModeMenu = s == 0,
                    RefreshSetting = (s, _) => s.optionList.SetOptionTo(settings.HideOrShowWithPlayModeMenu ? 0 : 1),
                });
                
                c.AddKeybind("Toggle Mod List", settings.keybinds.keyHideModList, new KeybindConfig
                {
                    Label = "Toggle Mod List",
                });
            });
        return Menu.Build();
    }
}