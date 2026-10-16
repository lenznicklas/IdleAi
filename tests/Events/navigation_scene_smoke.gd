extends SceneTree

# Real scene navigation and full-height layout, without developer Event overrides.
var failures := 0
var checks := 0
var game
var original_quit_on_back: bool
func check(value, message):
    checks += 1
    if not value:
        push_error(message)
        failures += 1
func _initialize(): call_deferred("run_checks")
func frames(count := 5):
    for i in range(count): await process_frame
func button(name): return game.get_node("BottomBar/Margin/HBox/" + name)
func page(): return game.get_node("EventPage")
func bar(): return game.get_node("BottomBar")
func enter_event():
    if not game.get_node("MapPage").visible: button("MapButton").pressed.emit()
    await frames()
    check(game.get_node("MapPage").find_child("EventEntryName",true,false).text == "Halloween Protocol", "Normal catalog selects Halloween despite saved Test Protocol ID")
    game.get_node("MapPage").find_child("EventMapEntry",true,false).pressed.emit()
    check(page().find_child("EventTitle",true,false).text == "HALLOWEEN PROTOCOL", "Normal Event open renders Halloween without developer commands")
    check(page().find_child("EventCurrency",true,false).text.contains("Souls"), "Normal open uses Souls")
    check(page().visible and not bar().visible, "Event hides the BottomBar synchronously")
    check(not quit_on_go_back, "Event intercepts system back instead of quitting")
    await frames()
func event_chrome(label):
    check(page().visible and not bar().is_visible_in_tree(), label + ": bar stays hidden")
    check(abs(page().get_global_rect().end.y - game.get_global_rect().end.y) < 0.5, label + ": full-height Event page")
    var tabs = page().find_child("EventTabs",true,false)
    check(abs(tabs.get_global_rect().end.y - (page().get_global_rect().end.y - 12)) < 1,
        label + ": tabs use freed bottom space")
func closed_to_map(label):
    check(not page().visible and game.get_node("MapPage").visible and bar().is_visible_in_tree(), label)
func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("event-navigation-test"):
        push_error("isolated event-navigation-test profile required"); quit(1); return
    original_quit_on_back = quit_on_go_back
    var now := int(Time.get_unix_time_from_system())
    var file = FileAccess.open("user://idle_ai_save.json",FileAccess.WRITE)
    file.store_string(JSON.stringify({"SaveVersion":22,"LastSaveUnix":now,"Rooms":[],"Events":{
        "Version":2,"LastEvaluationUnix":now,"LastEventId":"event_test_001","ProgressByEventId":{
            "event_test_001":{"Currency":500000,"TotalCurrencyEarned":500000,"LastUpdateUnix":now},
            "halloween_2026":{"Currency":1000000,"TotalCurrencyEarned":1000000,"LastUpdateUnix":now}}}})); file.close()
    file = FileAccess.open("user://singularity_save.json",FileAccess.WRITE)
    file.store_string(JSON.stringify({"SaveVersion":5,"Unlocked":true,"Matter":1000,"LastSaveUnix":now,
        "Sectors":[{"CoreUnlocked":true,"CoreLevel":1}]})); file.close()
    game = load("res://game.tscn").instantiate(); root.add_child(game)
    await frames(25)
    check(bar().visible,"Normal game starts with BottomBar")
    for target in ["Map", "Shop", "Lab", "Map"]:
        button(target + "Button").pressed.emit(); await frames()
        check(bar().is_visible_in_tree(), target + ": normal navigation retains bar")
    await enter_event()
    for viewport_size in [Vector2i(720,1280),Vector2i(540,960),Vector2i(1080,2400)]:
        root.size = viewport_size; await frames(6)
        for tab in ["Progress","Prestige","Bots","Rewards","Shop","Challenges"]:
            page().find_child("Event"+tab+"Tab",true,false).pressed.emit(); await frames(3)
            event_chrome(tab + " " + str(viewport_size))
    # Native event coordinates use the base viewport, matching the existing input smoke tests.
    root.size = Vector2i(720,1280); await frames(6)
    # Tap an Event tab in the area previously occupied by the global bar.
    var tap_position = page().find_child("EventShopTab",true,false).get_global_rect().get_center()
    var press = InputEventMouseButton.new(); press.button_index = MOUSE_BUTTON_LEFT
    press.position = tap_position; press.global_position = tap_position; press.pressed = true
    Input.parse_input_event(press); Input.flush_buffered_events(); await frames(2)
    press = press.duplicate(); press.pressed = false
    Input.parse_input_event(press); Input.flush_buffered_events(); await frames(2)
    check(page().find_child("EventShopPanel",true,false).is_visible_in_tree(), "Freed bottom space receives native Event input")
    # Simulate another room's deferred chrome restoration; Event must remain authoritative.
    bar().call_deferred("show"); await frames()
    event_chrome("deferred chrome restoration")
    var runtime = game.get_node("EventRuntime")
    runtime.notification(MainLoop.NOTIFICATION_APPLICATION_PAUSED)
    runtime.notification(MainLoop.NOTIFICATION_APPLICATION_RESUMED); await frames()
    event_chrome("app resume")
    page().find_child("EventCloseButton",true,false).pressed.emit()
    closed_to_map("Event close restores Map and bar immediately"); await frames()
    check(quit_on_go_back == original_quit_on_back,"Back policy restored outside Event")
    var first_room = null
    for control in game.get_node("MapPage").find_children("*","Button",true,false):
        if control.custom_minimum_size == Vector2(270,178) and control.position == Vector2(48,940): first_room = control
    check(first_room != null,"Normal room entry remains available after Event exit")
    if first_room != null:
        first_room.pressed.emit(); await frames()
        check(not game.get_node("MapPage").visible and not page().visible and bar().visible,"Event -> Map -> Room restores normal navigation")
    await enter_event()
    var cancel = InputEventAction.new(); cancel.action = "ui_cancel"; cancel.pressed = true
    Input.parse_input_event(cancel); Input.flush_buffered_events(); await frames()
    closed_to_map("Escape/controller back returns to Map")
    await enter_event()
    page().find_child("EventPrestigeTab",true,false).pressed.emit()
    page().find_child("EventPrestigeButton",true,false).pressed.emit()
    check(page().find_child("EventPrestigeConfirmation",true,false).visible,"Prestige confirmation opens")
    root.go_back_requested.emit(); await frames()
    check(not page().find_child("EventPrestigeConfirmation",true,false).visible,"First system back dismisses confirmation")
    event_chrome("back from Prestige confirmation")
    root.go_back_requested.emit(); await frames()
    closed_to_map("Android Window back returns to Map")
    check(quit_on_go_back == original_quit_on_back,"System back policy restored after handling event")
    for target in ["Map","Shop","Lab"]:
        await enter_event()
        button(target+"Button").pressed.emit(); await frames()
        check(not page().visible and game.get_node(target+"Page").visible and bar().visible,
            "Programmatic Event -> " + target + " restores bar")
    await enter_event()
    game.get_node("ShopPage").show(); await frames()
    check(not page().visible and bar().visible,"Direct global page open also leaves Event")
    button("MapButton").pressed.emit(); await frames()
    var singularity = game.get_node("MapPage").find_child("SingularityRoomNode",true,false)
    singularity.pressed.emit(); await frames()
    check(game.get_node("SingularityPage").visible and bar().visible,"Singularity opens with bar")
    await enter_event(); await frames(12)
    event_chrome("Singularity -> Map -> Event")
    page().find_child("EventCloseButton",true,false).pressed.emit(); await frames()
    singularity.pressed.emit(); await frames()
    check(game.get_node("SingularityPage").visible and bar().visible,"Event -> Map -> Singularity restores bar")
    await enter_event()
    game.get_node("SingularityPage").show(); await frames()
    check(not page().visible and bar().visible,"Direct Event -> Singularity restores bar")
    await enter_event()
    game.queue_free(); await frames(6)
    check(quit_on_go_back == original_quit_on_back,"Scene teardown releases Event back policy")
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25)
    check(bar().visible and not page().visible,"Reload starts outside Event with BottomBar restored")
    await enter_event(); root.go_back_requested.emit(); await frames()
    closed_to_map("Back works after scene recreation without stale callbacks")
    game.queue_free(); await frames(6)
    print("Event navigation smoke: ",checks," checks, failures=",failures)
    quit(1 if failures else 0)
