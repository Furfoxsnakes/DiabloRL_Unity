using System;
using UnityEngine;

namespace Common
{
    public class Menu
    {
        private const int MENU_WIDTH = 240;
        private const int MIN_HEADER_WIDTH = 128;
        private const int MIN_HEADER_HEIGHT = 12;
        private const int BORDER_PADDING = 8;

        protected string Header;
        protected string Summary;
        protected int SummaryPrintFlags = RB.ALIGN_V_TOP | RB.TEXT_OVERFLOW_WRAP;

        public Menu(string header, string summary = "")
        {
            Header = header;
            Summary = summary;
        }

        public virtual void Render()
        {
            var display = RB.DisplaySize;

            var menuRect = new Rect2i(display.width / 2 - MENU_WIDTH / 2, display.height / 2, MENU_WIDTH, 24);
            var summarySize = RB.PrintMeasure(menuRect, RB.ALIGN_H_CENTER | RB.TEXT_OVERFLOW_WRAP, Summary);
            menuRect.height += summarySize.height;
            
            // header
            var headerSize = RB.PrintMeasure(Header);
            headerSize.width += 8;
            var headerRect = new Rect2i(menuRect.x + menuRect.width / 2 - headerSize.width / 2, menuRect.y - 6, headerSize.width,
                MIN_HEADER_HEIGHT);
            
            var summaryRect = new Rect2i(menuRect.x + BORDER_PADDING, menuRect.y + BORDER_PADDING + 8, menuRect.width - BORDER_PADDING, menuRect.height - BORDER_PADDING);

            RB.DrawRectFill(menuRect, C.MenuBG);
            RB.DrawRect(menuRect, Color.white);
            RB.DrawRectFill(headerRect, Color.black);
            RB.DrawRect(headerRect, Color.white);
            RB.Print(headerRect, Color.white, RB.ALIGN_H_CENTER | RB.ALIGN_V_CENTER, Header);
            RB.Print(summaryRect, Color.white, SummaryPrintFlags, Summary);
            
        }
    }
}