using System;


namespace RogueGame {

    /// <summary>
    /// Tells teh renderer what section of the world to re-draw
    /// </summary>
    public class Camera {

        public enum EBound {
            Left, Right, Top, Bottom
        }

        public Vector2 position;

        public int width;
        public int height;

        // Screen offset coords to start drawing the camera image at
        public int screenX;
        public int screenY;

        public Camera(int x, int y, int width, int height) {
            this.width = width;
            this.height = height;
            this.screenX = x;
            this.screenY = y;
        }


        /// <summary>
        /// Returns the world position of the input frustum bound
        /// </summary>
        public int GetBound(EBound bound) {
            switch(bound) {
                case EBound.Left:
                    return position.X - width / 2;
                case EBound.Right:
                    return position.X + width / 2;
                case EBound.Top:
                    return position.Y - height / 2;
                case EBound.Bottom:
                    return position.Y + height / 2;
            }

            return 0;
        }
    }
}
