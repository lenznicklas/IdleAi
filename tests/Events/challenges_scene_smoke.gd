extends SceneTree

var checks := 0
var failures := 0
var game
var page
var code
var tabs := ["Progress","Prestige","Bots","Rewards","Shop","Challenges"]
func check(ok, message):
    checks += 1
    if not ok:
        failures += 1
        push_error(message)
func _initialize(): call_deferred("run_checks")
func frames(count := 6):
    for i in range(count): await process_frame
func saved(): return JSON.parse_string(FileAccess.get_file_as_string("user://idle_ai_save.json"))
func progress(): return saved().Events.ProgressByEventId.event_test_001
func tasks(): return progress().Challenges
func tab(name):
    page.find_child("Event"+name+"Tab",true,false).pressed.emit()
    await frames()
func command(text):
    code.text = text; code.text_submitted.emit(text)
func open_event():
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit(); await frames()
    for input in game.get_node("ShopPage").find_children("*","LineEdit",true,false):
        if input.placeholder_text == "ENTER CODE": code = input
    command("event-force:event_test_001")
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit(); await frames()
    game.get_node("MapPage").find_child("EventMapEntry",true,false).pressed.emit(); await frames()
    page = game.get_node("EventPage")
    game.get_node("EventRuntime/EventTickTimer").stop()
    await tab("Challenges")
func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("event-challenges-test") or not "--event-tools" in OS.get_cmdline_user_args():
        push_error("Use isolated event-challenges-test profile with --event-tools"); quit(1); return
    var now := int(Time.get_unix_time_from_system())
    var file = FileAccess.open("user://idle_ai_save.json",FileAccess.WRITE)
    file.store_string(JSON.stringify({"SaveVersion":22,"LastSaveUnix":now,"Rooms":[],"Events":{
        "Version":2,"LastEvaluationUnix":now,"LastEventId":"event_test_001","ProgressByEventId":{
            "event_test_001":{"Currency":100000,"TotalCurrencyEarned":100000,"LastUpdateUnix":now}}}})); file.close()
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25); await open_event()
    var cards = page.find_children("EventChallengeCard_*","Control",true,false)
    check(cards.size() == 11,"Three Dailies, seven Event goals and one Major goal")
    check(page.find_child("EventChallengesTab",true,false).text.contains("•"),"Already earned goals show badge immediately")
    check(page.find_child("EventChallengeClaimAll",true,false).visible,"Multiple completed goals show Claim All")
    var runtime = game.get_node("EventRuntime")
    var tick = runtime.get_node("EventTickTimer")
    tick.timeout.emit()
    await create_timer(0.03).timeout
    tick.timeout.emit()
    command("event-challenge-progress:major_complete:1")
    var active_seconds = tasks().ActivePlaySeconds
    check(active_seconds > 0,"Existing runtime timer records foreground Event play")
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit(); await frames()
    tick.timeout.emit(); await create_timer(0.03).timeout; tick.timeout.emit()
    command("event-challenge-progress:major_complete:1")
    check(tasks().ActivePlaySeconds == active_seconds,"Normal Map time does not count as Event play")
    await open_event()
    runtime.notification(MainLoop.NOTIFICATION_APPLICATION_PAUSED)
    await create_timer(0.03).timeout; tick.timeout.emit()
    runtime.notification(MainLoop.NOTIFICATION_APPLICATION_RESUMED)
    check(tasks().ActivePlaySeconds == active_seconds,"Background/resume does not add active play or offline play")
    var snapshot := {}
    for card in cards:
        var claim = card.find_children("EventChallengeClaim_*","Button",true,false)[0]
        snapshot[str(card.name)] = [card.get_instance_id(),claim.get_signal_connection_list("pressed").size()]
    for dimensions in [Vector2i(1080,2400),Vector2i(720,1600),Vector2i(360,780)]:
        root.size = dimensions; await frames()
        for width in [320,360,720]:
            game.size = Vector2(width,780); await frames()
            for card in cards:
                check(card.size.y == 328,"Stable challenge card height")
                check(card.get_global_rect().end.x <= page.get_global_rect().end.x+1,"Challenge portrait width")
                var image = card.find_child("EventImage",true,false)
                check(image.size.x <= 49 and image.size.y <= 49,"Challenge icon fitted in reusable slot")
                var claim = card.find_children("EventChallengeClaim_*","Button",true,false)[0]
                check(claim.size.y >= 48,"Challenge claim tap target")
                for state in ["normal","hover","pressed","hover_pressed","disabled"]:
                    check(claim.get_theme_stylebox(state) is StyleBoxFlat and claim.get_theme_stylebox(state).corner_radius_top_left >= 18,"Custom rounded challenge style: "+state)
            var nav = page.find_child("EventTabs",true,false)
            var scroll = page.find_child("EventScroll",true,false)
            check(scroll.get_global_rect().end.y <= nav.get_global_rect().position.y,"Content stays above fixed challenge navigation")
            check(page.find_child("EventChallengesTab",true,false).get_global_rect().end.x <= page.get_global_rect().end.x+1,"Challenges grid tab fits")
            check(page.find_children("*","ScrollContainer",true,false).filter(func(node): return node.is_visible_in_tree()).size() == 1 and scroll.find_children("*","ScrollContainer",true,false).is_empty(),"Challenges reuse one scroller")
    command("event-challenge-progress:major_complete:49"); await frames()
    var major = page.find_child("EventChallengeCard_major_complete",true,false)
    check(major.find_child("ChallengeProgress",true,false).text == "49 / 50","Development progress setter")
    command("event-challenges-complete"); await frames()
    check(page.find_child("EventChallengesTab",true,false).text.contains("11"),"Ready count updates immediately")
    check(major.find_child("ChallengeCategory",true,false).text == "MAJOR","Major goal labeled")
    var major_claim = page.find_child("EventChallengeClaim_major_complete",true,false)
    var before_shards = saved().DataShards
    major_claim.pressed.emit(); await frames()
    check(tasks().Progress.major_complete.Claimed and major_claim.disabled,"Individual challenge saved immediately")
    check(saved().DataShards == before_shards+25,"Major reward uses global Data Shards")
    major_claim.pressed.emit()
    check(saved().DataShards == before_shards+25,"Duplicate challenge tap blocked")
    command("event-challenges-ready"); await frames()
    check(tasks().Progress.major_complete.TimesClaimed == 1,"Mark-ready never resets claims")
    page.find_child("EventChallengeClaimAll",true,false).pressed.emit(); await frames()
    for state in tasks().Progress.values(): check(state.Claimed and state.TimesClaimed == 1,"Claim All persisted each selected challenge once")
    check(not page.find_child("EventChallengeClaimAll",true,false).visible,"Claim All hidden after batch")
    check(page.find_child("EventChallengesTab",true,false).text == "Challenges","Badge clears immediately")
    var claimed_shards = saved().DataShards
    page.find_child("EventChallengeClaimAll",true,false).pressed.emit()
    check(saved().DataShards == claimed_shards,"Repeated Claim All no duplicate reward")
    for name in tabs:
        await tab(name)
        check(not game.get_node("BottomBar").visible,"BottomBar hidden in "+name)
    await tab("Challenges")
    for card in cards:
        var claim = card.find_children("EventChallengeClaim_*","Button",true,false)[0]
        check(card.get_instance_id() == snapshot[str(card.name)][0],"Switching tabs keeps cached challenge nodes")
        check(claim.get_signal_connection_list("pressed").size() == snapshot[str(card.name)][1],"No duplicate challenge signal connections")
        check(card.modulate.r < 1,"Claimed cards visually subdued")
    var old_day = tasks().DailyUtcDay
    game.queue_free(); await frames(15)
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25); await open_event()
    check(tasks().DailyUtcDay == old_day and tasks().Progress.major_complete.Claimed,"Restart retains day and major claim")
    check(saved().DataShards == claimed_shards,"Restart retains global reward amount")
    check(page.find_child("EventChallengesTab",true,false).text == "Challenges","Restart cannot redisplay already claimed goals as ready")
    command("event-day-next"); await frames()
    check(tasks().DailyUtcDay == old_day+1,"Simulated UTC day advance")
    check(tasks().DailyIds.size() == 3 and tasks().DailyHistory.size() == 3,"Daily reset keeps three active tasks and claimed history")
    check(tasks().Progress.major_complete.Claimed and tasks().Progress.event_lifetime.Claimed,"Daily rotation preserves Event/Major claims")
    var daily_ids = tasks().DailyIds
    command("event-dailies-reset"); await frames()
    check(tasks().DailyIds == daily_ids,"Same-day debug reset deterministic")
    command("event-challenges-ready"); command("event-end"); await frames()
    var expired_day = tasks().DailyUtcDay
    command("event-day-next"); await frames()
    check(tasks().DailyUtcDay == expired_day,"Expired Event cannot create new Dailies")
    check(page.find_child("EventChallengeSummary",true,false).text.contains("EVENT ENDED"),"Expired challenge UI explains policy")
    page.find_child("EventChallengeClaimAll",true,false).pressed.emit(); await frames()
    for id in tasks().DailyIds: check(tasks().Progress[id].Claimed,"Expired earned permanent Daily reward claimable")
    page.find_child("EventCloseButton",true,false).pressed.emit(); await frames()
    check(game.get_node("BottomBar").visible and game.get_node("MapPage").visible,"Leaving Challenges restores normal navigation")
    game.queue_free(); await frames()
    print("Challenges scene smoke: %d checks, failures=%d" % [checks,failures])
    quit(1 if failures else 0)
