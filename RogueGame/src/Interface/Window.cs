using System;
using System.Collections.Generic;
using libtcod;

namespace RogueGame.Interface {

    public class Window {
        // HUD elements color themes
        public static TCODColor backgroundColor = new TCODColor(54, 73, 92);
        public static TCODColor foregroundColor = new TCODColor(255, 255, 255);
        public static TCODColor fadedTextColor = new TCODColor(200, 200, 200);
        public static TCODColor elementDarker = new TCODColor(39, 51, 64);
        public static TCODColor elementDark = new TCODColor(80, 106, 133);
        public static TCODColor elementLight = new TCODColor(109, 142, 176);
        public static TCODColor elementLighter = new TCODColor(104, 170, 237);
        
        public enum EBorderStyle {
            NONE, NORTH, SOUTH, EAST, WEST, ALL
        }

        
        /// <summary>
        /// Top left corner of the element
        /// </summary>
        public Vector2 origin;

        public string title;

        public TCODConsole console;
        public bool isVisible;

        protected EBorderStyle borderStyle;
        protected Entity player;

        protected HUD parent;
        protected Engine engine;

        protected bool markForDestroy = false;

        public Window(int x, int y, int w, int h, string title, EBorderStyle borderStyle, Entity player, HUD parent) {
            origin = new Vector2(x, y);
            console = new TCODConsole(w, h);
            this.title = title;
            this.borderStyle = borderStyle;
            this.player = player;
            this.parent = parent;

            engine = Engine.instance;
            isVisible = true;

            InitializeConsole();
        }


        public virtual void SetVisible(bool newVisible) {
            isVisible = newVisible;
        }

        /// <summary>
        /// Updates the consoles tiles to the default state of this window
        /// </summary>
        public virtual void InitializeConsole() {
            Vector2 screenCoords;
            TCODColor coordColor;
            Vector2 screenCenter = new Vector2(RogueGame.WindowWidth / 2, RogueGame.WindowHeight / 2);

            console.clear();

            for(int x = 0; x < console.getWidth(); x++) {
                for(int y = 0; y < console.getHeight(); y++) {
                    // Get the absolute screen coords
                    screenCoords = new Vector2(x + origin.x, y + origin.y);
                    coordColor = Renderer.GetFadedColor(screenCoords, screenCenter, backgroundColor);

                    console.setCharBackground(x, y, coordColor);
                    DrawBorderAt(x, y, console.getWidth(), console.getHeight(), screenCoords, screenCenter);
                }
            }

            // draw the windows title
            if(title != null) {
                DrawText(2, 0, title, TCODColor.white);
            }            
        }

        /// <summary>
        /// Called to update the contents of the console before drawing to screen
        /// </summary>
        public virtual TCODConsole GetUpdatedConsole() {
            return console;
        }

        /// <summary>
        /// Completely cleans up teh console
        /// </summary>
        public virtual void CleanupWindow() {
            engine.DestoyWindow(this);
        }


        public virtual void OnDestroy() {
            console.Dispose();
        }


        public void DrawRectBorderAt(int x, int y, int left, int top, int right, int bottom, EBorderStyle style, TCODColor borderCol) {
            // border cases
            if(x == left && (style == EBorderStyle.WEST || style == EBorderStyle.ALL)) {
                console.setCharForeground(x, y, borderCol);
                console.setChar(x, y, CharConstants.VERT_LINE);
            }

            if(x == right - 1 && (style == EBorderStyle.EAST || style == EBorderStyle.ALL)) {
                console.setCharForeground(x, y, borderCol);
                console.setChar(x, y, CharConstants.VERT_LINE);
            }

            if(y == top && (style == EBorderStyle.NORTH || style == EBorderStyle.ALL)) {
                console.setCharForeground(x, y, borderCol);
                console.setChar(x, y, CharConstants.HORZ_LINE);
            }

            if(y == bottom - 1 && (style == EBorderStyle.SOUTH || style == EBorderStyle.ALL)) {
                console.setCharForeground(x, y, borderCol);
                console.setChar(x, y, CharConstants.HORZ_LINE);
            }

            // Corner cases: char color has been set by the prev two cases: only if its all sides
            if(style == EBorderStyle.ALL) {
                if(x == left && y == top) {
                    console.setChar(x, y, CharConstants.NW_LINE);
                }
                if(x == right - 1 && y == top) {
                    console.setChar(x, y, CharConstants.NE_LINE);
                }
                if(x == left && y == bottom - 1) {
                    console.setChar(x, y, CharConstants.SW_LINE);
                }
                if(x == right - 1 && y == bottom - 1) {
                    console.setChar(x, y, CharConstants.SE_LINE);
                }
            }
        }
        

        public void DrawBorderAt(int x, int y, int right, int bottom, Vector2 screenCoords, Vector2 screenCenter) {
            TCODColor foreColorFaded = Renderer.GetFadedColor(screenCoords, screenCenter, foregroundColor);
            DrawRectBorderAt(x, y, 0, 0, right, bottom, borderStyle, foreColorFaded);
        }



        /// <summary>
        /// Custom string drawing method that vignets out the text using the renderer
        /// </summary>
        public void DrawText(int x, int y, string text, TCODColor color) {
            TCODColor fadedCol;
            Vector2 pos;
            for(int i = 0; i < text.Length; i++) {
                pos = new Vector2(x + i, y);
                fadedCol = Renderer.GetFadedColor(pos + origin, color);
                console.setCharForeground(pos.X, pos.Y, fadedCol);
                console.setChar(pos.X, pos.Y, text[i]);
            }
        }

        /// <summary>
        /// Draws a bar that has a foreground and background part. The percentage indicates
        /// how much of the bar is covered in the foreground
        /// </summary>
        public void DrawProgressBar(int x, int y, int width, float percentage, 
            TCODColor fg, TCODColor bg) {
            int fgWidth = (int)Math.Ceiling(width * percentage);

            TCODColor barCol = fg;
            for(int i = x; i < x + width; i++) {
                if(i - x >= fgWidth) {
                    barCol = bg;
                }

                console.setCharForeground(i, y, barCol);
                console.setChar(i, y, (char)205);
            }
        }


        /// <summary>
        /// Draws a simple rect
        /// </summary>
        public void DrawRect(int x, int y, int width, TCODColor color, bool resetChar = true) {
            TCODColor fadedCol;
            Vector2 pos;
            for(int i = 0; i < width; i++) {
                pos = new Vector2(x + i, y);
                fadedCol = Renderer.GetFadedColor(pos + origin, color);
                console.setCharBackground(pos.X, pos.Y, fadedCol);
                if(resetChar) {
                    console.setChar(pos.X, pos.Y, ' ');
                }
            }
        }
        


        /// <summary>
        /// Draws a simple rect
        /// </summary>
        public void DrawBox(int x, int y, int width, int height, TCODColor color, bool border = false) {
            TCODColor fadedCol;
            Vector2 pos;
            for(int i = 0; i < width; i++) {
                for(int j = 0; j < height; j++) {
                    pos = new Vector2(x + i, y + j);
                    fadedCol = Renderer.GetFadedColor(pos + origin, color);
                    console.setCharBackground(pos.X, pos.Y, fadedCol);
                    console.setChar(pos.X, pos.Y, ' ');

                    if(border) {
                        DrawRectBorderAt(pos.X, pos.Y, x, y, x + width, y + height, EBorderStyle.ALL, foregroundColor);
                    }
                }
            }
        }

        /// <summary>
        /// Draws a horinontal border line
        /// </summary>
        public void DrawDividerLine(int x, int y, int width) {
            string deviderLine = CharConstants.E_TJOINT + "";
            for(int i = x + 1; i < x + width - 1; i++) {
                deviderLine += CharConstants.HORZ_LINE;
            }
            deviderLine += CharConstants.W_TJOINT;

            DrawText(x, y, deviderLine, TCODColor.white);
        }


        /// <summary>
        /// Draws the text within the box defined by the inputs. Uses the advanced message formating so text
        /// can be richly coloured
        /// </summary>
        public void DrawTextBox(int x, int y, int width, string text) {
            int division = 0;
            int line = 1;
            int prevDivision = 0;

            while(division < text.Length - 1) {
                prevDivision = division;
                division = line * width;

                if(division >= text.Length) {
                    division = text.Length - 1;
                }

                // backtrack to the previous space character
                while(text[division] != ' ') {
                    division -= 1;
                }

                // the space is no longer needed since it will be the start of a new line
                if(text[division] == ' ') {
                    text = text.Remove(division, 1);
                }

                PrintMessage(x, y + line - 1, text.Substring(prevDivision, (division - prevDivision)), Constants.DEFAULT_COL_SET);
                line += 1;
            }
        }


        /// <summary>
        /// Prints the message text in a special formatted way, where % character intacate that the next 
        /// character is a color index into color set
        /// Will keep using that color until it reaches another % symbol, then goes back to colorSet[0]
        /// </summary>
        public void PrintMessage(int x, int y, string messageText, TCODColor[] colorSet, float fadeMultiplier = 1.0f) {
            const char colorBucketChar = '%';

            if(messageText == null || colorSet == null) {
                return;
            }

            // The default color is the first one
            TCODColor fadedCol;

            int colorIdx = 0;
            int letterIdx = 0;
            bool inColorBucket = false;
            Vector2 pos;
            for(int i = 0; i < messageText.Length; i++) {
                // Found a color index indicator character
                if(messageText[i] == colorBucketChar) {
                    i++;

                    // If were not in a bucket right now, then this % indicates a new one
                    if(!inColorBucket) {
                        string colorIdxChar = messageText.Substring(i, 1);
                        colorIdx = Convert.ToInt32(colorIdxChar);
                        if(colorIdx >= colorSet.Length) {
                            // Is not a valid color 
                            colorIdx = 0;
                        }
                        else {
                            inColorBucket = true;
                        }

                        // increment character again b/c this one was the color index number
                        i++;
                    }
                    else {
                        // Otherwise, it indactes the end of a color bucket
                        inColorBucket = false;
                        colorIdx = 0;
                    }
                }

                // Check that the % was not the last char
                if(i >= messageText.Length) {
                    break;
                }

                pos = new Vector2(x + letterIdx, y);

                // now were at the correct letter index and have the correct color index
                fadedCol = Renderer.GetFadedColor(pos + origin, colorSet[colorIdx]);
                fadedCol = Renderer.FadeColor(fadedCol, fadeMultiplier);

                console.setCharForeground(pos.X, pos.Y, fadedCol);
                console.setChar(pos.X, pos.Y, messageText[i]);
                letterIdx++;
            }
        }


        /// <summary>
        /// Transforms the input screen position into this windows console space
        /// </summary>
        public Vector2 ScreenToConsoleCoord(int screenX, int screenY) {
            return new Vector2(screenX - origin.x, screenY - origin.y);
        }

        /// <summary>
        /// Returns true if the input screen coords are within the console
        /// </summary>
        public bool ScreenCoordInConsole(Vector2 pos) {
            if(pos.X >= origin.X && pos.X < origin.X + console.getWidth() &&
                pos.Y >= origin.Y && pos.Y < origin.Y + console.getHeight()) {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Call this when this window is supposed to overlap the map, and the positioning could end up off screen
        /// </summary>
        public void OnInspectorOpen() {
            // double check the positioning
            if(origin.y + console.getHeight() > engine.mainCamera.screenY + engine.mainCamera.height) {
                origin.y = (engine.mainCamera.screenY + engine.mainCamera.height) - console.getHeight();
            }

            if(origin.y < engine.mainCamera.screenY) {
                origin.y = engine.mainCamera.screenY;
            }
        }

    }
}
