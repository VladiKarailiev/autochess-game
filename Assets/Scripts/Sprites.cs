using UnityEngine;

namespace AutoChess
{
    public static class Sprites
    {
        static Sprite cachedSquare;
        static Sprite cachedCircle;

        public static Sprite GetSquare()
        {
            if (cachedSquare == null)
            {
                var tex = Texture2D.whiteTexture;
                cachedSquare = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit: tex.width);
            }
            return cachedSquare;
        }

        public static Sprite GetCircle(int resolution = 128)
        {
            if (cachedCircle == null)
            {
                var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, mipChain: true);
                tex.filterMode = FilterMode.Trilinear;
                tex.wrapMode = TextureWrapMode.Clamp;

                float center = (resolution - 1) * 0.5f;
                float outerRadius = resolution * 0.5f - 0.5f;
                float innerRadius = outerRadius * 0.85f;
                var pixels = new Color32[resolution * resolution];

                const int sub = 4;
                const float subInv = 1f / sub;
                const int sub2 = sub * sub;

                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        int alphaCount = 0;
                        int fillCount = 0;
                        for (int sy = 0; sy < sub; sy++)
                        {
                            for (int sx = 0; sx < sub; sx++)
                            {
                                float fx = x + (sx + 0.5f) * subInv - 0.5f;
                                float fy = y + (sy + 0.5f) * subInv - 0.5f;
                                float dx = fx - center;
                                float dy = fy - center;
                                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                                if (dist <= outerRadius) alphaCount++;
                                if (dist <= innerRadius) fillCount++;
                            }
                        }
                        float alpha = (float)alphaCount / sub2;
                        float fill  = (float)fillCount / sub2;
                        byte v = (byte)(fill * 255f);
                        byte a = (byte)(alpha * 255f);
                        pixels[y * resolution + x] = new Color32(v, v, v, a);
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply(updateMipmaps: true);
                cachedCircle = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, resolution, resolution),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit: resolution);
            }
            return cachedCircle;
        }
    }
}
