using MonoMod.ModInterop;

namespace HideModList;

[ModExportName(nameof(HideModList))]
public static class HideModListExport
{
    public static void ShowList() => HideModList.ShowList();

    public static void HideList(bool usePlaceHolder) => HideModList.HideList(usePlaceHolder);

    public static void UpdateListState(bool isHidden, bool usePlaceHolder) => HideModList.UpdateListState(isHidden, usePlaceHolder);
}