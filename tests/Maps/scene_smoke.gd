extends SceneTree

# Run with an isolated XDG_DATA_HOME containing "map-lock". Optional save
# fixtures can cover completed predecessors and already-unlocked rooms.
var failures = 0

func _initialize():
    call_deferred("run_checks")

func check(value, message):
    if not value:
        push_error(message)
        failures += 1

func snapshot(page):
    var result = []
    for label in page.find_children("*", "Label", true, false):
        result.append([label.get_instance_id(), label.text, label.position, label.size])
    for button in page.find_children("*", "Button", true, false):
        result.append([button.get_instance_id(), button.position, button.size])
    return result

func geometry(page):
    var result = []
    # The header's discovery count can naturally change width on unlock.
    # Only the room cards and their labels must preserve geometry.
    for button in page.find_children("*", "Button", true, false):
        var labels = button.find_children("*", "Label", true, false)
        if labels.size() < 4 or not labels[0].text.begins_with("ROOM "): continue
        result.append([button.get_instance_id(), button.position, button.size])
        for label in labels:
            result.append([label.get_instance_id(), label.position, label.size])
    return result

func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("map-lock"):
        push_error("isolated map-lock profile required")
        quit(1)
        return
    root.size = Vector2i(720, 1280)
    var game = load("res://game.tscn").instantiate()
    root.add_child(game)
    for i in range(15): await process_frame
    var save = JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json"))
    var navigation = game.get_node("BottomBar/Margin/HBox/MapButton")
    navigation.pressed.emit()
    var page = game.get_node("MapPage")
    for i in range(8): await process_frame

    var tested = 0
    for button in page.find_children("*", "Button", true, false):
        var labels = button.find_children("*", "Label", true, false)
        if labels.size() < 4 or not labels[0].text.begins_with("ROOM "):
            continue
        var index = int(labels[0].text.trim_prefix("ROOM ")) - 1
        var unlocked = index < save.Rooms.size() and save.Rooms[index].Unlocked
        if unlocked: continue
        tested += 1
        var previous = save.Rooms[index - 1]
        var expected = "UNLOCK ROOM %d FIRST" % index
        if previous.Unlocked:
            var missing = 0
            for slot in previous.Slots:
                if not slot.Unlocked: missing += 1
            expected = "UNLOCK %d MORE SLOT%s" % [missing, "" if missing == 1 else "S"]
            if missing == 0: expected = "UNLOCK • "
        check(labels[2].text == "LOCKED", "one authoritative state for Room %d" % (index + 1))
        check(labels[3].text.begins_with(expected), "requirement for Room %d: %s" % [index + 1, labels[3].text])
        for label in [labels[2], labels[3]]:
            check(label.autowrap_mode == TextServer.AUTOWRAP_OFF, "status must not wrap")
            var font = label.get_theme_font("font")
            var text_size = font.get_string_size(label.text, HORIZONTAL_ALIGNMENT_LEFT, -1, label.get_theme_font_size("font_size"))
            check(text_size.x <= label.size.x, "readable requirement must fit: " + label.text)
    check(tested > 0, "fixture must include locked rooms")

    var stable = snapshot(page)
    var end = Time.get_ticks_msec() + 3000
    while Time.get_ticks_msec() < end:
        await process_frame
        if snapshot(page) != stable:
            check(false, "map text, node identity or geometry changed while idle")
            break
    for i in range(5):
        navigation.pressed.emit()
        navigation.pressed.emit()
        for frame in range(8): await process_frame
        check(snapshot(page) == stable, "map reopen must retain text and geometry")

    if OS.get_environment("MAP_TEST_UNLOCK_ROOM2") == "1":
        var before = geometry(page)
        var room2 = null
        var room3_labels = []
        for button in page.find_children("*", "Button", true, false):
            var labels = button.find_children("*", "Label", true, false)
            if labels.size() < 4: continue
            if labels[0].text == "ROOM 2": room2 = button
            if labels[0].text == "ROOM 3": room3_labels = labels
        check(room2 != null and room3_labels.size() == 4, "normal room cards found")
        room2.pressed.emit()
        for frame in range(8): await process_frame
        var updated_save = JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json"))
        check(updated_save.Rooms[1].Unlocked, "Room 2 actually purchased through gameplay handler")
        navigation.pressed.emit()
        for frame in range(8): await process_frame
        check(room3_labels[3].text == "UNLOCK 7 MORE SLOTS", "Room 3 updates after Room 2 unlock")
        var after = geometry(page)
        check(after == before, "unlock transition preserves card and label geometry")
        stable = snapshot(page)
        end = Time.get_ticks_msec() + 3000
        while Time.get_ticks_msec() < end:
            await process_frame
            if snapshot(page) != stable:
                check(false, "requirement must settle after unlock")
                break
    print("Maps lock presentation: %d locked cards, idle stability, geometry and rapid reopen checked; failures=%d" % [tested, failures])
    game.queue_free()
    for i in range(3): await process_frame
    quit(1 if failures > 0 else 0)
