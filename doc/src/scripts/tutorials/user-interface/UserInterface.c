#include <stdio.h>
#include <unistd.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <krpc_cnano/services/ui.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "User Interface Example");

  krpc_UI_Canvas_t canvas;
  krpc_UI_StockCanvas(conn, &canvas);

  krpc_UI_RectTransform_t canvas_rect;
  krpc_UI_Canvas_RectTransform(conn, &canvas_rect, canvas);
  krpc_tuple_double_double_t screen_size;
  krpc_UI_RectTransform_Size(conn, &screen_size, canvas_rect);

  // Add a panel to contain the UI elements. The user can drag it around the
  // screen with the mouse.
  krpc_UI_Panel_t panel;
  krpc_UI_Canvas_AddPanel(conn, &panel, canvas, true);
  krpc_UI_Panel_set_Draggable(conn, panel, true);

  // Position the panel on the left of the screen
  krpc_UI_RectTransform_t rect;
  krpc_UI_Panel_RectTransform(conn, &rect, panel);
  krpc_tuple_double_double_t panel_size = {200, 120};
  krpc_UI_RectTransform_set_Size(conn, rect, &panel_size);
  krpc_tuple_double_double_t panel_pos = {110 - (screen_size.e0 / 2), 0};
  krpc_UI_RectTransform_set_Position(conn, rect, &panel_pos);

  // Lay the contents of the panel out in a column
  krpc_UI_Layout_t layout;
  krpc_UI_Panel_AddVerticalLayout(conn, &layout, panel);
  krpc_tuple_int32_int32_int32_int32_t padding = {10, 10, 10, 10};
  krpc_UI_Layout_set_Padding(conn, layout, &padding);
  krpc_UI_Layout_set_Spacing(conn, layout, 6);

  // Add a button to set the throttle to maximum, with a tooltip shown while
  // the mouse rests on it
  krpc_UI_Button_t button;
  krpc_UI_Panel_AddButton(conn, &button, panel, "Full Throttle", true);
  krpc_UI_Button_set_Tooltip(conn, button, "Set the throttle to maximum");
  krpc_UI_LayoutElement_t button_element;
  krpc_UI_Button_LayoutElement(conn, &button_element, button);
  krpc_tuple_double_double_t button_size = {-1, 30};
  krpc_UI_LayoutElement_set_PreferredSize(conn, button_element, &button_size);

  // Add a slider to set the throttle by hand
  krpc_UI_Slider_t slider;
  krpc_UI_Panel_AddSlider(conn, &slider, panel, false, true);
  krpc_UI_LayoutElement_t slider_element;
  krpc_UI_Slider_LayoutElement(conn, &slider_element, slider);
  krpc_tuple_double_double_t slider_size = {-1, 20};
  krpc_UI_LayoutElement_set_PreferredSize(conn, slider_element, &slider_size);

  // Add some text displaying the total engine thrust
  krpc_UI_Text_t text;
  krpc_UI_Panel_AddText(conn, &text, panel, "Thrust: 0 kN", true);
  krpc_tuple_double_double_double_double_t color = {1, 1, 1, 1};
  krpc_UI_Text_set_Color(conn, text, &color);
  krpc_UI_Text_set_Size(conn, text, 18);

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  while (true) {
    // Handle the throttle button being clicked
    bool clicked;
    krpc_UI_Button_Clicked(conn, &clicked, button);
    if (clicked) {
      krpc_SpaceCenter_Control_t control;
      krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
      krpc_SpaceCenter_Control_set_Throttle(conn, control, 1);
      krpc_UI_Slider_set_Value(conn, slider, 1);
      krpc_UI_Button_set_Clicked(conn, button, false);
    }

    // Handle the user moving the throttle slider
    bool changed;
    krpc_UI_Slider_Changed(conn, &changed, slider);
    if (changed) {
      float value;
      krpc_UI_Slider_Value(conn, &value, slider);
      krpc_SpaceCenter_Control_t control;
      krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
      krpc_SpaceCenter_Control_set_Throttle(conn, control, value);
      krpc_UI_Slider_set_Changed(conn, slider, false);
    }

    float thrust;
    krpc_SpaceCenter_Vessel_Thrust(conn, &thrust, vessel);
    char content[32];
    snprintf(content, sizeof(content), "Thrust: %d kN", (int)(thrust / 1000));
    krpc_UI_Text_set_Content(conn, text, content);

    sleep(1);
  }
}
