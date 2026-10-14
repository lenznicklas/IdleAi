extends SceneTree

var checks := 0
var failures := 0
var game
var page
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
func tab(name):
    page.find_child("Event"+name+"Tab",true,false).pressed.emit()
    await frames()
func enter():
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit(); await frames()
    for input in game.get_node("ShopPage").find_children("*", "LineEdit", true, false):
        if input.placeholder_text == "ENTER CODE":
            input.text = "event-force:event_test_001"
            input.text_submitted.emit(input.text)
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit()
    await frames()
    game.get_node("MapPage").find_child("EventMapEntry",true,false).pressed.emit()
    await frames()
    page = game.get_node("EventPage")
    game.get_node("EventRuntime/EventTickTimer").stop()
    await tab("Rewards")
func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("event-rewards-test") or not "--event-tools" in OS.get_cmdline_user_args():
        push_error("Isolated event-rewards-test profile required"); quit(1); return
    var now := int(Time.get_unix_time_from_system())
    var file = FileAccess.open("user://idle_ai_save.json",FileAccess.WRITE)
    file.store_string(JSON.stringify({"SaveVersion":22,"LastSaveUnix":now,"Rooms":[],"Events":{
        "Version":2,"LastEvaluationUnix":now,"LastEventId":"event_test_001","ProgressByEventId":{
            "event_test_001":{"Currency":1500000,"TotalCurrencyEarned":1500000,"LastUpdateUnix":now}}}}))
    file.close()
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25); await enter()
    var cards = page.find_children("EventRewardCard_*","Control",true,false)
    check(cards.size() == 10,"Entire Test Protocol track visible including late rewards")
    var final_card = page.find_child("EventRewardCard_protocol_final",true,false)
    check(final_card.find_child("RewardStatus",true,false).text.contains("FINAL REWARD"),"Final reward label")
    check(final_card.find_child("RewardStatus",true,false).text.contains("LOCKED"),"Late reward locked")
    check(page.find_child("EventClaimAll",true,false).visible,"Multiple rewards show Claim All")
    var track = page.find_child("EventTrackProgress",true,false)
    var track_before = track.value
    var summary = page.find_child("EventRewardSummary",true,false)
    check(summary.text.contains("lifetime") and summary.text.contains("Next Reward"),"Lifetime and next target shown")
    for dimensions in [Vector2i(1080,2400),Vector2i(720,1600),Vector2i(360,780)]:
        root.size = dimensions; await frames()
        for width in [320,360,720]:
            game.size = Vector2(width,780); await frames()
            for card in cards:
                check(card.size.y == 270,"Stable reward card height")
                check(card.get_global_rect().end.x <= page.get_global_rect().end.x+1,"Card within portrait width")
                var image = card.find_child("EventImage",true,false)
                var area = card.find_child("EventImageArea",true,false)
                check(image.get_global_rect().size.x <= area.size.x+1 and image.get_global_rect().size.y <= area.size.y+1,"Bounded reward image")
                var button = card.find_children("EventReward_*","Button",true,false)[0]
                check(button.size.y >= 48,"Claim tap target")
                for state in ["normal","hover","pressed","hover_pressed","disabled"]:
                    check(button.has_theme_stylebox_override(state) and button.get_theme_stylebox(state) is StyleBoxFlat,"Custom rounded reward button: "+state)
    var first = page.find_child("EventReward_data_100",true,false)
    first.pressed.emit(); await frames()
    check(progress().ClaimedRewards.has("data_100") and first.disabled,"Individual claim immediately saved and disabled")
    first.pressed.emit()
    check(progress().DeveloperCredits == 1,"Duplicate tap blocked")
    var before_ids := []
    for card in cards: before_ids.append(card.get_instance_id())
    page.find_child("EventClaimAll",true,false).pressed.emit(); await frames()
    check(progress().ClaimedRewards.size() == 8,"Claim All claims eight earned rewards")
    check(progress().OwnedBots.has("protocol_helper"),"Claim All grants Event Bot")
    check(progress().PrestigeResource == 1,"Claim All grants Protocol Core")
    check(not page.find_child("EventClaimAll",true,false).visible,"Claim All hidden with fewer than two eligible")
    check(data().DataShards >= 5,"Claim All grants existing global Data Shards")
    for card in cards:
        check(before_ids.has(card.get_instance_id()),"Claim refresh reuses reward cards")
    await tab("Prestige")
    page.find_child("EventPrestigeButton",true,false).pressed.emit(); await frames()
    page.find_child("ConfirmEventPrestige",true,false).pressed.emit(); await frames()
    check(progress().PrestigeCount == 1,"Prestige succeeds")
    check(progress().ClaimedRewards.size() == 8,"Prestige keeps claims")
    await tab("Rewards")
    check(track.value >= track_before,"Prestige never resets track")
    check(not game.get_node("BottomBar").visible,"BottomBar remains hidden")
    game.queue_free(); await frames(15)
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25); await enter()
    check(progress().ClaimedRewards.size() == 8,"Restart restores all claims")
    check(page.find_child("EventReward_protocol_bot",true,false).disabled,"Restart cannot reclaim bot")
    check(page.find_child("EventRewardCard_protocol_bot",true,false).find_child("RewardStatus",true,false).text.contains("CLAIMED"),"Restart restores claimed styling")
    check(not page.find_child("EventClaimAll",true,false).visible,"Restart cannot batch reclaim")
    game.queue_free(); await frames()
    print("Reward track scene smoke: %d checks, failures=%d" % [checks,failures])
    quit(1 if failures else 0)
