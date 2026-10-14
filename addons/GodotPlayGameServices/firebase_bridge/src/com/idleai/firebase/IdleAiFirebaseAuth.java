package com.idleai.firebase;

import android.app.Activity;
import com.google.android.gms.common.api.ApiException;
import com.google.android.gms.games.PlayGames;
import java.util.Arrays;
import java.util.HashSet;
import java.util.Set;
import org.godotengine.godot.Godot;
import org.godotengine.godot.plugin.GodotPlugin;
import org.godotengine.godot.plugin.SignalInfo;
import org.godotengine.godot.plugin.UsedByGodot;

/** Companion only: the existing GodotPlayGameServices plugin owns SDK init/interactive login.
 * Every callback includes its invocation ID. No auth code or token is ever logged here.
 */
public final class IdleAiFirebaseAuth extends GodotPlugin {
    private volatile boolean destroyed;
    @Override public void onMainDestroy() { destroyed = true; super.onMainDestroy(); }
    private void checked(String id, boolean authenticated, String playerId, int status) {
        if (!destroyed) emitSignal("signInChecked", id, authenticated, playerId, status);
    }
    private void codeResult(String id, String code, int status) {
        if (!destroyed) emitSignal("serverAuthCodeCompleted", id, code, status);
    }
    public IdleAiFirebaseAuth(Godot godot) { super(godot); }
    @Override public String getPluginName() { return "IdleAiFirebaseAuth"; }
    @Override public Set<SignalInfo> getPluginSignals() {
        return new HashSet<>(Arrays.asList(
            new SignalInfo("signInChecked", String.class, Boolean.class, String.class, Integer.class),
            new SignalInfo("serverAuthCodeCompleted", String.class, String.class, Integer.class)));
    }
    private int status(Exception e) { return e instanceof ApiException ? ((ApiException)e).getStatusCode() : -2; }
    @UsedByGodot public void checkSignIn(String id) {
        runOnUiThread(() -> {
            Activity activity = getActivity();
            if (activity == null) { checked(id, false, "", -3); return; }
            try {
                PlayGames.getGamesSignInClient(activity).isAuthenticated().addOnCompleteListener(task -> {
                    if (!task.isSuccessful()) { checked(id, false, "", status(task.getException())); return; }
                    if (!task.getResult().isAuthenticated()) { checked(id, false, "", 0); return; }
                    try { PlayGames.getPlayersClient(activity).getCurrentPlayer().addOnCompleteListener(player -> {
                        if (!player.isSuccessful()) { checked(id, false, "", status(player.getException())); return; }
                        try {
                            String playerId = player.getResult() == null ? "" : player.getResult().getPlayerId();
                            checked(id, true, playerId == null ? "" : playerId, 0);
                        } catch (Exception e) { checked(id, false, "", status(e)); }
                    }); } catch (Exception e) { checked(id, false, "", status(e)); }
                });
            } catch (Exception e) { checked(id, false, "", status(e)); }
        });
    }
    @UsedByGodot public void requestServerAuthCode(String id, String webClientId) {
        runOnUiThread(() -> {
            Activity activity = getActivity();
            if (activity == null) { codeResult(id, "", -3); return; }
            try {
                PlayGames.getGamesSignInClient(activity).requestServerSideAccess(webClientId, false).addOnCompleteListener(task -> {
                    if (!task.isSuccessful()) { codeResult(id, "", status(task.getException())); return; }
                    String code = task.getResult();
                    codeResult(id, code == null ? "" : code, 0);
                });
            } catch (Exception e) { codeResult(id, "", status(e)); }
        });
    }
}
