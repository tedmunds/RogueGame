using System;
using libtcod;

namespace RogueGame.Interface {
    public class Button {
        public delegate void ClickHandler(Button clicked);

        public Vector2 origin;
        public Vector2 size;

        public string text;

        public TCODColor normalColor;
        public TCODColor hoverColor;
        public TCODColor textColor;

        /// <summary>
        /// An arbitrary data pointer the button can carry, that can be accessed inside the button handler
        /// </summary>
        public object payload = null;

        /// <summary>
        /// The handler to call when the button is clicked
        /// </summary>
        public ClickHandler clickHandler;

        public bool isHovered = false;

        public Button(int x, int y, int w, int h, string text, 
            TCODColor normalColor, TCODColor hoverColor, TCODColor textColor,
            ClickHandler clickHandler) {

            origin = new Vector2(x, y);
            size = new Vector2(w, h);

            this.text = text;
            this.normalColor = normalColor;
            this.hoverColor = hoverColor;
            this.textColor = textColor;
            this.clickHandler = clickHandler;
        }



        /// <summary>
        /// Update the button based on the input mouse state
        /// </summary>
        public void Update(TCODMouseData mState) {
            if(mState.CellX >= origin.X && mState.CellX < origin.X + size.X &&
               mState.CellY >= origin.Y && mState.CellY < origin.Y + size.Y) {
                isHovered = true;

                if(mState.LeftButtonPressed) {
                    clickHandler(this);
                }
            }
            else {
                isHovered = false;
            }
        }

        /// <summary>
        /// Draw the button to the input window
        /// </summary>
        public void Draw(Window ownerWindow) {
            TCODColor col = isHovered ? hoverColor : normalColor;

            Vector2 relativePos = ownerWindow.ScreenToConsoleCoord(origin.X, origin.Y);
            ownerWindow.DrawBox(relativePos.X, relativePos.Y, size.X, size.Y, col);
            ownerWindow.DrawText(
                relativePos.X + size.X / 2 - text.Length / 2,
                relativePos.Y + size.Y / 2, 
                text, textColor);
        }


    }
}
