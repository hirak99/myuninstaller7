using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace MyUninstaller7 {
    /***
     * Author: Arnab Bose
     ***/
    class ColoredListBox : ListBox {

        public class ColoredMsg {
            public Color? bgColor;
            public string message;
            public Image icon;
        }
        public static ColoredMsg CreateItem(Color? bg, string _message, Image _icon) {
            ColoredMsg cm = new ColoredMsg();
            cm.bgColor = bg;
            cm.message = _message;
            cm.icon = _icon;
            return cm;
        }
        public static ColoredMsg CreateItem(Color? bg, string _message) {
            return CreateItem(bg, _message, null);
        }

        public static ColoredMsg CreateItem(string _message) {
            return CreateItem(null, _message);
        }

        public ColoredMsg GetItemAt(int index) {
            try {
                return (ColoredMsg)Items[index];
            } catch (Exception) {
                return null;
            }
        }

        public ColoredListBox() {
            DrawMode = DrawMode.OwnerDrawFixed;
            DrawItem += new DrawItemEventHandler(FlexiListBox_DrawItem);
        }

        void FlexiListBox_DrawItem(object sender, DrawItemEventArgs e) {
            ColoredMsg cm = GetItemAt(e.Index);
            if (cm == null || cm.bgColor==null ||
                (e.State == (DrawItemState.Focus | DrawItemState.Selected))) 
                e.DrawBackground();
            else e.Graphics.FillRectangle(new SolidBrush((Color)cm.bgColor), e.Bounds);
            e.DrawFocusRectangle();
            if (cm != null) {
                e.Graphics.DrawString(cm.message,
                    Font,
                    new SolidBrush(ForeColor),
                    e.Bounds.Left, e.Bounds.Top);
                if (cm.icon != null) e.Graphics.DrawImageUnscaled(cm.icon, 0, 0);
            }
        }
    }
}
