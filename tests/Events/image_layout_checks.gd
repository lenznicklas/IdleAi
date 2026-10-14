extends RefCounted

# Inspect actual post-container geometry, rather than assuming configured sizes.
static func verify(page, check):
    for area in page.find_children("EventImageArea", "Control", true, false):
        if not area.is_visible_in_tree(): continue
        var image = area.find_child("EventImage", true, false)
        var card = area.get_parent()
        var label = str(card.name)
        check.call(image.texture != null, label + ": texture loaded")
        if image.texture == null: continue
        var native = image.texture.get_size()
        var bounds = area.get_global_rect()
        var rect = image.get_global_rect()
        check.call(image.expand_mode == TextureRect.EXPAND_IGNORE_SIZE, label + ": native size ignored")
        check.call(image.stretch_mode == TextureRect.STRETCH_KEEP_ASPECT_CENTERED, label + ": aspect fit centered")
        check.call(bounds.grow(0.5).encloses(rect), label + ": image stays inside slot")
        check.call(card.get_global_rect().grow(0.5).encloses(bounds), label + ": slot stays inside card")
        check.call(rect.size.x <= native.x and rect.size.y <= native.y, label + ": no native upscaling")
        check.call(rect.get_center().distance_to(bounds.get_center()) < 1, label + ": centered")
        check.call(card.get_global_rect().end.x <= page.get_global_rect().end.x + 0.5, label + ": no horizontal overflow")
        for child in card.get_children():
            if child is Label or child is Button:
                check.call(not bounds.intersects(child.get_global_rect()), label + ": image slot separated from " + str(child.name))
        if str(card.name).begins_with("EventGenerator_"):
            check.call(is_equal_approx(card.size.y, 490), label + ": consistent card height")
            check.call(is_equal_approx(area.size.y, 160), label + ": fixed image region height")
            check.call(rect.size.x <= 240.5 and rect.size.y <= 160.5, label + ": maximum image area")

static func verify_source_sizes(page, tree, check):
    var area = page.find_child("EventImageArea", true, false)
    var image = area.find_child("EventImage", true, false)
    var original = image.texture
    # Tiny, large, square, panoramic and portrait sources share the same layout.
    for dimensions in [Vector2i(16, 16), Vector2i(256, 256), Vector2i(1024, 1024), Vector2i(1024, 128), Vector2i(128, 1024)]:
        image.texture = ImageTexture.create_from_image(Image.create(dimensions.x, dimensions.y, false, Image.FORMAT_RGBA8))
        area.resized.emit()
        for frame in range(3): await tree.process_frame
        check.call(area.get_global_rect().grow(0.5).encloses(image.get_global_rect()), "synthetic texture fits: " + str(dimensions))
        check.call(image.size.x <= dimensions.x and image.size.y <= dimensions.y, "synthetic texture never upscaled: " + str(dimensions))
        check.call(image.size.x <= 240.5 and image.size.y <= 160.5, "synthetic texture respects maximum: " + str(dimensions))
    image.texture = original
    area.resized.emit()
    for frame in range(3): await tree.process_frame
