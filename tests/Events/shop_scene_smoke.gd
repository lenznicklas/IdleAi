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
func data(): return JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json"))
func progress(): return data().Events.ProgressByEventId.event_test_001
func find(name): return page.find_child(name, true, false)
func command(text):
    code.text = text; code.text_submitted.emit(text)
func open_event():
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit(); await frames()
    for input in game.get_node("ShopPage").find_children("*", "LineEdit", true, false):
        if input.placeholder_text == "ENTER CODE": code = input
    command("event-force:event_test_001")
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit(); await frames()
    game.get_node("MapPage").find_child("EventMapEntry", true, false).pressed.emit(); await frames()
    page = game.get_node("EventPage")
    game.get_node("EventRuntime/EventTickTimer").stop()
    find("EventShopTab").pressed.emit(); await frames()
func buy(id):
    find("EventShop_" + id).pressed.emit()
    await create_timer(.35).timeout
    await frames()
func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("event-shop-test") or not "--event-tools" in OS.get_cmdline_user_args():
        push_error("Use isolated event-shop-test profile with --event-tools"); quit(1); return
    # Reset only the isolated fixture's global cosmetic cache before the first launch.
    var cosmetics = FileAccess.open("user://idle_ai_cosmetics.json", FileAccess.WRITE)
    cosmetics.store_string(JSON.stringify({"OwnedSkinIds":[],"EquippedBotSkinId":"default"})); cosmetics.close()
    var now := int(Time.get_unix_time_from_system())
    var file = FileAccess.open("user://idle_ai_save.json", FileAccess.WRITE)
    file.store_string(JSON.stringify({"SaveVersion":22,"LastSaveUnix":now,"Rooms":[],"DataShards":5,"Events":{
        "Version":2,"LastEvaluationUnix":now,"LastEventId":"event_test_001","ProgressByEventId":{
            "event_test_001":{"Currency":1000,"TotalCurrencyEarned":1000,"PrestigeResource":3,"TotalPrestigeResourceEarned":3,"LastUpdateUnix":now}}}})); file.close()
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25); await open_event()
    var cards = page.find_children("EventShop_*Card", "Control", true, false)
    check(cards.size() == 8, "Eight configured Test Protocol offers")
    check(cards[0].name == "EventShop_optimizer_licenseCard", "Featured item first")
    check(find("EventShop_helper_license").disabled and find("EventShop_helper_license").text == "OWNED", "Owned helper is never repurchased")
    check(find("EventShop_large_data").text == "LOCKED", "Prestige-gated item visible and locked")
    check(find("EventShop_large_dataCard").find_child("ShopItemRequirement", true, false).text.contains("Event Prestige 1"), "Human-readable prestige requirement")
    var track_requirement = find("EventShop_shard_packCard").find_child("ShopItemRequirement", true, false).text
    check(track_requirement.contains("track reward") and track_requirement.contains("Event Data") and not track_requirement.contains("protocol_shards"), "Human-readable track milestone requirement")
    for size in [Vector2i(1080,2400), Vector2i(720,1600), Vector2i(540,960)]:
        root.size = size; await frames()
        for width in [320,360,720]:
            game.size = Vector2(width,960); await frames()
            for card in cards:
                check(card.size.y == 456 and card.get_global_rect().end.x <= page.get_global_rect().end.x+1, "Stable shop card portrait bounds")
                for image in card.find_children("EventImage", "TextureRect", true, false):
                    check(image.texture != null, "Item and currency icons loaded")
                    check(image.get_parent().get_parent().get_global_rect().encloses(image.get_global_rect()), "Shop image fits shared slot")
                var button = card.find_child("EventShop_*", true, false)
                for state in ["normal","hover","pressed","disabled","focus"]:
                    check(button.has_theme_stylebox_override(state), "No default Godot purchase styling")
                check(button.get_theme_stylebox("normal").get_border_width(SIDE_LEFT) == button.get_theme_stylebox("pressed").get_border_width(SIDE_LEFT), "No border-width jump")
                check(button.scale == Vector2.ONE, "No purchase scale animation")
    root.size = Vector2i(720,1600); await frames()
    command("event-shop-fund:optimizer_license")
    await buy("optimizer_license")
    check("protocol_optimizer" in progress().OwnedBots and find("EventShop_optimizer_license").text == "OWNED", "Bot purchase shares ownership and updates card")
    var small = find("EventShop_small_data")
    var cores = progress().PrestigeResource
    small.pressed.emit(); small.pressed.emit()
    await frames()
    check(progress().ShopPurchases.small_data == 1 and progress().PrestigeResource == cores-1, "Double tap charges and grants once")
    await create_timer(.35).timeout
    await buy("small_data"); await buy("small_data")
    check(progress().ShopPurchases.small_data == 3 and small.text == "SOLD OUT" and small.disabled, "Three-purchase limit enforced")
    var shards = data().DataShards
    await buy("training_data")
    check(data().DataShards == shards-1 and progress().ShopPurchases.training_data == 1, "Explicit Data Shard cost uses global wallet")
    check(find("EventShop_training_dataCard").find_child("ShopItemLimit", true, false).text.contains("No limit"), "Unlimited count displayed")
    command("event-shop-fund:protocol_core_offer")
    var core_before = progress().PrestigeResource
    await buy("protocol_core_offer")
    check(progress().PrestigeResource == core_before+1, "Shop uses shared Prestige currency reward")
    command("event-shop-fund:shard_pack")
    shards = data().DataShards
    await buy("shard_pack")
    check(data().DataShards == shards+3 and progress().ShopPurchases.shard_pack == 1, "Shared Data Shard reward and count")
    var stable_ids = cards.map(func(card): return card.get_instance_id())
    for i in range(10):
        find("EventProgressTab").pressed.emit(); find("EventShopTab").pressed.emit()
    await frames()
    check(page.find_children("EventShop_*Card", "Control", true, false).map(func(card): return card.get_instance_id()) == stable_ids, "Purchases and tab switching preserve card identities/order")
    for card in cards:
        var button = card.find_child("EventShop_*", true, false)
        check(button.get_signal_connection_list("pressed").size() == 1, "One purchase signal connection per card")
    command("event-ready-prestige")
    find("EventPrestigeTab").pressed.emit(); await frames()
    find("EventPrestigeButton").pressed.emit(); find("ConfirmEventPrestige").pressed.emit(); await frames()
    find("EventShopTab").pressed.emit(); await frames()
    check(progress().ShopPurchases.small_data == 3 and progress().ShopPurchases.optimizer_license == 1, "Prestige preserves shop counts")
    check(not find("EventShop_large_data").disabled, "Large cache becomes available after Prestige")
    var before_large = progress().Currency
    var before_large_cores = progress().PrestigeResource
    await buy("large_data")
    check(progress().PrestigeResource == before_large_cores-2 and progress().Currency >= before_large+40000, "Large cache pays configured cores and grants Event Data")
    check(find("EventShop_nature_bot_skin").text != "LOCKED", "Completed first Prestige challenge unlocks cosmetic")
    command("event-shop-fund:nature_bot_skin")
    check(data().DataShards == 50, "Debug funding uses actual configured Data Shard cost")
    await buy("nature_bot_skin")
    check(data().Events.CosmeticEntitlements.has("bot:nature_overgrown") and data().DataShards == 0, "Existing global skin purchased atomically")
    check(find("EventShop_nature_bot_skin").disabled and find("EventShop_nature_bot_skin").text == "OWNED", "Global cosmetic duplicate disabled")
    command("event-shop-fund:training_data")
    find("EventShop_training_data").pressed.emit(); find("EventShop_training_data").pressed.emit()
    # Reload immediately after a purchase, without waiting for another frame.
    cards.clear(); small = null
    game.queue_free(); await frames()
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25); await open_event()
    check(progress().ShopPurchases.training_data == 2 and data().DataShards == 0, "Immediate scene reload never replays an in-flight purchase")
    check(find("EventShop_nature_bot_skin").text == "OWNED" and find("EventShop_small_data").text == "SOLD OUT", "Restart preserves cosmetic and finite purchase history")
    command("event-shop-count:small_data:2"); await frames()
    check(find("EventShop_small_dataCard").find_child("ShopItemLimit", true, false).text == "2 / 3 purchased", "Debug count visible immediately")
    command("event-shop-unlock"); command("event-shop-print"); await frames()
    check(progress().UnlockedShopItems.size() == 8, "Debug unlock all persisted")
    command("event-shop-reset"); await frames()
    check(progress().ShopPurchases.is_empty() and find("EventShop_nature_bot_skin").text == "OWNED", "Reset purchase counts never removes permanent skin")
    command("event-shop-expire"); await frames()
    check(find("EventShopSummary").text.contains("Purchases are no longer available"), "Expired shop explains read-only history")
    for card in page.find_children("EventShop_*Card", "Control", true, false):
        check(card.find_child("EventShop_*", true, false).disabled, "All expired purchases disabled")
    check(find("EventShop_nature_bot_skinCard").find_child("ShopItemStatus", true, false).text.contains("OWNED"), "Expired cosmetic ownership stays visible")
    check(not game.get_node("BottomBar").visible, "Shop remains within full-screen Event")
    find("EventCloseButton").pressed.emit(); await frames()
    check(not page.visible and game.get_node("BottomBar").visible, "Leaving Shop restores global navigation")
    print("Event shop smoke: ", checks, " checks, failures=", failures)
    game.queue_free(); await frames(); quit(1 if failures else 0)
