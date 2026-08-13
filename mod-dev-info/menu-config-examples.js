// using .js as JavaScript syntax is similar to gml

// ### Basic Example ###
// @ gml_Object_obj_darkcontroller(_ch1)_Create_0
if (!variable_instance_exists(global, "modmenu_data"))
  global.modmenu_data = array_create(0);

var menudata = ds_map_create();
ds_map_add(menudata, "title_en", "My Mod's Menu");

var formdata = array_create(0);

var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "Example Toggle");
ds_map_add(rowdata, "value_range_en", "OFF=false;ON=true");
ds_map_add(rowdata, "value_name", "example_toggle");
array_push(formdata, rowdata);

var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "Example Slider");
ds_map_add(rowdata, "value_range_en", "OFF=-1;0~1000%;INF=2147483647");
ds_map_add(rowdata, "value_name", "example_slider");
array_push(formdata, rowdata);

var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "Example Header");
array_push(formdata, rowdata);

var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "Example Button");
global.button_func = function () {};
ds_map_add(rowdata, "func_name", "button_func");
array_push(formdata, rowdata);

ds_map_add(menudata, "form", formdata);
array_push(global.modmenu_data, menudata);

global.menu_my_mods_menu = menudata;

// ### Menu with optional fields ###
// @ gml_Object_obj_darkcontroller(_ch1)_Create_0
if (!variable_instance_exists(global, "modmenu_data"))
  global.modmenu_data = array_create(0);

var menudata = ds_map_create();
ds_map_add(menudata, "title_en", "My Mod's Menu");
ds_map_add(menudata, "left_margin_en ", 20);
ds_map_add(menudata, "left_value_pos_en ", 270);
global.on_menu_close = function () {};
ds_map_add(menudata, "on_close ", "on_menu_close");

var formdata = array_create(0);

// ...row definitions...

ds_map_add(menudata, "form", formdata);
array_push(global.modmenu_data, menudata);

global.menu_my_mods_menu = menudata;

// ### Menu with forced slider (force_scroll=true) ###
// @ gml_Object_obj_darkcontroller(_ch1)_Create_0
if (!variable_instance_exists(global, "modmenu_data"))
  global.modmenu_data = array_create(0);

var menudata = ds_map_create();
ds_map_add(menudata, "title_en", "My Mod's Menu");

var formdata = array_create(0);

var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "Example Slider");
ds_map_add(rowdata, "value_range_en", "SMALL=50%;MEDIUM=100%;LARGE=200%");
ds_map_add(rowdata, "value_name", "example_slider");
ds_map_add(rowdata, "force_scroll", true);
array_push(formdata, rowdata);

// ...other row definitions...

ds_map_add(menudata, "form", formdata);
array_push(global.modmenu_data, menudata);

global.menu_my_mods_menu = menudata;

// ### Menu with row that has optional fields ###
// @ gml_Object_obj_darkcontroller(_ch1)_Create_0
if (!variable_instance_exists(global, "modmenu_data"))
  global.modmenu_data = array_create(0);

var menudata = ds_map_create();
ds_map_add(menudata, "title_en", "My Mod's Menu");

var formdata = array_create(0);

var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "Example Slider");
ds_map_add(rowdata, "value_range_en", "OFF=-1;0~1000%;INF=2147483647");
ds_map_add(rowdata, "value_name", "example_slider");
ds_map_add(rowdata, "disabled", true);
ds_map_add(rowdata, "hidden", false);
global.menu_on_change = function () {};
ds_map_add(rowdata, "on_change", menu_on_change);
global.menu_on_func = function (arg0) {
	if (arg0) { // confirmed [Z]/(A)
		// ...do stuff...
	} else { //  cancelled [X]/(B)
		// ...do stuff...
	}
};
ds_map_add(rowdata, "func_name", menu_on_func);
array_push(formdata, rowdata);

// ...other row definitions...

ds_map_add(menudata, "form", formdata);
array_push(global.modmenu_data, menudata);

global.menu_my_mods_menu = menudata;

// ### Menu that dynamically disables row ###
// @ gml_Object_obj_darkcontroller(_ch1)_Create_0
if (!variable_instance_exists(global, "modmenu_data"))
  global.modmenu_data = array_create(0);

var menudata = ds_map_create();
ds_map_add(menudata, "title_en", "My Mod's Menu");

var formdata = array_create(0);

var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "Toggle Mod");
ds_map_add(rowdata, "value_range_en", "OFF=false;ON=true");
ds_map_add(rowdata, "value_name", "toggle_mod");
global.do_toggle_mod = function() {
	ds_map_set(global.row_to_toggle, "disabled", !global.toggle_mod);
}
ds_map_add(rowdata, "on_change", "do_toggle_mod");
array_push(formdata, rowdata);

var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "Example Slider");
ds_map_add(rowdata, "value_range_en", "OFF=-1;0~1000%;INF=2147483647");
ds_map_add(rowdata, "value_name", "example_slider");
ds_map_add(rowdata, "disabled", !global.toggle_mod);
array_push(formdata, rowdata);
global.row_to_toggle = rowdata;

// ...other row definitions...

ds_map_add(menudata, "form", formdata);
array_push(global.modmenu_data, menudata);

global.menu_my_mods_menu = menudata;

// ### Menu that dynamically edits multiple rows ###
// @ gml_Object_obj_darkcontroller(_ch1)_Create_0
if (!variable_instance_exists(global, "modmenu_data"))
  global.modmenu_data = array_create(0);

var menudata = ds_map_create();
ds_map_add(menudata, "title_en", "My Mod's Menu");

var formdata = array_create(0);
global.dynamic_rows = array_create(0);

var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "Example Toggle");
ds_map_add(rowdata, "value_range_en", "OFF=false;ON=true");
ds_map_add(rowdata, "value_name", "example_toggle");
array_push(formdata, rowdata);
array_push(dynamic_rows, rowdata);

var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "Example Slider");
ds_map_add(rowdata, "value_range_en", "OFF=-1;0~1000%;INF=2147483647");
ds_map_add(rowdata, "value_name", "example_slider");
array_push(formdata, rowdata);
array_push(dynamic_rows, rowdata);

var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "Example Header");
array_push(formdata, rowdata);

var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "Example Button");
global.button_func = function () {};
ds_map_add(rowdata, "func_name", "button_func");
array_push(formdata, rowdata);
array_push(dynamic_rows, rowdata);

ds_map_add(menudata, "form", formdata);
array_push(global.modmenu_data, menudata);

global.menu_my_mods_menu = menudata;

if (/* something happens*/) {
	for (var i = 0; i < array_length(global.dynamic_rows); i++) {
		var row = global.dynamic_rows[i];
		// do something with row
	}
}
