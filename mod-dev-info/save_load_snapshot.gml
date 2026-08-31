var _snap_post = "__snapch" + string(global.chapter);
var _debug = global.debug;
var _room_id_list = [];
array_copy(_room_id_list, 0, global.room_id_list, 0, array_length(global.room_id_list));
try {
    display_mouse_set(display_get_width() / 2, display_get_height() / 2);
    if show_question("SAVE (yes), or LOAD (no)?\n\nNote: won't remember position, progress through battles, or progress through cutscenes/dialogues.") {
        var _file = get_save_filename_ext(
            "save game (" + _snap_post + ")|*" + _snap_post,
            room_get_name(room) + ((global.chapter > 1 && scr_sideb_get_phase() >= 3) ? "_SideB" : "") + _snap_post,
            working_directory,
            "Save snapshot"
        );

        if (_file != "") {
            if (file_exists(_file))
                file_delete(_file);
            /*file <- */scr_tempsave();
            file_copy(file, _file);

            // save screenshot at 320 by 240 (will be a bit scuffed but good enough for a reference)
            var temp_app_surf = surface_create(320, 240);
            surface_set_target(temp_app_surf);
            draw_surface_stretched(application_surface, 0, 0, 320, 240);
            surface_save(temp_app_surf, _file + ".png");
            surface_reset_target();
            surface_free(temp_app_surf);

            display_mouse_set(display_get_width() / 2, display_get_height() / 2);
            show_message("Success: " + _file + " saved.");
        }
    } else {
        var _file = get_open_filename_ext(
            "save game (" + _snap_post + ")|*" + _snap_post,
            "",
            working_directory,
            "Load snapshot"
        );

        if (_file != "") {
            // bypass dogcheck and room list lookup
            global.debug = 1;
            global.room_id_list = [];
            var _room = room_first;
            while (_room >= 0) {
                array_push(global.room_id_list, new scr_room(_room, _room + (global.chapter * 10000)));
                _room = room_next(_room);
            }

            /*file <- */scr_tempsave();
            file_copy(_file, file);
            scr_tempload();
        }
    }
} catch (_e) { display_mouse_set(display_get_width() / 2, display_get_height() / 2); show_message(_e.longMessage); } finally {
    global.debug = _debug;
    global.room_id_list = [];
    array_copy(global.room_id_list, 0, _room_id_list, 0, array_length(_room_id_list));
}