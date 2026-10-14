extends SceneTree

# Run from the main project with an isolated XDG_DATA_HOME containing
# "singularity-performance". Creates its own 49-sector fixture.
var failures := 0

func check(value, message):
    if not value:
        push_error(message)
        failures += 1

func _initialize():
    call_deferred("run_checks")

func drag_at(point, movement):
    var drag = InputEventScreenDrag.new()
    drag.index = 0
    drag.position = point
    drag.relative = movement
    Input.parse_input_event(drag)
    Input.flush_buffered_events()

func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("singularity-performance"):
        push_error("isolated singularity-performance profile required")
        quit(1)
        return
    var sectors = []
    for sector in range(49):
        var nodes = []
        for index in range(8):
            var node = {"Type": index % 4 + 1, "Level": 9}
            if index == 0:
                node.merge({"BotRarity": 0, "BotDurabilityInitialized": true,
                    "BotDurabilitySecondsRemaining": 1e6})
            nodes.append(node)
        sectors.append({"CoreUnlocked": true, "CoreLevel": 9, "Nodes": nodes})
    var file = FileAccess.open("user://singularity_save.json", FileAccess.WRITE)
    file.store_string(JSON.stringify({"SaveVersion": 5, "Unlocked": true,
        "Matter": 1e12, "Sectors": sectors}))
    file.close()
    root.size = Vector2i(720, 1280)
    var game = load("res://game.tscn").instantiate()
    root.add_child(game)
    for frame in range(20): await process_frame
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit()
    for frame in range(5): await process_frame
    for button in game.get_node("MapPage").find_children("*", "Button", true, false):
        for label in button.find_children("*", "Label", true, false):
            if label.text == "ROOM 5": button.pressed.emit()
    for frame in range(10): await process_frame
    var page = game.get_node("SingularityPage")
    check(page.visible, "Singularity opened")
    var world = page.find_child("SingularityMapWorld", true, false)
    var viewport = page.find_child("SingularityMapViewport", true, false)
    var first = world.get_node("Sector_1")
    var first_id = first.get_instance_id()
    var bot = first.find_child("SingularityMapBot_0", true, false)
    check(bot != null, "bot fixture rendered")
    var frame_panel = first.get_child(0)
    var style_id = frame_panel.get_theme_stylebox("panel").get_instance_id()
    var point = viewport.get_global_rect().position + Vector2(10, 10)
    var touch = InputEventScreenTouch.new()
    touch.index = 0
    touch.position = point
    touch.pressed = true
    Input.parse_input_event(touch)
    await process_frame
    var start = world.position
    for frame in range(10):
        drag_at(point, Vector2(-2, 0))
        await process_frame
    check(world.position != start, "touch drag moves map")
    check(frame_panel.get_theme_stylebox("panel").get_instance_id() == style_id,
        "ordinary scrolling retains sector style resource")
    drag_at(point, Vector2(-1900, 0))
    await process_frame
    check(not first.visible, "distant sector hidden")
    check(first.process_mode == Node.PROCESS_MODE_DISABLED, "distant sector processing disabled")
    var hidden_bot_position = bot.position
    for frame in range(3): await process_frame
    check(bot.position == hidden_bot_position, "hidden bot animation paused")
    drag_at(point, Vector2(1900, 0))
    await process_frame
    check(first.visible, "returning sector visible")
    check(first.get_instance_id() == first_id, "returning sector reused")
    # Visit distant map cells to exercise bounded eviction.
    for movement in [Vector2(1900, 0), Vector2(0, 1900), Vector2(-3800, 0),
        Vector2(0, -3800), Vector2(3800, 0)]:
        drag_at(point, movement)
        await process_frame
    var count = 0
    for child in world.get_children():
        if child.name.begins_with("Sector_"): count += 1
    check(count <= 24, "sector cache remains bounded")
    touch.pressed = false
    Input.parse_input_event(touch)
    game.queue_free()
    for frame in range(3): await process_frame
    print("Singularity performance scene smoke: failures=%d" % failures)
    quit(1 if failures else 0)
