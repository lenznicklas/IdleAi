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
    if not OS.get_environment("XDG_DATA_HOME").contains("level-badge-scene-test"):
        push_error("Isolated level-badge-scene-test profile required"); quit(1); return
    var file = FileAccess.open("user://idle_ai_save.json", FileAccess.WRITE)
    file.store_string(JSON.stringify({"SaveVersion":22,"LastSaveUnix":int(Time.get_unix_time_from_system()),
        "LifetimeLevelBase":149,"DataShards":17,"HighestClaimedLevelReward":0,"Rooms":[]})); file.close()
    var game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25)
    check(game.find_child("LevelBadgePopup",true,false) == null, "Loading an earned badge does not replay its popup")
    check(game.find_child("LevelRewardsOverlay",true,false) == null, "No obsolete shard claim overlay")
    game.find_child("LevelCard",true,false).pressed.emit(); await frames()
    check(game.get_node("LevelPopup").visible and not game.get_node("StatsOverlay").visible, "Level card opens a small tooltip instead of Stats")
    check(game.get_node("LevelPopup/Label").text.contains("Every 150 Levels"), "Level tooltip explains badge upgrades")
    var level_popup = game.get_node("LevelPopup")
    var token_popup = game.get_node("TokenPopup")
    check(level_popup.get_theme_stylebox("panel") == token_popup.get_theme_stylebox("panel"), "Level and Token use the same panel style")
    for width in [360,720]:
        game.size = Vector2(width,1280); await frames()
        var bounds = level_popup.get_global_rect()
        check(bounds.position.x >= 11 and bounds.end.x <= width-11, "Tooltip stays inside mobile screen edges")
        check(bounds.position.y >= game.find_child("LevelCard",true,false).get_global_rect().end.y+6, "Tooltip does not cover TopBar buttons")
        check(bounds.size.y < 250, "Level explanation stays compact")
    var touch = InputEventScreenTouch.new(); touch.index = 0; touch.position = Vector2(360,1100); touch.pressed = true
    Input.parse_input_event(touch); Input.flush_buffered_events(); await frames()
    check(not level_popup.visible, "Outside mobile tap dismisses Level tooltip")
    touch = touch.duplicate(); touch.pressed = false; Input.parse_input_event(touch); Input.flush_buffered_events()
    game.find_child("TokenCard",true,false).pressed.emit(); await frames()
    check(token_popup.visible and not level_popup.visible, "Token tooltip retains its interaction")
    game.find_child("LevelCard",true,false).pressed.emit(); await frames()
    check(level_popup.visible and not token_popup.visible, "Switching displays keeps only one tooltip open")
    game.find_child("LevelCard",true,false).pressed.emit(); await frames()
    check(not level_popup.visible, "Tapping Level again toggles tooltip closed")
    game.find_child("LevelCard",true,false).pressed.emit(); await frames()
    game.find_child("StatsCard",true,false).pressed.emit(); await frames()
    check(game.get_node("StatsOverlay").visible and not game.get_node("LevelPopup").visible, "Stats card still opens Stats and dismisses tooltip")
    var archive_button = game.get_node("StatsOverlay").find_child("AiArchiveEntry",true,false).find_children("*","Button",true,false)[0]
    archive_button.pressed.emit(); await frames()
    var archive = game.get_node("AiArchiveOverlay")
    for button in archive.find_children("*","Button",true,false):
        if button.text.begins_with("BADGES "):
            button.pressed.emit(); break
    await frames()
    var badge = archive.find_child("LevelBadgeImage",true,false)
    check(badge != null and badge.is_visible_in_tree(), "Level 150 displays the evolving Level Badge")
    var saved = JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json"))
    check(saved.Archive.HighestLevelBadgeTier == 1, "Automatic badge tier persisted")
    check(saved.DataShards == 17, "Level 150 grants no Data Shards")
    check(archive.find_children("LevelBadgeImage","Control",true,false).size() == 1, "One badge image, no tier duplicates")
    game.queue_free(); await frames()
    print("Level Badge scene: failures=",failures)
    quit(1 if failures else 0)
