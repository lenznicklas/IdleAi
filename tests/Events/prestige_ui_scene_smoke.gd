extends SceneTree

var checks := 0
var failures := 0
var game
var page
var code
func check(ok, message):
    checks += 1
    if not ok:
        failures += 1
        push_error(message)
func _initialize(): call_deferred("run_checks")
func frames(n := 6):
    for i in range(n): await process_frame
func find(name): return page.find_child(name, true, false)
func command(text): code.text = text; code.text_submitted.emit(text)
func saved(id): return JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json")).Events.ProgressByEventId[id]
func capture(name):
    if not "--capture-prestige" in OS.get_cmdline_user_args(): return
    await create_timer(5).timeout # Let the developer command toast clear for rendered previews.
    await RenderingServer.frame_post_draw
    DirAccess.make_dir_recursive_absolute("/tmp/idle-ai-prestige-ui")
    root.get_texture().get_image().save_png("/tmp/idle-ai-prestige-ui/"+name+".png")
func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("event-prestige-test") or not "--event-tools" in OS.get_cmdline_user_args():
        push_error("Use isolated event-prestige-test profile with --event-tools"); quit(1); return
    var now := int(Time.get_unix_time_from_system())
    if "--capture-prestige" in OS.get_cmdline_user_args():
        root.size = Vector2i(540,960); root.content_scale_aspect = Window.CONTENT_SCALE_ASPECT_KEEP
    var fixture = {"SaveVersion":22,"LastSaveUnix":now,"Rooms":[],"Events":{"Version":2,"LastEvaluationUnix":now+120,"ProgressByEventId":{
        "halloween_2026":{"Currency":251000,"TotalCurrencyEarned":251000,"LastUpdateUnix":now+120},
        "event_test_001":{"Currency":125000,"TotalCurrencyEarned":125000,"LastUpdateUnix":now+120}}}}
    var file = FileAccess.open("user://idle_ai_save.json", FileAccess.WRITE); file.store_string(JSON.stringify(fixture)); file.close()
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25)
    for button in game.find_children("*","Button",true,false):
        if button.text == "CONTINUE": button.pressed.emit()
    game.get_node("EventRuntime/EventTickTimer").stop()
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit(); await frames()
    for input in game.get_node("ShopPage").find_children("*","LineEdit",true,false):
        if input.placeholder_text == "ENTER CODE": code = input
    check(code != null, "Existing developer command input")
    for spec in [["halloween_2026","Souls","Cursed Cores",25.1,749000,20,"souls.png","cursed_core.png"],
        ["event_test_001","Event Data","Protocol Cores",25.0,375000,100,"shop_production.png","core_level.png"]]:
        var id = spec[0]
        command("event-force:"+id)
        game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit(); await frames()
        game.get_node("MapPage").find_child("EventMapEntry",true,false).pressed.emit(); await frames()
        page = game.get_node("EventPage")
        find("EventPrestigeTab").pressed.emit(); await frames()
        var panel = find("EventPrestigePanel")
        check(panel.find_children("Prestige*Card","PanelContainer",true,false).size() == 4, "Four compact cards: "+id)
        check(is_equal_approx(find("PrestigeProgressBar").value, spec[3]), "Correct initial run earnings percentage: "+id)
        check(find("PrestigeRequirement").text.contains(spec[1]) and find("PrestigeLifetime").text.contains(spec[1]), "Active currency and lifetime earnings")
        check(find("PrestigeResource").text.contains(spec[2]) and find("PrestigePreview").text.contains(spec[2]), "Active Prestige resource")
        check(find("PrestigeAvailability").text.begins_with("Requires ") and find("EventPrestigeButton").disabled, "Unavailable Prestige explained and disabled")
        check(find("PrestigeBonus").text == "+0% Event Production", "Current bonus from actual earned cores")
        for image in panel.find_children("EventImage","TextureRect",true,false):
            check(image.texture != null and image.size.x > 0 and image.size.y > 0, "Currency icon has real drawable texture")
            check(image.texture.resource_path.ends_with(spec[6]) or image.texture.resource_path.ends_with(spec[7]), "Definition-specific currency icons")
        await capture(id+"_progress")
        for width in [720,360,320]:
            game.size = Vector2(width,1280); await frames()
            for label in panel.find_children("*","Label",true,false):
                check(label.size.y >= 20 and label.text != "", "Readable nonempty label: "+label.name)
                check(label.get_visible_line_count() == label.get_line_count(), "No collapsed/clipped Prestige text: "+label.name)
                check(label.get_global_rect().end.x <= page.get_global_rect().end.x+1, "Portrait text fits: "+label.name)
            check(find("EventScroll").scroll_horizontal == 0 and not game.get_node("BottomBar").visible, "Portrait scroll/chrome preserved")
        game.size = Vector2(720,1280); await frames()
        var before = find("PrestigeProgressBar").value
        find("EventUpgradeButton").pressed.emit()
        check(find("PrestigeProgressBar").value == before, "Spending Event currency does not remove earned Prestige progress")
        command("event-add:"+str(spec[4]))
        check(find("PrestigeProgressBar").value == 100, "Earnings update bar immediately")
        check(find("PrestigeAvailability").text == "PRESTIGE READY" and not find("EventPrestigeButton").disabled, "Ready state updates without reopening tab")
        check(find("PrestigeAfterBonus").text == "After Prestige: +"+str(spec[5])+"% Event Production", "Projected bonus uses configured multiplier")
        check(find("EventPrestigeButton").text.contains(spec[2]), "Ready button previews active resource")
        await capture(id+"_ready")
        var before_nodes = panel.get_child_count()
        game.size = Vector2(320,720); await frames()
        find("EventPrestigeButton").pressed.emit(); await frames()
        check(find("EventPrestigeConfirmation").visible and find("ConfirmPrestigeText").size.y > 24, "Readable existing confirmation")
        check(find("ConfirmPrestigeText").get_visible_line_count() == find("ConfirmPrestigeText").get_line_count(), "Confirmation text fully laid out")
        check(page.get_global_rect().grow(1).encloses(find("PrestigeConfirmationPanel").get_global_rect()), "Confirmation fits narrow portrait")
        find("ConfirmEventPrestige").pressed.emit(); await frames()
        check(saved(id).PrestigeCount == 1 and saved(id).PrestigeResource == 1, "Existing central Prestige action saves reward")
        check(find("PrestigeProgressBar").value == 0 and find("EventPrestigeButton").disabled, "New run starts with zero eligibility")
        check(find("PrestigeBonus").text == "+"+str(spec[5])+"% Event Production", "Bonus immediately updates after Prestige")
        check(find("PrestigeRequirement").text.contains("10.0M" if id == "halloween_2026" else "650K"), "Next requirement follows configured Prestige growth")
        check(saved(id).TotalCurrencyEarned >= (1000000 if id == "halloween_2026" else 500000), "Total lifetime progress survives Prestige")
        check(panel.get_child_count() == before_nodes, "Updates reuse compact card nodes")
        check(find("PrestigeResetInfo").text.contains(spec[1]) and find("PrestigeKeepInfo").text.contains(spec[2]), "Reset/keep explanations use active names")
        game.size = Vector2(720,1280); await frames()
        await capture(id+"_after")
        command("event-end"); await frames()
        check(find("EventPrestigeButton").disabled and find("PrestigeAvailability").text == "EVENT INACTIVE", "Expired Event cannot Prestige")
        find("EventCloseButton").pressed.emit(); await frames()
    print("Prestige UI scene: ",checks," checks, failures=",failures)
    game.queue_free(); await frames(); quit(1 if failures else 0)
