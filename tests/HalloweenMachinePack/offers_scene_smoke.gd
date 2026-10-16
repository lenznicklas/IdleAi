extends SceneTree

var failures := 0
var checks := 0
var game
const CONTENT := "ShopPage/ShopMargin/LayoutRoot/Scroll/ScrollMargin/Content"

func _initialize():
    call_deferred("run_checks")

func check(value, message):
    checks += 1
    if not value:
        push_error(message)
        failures += 1

func frames(count := 5):
    for i in range(count): await process_frame

func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("halloween-offers-test"):
        push_error("Use an isolated halloween-offers-test profile")
        quit(1)
        return
    var reload := "--verify-reload" in OS.get_cmdline_user_args()
    var event_reload := "--verify-event-reload" in OS.get_cmdline_user_args()
    var event_purchase := "--event-purchase" in OS.get_cmdline_user_args()
    if not reload and not event_reload:
        var save = FileAccess.open("user://idle_ai_save.json", FileAccess.WRITE)
        save.store_string(JSON.stringify({"SaveVersion":22,"LastSaveUnix":int(Time.get_unix_time_from_system()),"Rooms":[],"DataShards":200}))
        save.close()
    game = load("res://game.tscn").instantiate()
    root.add_child(game)
    await frames(20)
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit()
    await frames()
    var content = game.get_node(CONTENT)
    var bot = content.get_node("HalloweenOfferCard")
    var machine = content.get_node("HalloweenMachineOfferCard")
    var header = content.get_node("HalloweenOfferSectionHeader")
    if event_purchase or event_reload:
        pass
    elif reload:
        check(not bot.visible and not machine.visible, "Both offers remain hidden after process restart")
        check(not header.visible, "Empty offers header remains hidden after restart")
    else:
        check(bot.visible and machine.visible, "Unowned offers are visible")
        var bot_image = bot.find_child("HalloweenBotPreview", true, false)
        var machine_image = machine.find_child("HalloweenMachinePreview", true, false)
        for viewport in [Vector2i(1080,2400), Vector2i(720,1280), Vector2i(540,960)]:
            root.size = viewport
            await frames(10)
            check(bot.size.is_equal_approx(machine.size), "Equal card size at " + str(viewport))
            check(bot_image.size == Vector2(270,270) and machine_image.size == bot_image.size, "Identical image rectangles at " + str(viewport))
            for image in [bot_image, machine_image]:
                check(image.stretch_mode == TextureRect.STRETCH_KEEP_ASPECT_CENTERED, "Aspect fit without cropping")
                check(image.expand_mode == TextureRect.EXPAND_IGNORE_SIZE, "Native dimensions ignored")
            for name in ["OfferImageArea", "OfferTitle", "OfferDescription", "OfferPriceRow", "OfferStatus", "OfferBuyButton"]:
                var a = bot.find_child(name, true, false)
                var b = machine.find_child(name, true, false)
                check(a.size.is_equal_approx(b.size), "Matching slot dimensions: " + name)
                check(is_equal_approx(a.global_position.y-bot.global_position.y, b.global_position.y-machine.global_position.y), "Matching slot offsets: " + name)
        var buy_bot = bot.find_child("OfferBuyButton", true, false)
        var buy_machine = machine.find_child("OfferBuyButton", true, false)
        check(not buy_bot.disabled and not buy_machine.disabled, "Both purchases funded")
        buy_bot.pressed.emit()
        check(not bot.visible and machine.visible, "Bot offer hides synchronously, machine stays")
        buy_machine.pressed.emit()
        check(not machine.visible and not header.visible, "Machine offer and empty header hide synchronously")
        game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit()
        await frames()
        game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit()
        await frames()
        check(not bot.visible and not machine.visible, "Offers stay hidden after reopening Shop")
        for path in ["user://idle_ai_cosmetics.json", "user://idle_ai_machine_skins.json"]:
            check(FileAccess.file_exists(path), "Existing ownership save written: " + path)
    # The same permanent entitlements also hide redundant Event skin offers.
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit()
    await frames()
    for input in game.get_node("ShopPage").find_children("*", "LineEdit", true, false):
        if input.placeholder_text == "ENTER CODE":
            input.text = "halloween-force"
            input.text_submitted.emit(input.text)
            if event_purchase:
                input.text = "event-add:10000000"
                input.text_submitted.emit(input.text)
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit()
    await frames()
    game.get_node("MapPage").find_child("EventMapEntry", true, false).pressed.emit()
    await frames()
    var page = game.get_node("EventPage")
    page.find_child("EventShopTab", true, false).pressed.emit()
    await frames()
    for id in ["bot_skin", "garage_skin"]:
        var card = page.find_child("EventShop_" + id + "Card", true, false)
        if event_purchase:
            check(card.visible, "Unowned Event skin offer visible: " + id)
            var buy = page.find_child("EventShop_" + id, true, false)
            check(not buy.disabled, "Event skin purchase funded: " + id)
            buy.pressed.emit()
            check(not card.visible, "Event skin offer hides synchronously: " + id)
        check(not card.visible, "Owned Event skin offer hidden: " + id)
    check(page.find_child("EventShop_soul_cacheCard", true, false).visible, "Non-skin Event offers remain visible")
    print("Halloween offers smoke: ", checks, " checks, failures=", failures, ", reload=", reload)
    game.queue_free()
    await frames()
    quit(1 if failures else 0)
