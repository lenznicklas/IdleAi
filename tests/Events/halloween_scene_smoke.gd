extends SceneTree

var failures := 0
var code_input
func check(value, message):
    if not value:
        push_error(message)
        failures += 1
func _initialize(): call_deferred("run_checks")
func save_file(): return JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json"))
func halloween(): return save_file().Events.ProgressByEventId.halloween_2026
func command(value):
    code_input.text = value
    code_input.text_submitted.emit(value)
func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("halloween-scene-test"):
        push_error("isolated halloween-scene-test profile required")
        quit(1)
        return
    var now = int(Time.get_unix_time_from_system())
    var fixture = {"SaveVersion":22, "LastSaveUnix":now, "Rooms":[], "Events": {
        "Version":1, "LastEvaluationUnix":now, "LastEventId":"event_test_001", "ProgressByEventId": {
            "event_test_001":{"Currency":42, "TotalCurrencyEarned":42, "ProgressionLevel":3, "HighestProgressionReached":3}}}}
    var file = FileAccess.open("user://idle_ai_save.json", FileAccess.WRITE)
    file.store_string(JSON.stringify(fixture)); file.close()
    root.size = Vector2i(720, 1280)
    var game = load("res://game.tscn").instantiate(); root.add_child(game)
    for frame in range(20): await process_frame
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit()
    for frame in range(12): await process_frame
    for input in game.get_node("ShopPage").find_children("*", "LineEdit", true, false):
        if input.placeholder_text == "ENTER CODE": code_input = input
    check(code_input != null, "debug code input")
    if code_input == null: quit(1); return
    command("halloween-force")
    var test_wallet = save_file().Events.ProgressByEventId.event_test_001.Currency
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit()
    for frame in range(5): await process_frame
    var entry = game.get_node("MapPage").find_child("EventMapEntry", true, false)
    check(entry.find_child("EventEntryName", true, false).text == "Halloween Protocol", "Halloween map name")
    check(entry.find_child("EventEntryStatus", true, false).text == "ACTIVE", "Halloween active card")
    entry.pressed.emit()
    for frame in range(6): await process_frame
    var page = game.get_node("EventPage")
    check(page.find_child("EventTitle", true, false).text == "HALLOWEEN PROTOCOL", "Halloween title")
    var cards = page.find_children("EventGenerator_*", "Control", true, false)
    check(cards.size() == 6, "six generator cards")
    var sizes = []
    var identities = []
    for card in cards:
        sizes.append(card.size); identities.append(card.get_instance_id())
        check(card.size.y == 490, "fixed generator card height")
        var icon = card.find_children("*", "TextureRect", true, false)[0]
        check(icon.texture != null, "existing generator asset loaded")
    command("event-add:100")
    check(halloween().Generators.haunted_workstation.Unlocked, "Souls unlock second generator")
    page.find_child("EventUpgradeButton", true, false).pressed.emit()
    check(halloween().Generators.abandoned_terminal.Level == 2, "Halloween upgrade")
    page.find_child("EventRewardsTab", true, false).pressed.emit()
    var reward = page.find_child("EventReward_souls_100", true, false)
    check(not reward.disabled, "Ghost Bot reward available")
    reward.pressed.emit()
    check(halloween().OwnedBots.has("ghost_bot"), "bot reward ownership")
    reward.pressed.emit()
    check(halloween().ClaimedRewards.count("souls_100") == 1, "reward cannot duplicate")
    page.find_child("EventBotsTab", true, false).pressed.emit()
    var bots = page.find_children("EventBot_*Card", "Control", true, false)
    check(bots.size() == 4, "four reusable bot cards")
    page.find_child("EventBot_ghost_bot", true, false).pressed.emit()
    check(halloween().ActiveBotId == "ghost_bot", "one active event bot")
    command("event-add:4000000")
    page.find_child("EventPrestigeTab", true, false).pressed.emit()
    var prestige = page.find_child("EventPrestigeButton", true, false)
    check(not prestige.disabled, "prestige available")
    var old_count = halloween().PrestigeCount
    prestige.pressed.emit()
    var confirm = page.find_child("EventPrestigeConfirmation", true, false)
    check(confirm.visible and halloween().PrestigeCount == old_count, "prestige requires confirmation")
    page.find_child("CancelEventPrestige", true, false).pressed.emit()
    check(not confirm.visible and halloween().PrestigeCount == old_count, "cancel preserves progression")
    prestige.pressed.emit()
    page.find_child("ConfirmEventPrestige", true, false).pressed.emit()
    check(halloween().PrestigeCount == old_count + 1 and halloween().PrestigeResource >= 2, "confirmed prestige grants Cursed Cores")
    check(halloween().Currency == 0 and halloween().Generators.abandoned_terminal.Level == 1, "atomic prestige reset")
    check(halloween().ClaimedRewards.has("souls_100") and halloween().ActiveBotId == "ghost_bot", "prestige preserves rewards and bot")
    page.find_child("ConfirmEventPrestige", true, false).pressed.emit()
    check(halloween().PrestigeCount == old_count + 1, "duplicate confirmation ignored")
    page.find_child("EventProgressTab", true, false).pressed.emit()
    for frame in range(5): await process_frame
    for i in range(cards.size()):
        check(cards[i].size == sizes[i] and cards[i].get_instance_id() == identities[i], "prestige retains generator geometry and identity")
    page.find_child("EventShopTab", true, false).pressed.emit()
    var buy = page.find_child("EventShop_soul_cache", true, false)
    check(not buy.disabled, "Cursed Core shop purchase available")
    buy.pressed.emit()
    check(halloween().ShopPurchases.soul_cache == 1, "shop count persistent")
    command("event-ready-rewards")
    page.find_child("EventRewardsTab", true, false).pressed.emit()
    page.find_child("EventReward_souls_1000000000", true, false).pressed.emit()
    page.find_child("EventReward_souls_5000000", true, false).pressed.emit()
    check(save_file().Events.CosmeticEntitlements.size() == 5, "real skin entitlements saved atomically")
    var machine_skins = JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_machine_skins.json"))
    var bot_skins = JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_cosmetics.json"))
    check(machine_skins.OwnedSkinIds.has("room_4_halloween") and bot_skins.OwnedSkinIds.has("halloween"), "global skin ownership stores synced")
    command("event-unlock-bots")
    check(halloween().OwnedBots.size() == 4, "unlock-all bot debug tool")
    command("event-unlock-generators")
    command("event-level:nightmare_core:10")
    check(halloween().Generators.nightmare_core.Level == 10, "set generator level debug tool")
    command("event-cores:3")
    command("event-ending-soon")
    page.find_child("EventCloseButton", true, false).pressed.emit()
    check(entry.find_child("EventEntryStatus", true, false).text == "ENDING SOON", "last 24h map state")
    entry.pressed.emit()
    command("event-end")
    check(halloween().Ended and halloween().Archive != null, "Halloween expiration archives result")
    page.find_child("EventShopTab", true, false).pressed.emit()
    check(buy.disabled, "expired shop disabled")
    page.find_child("EventPrestigeTab", true, false).pressed.emit()
    check(prestige.disabled, "expired prestige disabled")
    page.find_child("EventArchiveTab", true, false).pressed.emit()
    check(page.find_child("Archive_halloween_2026", true, false) != null, "archive entry UI")
    check(save_file().Events.ProgressByEventId.event_test_001.Currency == test_wallet, "Test Protocol currency isolated during Halloween")
    # Validate native logical bounds at two portrait viewport sizes.
    for viewport_size in [Vector2i(1080, 2400), Vector2i(720, 1600), Vector2i(540, 960)]:
        root.size = viewport_size
        for frame in range(5): await process_frame
        for tab in ["Progress", "Prestige", "Bots", "Rewards", "Shop", "Archive", "Challenges"]:
            page.find_child("Event" + tab + "Tab", true, false).pressed.emit()
            for frame in range(3): await process_frame
            load("res://tests/Events/image_layout_checks.gd").verify(page, check)
            for button in page.find_children("*", "Button", true, false):
                if not button.is_visible_in_tree(): continue
                check(button.get_global_rect().end.x <= game.size.x + 0.5, "mobile width: " + str(button.name))
                check(button.has_theme_stylebox_override("pressed") and button.has_theme_stylebox_override("hover_pressed")
                    and button.has_theme_stylebox_override("focus"), "no default button visuals: " + str(button.name))
                check(button.get_theme_stylebox("pressed").corner_radius_top_left >= 18,
                    "rounded pressed state: " + str(button.name))
    var test_before_reset = save_file().Events.ProgressByEventId.event_test_001
    command("halloween-reset")
    check(halloween().Currency == 0 and halloween().PrestigeResource == 0, "only Halloween reset")
    var test_after_reset = save_file().Events.ProgressByEventId.event_test_001
    # Test Protocol becomes active after event-end. Reset settles real elapsed time,
    # so legitimate production may accrue while slower mobile/layout checks run.
    var earned_during_reset = test_after_reset.TotalCurrencyEarned - test_before_reset.TotalCurrencyEarned
    check(earned_during_reset >= 0 and is_equal_approx(test_after_reset.Currency - test_before_reset.Currency, earned_during_reset), "reset preserves Test Protocol wallet except legitimate production")
    check(test_after_reset.ClaimedRewards == test_before_reset.ClaimedRewards and test_after_reset.PrestigeCount == test_before_reset.PrestigeCount, "reset preserves Test Protocol reward and prestige state")
    check(save_file().Events.CosmeticEntitlements.size() == 5, "reset preserves global cosmetics")
    root.size = Vector2i(720, 1280)
    for frame in range(4): await process_frame
    game.queue_free()
    for frame in range(4): await process_frame
    # Reload: global skin ownership remains, Test Protocol is still the scheduled event.
    game = load("res://game.tscn").instantiate(); root.add_child(game)
    for frame in range(20): await process_frame
    check(save_file().Events.CosmeticEntitlements.size() == 5, "cosmetic entitlements survive app scene reload")
    game.queue_free()
    for frame in range(4): await process_frame
    print("Halloween scene smoke: failures=%d" % failures)
    quit(1 if failures else 0)
