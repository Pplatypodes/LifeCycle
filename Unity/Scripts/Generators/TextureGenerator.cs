using UnityEngine;

public static class TextureGenerator
{
    /* Crée une texture à partir d'une height map */
    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        Texture2D texture = new Texture2D(width, height);
        Color[] colorMap = new Color[width * height];

        // Parcourt chaque pixel et génère une couleur en interpolant entre noir et blanc
        for (int y = 0; y < height; y++) 
        {
            for (int x = 0; x < width; x++) 
            {
                float value = heightMap[x, y];
                // Ajuste le gradient selon les besoins
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, value);
            }
        }

        // Configure le mode de filtrage et d'emballage de la texture
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();

        return texture;
    }

    /* Crée une texture à partir d'une carte de couleurs */
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        
        // Configure le mode de filtrage et d'emballage de la texture
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }

    /* Crée une miniature de la texture originale */
    public static Texture2D CreateThumbnail(Texture2D original, int targetWidth, int targetHeight)
    {
        Texture2D thumb = new Texture2D(targetWidth, targetHeight);
        
        // Parcourt chaque pixel de la miniature et copie la couleur correspondante
        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                float u = x / (float)(targetWidth - 1);
                float v = y / (float)(targetHeight - 1);
                Color col = original.GetPixelBilinear(u, v);
                thumb.SetPixel(x, y, col);
            }
        }
        thumb.Apply();
        return thumb;
    }
}
