// using .js as JavaScript syntax is similar to gml

// ### Basic Example ###
// @ bottom of function scr_gamestart(_ch1)
if (variable_instance_exists(global, "modmenu")) {
  global.menu_my_mods_menu = global.modmenu.create({
    title: "My Mod's Menu",
    ini_name: "my_mods_menu",
    form: [
      {
        type: "Toggle",
        title: "Example Toggle",
        data_ref: { var_name: "example_toggle", default_value: false },
        value_range: "OFF=false;ON=true"
      },{
        type: "Slider",
        title: "Example Slider",
        data_ref: { var_name: "example_slider", default_value: -1 },
        value_range: "OFF=-1;0~1000%;INF=2147483647"
      },{
        type: "Header",
        title: "Example Header"
      },{
        type: "Button",
        title: "Example Button",
        trigger_func: function () {}
      }
    ]
  });
}

// ### Menu with optional fields ###
// @ bottom of function scr_gamestart(_ch1)
if (variable_instance_exists(global, "modmenu")) {
  global.on_menu_close = function () {};

  global.menu_my_mods_menu = global.modmenu.create({
    title: "My Mod's Menu",
    ini_name: "my_mods_menu",
	style: { dark: { left_margin: 20, left_value_pos: 270 } },
	close_func: global.on_menu_close,
    form: [
      // ...row definitions...
    ]
  });
}

// ### Menu with forced slider (force_scroll=true) ###
// @ bottom of function scr_gamestart(_ch1)
if (variable_instance_exists(global, "modmenu")) {
  global.menu_my_mods_menu = global.modmenu.create({
    title: "My Mod's Menu",
    ini_name: "my_mods_menu",
    form: [
	  {
        type: "Slider",
        title: "Example Slider",
        data_ref: { var_name: "example_slider", default_value: 1 },
        value_range: "SMALL=50%;MEDIUM=100%;LARGE=200%"
      },
      // ...other row definitions...
    ]
  });
}

// ### Menu with row that has optional fields ###
// @ bottom of function scr_gamestart(_ch1)
if (variable_instance_exists(global, "modmenu")) {
  global.menu_on_change = function () {};
  global.menu_on_accept = function () {}; // confirmed [Z]/(A)
  global.menu_on_cancel = function () {}; // cancelled [X]/(B)

  global.menu_my_mods_menu = global.modmenu.create({
    title: "My Mod's Menu",
    ini_name: "my_mods_menu",
    form: [
      {
        type: "Slider",
        title: "Example Slider",
        data_ref: { var_name: "example_slider", default_value: -1 },
        value_range: "OFF=-1;0~1000%;INF=2147483647",
		disbled: true,
		hidden: false,
		change_func: global.menu_on_change,
		cancel_func: global.menu_on_cancel,
		accept_func: global.menu_on_accept
      },
      // ...other row definitions...
    ]
  });
}

// ### Menu that dynamically disables row ###
// @ bottom of function scr_gamestart(_ch1)
if (variable_instance_exists(global, "modmenu")) {
  global.menu_my_mods_menu = global.modmenu.create({
    title: "My Mod's Menu",
    ini_name: "my_mods_menu",
    form: [
      {
        type: "Toggle",
        title: "Toggle Mod",
        data_ref: { var_name: "toggle_mod", default_value: false },
        value_range: "OFF=false;ON=true"
      },{
        type: "Slider",
        title: "Example Slider",
        data_ref: { var_name: "example_slider", default_value: -1 },
        value_range: "OFF=-1;0~1000%;INF=2147483647",
		disabled: function() { return !global.toggle_mod; }
      },
	  // ...other row definitions...
    ]
  });
}

// ### Menu that dynamically edits multiple rows ###
// @ bottom of function scr_gamestart(_ch1)
if (variable_instance_exists(global, "modmenu")) {
  global.menu_my_mods_menu = global.modmenu.create({
    title: "My Mod's Menu",
    ini_name: "my_mods_menu",
    form: [
      {
        type: "Toggle",
        title: "Example Toggle",
        data_ref: { var_name: "example_toggle", default_value: false },
        value_range: "OFF=false;ON=true",
		ref: { var_name: "dynamic_rows[0]" }
      },{
        type: "Slider",
        title: "Example Slider",
        data_ref: { var_name: "example_slider", default_value: -1 },
        value_range: "OFF=-1;0~1000%;INF=2147483647",
		ref: { var_name: "dynamic_rows[1]" }
      },{
        type: "Header",
        title: "Example Header"
      },{
        type: "Button",
        title: "Example Button",
        trigger_func: function () {},
		ref: { var_name: "dynamic_rows[2]" }
      }
    ]
  });
}

if (/* something happens*/) {
	for (var i = 0; i < array_length(global.dynamic_rows); i++) {
		var row = global.dynamic_rows[i];
		// do something with row
	}
}
