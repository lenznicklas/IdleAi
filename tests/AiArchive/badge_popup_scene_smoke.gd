extends SceneTree
var failures := 0
func check(ok, message):
    if not ok:
        failures += 1
        push_error(message)
func _initialize(): call_deferred("run_checks")
func frames(count := 6):
    for i in range(count): await process_frame
func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("badge-popup-scene-test"):
        push_error("Isolated badge-popup-scene-test profile required"); quit(1); return
    var file = FileAccess.open("user://idle_ai_save.json", FileAccess.WRITE)
    file.store_string(JSON.stringify({"SaveVersion":22,"LastSaveUnix":int(Time.get_unix_time_from_system()),
        "LifetimeLevelBase":148,"Tokens":1000000000,"DataShards":17,"Rooms":[]})); file.close()
    var game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25)
    check(game.find_child("LevelBadgePopup",true,false) == null, "No celebration below threshold")
    check(game.has_method("OnSlotActionRequested"), "Game action fixture is callable")
    if not game.has_method("OnSlotActionRequested"): game.queue_free(); quit(1); return
    # Unlocking the second slot adds the total level that crosses 149 -> 150.
    game.call("OnSlotActionRequested",1); await frames(2)
    var popup = game.find_child("LevelBadgePopup",true,false)
    check(popup != null and popup.is_visible_in_tree(), "Real gameplay action automatically displays badge popup")
    var saved = JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json"))
    check(saved.Archive.HighestLevelBadgeTier == 1, "Archive tier saved at the same milestone")
    check(saved.DataShards == 17, "Celebration grants no currency")
    await create_timer(2.2).timeout; await frames()
    check(game.find_child("LevelBadgePopup",true,false) == null, "Popup cleans up after fade out")
    game.queue_free(); await frames()
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25)
    check(game.find_child("LevelBadgePopup",true,false) == null, "Restart never replays saved milestone")
    game.queue_free(); await frames()
    print("Level Badge popup scene: failures=",failures)
    quit(1 if failures else 0)
