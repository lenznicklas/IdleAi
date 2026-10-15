extends SceneTree

var failures := 0
var checks := 0
var game
var page
var code
func check(ok, message):
    checks += 1
    if not ok:
        failures += 1
        push_error(message)
func _initialize(): call_deferred("run_checks")
func frames(count := 6):
    for i in range(count): await process_frame
func command(text):
    code.text = text
    code.text_submitted.emit(text)
func progress(): return JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json")).Events.ProgressByEventId.event_test_001
func find(name): return page.find_child(name, true, false)
func open_event():
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit(); await frames()
    for input in game.get_node("ShopPage").find_children("*", "LineEdit", true, false):
        if input.placeholder_text == "ENTER CODE": code = input
    command("event-force:event_test_001")
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit(); await frames()
    game.get_node("MapPage").find_child("EventMapEntry", true, false).pressed.emit(); await frames()
    page = game.get_node("EventPage")
    game.get_node("EventRuntime/EventTickTimer").stop()
    find("EventBotsTab").pressed.emit(); await frames()
func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("event-bots-test") or not "--event-tools" in OS.get_cmdline_user_args():
        push_error("Use isolated event-bots-test profile with --event-tools"); quit(1); return
    var now := int(Time.get_unix_time_from_system())
    var file = FileAccess.open("user://idle_ai_save.json", FileAccess.WRITE)
    file.store_string(JSON.stringify({"SaveVersion":22,"LastSaveUnix":now,"Rooms":[],"Events":{
        "Version":2,"LastEvaluationUnix":now,"LastEventId":"event_test_001","ProgressByEventId":{
            "event_test_001":{"Currency":100,"TotalCurrencyEarned":100,"LastUpdateUnix":now}}}})); file.close()
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25); await open_event()
    check(page.find_children("EventBot_*Card", "Control", true, false).size() == 4, "Four cached Test Protocol bot cards")
    check(find("EventBot_protocol_helper").text == "EQUIP", "Early helper available")
    check(find("EventBot_protocol_neural").disabled, "Locked epic cannot equip")
    var before = find("EventProduction").text
    find("EventBot_protocol_helper").pressed.emit(); await frames()
    check(progress().ActiveBotId == "protocol_helper", "Equip saved immediately")
    check(find("EventProduction").text != before, "Equip immediately updates central production UI")
    check(find("EventBot_protocol_helper").text == "EQUIPPED" and find("EventBot_protocol_helper").disabled, "Equipped state explicit")
    for viewport_size in [Vector2i(1080,2400), Vector2i(720,1600), Vector2i(540,960)]:
        root.size = viewport_size; await frames()
        var scroll = find("EventScroll")
        for card in page.find_children("EventBot_*Card", "Control", true, false):
            check(card.size.y == 364 and card.size.x <= scroll.size.x + 1, "Stable mobile card bounds " + str(viewport_size))
            for image in card.find_children("EventImage", "TextureRect", true, false):
                var area = image.get_parent().get_parent()
                check(area.get_global_rect().encloses(image.get_global_rect()), "Bot image fits reusable image slot")
        find("EventBotDetails_protocol_prototype").pressed.emit(); await frames()
        var modal = find("EventBotDetail")
        await create_timer(.35).timeout
        check(modal.visible, "Shared room preview opens")
        check(find("PreviewTitle").text == "Prototype Intelligence", "Detail correct identity")
        var details = find("BotDetailText").text
        check(details.contains("Complete Test Protocol") and not details.contains("major_complete"), "Requirement uses player-facing challenge title")
        check(details.contains("Event production") and details.contains("LOCKED"), "Detail includes bonus and ownership status")
        check(find("EventBotDetailEquip") == null, "Tap-to-close preview leaves equip on bot card")
        check(not game.get_node("BottomBar").visible, "Global bar hidden behind bot detail")
        var panel = find("PreviewPanel")
        check(panel.size.x >= page.size.x * .8 and panel.size.y >= page.size.y * .8, "Room preview fills nearly the whole screen")
        var image = modal.find_children("*", "TextureRect", true, false)[0]
        check(image.size.y >= 250 and image.stretch_mode == TextureRect.STRETCH_KEEP_ASPECT_CENTERED, "Large bot graphic preserves aspect")
        for position in [modal.get_global_rect().position + Vector2(2, 2), panel.get_global_rect().get_center()]:
            var tap = InputEventMouseButton.new()
            tap.button_index = MOUSE_BUTTON_LEFT; tap.pressed = false; tap.position = position
            modal.gui_input.emit(tap); await frames()
            check(not modal.visible and page.visible, "Tap outside or on preview closes only detail")
            find("EventBotDetails_protocol_prototype").pressed.emit()
            await create_timer(.35).timeout
        root.go_back_requested.emit(); await frames()
        check(not modal.visible and page.visible and find("EventBotsTab").button_pressed, "Back closes detail first and preserves Bots tab")
    command("event-add:5000")
    find("EventShopTab").pressed.emit(); await frames()
    check(not find("EventShop_optimizer_license").disabled, "Alternative shop acquisition available")
    find("EventShop_optimizer_license").pressed.emit(); await frames()
    check(find("EventShop_optimizer_license").disabled and find("EventShop_optimizer_license").text == "OWNED", "Shop prevents duplicate bot purchase")
    find("EventBotsTab").pressed.emit(); await frames()
    find("EventBot_protocol_optimizer").pressed.emit(); await frames()
    check(progress().ActiveBotId == "protocol_optimizer" and find("EventBot_protocol_optimizer").text == "EQUIPPED", "Card equip replaces bot")
    find("EventBotDetails_protocol_optimizer").pressed.emit(); await frames()
    check(find("BotDetailText").text.contains("EQUIPPED"), "Shared preview reflects equipped state")
    root.go_back_requested.emit(); await frames()
    command("event-ready-prestige")
    find("EventPrestigeTab").pressed.emit(); await frames()
    find("EventPrestigeButton").pressed.emit(); find("ConfirmEventPrestige").pressed.emit(); await frames()
    check(progress().ActiveBotId == "protocol_optimizer" and "protocol_neural" in progress().OwnedBots, "Prestige preserves equipped bot and unlocks epic")
    find("EventBotsTab").pressed.emit(); await frames()
    var cards = page.find_children("EventBot_*Card", "Control", true, false)
    var identities = cards.map(func(card): return card.get_instance_id())
    for i in range(10):
        find("EventProgressTab").pressed.emit(); find("EventBotsTab").pressed.emit()
    await frames()
    check(page.find_children("EventBot_*Card", "Control", true, false).map(func(card): return card.get_instance_id()) == identities, "Tab switches reuse bot nodes")
    for card in cards:
        var equip = card.find_child("EventBot_*", true, false)
        check(equip.get_signal_connection_list("pressed").size() == 1, "No duplicate bot signals")
    game.queue_free(); await frames()
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25); await open_event()
    check(find("EventBot_protocol_optimizer").text == "EQUIPPED", "Reload preserves equipped bot")
    command("event-challenge-progress:major_complete:50"); await frames()
    check("protocol_prototype" in progress().OwnedBots and not find("EventBot_protocol_prototype").disabled, "Major completion unlocks Legendary immediately")
    find("EventBot_protocol_prototype").pressed.emit(); await frames()
    check(progress().ActiveBotId == "protocol_prototype", "Legendary can replace equipped bot")
    command("event-bots-reset"); await frames()
    check(progress().OwnedBots.is_empty() and progress().ActiveBotId == null, "Debug ownership reset preserves unlock eligibility")
    command("event-bot-grant:protocol_neural"); command("event-bot-equip:protocol_neural"); await frames()
    check(progress().ActiveBotId == "protocol_neural" and "protocol_neural" in progress().OwnedBots, "Specific debug grant/equip use central rules")
    command("event-end"); await frames()
    check(find("EventBot_protocol_helper").disabled and find("EventBot_protocol_neural").disabled, "Expired bots read-only")
    find("EventCloseButton").pressed.emit(); await frames()
    check(not page.visible and game.get_node("BottomBar").visible, "Leaving Bots restores global navigation")
    print("Event bots smoke: ", checks, " checks, failures=", failures)
    game.queue_free(); await frames(); quit(1 if failures else 0)
