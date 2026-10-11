extends SceneTree
var failures = 0
func _initialize():
    call_deferred("run_checks")
func check(value, message):
    if not value:
        push_error(message)
        failures += 1
func run_checks():
    check(OS.get_environment("XDG_DATA_HOME").contains("archive"), "isolated profile required")
    root.size = Vector2i(720,1280)
    var game = load("res://game.tscn").instantiate()
    root.add_child(game)
    for i in range(15): await process_frame
    var stats = game.get_node("StatsOverlay")
    var list = game.get_node("StatsOverlay/StatsPanel/Margin/Scroll/VBox")
    check(list.get_child(0).name == "GooglePlayLeaderboards", "leaderboards first")
    check(list.get_child(1).name == "AiArchiveEntry", "archive second")
    check(list.get_child(2).text == "STATISTICS", "stats third")
    game.find_child("StatsCard", true, false).pressed.emit()
    await create_timer(.5).timeout
    var scroll = game.get_node("StatsOverlay/StatsPanel/Margin/Scroll")
    scroll.scroll_vertical = 70
    await process_frame
    var saved_scroll = scroll.scroll_vertical
    var button = list.get_node("AiArchiveEntry").find_children("*", "Button", true, false)[0]
    button.pressed.emit()
    for i in range(3): await process_frame
    var archive = game.get_node("AiArchiveOverlay")
    check(archive.visible, "archive opens")
    var category_buttons = archive.find_children("*", "Button", true, false)
    check(category_buttons.size() == 7, "six categories and back")
    var back = category_buttons[0]
    check(not back.visible, "back hidden in overview")
    for index in range(1,7):
        var category = archive.find_children("*", "Button", true, false)[index]
        category.pressed.emit()
        for i in range(3): await process_frame
        check(back.visible, "category back visible")
        for label in archive.find_children("*", "Label", true, false):
            check(label.get_global_rect().end.x <= 721, "portrait label overflow: " + label.text)
        back.pressed.emit()
        for i in range(2): await process_frame
    archive.find_child("CloseButton", true, false).pressed.emit()
    await process_frame
    check(not archive.visible and stats.visible, "close returns to stats")
    check(scroll.scroll_vertical == saved_scroll, "stats scroll preserved")
    print("AI Archive scene: ordering, six category pages, portrait bounds, navigation and Stats state passed")
    game.queue_free()
    for i in range(3): await process_frame
    quit(1 if failures > 0 else 0)
