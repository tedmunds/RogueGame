using System;
using libtcod;
using System.Collections.Generic;


namespace RogueGame.Interface {
    public class TextField {
        public Vector2 origin;
        public Vector2 size;

        public string text;

        public TCODColor bgColor;
        public TCODColor focusedColor;
        public TCODColor textColor;

        /// <summary>
        /// The handler to call when thetextfield is in focus and user presses enter
        /// </summary>
        public Action enterHandler;

        public bool isInFocus;


        public TextField(int x, int y, int w, int h,
            TCODColor bgColor, TCODColor focusedColor, TCODColor textColor,
            Action enterHandler) {

            origin = new Vector2(x, y);
            size = new Vector2(w, h);
            
            this.bgColor = bgColor;
            this.focusedColor = focusedColor;
            this.textColor = textColor;
            this.enterHandler = enterHandler;

            text = "";
            isInFocus = false;
        }



        /// <summary>
        /// Update the button based on the input mouse state
        /// </summary>
        public void Update(TCODMouseData mState, TCODKey kState) {
            if(mState.CellX >= origin.X && mState.CellX < origin.X + size.X &&
               mState.CellY >= origin.Y && mState.CellY < origin.Y + size.Y) {
                
                if(mState.LeftButtonPressed) {
                    isInFocus = true;
                }
            }
            else if(mState.LeftButtonPressed) {
                isInFocus = false;
            }

            if(isInFocus) {
                char ch = kState.Character;
                
                // append the character, unless it would be longer than the size of the box
                if((ch >= 'A' && ch <= 'z' || ch == ' ' ) && text.Length + 1 < size.X) {
                    text += ch;
                }

                // do backspace to remove char
                if(kState.KeyCode == TCODKeyCode.Backspace && text.Length >= 1) {
                    text = text.Remove(text.Length - 1, 1);
                }
            }

            if(kState.KeyCode == TCODKeyCode.Enter) {
                isInFocus = false;
                enterHandler();
            }
        }

        /// <summary>
        /// Draw the button to the input window
        /// </summary>
        public void Draw(Window ownerWindow) {
            TCODColor col = isInFocus ? bgColor : focusedColor;

            Vector2 relativePos = ownerWindow.ScreenToConsoleCoord(origin.X, origin.Y);

            // draw the background
            ownerWindow.DrawBox(relativePos.X, relativePos.Y, size.X, size.Y, col);
            ownerWindow.DrawText(relativePos.X, relativePos.Y,
                text, textColor);
        }


    }
}
