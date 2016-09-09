using System;
using System.Collections.Generic;
using libtcod;
using RogueGame.Map;
using RogueGame.Components;
using RogueGame.Interface;

namespace RogueGame {
    public class Renderer {

        // Fades out the colors from the middle of the screen tothe edge, purely visual effect
        private const bool doCircularFade = true;
        private const float minBrightness = 0.9f;
        private const float fadeExponent = 5.0f;
        private const float blockedBrightness = 0.8f;


        // Should tiles be drawn normally if they are out of LOS and should they be drawn if they havnt been explored yet
        private bool showOnlyLOS = true;
        private bool showOnlyExplored = true;

        public List<RenderComponent> drawList;

        private Engine engine;

        public Renderer()  {
            engine = Engine.instance;
            drawList = new List<RenderComponent>();
        }


        public void DrawMenu(TCODConsole targetConsole, WindowGameMenu menu) {
            if(!menu.isVisible) {
                return;
            }

            // ask for sub menus, this is completely optional on the part of the menu
            List<Window> subMenus =  menu.GetSubMenus();            
            TCODConsole console = menu.GetUpdatedConsole();

            TCODConsole.blit(console, 0, 0, console.getWidth(), console.getHeight(),           // source
                             targetConsole, menu.origin.X, menu.origin.Y, 1);
            
            // handle dragdrops
            var dragdrop = menu.dragdrop;
            if(dragdrop.active) {
                DrawRect(targetConsole, dragdrop.x, dragdrop.y, dragdrop.text.Length + 1, Window.elementDark);
                DrawText(targetConsole, dragdrop.x + 1, dragdrop.y, dragdrop.text, Constants.COL_FRIENDLY);
                DrawText(targetConsole, dragdrop.x, dragdrop.y, "" + (char)(25), TCODColor.white);
            }

            // sub menus (if any)
            foreach(Window sub in subMenus) {
                if(!sub.isVisible) {
                    continue;
                }

                console = sub.GetUpdatedConsole();
                TCODConsole.blit(console, 0, 0, console.getWidth(), console.getHeight(),           // source
                            targetConsole, sub.origin.X, sub.origin.Y, 1);
            }
        }


        public void ReDraw(TCODConsole targetConsole, AreaMap currentMap) {
            Vector2 screenCenter = new Vector2(Engine.instance.consoleWidth / 2,
                                               Engine.instance.consoleHeight / 2);
            float maxDist = screenCenter.Magnitude();

            Camera camera = engine.mainCamera;

            // Render the level background, then drawComponents, then HUD
            RenderGameBackground(targetConsole, camera, currentMap, screenCenter);
            RenderEntities(targetConsole, camera, currentMap, screenCenter);
            RenderGameHUD(targetConsole, camera, screenCenter);            
        }


        /// <summary>
        /// Renders the current map as the background of the level
        /// </summary>
        private void RenderGameBackground(TCODConsole targetConsole, Camera camera, AreaMap currentMap,
                                          Vector2 screenCenter) {

            int left = camera.GetBound(Camera.EBound.Left);
            int right = camera.GetBound(Camera.EBound.Right);
            int top = camera.GetBound(Camera.EBound.Top);
            int bottom = camera.GetBound(Camera.EBound.Bottom);
            
            if(left < 0) {
                left = 0;
                right = camera.width;
            }

            if(top < 0) {
                top = 0;
                bottom = camera.height;
            }

            if(right > currentMap.width) {
                left = currentMap.width - camera.width;
                right = currentMap.width;
            }

            if(bottom > currentMap.height) {
                top = currentMap.height - camera.height;
                bottom = currentMap.height;
            }

            TCODColor fg, bg;
            for(int y = top; y < bottom; y++) {
                for(int x = left; x < right; x++) {
                    // Translate the world coords into screen coords
                    int screenX = camera.screenX + x - left;
                    int screenY = camera.screenY + y - top;

                    var tile = currentMap.GetTile(x, y);
                    fg = tile.terrain.fg;
                    bg = tile.terrain.bg;
                    char ch = tile.terrain.Ch;

                    if(!tile.explored && showOnlyExplored) {
                        bg = AreaMap.unexplored.bg;
                        fg = AreaMap.unexplored.fg;
                        ch = AreaMap.unexplored.Ch;
                    }

                    // If the tile is not in the current LOS, fade its color
                    if(!tile.cachedLOS && showOnlyLOS) {
                        bg = FadeColor(bg, blockedBrightness);
                        fg = FadeColor(fg, blockedBrightness);
                    }

                    if(doCircularFade) {
                        bg = GetFadedColor(new Vector2(screenX, screenY), screenCenter, bg);
                        fg = GetFadedColor(new Vector2(screenX, screenY), screenCenter, fg);
                    }

                    targetConsole.setCharBackground(screenX, screenY, bg);
                    targetConsole.setCharForeground(screenX, screenY, fg);
                    targetConsole.setChar(screenX, screenY, ch);
                }
            }
        }



        /// <summary>
        /// Renders all game objects that have a draw component attached onto the target console, at their world positions
        /// Also obeys current visibility rules
        /// </summary>
        private void RenderEntities(TCODConsole targetConsole, Camera camera, AreaMap currentMap,
                                       Vector2 screenCenter) {

            int left = camera.GetBound(Camera.EBound.Left);
            int right = camera.GetBound(Camera.EBound.Right);
            int top = camera.GetBound(Camera.EBound.Top);
            int bottom = camera.GetBound(Camera.EBound.Bottom);

            if(left < 0) {
                left = 0;
                right = camera.width;
            }

            if(top < 0) {
                top = 0;
                bottom = camera.height;
            }

            if(right > currentMap.width) {
                left = currentMap.width - camera.width;
                right = currentMap.width;
            }

            if(bottom > currentMap.height) {
                top = currentMap.height - camera.height;
                bottom = currentMap.height;
            }

            Vector2 pos;
            TCODColor col;
            for(int i = drawList.Count - 1; i >= 0; i--) {
                pos = drawList[i].owner.position;
            
                bool los = currentMap.GetTile(pos.X, pos.Y).cachedLOS;
                bool explored = currentMap.GetTile(pos.X, pos.Y).explored;
                bool isVisible = true;

                // Do not draw object if it is not in los and is not static, or if it is static, then only if it hasn't been explored yet
                if(!explored) {
                    continue;
                }
                else if(!los) {
                    // check if the entity wants to be rendered anyways
                    if(!drawList[i].owner.Has(typeof(StaticComponent))) {
                        continue;
                    }

                    isVisible = false;
                }

                col = drawList[i].color;
                TCODColor fadedColor = new TCODColor(col.Red, col.Green, col.Blue);
            
                // Fade the color based on how far from the center it is
                if(doCircularFade) {
                    fadedColor = GetFadedColor(pos, screenCenter, col);
                }

                if(!isVisible) {
                    fadedColor = FadeColor(fadedColor, 0.5f);
                }
                
                int screenX = camera.screenX + pos.X - left;
                int screenY = camera.screenY  + pos.Y - top;
                
                // Draw the char to the forground layer
                targetConsole.setCharForeground(screenX, screenY, fadedColor);
                targetConsole.setChar(screenX, screenY, drawList[i].ch);
            }
        }


        /// <summary>
        /// Renders the games HUD elemnts
        /// Should be called last so that it blits consoles on top of game content if need be
        /// </summary>
        private void RenderGameHUD(TCODConsole targetConsole, Camera camera, Vector2 screenCenter) {
            HUD hud = engine.playerController.hud;
            
            // Allow the generalized HUD to render
            hud.Draw(targetConsole, camera);
            
            TCODConsole hudConsole;
            for(int i = 0; i < hud.windows.Count; i++) {
                if(!hud.windows[i].isVisible) {
                    continue;
                }
                
                hudConsole = hud.windows[i].GetUpdatedConsole();
                if(!hud.windows[i].isVisible) {
                    continue;
                }
                TCODConsole.blit(hudConsole, 0, 0, hudConsole.getWidth(), hudConsole.getHeight(),           // source
                                 targetConsole, hud.windows[i].origin.X, hud.windows[i].origin.Y, 1);       // destination
            }

            // draw drag drop data if there is any
            var dragdrop = hud.dragdrop;
            if(dragdrop.active) {
                DrawRect(targetConsole, dragdrop.x, dragdrop.y, dragdrop.text.Length + 1, Window.elementDark);
                DrawText(targetConsole, dragdrop.x + 1, dragdrop.y, dragdrop.text, Constants.COL_FRIENDLY);
                DrawText(targetConsole, dragdrop.x, dragdrop.y, "" + (char)(25), TCODColor.white);        
            }
        }



        public static TCODColor GetFadedColor(Vector2 position, TCODColor baseColor) {
            Vector2 screenCenter = new Vector2(RogueGame.WindowWidth / 2, RogueGame.WindowHeight / 2);
            return GetFadedColor(position, screenCenter, baseColor);
        }


        /// <summary>
        /// Fades out the input color to black based on how close it is to the center
        /// </summary>
        public static TCODColor GetFadedColor(Vector2 position, Vector2 center, TCODColor baseColor) {
            Vector2 displacement = position - center;
            float maxDist = (new Vector2() - center).SqrMagnitude();

            float dist = displacement.SqrMagnitude();
            float distPct = 1.0f - (dist / (maxDist));
            float fadePct = distPct + ((1.0f - distPct) * minBrightness);

            // fade out on a quadratic curve
            fadePct = (float)Math.Pow(fadePct, fadeExponent);

            TCODColor fadedColor = new TCODColor(baseColor.Red, baseColor.Green, baseColor.Blue);
            fadedColor.Red = (byte)(baseColor.Red * fadePct);
            fadedColor.Green = (byte)(baseColor.Green * fadePct);
            fadedColor.Blue = (byte)(baseColor.Blue * fadePct);

            return fadedColor;
        }


        public static TCODColor FadeColor(TCODColor color, float multiplier, bool toGrey = false) {
            TCODColor fadedColor = new TCODColor(color.Red, color.Green, color.Blue);
            if(toGrey) {
                int grey = (color.Red + color.Green + color.Blue) / 3;
                fadedColor.Red = (byte)(grey * multiplier);
                fadedColor.Green = (byte)(grey * multiplier);
                fadedColor.Blue = (byte)(grey * multiplier);
            }
            else {
                fadedColor.Red = (byte)(color.Red * multiplier);
                fadedColor.Green = (byte)(color.Green * multiplier);
                fadedColor.Blue = (byte)(color.Blue * multiplier);
            }

            return fadedColor;
        }


        /// <summary>
        /// Fade from color a to bolor b by the normalized ratio dx
        /// </summary>
        public static TCODColor CrossFadeColor(TCODColor colora, TCODColor colorb, float dx) {
            float redVal = colora.Red * dx + colorb.Red * (1.0f - dx);
            float greenVal = colora.Green * dx + colorb.Green * (1.0f - dx);
            float blueVal = colora.Blue * dx + colorb.Blue * (1.0f - dx);

            return new TCODColor((byte)redVal, (byte)greenVal, (byte)blueVal);
        }



        /// <summary>
        /// Custom string drawing method that vignets out the text using the renderer
        /// </summary>
        public static void DrawText(TCODConsole console, int x, int y, string text, TCODColor color) {
            TCODColor fadedCol;
            Vector2 pos;
            for(int i = 0; i < text.Length; i++) {
                pos = new Vector2(x + i, y);
                fadedCol = GetFadedColor(pos, color);
                console.setCharForeground(pos.X, pos.Y, fadedCol);
                console.setChar(pos.X, pos.Y, text[i]);
            }
        }

        /// <summary>
        /// Draws a simple rect
        /// </summary>
        public static void DrawRect(TCODConsole console, int x, int y, int width, TCODColor color) {
            TCODColor fadedCol;
            Vector2 pos;
            for(int i = 0; i < width; i++) {
                pos = new Vector2(x + i, y);
                fadedCol = GetFadedColor(pos, color);
                console.setCharBackground(pos.X, pos.Y, fadedCol);
                console.setChar(pos.X, pos.Y, ' ');
            }
        }


        /// <summary>
        /// Draws a line on the console from start to end
        /// </summary>
        public static void DrawLine(TCODConsole console, Vector2 from, Vector2 to, TCODColor color, bool fade = true) {
            if(from == to) {
                return;
            }

            Vector2 delta = from - to;
            int absDeltax = Math.Abs(delta.X);
            int absDeltay = Math.Abs(delta.Y);
            int signx = Math.Sign(delta.x);
            int signy = Math.Sign(delta.y);
            int error = 0;

            int x = to.X;
            int y = to.Y;
            Vector2 current;
            TCODColor currentBg;

            // Is the line x or y dominant
            if(absDeltax > absDeltay) {
                error = absDeltay * 2 - absDeltax;
                do {
                    if(error >= 0) {
                        y += signy;
                        error -= absDeltax * 2;
                    }

                    x += signx;
                    error += absDeltay * 2;

                    if(x == from.x && y == from.y) {
                        break;
                    }

                    current = new Vector2(x - to.x, y - to.y);
                    float mag = fade ? current.SqrMagnitude() / (to - from).SqrMagnitude() : 1.0f;
                    currentBg = console.getCharBackground(x, y);


                    console.setCharBackground(x, y, CrossFadeColor(color, currentBg, mag));
                } while(x != from.x || y != from.y);

            }
            else {
                error = absDeltax * 2 - absDeltay;
                do {
                    if(error >= 0) {
                        x += signx;
                        error -= absDeltay * 2;
                    }

                    y += signy;
                    error += absDeltax * 2;

                    if(x == from.x && y == from.y) {
                        break;
                    }

                    current = new Vector2(x - to.x, y - to.y);
                    float mag = fade ? current.SqrMagnitude() / (to - from).SqrMagnitude() : 1.0f;
                    currentBg = console.getCharBackground(x, y);

                    console.setCharBackground(x, y, CrossFadeColor(color, currentBg, mag));
                } while(x != from.x || y != from.y);

            }
        }


        /// <summary>
        /// Draw a cricle, which optionally fades out towards the edges. Only sets cell backgrounds
        /// </summary>
        public static void DrawCircle(TCODConsole console, Vector2 center, float radius, TCODColor color, bool fade = true) {
            TCODColor currentCol;
            Vector2 pos;
            int e = (int)(center.x + radius);
            int w = (int)(center.x - radius);
            int n = (int)(center.y - radius);
            int s = (int)(center.y + radius);

            // small optimization so that the distance doesnt have to be calculated for each tile
            float radSqr = radius * radius;

            for(int x = w; x <= e; x++) {
                for(int y = n; y <= s;y ++) {
                    pos.x = x;
                    pos.y = y;

                    float sqrDist = (center - pos).SqrMagnitude();
                    if(sqrDist > radSqr) {
                        continue;
                    }

                    float mag = fade ? 1.0f - (sqrDist / radSqr) : 1.0f;

                    currentCol = console.getCharBackground(x, y);
                    console.setCharBackground(x, y, CrossFadeColor(color, currentCol, mag));
                }
            }
        }



        /// <summary>
        /// Translates the input world coordinates into screen coordinates
        /// </summary>
        public static Vector2 WorldToScreenCoord(Camera camera, Vector2 worldCoords) {
            int left = camera.GetBound(Camera.EBound.Left);
            int right = camera.GetBound(Camera.EBound.Right);
            int top = camera.GetBound(Camera.EBound.Top);
            int bottom = camera.GetBound(Camera.EBound.Bottom);

            AreaMap world = Engine.instance.world.currentMap;

            if(left < 0) {
                left = 0;
                right = camera.width;
            }

            if(top < 0) {
                top = 0;
                bottom = camera.height;
            }

            if(right > world.width) {
                left = world.width - camera.width;
                right = world.width;
            }

            if(bottom > world.height) {
                top = world.height - camera.height;
                bottom = world.height;
            }

            Vector2 screen;
            screen.x = camera.screenX + worldCoords.X - left;
            screen.y = camera.screenY + worldCoords.Y - top;

            return screen;
        }


        /// <summary>
        /// Translates the input screen coordinates into world coordinates
        /// </summary>
        public static Vector2 ScreenToWorldCoord(Camera camera, Vector2 screenCoords) {
            int left = camera.GetBound(Camera.EBound.Left);
            int right = camera.GetBound(Camera.EBound.Right);
            int top = camera.GetBound(Camera.EBound.Top);
            int bottom = camera.GetBound(Camera.EBound.Bottom);

            AreaMap world = Engine.instance.world.currentMap;

            if(left < 0) {
                left = 0;
                right = camera.width;
            }

            if(top < 0) {
                top = 0;
                bottom = camera.height;
            }

            if(right > world.width) {
                left = world.width - camera.width;
                right = world.width;
            }

            if(bottom > world.height) {
                top = world.height - camera.height;
                bottom = world.height;
            }

            Vector2 worldLoc;
            worldLoc.x = screenCoords.X + left - camera.screenX;
            worldLoc.y = screenCoords.Y + top - camera.screenY;

            return worldLoc;
        }

    }
}

