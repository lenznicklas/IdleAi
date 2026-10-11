extends SceneTree

# Isolated profile fixture: Singularity unlocked, ample Matter, first four
# Nodes Compute/Amplifier/Cooling/Quantum at level 9, Core at level 9.
var failures = 0

func _initialize():
    call_deferred("run_checks")

func check(value, message):
    if not value:
        push_error(message)
        failures += 1

func snapshot(view):
    var result = []
    for control in view.find_children("*", "Control", true, false):
        if not control.is_visible_in_tree(): continue
        result.append([control.get_instance_id(), control.position, control.size,
            control.scale, control.rotation, control.pivot_offset, control.modulate, control.self_modulate])
    return result

func button_with_prefix(view, prefix):
    for button in view.find_children("*", "Button", true, false):
        if button.text.begins_with(prefix): return button
    return null

func verify_upgrades(view, prefix, count):
    for i in range(8): await process_frame
    var stable = snapshot(view)
    var upgrade = button_with_prefix(view, prefix)
    check(upgrade != null, "upgrade button exists: " + prefix)
    if upgrade == null: return
    for i in range(count):
        var old_text = upgrade.text
        upgrade.pressed.emit()
        check(upgrade.text != old_text, "successful upgrade updates existing button")
        for frame in range(8):
            await process_frame
            if snapshot(view) != stable:
                var changed = snapshot(view)
                for index in range(min(stable.size(), changed.size())):
                    if stable[index] != changed[index]:
                        print("Detail geometry change: ", stable[index], " -> ", changed[index])
                check(false, "upgrade changed control identity, geometry or modulation: " + prefix)
                return
    for i in range(30):
        await process_frame
        check(snapshot(view) == stable, "timer refresh must not alter detail view")
    var pointer = upgrade.get_global_rect().get_center()
    Input.parse_input_event(InputEventMouseMotion.new())
    var press = InputEventMouseButton.new()
    press.button_index = MOUSE_BUTTON_LEFT
    press.position = pointer
    press.global_position = pointer
    press.pressed = true
    Input.parse_input_event(press)
    for frame in range(3):
        await process_frame
        check(snapshot(view) == stable, "held native button state must preserve geometry")
    press = InputEventMouseButton.new()
    press.button_index = MOUSE_BUTTON_LEFT
    press.position = pointer
    press.global_position = pointer
    press.pressed = false
    Input.parse_input_event(press)
    for frame in range(3):
        await process_frame
        check(snapshot(view) == stable, "release must preserve geometry")

func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("detail-stability"):
        push_error("isolated detail-stability profile required")
        quit(1)
        return
    root.size = Vector2i(720, 1280)
    var game = load("res://game.tscn").instantiate()
    root.add_child(game)
    for i in range(15): await process_frame
    var navigation = game.get_node("BottomBar/Margin/HBox/MapButton")
    navigation.pressed.emit()
    for i in range(5): await process_frame
    for button in game.get_node("MapPage").find_children("*", "Button", true, false):
        for label in button.find_children("*", "Label", true, false):
            if label.text == "ROOM 5": button.pressed.emit()
    for i in range(8): await process_frame
    var page = game.get_node("SingularityPage")
    var view = game.get_node("SingularityDetailOverlay")
    var core = page.find_child("CoreButton", true, false)
    var nodes = []
    for child in core.get_parent().get_children():
        if child is Button and child != core: nodes.append(child)
    check(nodes.size() == 8, "eight map nodes discovered")
    if nodes.size() != 8:
        quit(1)
        return
    var panel_size = Vector2.ZERO
    for node_index in range(4):
        nodes[node_index].pressed.emit()
        check(view.visible, "Node opens immediately")
        await verify_upgrades(view, "UPGRADE TO LEVEL", 12)
        var panel = view.get_child(1).get_child(0)
        if panel_size == Vector2.ZERO: panel_size = panel.size
        check(panel.size == panel_size, "same panel size across Node types")
        var stable = snapshot(view)
        for i in range(5):
            view.find_child("CloseButton", true, false).pressed.emit()
            check(not view.visible, "Node closes immediately")
            nodes[node_index].pressed.emit()
            check(view.visible, "Node reopens immediately")
            for frame in range(4): await process_frame
            check(snapshot(view) == stable, "same Node reopen retains controls and transforms")
        view.find_child("CloseButton", true, false).pressed.emit()

    core.pressed.emit()
    await verify_upgrades(view, "UPGRADE CORE", 12)
    check(view.get_child(1).get_child(0).size == panel_size, "Core retains same panel size")
    var stable_core = snapshot(view)
    var guide = button_with_prefix(view, "Formation guide")
    guide.pressed.emit()
    for frame in range(8): await process_frame
    check(view.get_child(1).get_child(0).size == panel_size, "guide does not resize panel")
    guide.pressed.emit()
    for frame in range(8): await process_frame
    check(snapshot(view) == stable_core, "guide closes without transform residue")
    view.find_child("CloseButton", true, false).pressed.emit()
    core.pressed.emit()
    for frame in range(8): await process_frame
    check(snapshot(view) == stable_core, "Core reopen retains controls")

    view.find_child("CloseButton", true, false).pressed.emit()
    var saved = JSON.parse_string(FileAccess.get_file_as_string("user://singularity_save.json"))
    var empty_index = -1
    for index in range(4, 8):
        if saved.Sectors[0].Nodes[index].Type == 0:
            empty_index = index
            break
    check(empty_index >= 0, "fixture has an empty Node for purchase")
    if empty_index < 0:
        quit(1)
        return
    nodes[empty_index].pressed.emit()
    for frame in range(8): await process_frame
    var buy = button_with_prefix(view, "BUY")
    check(buy != null, "empty Node offers purchase")
    if buy == null:
        quit(1)
        return
    buy.pressed.emit()
    for frame in range(8): await process_frame
    check(view.get_child(1).get_child(0).size == panel_size, "purchase does not resize panel")
    await verify_upgrades(view, "UPGRADE TO LEVEL", 2)
    print("Singularity detail stability: all four Node types, Core, repeated upgrades, reopen, guide and purchase; failures=%d" % failures)
    game.queue_free()
    for i in range(3): await process_frame
    quit(1 if failures > 0 else 0)
