using System;


namespace RogueGame.Utilities {
    
   public class PerlinNoise {
   
       private int seed;
   
       public PerlinNoise(int seed) {
           this.seed = seed;
       }
   
   
   
       private float Interpolate(float a, float b, float x) {
           float ft = x * 3.1415927f;
           float f = (1.0f - (float)Math.Cos(ft)) * 0.5f;
   
           return (a * (1.0f - f)) + (b * f);
       }
   
   
       private float Noise(int x, int y) {
           int n = x + y * 57;
           n = (n << 13) ^ seed;
           return (1.0f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f);
       }
   
   
       private float SmoothNoise(int x, int y) {
           float corners = (Noise(x - 1, y - 1) + Noise(x + 1, y - 1) + Noise(x - 1, y + 1) + Noise(x + 1, y + 1)) / 16.0f;
           float sides = (Noise(x - 1, y) + Noise(x + 1, y - 1) + Noise(x, y - 1) + Noise(x, y + 1)) / 8.0f;
           float center = Noise(x, y) / 4.0f;
   
           return corners + sides + center;
       }
   
   
   
       private float InterpNoise(float x, float y) {
           int ix = (int)x;
           float fracx = x - ix;
   
           int iy = (int)y;
           float fracy = y - iy;
   
           float v1 = SmoothNoise(ix, iy);
           float v2 = SmoothNoise(ix + 1, iy);
           float v3 = SmoothNoise(ix, iy + 1);
           float v4 = SmoothNoise(ix + 1, iy + 1);
   
           float i1 = Interpolate(v1, v2, fracx);
           float i2 = Interpolate(v3, v4, fracx);
   
           return Interpolate(i1, i2, fracy);
       }
   
       /// <summary>
       /// Sample the raw noise value at the given coordinate
       /// </summary>
       public float Sample(float x, float y) {
           return InterpNoise(x, y);
       }
   
   
       /// <summary>
       /// Get the perlin noise value at the coordinates, summed over several octaves
       /// </summary>
       public float GetPerlinNoise(float x, float y) {
           const int octaves = 8;
           const float persistance = 0.5f;
           return GetPerlinNoise(x, y, octaves, persistance);
       }
   
       /// <summary>
       /// Get the perlin noise value at the coordinates, summed over a specified number of octaves
       /// </summary>
       public float GetPerlinNoise(float x, float y, int octaves) {
           const float persistance = 0.5f;
           return GetPerlinNoise(x, y, octaves, persistance);
       }
   
       /// <summary>
       /// Get the perlin noise value at the coordinates, summed over several octaves
       /// </summary>
       public float GetPerlinNoise(float x, float y, float roughness) {
           const int octaves = 8;
           return GetPerlinNoise(x, y, octaves, roughness);
       }
   
       /// <summary>
       /// Get the perlin noise value at the coordinates, summed over a specified number of octaves
       /// </summary>
       public float GetPerlinNoise(float x, float y, int octaves, float roughness) {
           float n = 0.0f;
           for(int i = 0; i < octaves; i++) {
               float frequency = (float)Math.Pow(2.0f, i);
               float amplitude = (float)Math.Pow(roughness, i);
   
               n += InterpNoise(x * frequency, y * frequency) * amplitude;
           }
   
           return n;
       }
   }


}
