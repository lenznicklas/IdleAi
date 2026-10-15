extends SceneTree

# Isolated real-scene checks for cached tab navigation, layout, styles and scroll state.
var failures := 0
var checks := 0
var game
var page
var tabs := ["Progress","Prestige","Bots","Rewards","Shop","Archive","Challenges"]
func check(ok, message):
    checks += 1
    if not ok:
        push_error(message)
        failures += 1
func _initialize(): call_deferred("run_checks")
func frames(count := 5):
    for i in range(count): await process_frame
func tab_button(tab): return page.find_child("Event"+tab+"Tab",true,false)
func switch_tab(tab):
    tab_button(tab).pressed.emit()
    await frames()
func identities(node):
    var result := [node.get_instance_id()]
    for child in node.get_children(): result.append_array(identities(child))
    return result
func verify_content_edges(scroll, navigation):
    var frame = page.find_child("EventContentFrame", true, false)
    var backdrop = page.find_child("EventContentBackdrop", true, false)
    var top = page.find_child("EventContentTopFade", true, false)
    var bottom = page.find_child("EventContentBottomFade", true, false)
    check(scroll.get_parent() == frame and scroll.get_global_rect() == frame.get_global_rect(), "Edge treatment reserves no extra viewport space")
    check(backdrop.get_global_rect().position.y < scroll.get_global_rect().position.y, "Backdrop continues upward through layout gap")
    check(bottom.get_global_rect().end.y <= navigation.get_global_rect().position.y + .5, "Fade stops before tab buttons")
    check(bottom.size.y <= 60.5, "Bottom transition stays subtle on mobile")
    for layer in [frame, backdrop, top, bottom]:
        check(layer.mouse_filter == Control.MOUSE_FILTER_IGNORE, "Visual layer passes touch/scroll input: " + layer.name)
    for layer in [backdrop, top, bottom]:
        var gradient = layer.get_theme_stylebox("panel").texture.gradient
        check(gradient.sample(0).a == 0 and gradient.sample(1).a == 0, "Gradient blends into page at both edges: " + layer.name)
    scroll.scroll_vertical = 0; await frames(2)
    check(not top.visible, "Unscrolled heading remains clear")
    scroll.scroll_vertical = int(scroll.get_v_scroll_bar().max_value); await frames(2)
    check(top.visible == (scroll.scroll_vertical > 0), "Top edge softens only clipped scrolling content")
    var sections = scroll.find_child("EventContent", true, false).get_children().filter(func(node): return node.visible and node.name != "EventContentBottomClearance")
    check(sections.size() == 1 and sections[0].get_global_rect().end.y <= bottom.get_global_rect().position.y + 1, "Last content can scroll completely above fade")
    scroll.scroll_vertical = 0; await frames(2)
func run_checks():
    if not OS.get_environment("XDG_DATA_HOME").contains("event-tabs-test") or not "--event-tools" in OS.get_cmdline_user_args():
        push_error("Use isolated event-tabs-test profile with --event-tools"); quit(1); return
    var now := int(Time.get_unix_time_from_system())
    var file = FileAccess.open("user://idle_ai_save.json",FileAccess.WRITE)
    file.store_string(JSON.stringify({"SaveVersion":22,"LastSaveUnix":now,"Rooms":[]})); file.close()
    game = load("res://game.tscn").instantiate(); root.add_child(game); await frames(25)
    game.get_node("BottomBar/Margin/HBox/ShopButton").pressed.emit(); await frames(12)
    var code = null
    for input in game.get_node("ShopPage").find_children("*","LineEdit",true,false):
        if input.placeholder_text == "ENTER CODE": code = input
    check(code != null,"Developer fixture can select existing Halloween content")
    if code == null: quit(1); return
    code.text = "halloween-force"; code.text_submitted.emit(code.text)
    code.text = "event-ready-prestige"; code.text_submitted.emit(code.text)
    game.get_node("BottomBar/Margin/HBox/MapButton").pressed.emit(); await frames()
    game.get_node("MapPage").find_child("EventMapEntry",true,false).pressed.emit(); await frames()
    page = game.get_node("EventPage")
    game.get_node("EventRuntime/EventTickTimer").stop() # Freeze only the fixture's runtime; UI must never settle time.
    var scroll = page.find_child("EventScroll",true,false)
    var header = page.find_child("EventHeader",true,false)
    var navigation = page.find_child("EventTabs",true,false)
    check(page.find_children("*","ScrollContainer",true,false).filter(func(node): return node.is_visible_in_tree()).size() == 1 and scroll.find_children("*","ScrollContainer",true,false).is_empty(),"One content scroller, no nested scrolling")
    for dimensions in [Vector2i(1080,2400),Vector2i(720,1600),Vector2i(540,960),Vector2i(360,780)]:
        root.size = dimensions; await frames(8)
        var fixed_header = header.get_global_rect()
        var fixed_tabs = navigation.get_global_rect()
        var fixed_scroll = scroll.get_global_rect()
        for tab in tabs:
            await switch_tab(tab)
            await verify_content_edges(scroll, navigation)
            check(header.get_global_rect() == fixed_header,tab+": fixed header at "+str(dimensions))
            check(navigation.get_global_rect() == fixed_tabs,tab+": fixed navigation at "+str(dimensions))
            check(scroll.get_global_rect() == fixed_scroll,tab+": shared content bounds at "+str(dimensions))
            check(header.size.y <= 180,tab+": compact header")
            check(scroll.get_global_rect().position.y >= header.get_global_rect().end.y,tab+": content below header")
            check(scroll.get_global_rect().end.y <= navigation.get_global_rect().position.y,tab+": content above tabs")
            check(not game.get_node("BottomBar").visible,tab+": global navigation hidden")
            check(scroll.scroll_horizontal == 0 and scroll.horizontal_scroll_mode == ScrollContainer.SCROLL_MODE_DISABLED,tab+": no horizontal scrolling")
            for name in tabs:
                var button = tab_button(name)
                check(button.button_pressed == (tab == name),tab+": exactly one active state: "+name)
                check(button.get_global_rect().end.x <= page.get_global_rect().end.x+0.5,name+": mobile horizontal bounds")
                check(button.size.y >= 48 and button.size.x >= 70,name+": usable tap target")
                for state in ["normal","hover","pressed","hover_pressed","disabled","focus"]:
                    check(button.has_theme_stylebox_override(state) and button.get_theme_stylebox(state) is StyleBoxFlat,name+": rounded custom "+state)
            check(tab_button(tab).get_theme_stylebox("pressed").border_color != tab_button(tabs[(tabs.find(tab)+1)%tabs.size()]).get_theme_stylebox("normal").border_color,tab+": active accent distinct")
    # Also constrain the logical UI itself, rather than only resizing a stretched physical window.
    for logical_size in [Vector2(360,780),Vector2(320,720)]:
        game.size = logical_size; await frames(8)
        for tab in tabs:
            await switch_tab(tab)
            check(navigation.get_global_rect().end.x <= page.get_global_rect().end.x+0.5,tab+": narrow logical navigation fits")
            check(scroll.get_global_rect().end.x <= page.get_global_rect().end.x+0.5,tab+": narrow logical content fits")
            for name in tabs:
                var button = tab_button(name)
                var text_width = button.get_theme_font("font").get_string_size(name,HORIZONTAL_ALIGNMENT_LEFT,-1,button.get_theme_font_size("font_size")).x
                check(text_width <= button.size.x-4,name+": narrow label readable without clipping")
    root.size = Vector2i(720,1280); game.size = Vector2(720,1280); await frames(8)
    await switch_tab("Progress")
    scroll.scroll_vertical = 150; await frames(2)
    var touch_start = scroll.get_global_rect().get_center()
    var touch = InputEventScreenTouch.new(); touch.index = 0; touch.position = touch_start; touch.pressed = true
    Input.parse_input_event(touch); Input.flush_buffered_events(); await frames(2)
    var drag = InputEventScreenDrag.new(); drag.index = 0; drag.position = touch_start + Vector2(0,-100); drag.relative = Vector2(0,-100)
    Input.parse_input_event(drag); Input.flush_buffered_events(); await frames(2)
    drag = drag.duplicate(); drag.position += Vector2(0,-100); drag.relative = Vector2(0,-100)
    Input.parse_input_event(drag); Input.flush_buffered_events(); await frames(2)
    touch = touch.duplicate(); touch.position = drag.position; touch.pressed = false
    Input.parse_input_event(touch); Input.flush_buffered_events(); await frames(2)
    check(scroll.scroll_vertical > 150,"Portrait touch drag scrolls only tab content")
    var after_drag_save = FileAccess.get_file_as_string("user://idle_ai_save.json")
    page.find_child("EventUpgradeButton",true,false).pressed.emit()
    check(FileAccess.get_file_as_string("user://idle_ai_save.json") == after_drag_save,"Drag release cannot accidentally purchase an upgrade")
    # Each long tab gets a distinct position. Short tabs correctly retain zero.
    var offsets := {}
    for tab in tabs:
        await switch_tab(tab)
        var maximum = max(0,scroll.get_v_scroll_bar().max_value-scroll.get_v_scroll_bar().page)
        scroll.scroll_vertical = min(int(maximum),100+tabs.find(tab)*81)
        await frames(2)
        offsets[tab] = scroll.scroll_vertical
    for cycle in range(3):
        for tab in tabs:
            await switch_tab(tab)
            check(scroll.scroll_vertical == offsets[tab],tab+": independent restored scroll position")
    # Fast changes invalidate old pending restoration, rather than applying another tab's offset.
    for tab in tabs: tab_button(tab).pressed.emit()
    await frames()
    check(scroll.scroll_vertical == offsets.Archive,"Rapid switching restores final tab only")
    await create_timer(0.2).timeout # Existing mobile gesture guard intentionally suppresses post-drag taps.
    await switch_tab("Prestige")
    var font_size = tab_button("Prestige").get_theme_font_size("font_size")
    tab_button("Prestige").grab_focus(); tab_button("Prestige").pressed.emit(); await frames()
    check(tab_button("Prestige").button_pressed,"Re-selecting active tab cannot toggle it off")
    page.find_child("EventPrestigeButton",true,false).pressed.emit(); await frames()
    check(page.find_child("EventPrestigeConfirmation",true,false).visible,"Temporary confirmation opens")
    check(not page.find_child("EventMobileScroll",true,false).is_processing_input(),"Modal blocks underlying touch scrolling")
    root.go_back_requested.emit(); await frames()
    check(page.visible and tab_button("Prestige").button_pressed,"Modal back preserves selected tab")
    check(not page.find_child("EventPrestigeConfirmation",true,false).visible,"Modal back closes only confirmation")
    check(page.find_child("EventMobileScroll",true,false).is_processing_input(),"Dismissing modal restores scrolling")
    var stable_nodes = identities(page)
    var texture = page.find_child("EventImage",true,false).texture
    var save_before = FileAccess.get_file_as_string("user://idle_ai_save.json")
    var currency_before = page.find_child("EventCurrency",true,false).text
    var connection_counts := {}
    for tab in tabs: connection_counts[tab] = tab_button(tab).get_signal_connection_list("pressed").size()
    for cycle in range(12):
        for tab in tabs: tab_button(tab).pressed.emit()
        await frames(2)
    check(identities(page) == stable_nodes,"Tab switching preserves all node identities")
    check(page.find_child("EventImage",true,false).texture == texture,"Tab switching reuses cached textures")
    check(FileAccess.get_file_as_string("user://idle_ai_save.json") == save_before,"Tabs do not load/settle/save progression")
    check(page.find_child("EventCurrency",true,false).text == currency_before,"Tabs do not double-count production/offline earnings")
    for tab in tabs:
        check(tab_button(tab).get_signal_connection_list("pressed").size() == connection_counts[tab],tab+": signal connections stay constant")
        check(tab_button(tab).get_theme_font_size("font_size") == font_size,tab+": styling preserves readability font size")
    # Very long names/numbers cannot change header geometry or force horizontal growth.
    var header_bounds = header.get_global_rect()
    for label_name in ["EventTitle","EventCurrency","EventCores","EventProduction"]:
        page.find_child(label_name,true,false).text = "999999999999999999999999999999999999999999999999999999999999999999 LONG EVENT NAME"
    await frames(8)
    check(header.get_global_rect() == header_bounds,"Long header values do not resize the header")
    for label_name in ["EventTitle","EventCurrency","EventCores","EventProduction"]:
        var label = page.find_child(label_name,true,false)
        check(label.clip_text and label.max_lines_visible == 1,label_name+": bounded single-line value")
        check(header.get_global_rect().encloses(label.get_global_rect()),label_name+": long value stays within header")
    await switch_tab("Rewards")
    var remembered = scroll.scroll_vertical
    page.find_child("EventCloseButton",true,false).pressed.emit(); await frames()
    check(game.get_node("BottomBar").visible,"Leaving Event restores normal bar")
    game.get_node("MapPage").find_child("EventMapEntry",true,false).pressed.emit(); await frames()
    check(tab_button("Rewards").button_pressed and scroll.scroll_vertical == remembered,"Reopening same Event preserves tab and scroll")
    game.queue_free(); await frames(6)
    print("Event tabs smoke: ",checks," checks, failures=",failures)
    quit(1 if failures else 0)
