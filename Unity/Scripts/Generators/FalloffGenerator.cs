using UnityEngine;

public static class FalloffGenerator
{
    public static float falloffA = 3f;
    public static float falloffB = 2.2f;

    /* Génère une carte de falloff pour une taille donnée */
    public static float[,] GenerateFalloffMap(int size)
    {
        float[,] map = new float[size, size];

        // Parcourt chaque cellule et évalue le falloff basé sur la distance normalisée
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                float x = i / (float)size * 2f - 1f;
                float y = j / (float)size * 2f - 1f;
                
                // Utilise la valeur maximale absolue pour évaluer la distance
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = Evaluate(value);
            }
        }

        return map;
    }

    /* Calcule le falloff pour une valeur donnée */
    public static float Evaluate(float value)
    {
        float numerator = Mathf.Pow(value, falloffA);
        float denominator = numerator + Mathf.Pow(falloffB - falloffB * value, falloffA);
        return numerator / denominator;
    }
}
