using Godot;

namespace IdleAi;

/*
 * Navigation lifecycle helpers for The Singularity.
 *
 * SingularityController.Hide() is still correct when the player really leaves
 * the room. MAP / SHOP / LAB / SETTINGS are different: they temporarily cover
 * the current room, so the Singularity BottomBar theme/layer must stay active
 * just as a normal room remains the underlying room while those pages are open.
 */
public sealed partial class SingularityController
{
    public void PreserveRoomChromeForExternalNavigation()
    {
        if (_bottomBar == null || !GodotObject.IsInstanceValid(_bottomBar))
            return;

        ApplySingularityBottomBarTheme();
        ApplySingularityBottomBarLayer();

        _bottomBar.Show();
        _bottomBar.MoveToFront();

        // GUI hit testing follows tree order, independently of ZIndex.
        // Keep the modal ahead of navigation after restoring room chrome.
        Control? settings = _root.GetNodeOrNull<Control>("SettingsOverlay");
        if (settings?.Visible == true)
            settings.MoveToFront();
    }

    public void PrepareNormalRoomSelectionFromExternalNavigation()
    {
        if (_bottomBar == null || !GodotObject.IsInstanceValid(_bottomBar))
            return;

        /*
         * Run before MapController changes rooms. This restores the bar state
         * captured when Singularity was entered. GameUiController is then free
         * to apply the newly selected room's own theme afterwards.
         */
        RestoreNormalBottomBarTheme();
        RestoreNormalBottomBarLayer();
    }

    public void FinishExternalNavigationToNormalRoom()
    {
        CloseDetailOverlay();
        HideStatInfoPopup();
        _panInput?.StopMotion();
        _page?.Hide();

        /*
         * Do not restore the cached theme here: GameUiController may already
         * have applied a different normal room's theme. Only normalize layering.
         */
        RestoreNormalBottomBarLayer();
    }
}
