extends SceneTree

# Run without --event-tools and only with an isolated event-disabled-test profile.
var failures := 0
var checks := 0
func check(value, message):
    checks += 1
    if not value:
        push_error(message)
        failures += 1
func _initialize(): call_deferred("run_checks")
func saved_events():
    return JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json")).Events
func no_event_ui(game):
    check(game.find_child("EventMapEntry", true, false) == null, "Event Map button is not created")
    check(not game.has_node("EventPage"), "Event page is not created or reachable")
    check(not game.has_node("EventRuntime"), "No automatic Event ticks or navigation")
func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("event-disabled-test") or "--event-tools" in OS.get_cmdline_user_args():
        push_error("Use an isolated event-disabled-test profile without --event-tools")
        quit(1)
        return
    var now := int(Time.get_unix_time_from_system())
    var progress := {"GeneratorDataVersion":2, "Currency":1234, "TotalCurrencyEarned":5678,
        "ProgressionLevel":7, "HighestProgressionReached":7, "LastUpdateUnix":now-3600,
        "PrestigeCount":2, "PrestigeResource":3, "TotalPrestigeResourceEarned":4,
        "Generators":{"data_terminal":{"Level":7,"Unlocked":true,"CycleElapsedSeconds":2,"Multiplier":1}},
        "OwnedBots":["test_bot"], "ActiveBotId":"test_bot", "ShopPurchases":{"test_item":1},
        "UnlockedMilestones":["data_100"], "ClaimedRewards":["data_100"]}
    var events := {"Version":1, "LastEvaluationUnix":now-3600, "LastEventId":"event_test_001",
        "CosmeticEntitlements":["cosmetic:test_badge"], "ProgressByEventId":{"event_test_001":progress}}
    var fixture := {"SaveVersion":22, "LastSaveUnix":now, "Rooms":[], "Events":events}
    var file := FileAccess.open("user://idle_ai_save.json", FileAccess.WRITE)
    file.store_string(JSON.stringify(fixture)); file.close()
    # Normalize expected nested numeric types only after writing valid integer save fields.
    progress = JSON.parse_string(JSON.stringify(progress))
    var game = load("res://game.tscn").instantiate(); root.add_child(game)
    for frame in range(25): await process_frame
    var snapshot = saved_events()
    check(snapshot.Version == 1 and snapshot.LastEvaluationUnix == now-3600, "Disabled startup does not migrate or settle Event state")
    var restored = snapshot.ProgressByEventId.event_test_001
    for key in progress:
        check(restored[key] == progress[key], "Existing Event field preserved: " + key)
    check(snapshot.CosmeticEntitlements == events.CosmeticEntitlements, "Event cosmetic ownership preserved")
    for viewport_size in [Vector2i(720,1280), Vector2i(540,960), Vector2i(1080,2400)]:
        root.size = viewport_size
        if not game.get_node("MapPage").visible:
            game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit()
        for frame in range(6): await process_frame
        no_event_ui(game)
        var map = game.get_node("MapPage")
        check(map.visible, "Map still opens")
        var singularity = map.find_child("SingularityRoomNode", true, false)
        check(singularity != null and singularity.position == Vector2(48,12), "Singularity position unchanged")
        if singularity == null: continue
        var canvas = singularity.get_parent()
        check(canvas.custom_minimum_size == Vector2(650,1180), "No Event spacer or canvas growth")
        var expected := [Vector2(48,940),Vector2(330,700),Vector2(48,460),Vector2(330,220)]
        var rooms := 0
        for child in canvas.get_children():
            if child is Button and child.custom_minimum_size == Vector2(270,178):
                rooms += 1
                check(child.position in expected, "Room card layout unchanged")
            check(not (child is BaseButton and child.position == Vector2(398,12)), "No invisible hit area in former Event position")
        check(rooms == 4, "All four normal Room cards remain")
    for button in ["ShopButton", "LabButton", "MapButton"]:
        game.get_node("BottomBar/Margin/HBox/" + button).pressed.emit()
        for frame in range(6): await process_frame
        no_event_ui(game)
    game.notification(MainLoop.NOTIFICATION_APPLICATION_RESUMED)
    game.queue_free()
    for frame in range(6): await process_frame
    check(saved_events() == snapshot, "Full Event state unchanged after navigation, resume and save")
    game = load("res://game.tscn").instantiate(); root.add_child(game)
    for frame in range(25): await process_frame
    no_event_ui(game)
    check(saved_events() == snapshot, "Full Event state survives reopening unchanged")
    game.queue_free()
    for frame in range(5): await process_frame
    print("Disabled Events scene smoke: ", checks, " checks, failures=", failures)
    quit(1 if failures else 0)
