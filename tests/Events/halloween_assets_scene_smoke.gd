extends SceneTree

var checks := 0
var failures := 0
var game
var page
var code
const DIR := "res://assets/halloween_event/"
const FILES := ["event_icon", "background", "souls", "cursed_core", "abandoned_terminal", "haunted_workstation", "possessed_server", "cursed_datacenter", "soul_processor", "nightmare_ai_core", "ghost_bot", "pumpkin_bot", "skeleton_bot", "reaper_bot", "reward_chest", "machine_skin_pack", "bot_skin_pack"]
func check(ok, message):
    checks += 1
    if not ok:
        failures += 1
        push_error(message)
func _initialize(): call_deferred("run_checks")
func frames(n := 6):
    for i in range(n): await process_frame
func command(text): code.text = text; code.text_submitted.emit(text)
func find(name): return page.find_child(name, true, false)
func tab(name): find("Event" + name + "Tab").pressed.emit(); await frames()
func images(node, filename):
    return node.find_children("*", "TextureRect", true, false).filter(func(image): return image.texture != null and image.texture.resource_path == DIR + filename + ".png")
func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("halloween-assets-test") or not "--event-tools" in OS.get_cmdline_user_args():
        push_error("Use isolated halloween-assets-test profile with --event-tools"); quit(1); return
    for filename in FILES:
        var path = DIR + filename + ".png"
        check(ResourceLoader.exists(path), "Imported exact-case resource: " + path)
        var texture = load(path)
        check(texture is Texture2D and texture.get_width() > 0, "Valid texture: " + filename)
    var now := int(Time.get_unix_time_from_system())
    var save = FileAccess.open("user://idle_ai_save.json", FileAccess.WRITE)
    save.store_string(JSON.stringify({"SaveVersion":22,"LastSaveUnix":now,"Rooms":[],"Events":{"Version":2,"LastEvaluationUnix":now}})); save.close()
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25)
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit(); await frames()
    for input in game.get_node("ShopPage").find_children("*", "LineEdit", true, false):
        if input.placeholder_text == "ENTER CODE": code = input
    # The production catalog, without any force command, must select Halloween now.
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit(); await frames()
    var initial_entry = game.get_node("MapPage").find_child("EventMapEntry", true, false)
    check(initial_entry.find_child("EventEntryName", true, false).text == "Halloween Protocol", "Live catalog selects Halloween without debug forcing")
    initial_entry.pressed.emit(); await frames(); page = game.get_node("EventPage")
    check(find("EventTitle").text == "HALLOWEEN PROTOCOL", "Fresh Event renders the actual live Halloween definition")
    # Open Test Protocol first: switching the active definition must invalidate
    # cached cards and textures without recreating or reopening EventPage.
    command("event-force:event_test_001")
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit(); await frames()
    game.get_node("MapPage").find_child("EventMapEntry", true, false).pressed.emit(); await frames()
    page = game.get_node("EventPage")
    var page_id = page.get_instance_id()
    check(find("EventTitle").text == "Test Protocol", "Test Protocol initially selected")
    command("halloween-force")
    await frames()
    check(find("EventTitle").text == "HALLOWEEN PROTOCOL", "Already-open Event follows active Halloween definition")
    check(find("EventGenerator_abandoned_terminal") != null and find("EventGenerator_data_terminal") == null, "Live switch replaces cached generator cards")
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit(); await frames()
    var entry = game.get_node("MapPage").find_child("EventMapEntry", true, false)
    check(images(entry,"event_icon").size() == 1, "Map uses real event icon")
    var map_image = images(entry,"event_icon")[0]
    check(map_image.stretch_mode == TextureRect.STRETCH_KEEP_ASPECT_CENTERED and map_image.size.x < 100, "Map image remains bounded/aspect fitted")
    entry.pressed.emit(); await frames(); page = game.get_node("EventPage")
    game.get_node("EventRuntime/EventTickTimer").stop()
    check(images(page,"background").size() == 1, "Real full-screen background")
    var bg = find("EventBackground")
    check(bg.expand_mode == TextureRect.EXPAND_IGNORE_SIZE and bg.stretch_mode == TextureRect.STRETCH_KEEP_ASPECT_COVERED and bg.modulate.a <= .5, "Background covers without changing layout, with readable tint")
    command("event-unlock-generators"); command("event-unlock-bots")
    var machines = {"abandoned_terminal":"abandoned_terminal", "haunted_workstation":"haunted_workstation", "possessed_server":"possessed_server", "cursed_datacenter":"cursed_datacenter", "soul_processor":"soul_processor", "nightmare_core":"nightmare_ai_core"}
    for id in machines:
        check(images(find("EventGenerator_"+id), machines[id]).size() == 1, "Generator identity: " + id)
        check(images(find("EventGenerator_"+id), "souls").size() == 1, "Generator cost uses Souls")
        var expected_name = "Nightmare AI Core" if id == "nightmare_core" else id.replace("_", " ").capitalize()
        check(find("EventGenerator_"+id).find_child("GeneratorName", true, false).text == expected_name, "Halloween generator name: " + id)
    check(images(find("EventHeader"), "souls").size() == 1 and images(find("EventHeader"), "cursed_core").size() == 1, "Both header currencies")
    await tab("Prestige")
    check(images(find("EventPrestigePanel"), "souls").size() == 1 and images(find("EventPrestigePanel"), "cursed_core").size() == 1, "Prestige currencies")
    await tab("Bots")
    for id in ["ghost_bot","pumpkin_bot","skeleton_bot","reaper_bot"]:
        check(images(find("EventBot_"+id+"Card"), id).size() == 1, "Inventory bot: " + id)
        check(find("EventBot_"+id+"Card").find_child("BotName", true, false).text == id.replace("_", " ").capitalize(), "Halloween bot name: " + id)
        find("EventBotDetails_"+id).pressed.emit(); await frames()
        check(images(find("EventBotDetail"), id).size() == 1, "Detail bot: " + id)
        check(find("EventBotDetail").scale == Vector2.ONE, "No detail scale animation")
        find("EventBotDetailClose").pressed.emit(); await frames()
    await tab("Rewards")
    for id in ["ghost_bot","pumpkin_bot","skeleton_bot","reaper_bot","bot_skin_pack","machine_skin_pack"]:
        check(images(find("EventRewardsPanel"), id).size() >= 1, "Reward preview: " + id)
    await tab("Shop")
    check(images(find("EventShop_soul_cacheCard"), "reward_chest").size() == 1, "Chest shop icon")
    check(images(find("EventShop_soul_cacheCard"), "cursed_core").size() == 1, "Core shop cost")
    check(images(find("EventShop_bot_skinCard"), "bot_skin_pack").size() == 1, "Bot pack shop icon")
    check(images(find("EventShop_bot_skinCard"), "souls").size() == 1, "Soul shop cost")
    for size in [Vector2i(1080,2400),Vector2i(720,1600),Vector2i(540,960)]:
        root.size = size; await frames()
        for name in ["Progress","Prestige","Bots","Rewards","Shop","Archive","Challenges"]:
            await tab(name)
            load("res://tests/Events/image_layout_checks.gd").verify(page, check)
            for image in page.find_children("*","TextureRect",true,false):
                if image.texture == null or not image.is_visible_in_tree() or image == bg: continue
                check(image.expand_mode == TextureRect.EXPAND_IGNORE_SIZE and image.stretch_mode == TextureRect.STRETCH_KEEP_ASPECT_CENTERED, "All visible sprites fitted")
                check(image.get_global_rect().end.x <= page.get_global_rect().end.x+1, "No image horizontal overflow")
            check(find("EventScroll").scroll_horizontal == 0 and not game.get_node("BottomBar").visible, "Stable full-screen portrait navigation")
    command("event-end"); await tab("Archive")
    find("ViewArchive_halloween_2026").pressed.emit(); await frames(); await tab("Progress")
    for icon in ["event_icon","souls","cursed_core"]:
        check(images(find("EventEndIcons"),icon).size() == 1, "Final results identity/currency: " + icon)
    await tab("Archive")
    for icon in ["event_icon","souls","cursed_core"]:
        check(images(find("ArchiveIcons_halloween_2026"),icon).size() == 1, "Archive identity/currency: " + icon)
    var seen = {}
    for image in page.find_children("*","TextureRect",true,false):
        if image.texture == null or not image.texture.resource_path.begins_with(DIR): continue
        var path = image.texture.resource_path
        if path in seen: check(seen[path] == image.texture.get_instance_id(), "Shared cached texture: " + path)
        seen[path] = image.texture.get_instance_id()
    check(seen.size() == 17, "All 17 source assets represented without placeholder duplication")
    # Explicit archive browsing is intentionally historical; reopening follows active data.
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit(); await frames()
    entry.pressed.emit(); await frames()
    # Switch away from ended results, then back again with every tab instantiated.
    command("halloween-force"); await frames()
    for event_id in ["event_test_001", "halloween_2026", "event_test_001", "halloween_2026"]:
        command("event-force:" + event_id); await frames()
        var halloween = event_id == "halloween_2026"
        check(page.get_instance_id() == page_id, "Definition changes reuse the existing Event scene")
        check(find("EventTitle").text == ("HALLOWEEN PROTOCOL" if halloween else "Test Protocol"), "Title follows current definition: " + event_id)
        check(find("EventBackground").texture != null if halloween else find("EventBackground").texture == null, "Background follows current definition")
        check(find("EventGenerator_abandoned_terminal" if halloween else "EventGenerator_data_terminal") != null, "Generator definitions restored")
        check(find("EventGenerator_data_terminal" if halloween else "EventGenerator_abandoned_terminal") == null, "Previous generator cards removed")
        for name in ["Progress", "Prestige", "Bots", "Rewards", "Challenges", "Shop"]:
            await tab(name)
            var forbidden = ["Test Protocol", "Event Data", "Protocol Cores", "Data Terminal", "Compute Rack", "Helper Drone", "Optimizer Bot"] if halloween else ["Souls", "Cursed Cores", "Ghost Bot", "Pumpkin Bot"]
            for label in page.find_children("*", "Label", true, false):
                if not label.is_visible_in_tree(): continue
                for text in forbidden:
                    check(not text in label.text, name + " has no stale text: " + text)
            if not halloween:
                for image in page.find_children("*", "TextureRect", true, false):
                    if image.is_visible_in_tree() and image.texture != null:
                        check(not image.texture.resource_path.begins_with(DIR), name + " restores Test Protocol images")
            check(not game.get_node("BottomBar").visible, "Definition changes keep Event full-screen")
        game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit(); await frames()
        check(entry.find_child("EventEntryName", true, false).text == ("Halloween Protocol" if halloween else "Test Protocol"), "Map follows active event")
        check(images(entry, "event_icon").size() == (1 if halloween else 0), "Map icon follows active event")
        entry.pressed.emit(); await frames()
    print("Halloween PNG assets smoke: ",checks," checks, failures=",failures)
    game.queue_free(); await frames(); quit(1 if failures else 0)
