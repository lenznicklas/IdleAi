extends SceneTree

# Run from main project with isolated XDG_DATA_HOME containing "event-scene-test"
# and -- --event-tools. This fixture never touches a real player's save.
var failures := 0

func check(value, message):
    if not value:
        push_error(message)
        failures += 1

func _initialize():
    call_deferred("run_checks")

func saved_progress():
    var saved = JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json"))
    return saved.Events.ProgressByEventId.event_test_001

func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("event-scene-test"):
        push_error("isolated event-scene-test profile required")
        quit(1)
        return
    var now = int(Time.get_unix_time_from_system())
    var fixture = {"SaveVersion": 22, "LastSaveUnix": now, "Rooms": [], "Events": {
        "Version": 1, "LastEvaluationUnix": now, "LastEventId": "event_test_001",
        "ProgressByEventId": {"event_test_001": {"Currency": 100, "TotalCurrencyEarned": 100,
            "ProgressionLevel": 1, "HighestProgressionReached": 1, "LastUpdateUnix": now,
            "UnlockedMilestones": ["data_100"], "ClaimedRewards": []}}}}
    var file = FileAccess.open("user://idle_ai_save.json", FileAccess.WRITE)
    file.store_string(JSON.stringify(fixture))
    file.close()
    root.size = Vector2i(720, 1280)
    var game = load("res://game.tscn").instantiate()
    root.add_child(game)
    for frame in range(20): await process_frame
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit()
    for frame in range(5): await process_frame
    for input in game.get_node("ShopPage").find_children("*", "LineEdit", true, false):
        if input.placeholder_text == "ENTER CODE":
            input.text = "event-force:event_test_001"
            input.text_submitted.emit(input.text)
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit()
    for frame in range(5): await process_frame
    var entry = game.get_node("MapPage").find_child("EventMapEntry", true, false)
    check(entry != null and entry.visible, "persistent Event map entry")
    check(entry.find_child("EventEntryName", true, false).text == "Test Protocol", "active event name")
    check(entry.find_child("EventEntryStatus", true, false).text == "ACTIVE", "active status")
    entry.pressed.emit()
    for frame in range(5): await process_frame
    var page = game.get_node("EventPage")
    check(page.visible and not game.get_node("MapPage").visible, "Event opens from map")
    check(not game.get_node("BottomBar").visible, "global bottom navigation hidden inside Event")
    check(abs(page.get_global_rect().end.y - game.get_global_rect().end.y) < 0.5,
        "Event fills the space previously reserved for global navigation")
    var image_checks = load("res://tests/Events/image_layout_checks.gd")
    await image_checks.verify_source_sizes(page, self, check)
    for viewport_size in [Vector2i(1080, 2400), Vector2i(720, 1600), Vector2i(540, 960)]:
        root.size = viewport_size
        for frame in range(6): await process_frame
        image_checks.verify(page, check)
        check(page.find_children("EventGenerator_*", "Control", true, false).size() == 5, "all Test Protocol images checked")
    root.size = Vector2i(720, 1280)
    for frame in range(6): await process_frame
    var upgrade = page.find_child("EventUpgradeButton", true, false)
    var identity = upgrade.get_instance_id()
    check(not upgrade.disabled, "affordable upgrade enabled")
    upgrade.pressed.emit()
    check(saved_progress().ProgressionLevel == 2, "upgrade saved")
    check(abs(saved_progress().Currency - (saved_progress().TotalCurrencyEarned - 5)) < 0.001, "event currency spent")
    page.find_child("EventRewardsTab", true, false).pressed.emit()
    var reward = page.find_child("EventReward_data_100", true, false)
    check(reward.is_visible_in_tree() and not reward.disabled, "Rewards tab has claimable milestone")
    reward.pressed.emit()
    check(saved_progress().ClaimedRewards.has("data_100"), "claimed reward saved")
    check(saved_progress().DeveloperCredits == 1, "placeholder reward credited")
    reward.pressed.emit()
    check(saved_progress().DeveloperCredits == 1, "repeated claim blocked")
    page.find_child("EventShopTab", true, false).pressed.emit()
    check(page.find_child("EventShopPanel", true, false).is_visible_in_tree()
        and page.find_children("EventShop_*Card", "Control", true, false).size() == 8, "Configured sandbox Shop navigation")
    page.find_child("EventProgressTab", true, false).pressed.emit()
    var before = float(page.find_child("EventCurrency", true, false).text.split(" ")[0])
    var after = before
    for tick in range(8):
        await create_timer(1.0).timeout
        after = float(page.find_child("EventCurrency", true, false).text.split(" ")[0])
        if after > before: break
    check(after > before, "online generator produces event currency")
    check(upgrade.get_instance_id() == identity, "production ticks preserve controls")
    root.size = Vector2i(720, 1240)
    for frame in range(4): await process_frame
    check(page.visible, "event view adapts to viewport resize")
    root.size = Vector2i(720, 1280)
    for frame in range(4): await process_frame
    for button in page.find_children("*", "Button", true, false):
        check(button.has_theme_stylebox_override("pressed") and button.has_theme_stylebox_override("hover_pressed")
            and button.has_theme_stylebox_override("disabled"), "button theme states: " + str(button.name))
        check(button.get_global_rect().end.x <= 720.5, "button fits mobile width: " + str(button.name))
    page.find_child("EventCloseButton", true, false).pressed.emit()
    check(not page.visible and game.get_node("MapPage").visible, "close returns to map")
    entry.pressed.emit()
    check(upgrade.get_instance_id() == identity, "reopen preserves controls")
    page.find_child("EventCloseButton", true, false).pressed.emit()
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit()
    for frame in range(12): await process_frame
    var code_input = null
    for input in game.get_node("ShopPage").find_children("*", "LineEdit", true, false):
        if input.placeholder_text == "ENTER CODE": code_input = input
    check(code_input != null, "existing developer code box discovered")
    if code_input != null:
        code_input.text = "event-end"
        code_input.text_submitted.emit("event-end")
        check(saved_progress().Ended, "developer expiration persisted")
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit()
    for frame in range(5): await process_frame
    check(entry.visible, "map entry remains visible after event ends")
    check(entry.find_child("EventEntryName", true, false).text == "No active event", "inactive card state")
    entry.pressed.emit()
    check(page.find_child("EventStatus", true, false).text == "EVENT ENDED", "ended view state")
    check(upgrade.disabled, "expired upgrade disabled")
    check(page.find_child("EventReward_data_100", true, false).disabled, "claimed reward stays disabled")
    var expired_currency = page.find_child("EventCurrency", true, false).text
    await create_timer(1.2).timeout
    check(page.find_child("EventCurrency", true, false).text == expired_currency, "expired production stops")
    check(game.has_node("EventRuntime/EventTickTimer"), "single event timer path")
    if code_input != null:
        code_input.text = "event-force:event_test_001"
        code_input.text_submitted.emit(code_input.text)
        check(not saved_progress().Ended, "force tool reactivates chosen event")
        check(saved_progress().ClaimedRewards.has("data_100"), "force preserves claimed rewards")
        var normal_before = JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json"))
        code_input.text = "event-add:1000"
        code_input.text_submitted.emit(code_input.text)
        check(saved_progress().Currency >= 1000, "currency debug tool")
        var normal_after = JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json"))
        check(normal_before.Tokens == normal_after.Tokens and normal_before.DataShards == normal_after.DataShards,
            "event currency remains isolated")
        code_input.text = "event-reset"
        code_input.text_submitted.emit(code_input.text)
        check(saved_progress().Currency == 0 and saved_progress().ProgressionLevel == 1,
            "test reset tool clears progression")
        check(saved_progress().ClaimedRewards.is_empty(), "test reset clears test claims")
    page.find_child("EventCloseButton", true, false).pressed.emit()
    check(game.get_node("BottomBar").visible, "closing Event restores bottom navigation before native input")
    var shop_button = game.get_node("BottomBar/Margin/HBox/ShopButton")
    var click = InputEventMouseButton.new()
    click.button_index = MOUSE_BUTTON_LEFT
    click.position = shop_button.get_global_rect().get_center()
    click.global_position = click.position
    click.pressed = true
    Input.parse_input_event(click)
    Input.flush_buffered_events()
    for frame in range(2): await process_frame
    click = click.duplicate()
    click.pressed = false
    Input.parse_input_event(click)
    Input.flush_buffered_events()
    for frame in range(2): await process_frame
    check(not page.visible and game.get_node("ShopPage").visible, "native bottom navigation works after leaving Event")
    game.queue_free()
    for frame in range(4): await process_frame
    # A saved future cursor acts as an injected clock for the ending-soon UI.
    # This also checks scene teardown/recreation without retaining Event callbacks.
    var near_end = int(Time.get_unix_time_from_datetime_string("2030-01-01T00:00:00")) - 1800
    fixture.Events.LastEvaluationUnix = near_end
    fixture.Events.ProgressByEventId.event_test_001.LastUpdateUnix = near_end
    file = FileAccess.open("user://idle_ai_save.json", FileAccess.WRITE)
    file.store_string(JSON.stringify(fixture))
    file.close()
    game = load("res://game.tscn").instantiate()
    root.add_child(game)
    for frame in range(20): await process_frame
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit()
    for frame in range(5): await process_frame
    for input in game.get_node("ShopPage").find_children("*", "LineEdit", true, false):
        if input.placeholder_text == "ENTER CODE":
            input.text = "event-force:event_test_001"
            input.text_submitted.emit(input.text)
            input.text = "event-ending-soon"
            input.text_submitted.emit(input.text)
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit()
    for frame in range(4): await process_frame
    entry = game.get_node("MapPage").find_child("EventMapEntry", true, false)
    check(entry.find_child("EventEntryStatus", true, false).text == "ENDING SOON", "ending-soon map state")
    entry.pressed.emit()
    page = game.get_node("EventPage")
    check(page.find_child("EventCountdown", true, false).text.begins_with("Ends in 23h"), "remaining time display")
    check(page.visible, "recreated event scene opens")
    game.queue_free()
    for frame in range(4): await process_frame
    print("Events scene smoke: failures=%d" % failures)
    quit(1 if failures else 0)
