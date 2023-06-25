using MonoMod.ModInterop;

namespace HideModList;

[ModExportName(nameof(HideModList))]
public static class HideModListExport
{
    public static void ShowList() => HideModList.ShowList();

    public static void HideList() => HideModList.HideList();

    public static void UpdateListState(bool isHidden) => HideModList.UpdateListState(isHidden);
}