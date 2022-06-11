global using GlobalEnums;
global using Modding;
global using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using Modding.Menu;
using Modding.Menu.Config;
using Mono.Cecil.Cil;
using MonoMod.Cil;
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

    public static HideModList Instance;
    public MenuOptionHorizontal HideModListToggle;

    public bool ToggleButtonInsideMenu { get; set; }
    public static GlobalSettings settings { get; set; } = new GlobalSettings();
    public void OnLoadGlobal(GlobalSettings s) => settings = s;
    public GlobalSettings OnSaveGlobal() => settings;

    private void CallUpdateModText() => updateModTextFunction.Invoke(null, null);
    public override string GetVersion() => "2.2";

    public override void Initialize()
    {
        Instance ??= this;
        ModHooks.FinishedLoadingModsHook += () =>
        {
            //makes sure modlog log doesnt get yeeted
            if (settings.modListHidden) CreateILHook();
        };
        On.UIManager.SetMenuState += OnUIManagerSetMenuState;
    }

    private void OnUIManagerSetMenuState(On.UIManager.orig_SetMenuState orig, UIManager self, MainMenuState newstate)
    {
        if (settings.HideOrShowWithPlayModeMenu)
        {
            if (newstate == MainMenuState.PLAY_MODE_MENU)
            {
                settings.modListHidden = true;
                
                CreateILHook();

                HideModListToggle.SetOptionTo(settings.modListHidden ? 0 : 1);
            }
            else
            {
                settings.modListHidden = false;
                
                RemoveILHook();

                HideModListToggle.SetOptionTo(settings.modListHidden ? 0 : 1);
            }
        }

        orig(self, newstate);
    }

    public HideModList()
    {
        GameObject HideModListGo = new GameObject("HideModListGo", typeof(KeyAndTextMonoBehaviour));
        GameObject.DontDestroyOnLoad(HideModListGo);
    }

    public void CreateILHook()
    {
        if (updateModTextHook == null)
        {
            updateModTextHook = new ILHook(updateModTextMethodInfo, NewUpdateModText);
        }

        CallUpdateModText();
    }

    public void RemoveILHook()
    {
        updateModTextHook?.Dispose();
        updateModTextHook = null;
        CallUpdateModText();
    }

    private void NewUpdateModText(ILContext il)
    {
        ILCursor cursor = new ILCursor(il).Goto(0);

        //for verification reasons not going to publicly explain what is happening here
        if (cursor.TryGotoNext(i => i.MatchLdstr(" : ")))
        {
            cursor.Emit(OpCodes.Pop);


            for (int i = 0; i < 5; i++) cursor.Remove();
            cursor.EmitDelegate(() => settings.placeHolder);
        }

        cursor.Goto(0);
        if (cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld<ModHooks>("ModVersion")))
        {
            cursor.EmitDelegate<Func<string,string>>(GetNumMods);
        }
    }

    private string GetNumMods(string version)
    {
        return version + $"\nWith {ModHooks.GetAllMods(false, true).Count().ToString()} Mods";
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