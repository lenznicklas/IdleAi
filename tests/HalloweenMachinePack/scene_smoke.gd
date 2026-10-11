extends SceneTree
var failures = 0
func _initialize():
    call_deferred("run_checks")
func check(value, message):
    if not value:
        push_error(message)
        failures += 1
func run_checks():
    check(OS.get_environment("XDG_DATA_HOME").contains("halloween-test"), "isolated profile required")
    root.size = Vector2i(720, 1280)
    var game = load("res://game.tscn").instantiate()
    root.add_child(game)
    for i in range(15): await process_frame
    var shop = game.get_node("ShopPage")
    shop.show()
    var content = game.get_node("ShopPage/ShopMargin/LayoutRoot/Scroll/ScrollMargin/Content")
    var bot = content.get_node("HalloweenOfferCard")
    var machine = content.get_node("HalloweenMachineOfferCard")
    check(machine.get_index() == bot.get_index() + 1, "machine offer directly below bot")
    var preview = machine.find_child("HalloweenMachinePreview", true, false)
    check(preview.texture != null, "promo imported")
    check(preview.texture.resource_path == "res://assets/shop/events/halloween_machine_skin.png", "single exact promo")
    check(preview.expand_mode == TextureRect.EXPAND_IGNORE_SIZE, "texture does not widen card")
    check(preview.stretch_mode == TextureRect.STRETCH_KEEP_ASPECT_CENTERED, "uncropped aspect preserved")
    for i in range(5): await process_frame
    check(abs(machine.size.x - bot.size.x) < 1, "matching card widths")
    check(machine.get_global_rect().end.x <= 721, "portrait card fits")
    for label in machine.find_children("*", "Label", true, false):
        check(label.size.x > 100, "text has usable width: " + label.text)
    var button = machine.find_children("*", "Button", true, false)[0]
    var funded = OS.get_environment("HALLOWEEN_FUNDED") == "1"
    check(button.text == "BUY • 100 SHARDS" and button.disabled == not funded, "unowned button affordability")
    for room in range(1,5):
        var card = game.find_child("MachineSkinCard_room_%d_halloween" % room, true, false)
        check(card != null, "catalog collection card room %d" % room)
        if card:
            check(card.find_children("*", "Button", true, false)[0].disabled, "unowned collection action disabled")
    if funded:
        button.pressed.emit()
        for i in range(3): await process_frame
        check(button.text == "OWNED" and button.disabled, "purchase changes offer to owned")
        for room in range(1,5):
            var card = game.find_child("MachineSkinCard_room_%d_halloween" % room, true, false)
            var equip = card.find_children("*", "Button", true, false)[0]
            check(equip.text == "EQUIP" and not equip.disabled, "purchase refreshes Collection equip")
            equip.pressed.emit()
            for i in range(2): await process_frame
            check(equip.text == "ACTIVE" and equip.disabled, "Collection equips room %d" % room)
        var file = FileAccess.open("user://idle_ai_machine_skins.json", FileAccess.READ)
        var save = JSON.parse_string(file.get_as_text())
        check(save.OwnedSkinIds.size() == 4, "purchase persists four skins")
        check(save.EquippedSkinByRoom.size() >= 4, "Collection equip persists")
    var scroll = game.get_node("ShopPage/ShopMargin/LayoutRoot/Scroll")
    scroll.scroll_vertical = 500
    for i in range(3): await process_frame
    check(scroll.scroll_vertical > 0, "shop scroll works")
    print("Halloween shop scene: adjacent offers, promo, portrait bounds, locked Collection and scrolling checked; failures: ", failures)
    game.queue_free()
    for i in range(3): await process_frame
    quit(1 if failures > 0 else 0)
