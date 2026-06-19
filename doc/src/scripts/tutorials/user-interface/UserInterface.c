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

  krpc_UI_Panel_t panel;
  krpc_UI_Canvas_AddPanel(conn, &panel, canvas, true);

  krpc_UI_RectTransform_t rect;
  krpc_UI_Panel_RectTransform(conn, &rect, panel);
  krpc_tuple_double_double_t panel_size = {200, 100};
  krpc_UI_RectTransform_set_Size(conn, rect, &panel_size);
  krpc_tuple_double_double_t panel_pos = {110 - (screen_size.e0 / 2), 0};
  krpc_UI_RectTransform_set_Position(conn, rect, &panel_pos);

  krpc_UI_Button_t button;
  krpc_UI_Panel_AddButton(conn, &button, panel, "Full Throttle", true);
  krpc_UI_RectTransform_t button_rect;
  krpc_UI_Button_RectTransform(conn, &button_rect, button);
  krpc_tuple_double_double_t button_pos = {0, 20};
  krpc_UI_RectTransform_set_Position(conn, button_rect, &button_pos);

  krpc_UI_Text_t text;
  krpc_UI_Panel_AddText(conn, &text, panel, "Thrust: 0 kN", true);
  krpc_UI_RectTransform_t text_rect;
  krpc_UI_Text_RectTransform(conn, &text_rect, text);
  krpc_tuple_double_double_t text_pos = {0, -20};
  krpc_UI_RectTransform_set_Position(conn, text_rect, &text_pos);
  krpc_tuple_double_double_double_t color = {1, 1, 1};
  krpc_UI_Text_set_Color(conn, text, &color);
  krpc_UI_Text_set_Size(conn, text, 18);

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  while (true) {
    bool clicked;
    krpc_UI_Button_Clicked(conn, &clicked, button);
    if (clicked) {
      krpc_SpaceCenter_Control_t control;
      krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
      krpc_SpaceCenter_Control_set_Throttle(conn, control, 1);
      krpc_UI_Button_set_Clicked(conn, button, false);
    }

    float thrust;
    krpc_SpaceCenter_Vessel_Thrust(conn, &thrust, vessel);
    char content[32];
    snprintf(content, sizeof(content), "Thrust: %d kN", (int)(thrust / 1000));
    krpc_UI_Text_set_Content(conn, text, content);

    sleep(1);
  }
}
