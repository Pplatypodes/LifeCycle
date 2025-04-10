using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SystemEvolutionLogger : MonoBehaviour
{
    /* Instance unique du logger d'évolution système */
    public static SystemEvolutionLogger Instance { get; private set; }

    [System.Serializable]
    public class DataPoint
    {
        /* Stocke les données enregistrées à un moment donné */
        public float time;
        public int healthyCount;
        public int burningCount;
        public int ashCount;
        public int healthyGrassCount;
        public int hiddenGrassCount;
        public float precipitation;
        public float temperature;
        public int predatorCount; 
        public int preyCount;
        public int sickDeerCount;
        public int deadDeerCount;
        public int sickBearCount;   // Added for tracking sick bears
        public int deadBearCount;   // Added for tracking dead bears
    }

    public float temperatureScaleFactor = 10f;
    public float precipitationScaleFactor = 10f;
    public float loggingInterval = 1f;
    private float timer = 0f;
    public List<DataPoint> dataPoints = new List<DataPoint>();

    private bool showGraph = false; 
    private bool showAnimalGraph = false;

    private GUIStyle labelStyle;
    public int fixedYAxisInterval = 500;

    /* Initialisation de l'instance unique du logger */
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(this);
    }

    /* Initialise le logger avec la référence à ObjectStorage */
    public void InitializeLogger(ObjectStorage storage)
    {
        // Debug.Log("Logger initialized");
    }

    /* Met à jour le logger et gère l'affichage des graphes */
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= loggingInterval)
        {
            timer = 0f;
            // Enregistre un point de données
            LogDataPoint();
        }
        // Bascule l'affichage des graphes avec G et H
        if (Input.GetKeyDown(KeyCode.G))
            showGraph = !showGraph;

        if (Input.GetKeyDown(KeyCode.H))
            showAnimalGraph = !showAnimalGraph;
    }

    /* Enregistre un point de données concernant l'évolution du système */
    void LogDataPoint()
    {
        Vegetation[] vegetationObjects = Object.FindObjectsByType<Vegetation>(FindObjectsSortMode.None);
        int healthy = 0, burning = 0, ash = 0;
        foreach (Vegetation veg in vegetationObjects)
        {
            string state = veg.currentState;
            if (state == "Healthy")
                healthy++;
            else if (state == "Burning")
                burning++;
            else if (state == "Ash")
                ash++;
        }

        Grass[] grassObjects = Object.FindObjectsByType<Grass>(FindObjectsSortMode.None);
        int healthyGrass = 0;
        int hiddenGrass = 0;
        foreach (Grass gr in grassObjects)
        {
            if (gr.currentState == "Healthy")
                healthyGrass++;
            else if (gr.currentState == "Hidden")
                hiddenGrass++;
        }

        float precipitationValue = WeatherSystem.GetCurrentPrecipitation();
        float currentTemperature = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.GetGlobalTemperature() : 0f;

        // Bears are considered as predators in this simulation
        BearAI[] predatorObjects = Object.FindObjectsByType<BearAI>(FindObjectsSortMode.None);
        int predatorCount = predatorObjects.Length;
        int sickBearCount = predatorObjects.Count(bear => bear.IsSick);
        int deadBearCount = BearAI.DeadBearCount; // Assumes BearAI tracks dead bears similarly to DeerAI

        DeerAI[] preyObjects = Object.FindObjectsByType<DeerAI>(FindObjectsSortMode.None);
        int preyCount = preyObjects.Length;
        int sickDeerCount = preyObjects.Count(deer => deer.IsSick);
        int deadDeerCount = DeerAI.DeadDeerCount;

        // Crée et ajoute le point de données à la liste
        DataPoint dp = new DataPoint
        {
            time = Time.time,
            healthyCount = healthy,
            burningCount = burning,
            ashCount = ash,
            healthyGrassCount = healthyGrass,
            hiddenGrassCount = hiddenGrass,
            precipitation = precipitationValue,
            temperature = currentTemperature,
            predatorCount = predatorCount,
            preyCount = preyCount,
            sickDeerCount = sickDeerCount,
            deadDeerCount = deadDeerCount,
            sickBearCount = sickBearCount,
            deadBearCount = deadBearCount
        };
        dataPoints.Add(dp);
    }

    /* Affiche les graphiques de l'évolution du système et des animaux */
    void OnGUI()
    {
        // -----------------------------
        // Panneau transparent de température
        // -----------------------------
        GUIStyle temperatureStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        Color originalGUIColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.5f);

        Rect temperatureRect = new Rect(40, 60, 110, 50);
        string temperatureText = (TemperatureSystem.Instance != null)
            ? TemperatureSystem.Instance.GetGlobalTemperature().ToString("F1") + "°C"
            : "N/A";

        GUI.Box(temperatureRect, temperatureText, temperatureStyle);
        GUI.color = originalGUIColor;

        if (dataPoints.Count < 2)
            return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.black;
            labelStyle.fontSize = 18;
        }

        float startTime = dataPoints[0].time;
        float endTime = dataPoints[dataPoints.Count - 1].time;
        float timeSpan = endTime - startTime;
        if (timeSpan <= 0f) timeSpan = 1f;

        // -----------------------------
        // Graphique pour la végétation et l'environnement (touche G)
        // -----------------------------
        if (showGraph)
        {
            Rect graphRect = new Rect(300, 100, 750, 400);

            GUI.color = new Color(1, 1, 1, 0.5f);
            GUI.Box(graphRect, "");
            GUI.color = originalGUIColor;

            GUI.Label(new Rect(graphRect.x, graphRect.y - 45, graphRect.width, 30),
                "Evolution des Arbres et Herbe (avec précipitation et température)", labelStyle);

            int maxCount = dataPoints.Max(dp => Mathf.Max(
                dp.healthyCount,
                dp.healthyGrassCount,
                dp.hiddenGrassCount,
                dp.burningCount,
                dp.ashCount,
                Mathf.RoundToInt(dp.precipitation * precipitationScaleFactor),
                Mathf.RoundToInt(dp.temperature * temperatureScaleFactor)
            ));
            if (maxCount == 0) maxCount = 1;
            int adjustedMax = Mathf.CeilToInt(maxCount / (float)fixedYAxisInterval) * fixedYAxisInterval;
            if (adjustedMax < fixedYAxisInterval) adjustedMax = fixedYAxisInterval;

            float xScale = graphRect.width / timeSpan;
            float yScale = graphRect.height / adjustedMax;

            // Trace chaque série de données
            DrawStateGraph(graphRect, dp => dp.healthyCount, Color.green, startTime, xScale, yScale);
            DrawStateGraph(graphRect, dp => dp.healthyGrassCount, Color.cyan, startTime, xScale, yScale);
            DrawStateGraph(graphRect, dp => dp.hiddenGrassCount, new Color(1f, 0.5f, 0f), startTime, xScale, yScale);
            DrawStateGraph(graphRect, dp => dp.burningCount, Color.red, startTime, xScale, yScale);
            DrawStateGraph(graphRect, dp => dp.ashCount, Color.gray, startTime, xScale, yScale);
            DrawStateGraph(graphRect, dp => Mathf.RoundToInt(dp.precipitation * precipitationScaleFactor), Color.blue, startTime, xScale, yScale);
            DrawStateGraph(graphRect, dp => Mathf.RoundToInt(dp.temperature * temperatureScaleFactor), Color.magenta, startTime, xScale, yScale);

            // Traces et étiquettes de l'axe X
            int numXTicks = 5;
            for (int i = 0; i <= numXTicks; i++)
            {
                float t = startTime + (timeSpan * i / numXTicks);
                float xPos = graphRect.x + (graphRect.width * i / numXTicks);
                GUI.Label(new Rect(xPos - 20, graphRect.y + graphRect.height + 5, 40, 25), t.ToString("F1"), labelStyle);
            }

            // Étiquette de l'axe X
            GUI.Label(new Rect(
                graphRect.x + graphRect.width / 2 - 50,
                graphRect.y + graphRect.height + 35,
                100, 30),
                "Temps (s)",
                labelStyle);

            // Traces et étiquettes de l'axe Y
            int numYTicks = adjustedMax / fixedYAxisInterval;
            for (int j = 0; j <= numYTicks; j++)
            {
                int val = j * fixedYAxisInterval;
                float yPos = graphRect.y + graphRect.height - (val / (float)adjustedMax) * graphRect.height;
                GUI.Label(new Rect(graphRect.x - 45, yPos - 10, 40, 25), val.ToString(), labelStyle);
            }

            // Étiquette de l'axe Y (rotation pour affichage vertical)
            Matrix4x4 oldMatrix = GUI.matrix;
            Vector2 pivotPoint = new Vector2(graphRect.x - 55, graphRect.y + (graphRect.height / 2));
            GUIUtility.RotateAroundPivot(-90, pivotPoint);
            GUI.Label(new Rect(pivotPoint.x - 80, pivotPoint.y - 20, 200, 40), "Nb Arbres et Herbe", labelStyle);
            GUI.matrix = oldMatrix;

            // Légende
            float legendX = graphRect.x + graphRect.width + 25;
            float legendY = graphRect.y + 10;
            float lineHeight = 30;

            GUI.Label(new Rect(legendX, legendY + (lineHeight * 0), 250, 25), "Vert: Sain (Arbres)", labelStyle);
            GUI.Label(new Rect(legendX, legendY + (lineHeight * 1), 250, 25), "Cyan: Herbe Saine", labelStyle);
            GUI.Label(new Rect(legendX, legendY + (lineHeight * 2), 250, 25), "Orange: Herbe Mangée", labelStyle);
            GUI.Label(new Rect(legendX, legendY + (lineHeight * 3), 250, 25), "Rouge: En Feu", labelStyle);
            GUI.Label(new Rect(legendX, legendY + (lineHeight * 4), 250, 25), "Gris: Cendre", labelStyle);
            GUI.Label(new Rect(legendX, legendY + (lineHeight * 5), 250, 25), "Bleu: Précipitation", labelStyle);
            GUI.Label(new Rect(legendX, legendY + (lineHeight * 6), 250, 25), "Magenta: Température", labelStyle);
        }

        // -----------------------------
        // Graphique des animaux (touche H)
        // -----------------------------
        if (showAnimalGraph)
        {
            Rect animalGraphRect = new Rect(300, 750, 750, 400);

            GUI.color = new Color(1, 1, 1, 0.5f);
            GUI.Box(animalGraphRect, "");
            GUI.color = originalGUIColor;

            GUI.Label(new Rect(animalGraphRect.x, animalGraphRect.y - 40, animalGraphRect.width, 30),
                "Evolution des Animaux (Cerfs et Ours)", labelStyle);

            int maxAnimalCount = dataPoints.Max(dp =>
                Mathf.Max(
                    dp.preyCount,
                    Mathf.Max(dp.sickDeerCount,
                    Mathf.Max(dp.deadDeerCount,
                    Mathf.Max(dp.predatorCount,
                    Mathf.Max(dp.sickBearCount, dp.deadBearCount))))
                ));
            if (maxAnimalCount == 0) maxAnimalCount = 1;
            int adjustedMaxAnimal = Mathf.CeilToInt(maxAnimalCount / (float)fixedYAxisInterval) * fixedYAxisInterval;
            if (adjustedMaxAnimal < fixedYAxisInterval) adjustedMaxAnimal = fixedYAxisInterval;

            float xScaleAnimal = animalGraphRect.width / timeSpan;
            float yScaleAnimal = animalGraphRect.height / adjustedMaxAnimal;

            // Trace chaque série de données pour les animaux
            DrawStateGraph(animalGraphRect, dp => dp.preyCount, Color.yellow, startTime, xScaleAnimal, yScaleAnimal);
            DrawStateGraph(animalGraphRect, dp => dp.sickDeerCount, Color.red, startTime, xScaleAnimal, yScaleAnimal);
            DrawStateGraph(animalGraphRect, dp => dp.deadDeerCount, Color.black, startTime, xScaleAnimal, yScaleAnimal);
            Color bearColor = new Color(0.55f, 0.27f, 0.07f);  // Marron pour Ours
            DrawStateGraph(animalGraphRect, dp => dp.predatorCount, bearColor, startTime, xScaleAnimal, yScaleAnimal);
            // Added graph lines for sick and dead bears
            DrawStateGraph(animalGraphRect, dp => dp.sickBearCount, new Color(1f, 0.3f, 0.3f), startTime, xScaleAnimal, yScaleAnimal);
            DrawStateGraph(animalGraphRect, dp => dp.deadBearCount, new Color(0.3f, 0.3f, 0.3f), startTime, xScaleAnimal, yScaleAnimal);

            // Traces et étiquettes pour l'axe X
            int numXTicksAnimal = 5;
            for (int i = 0; i <= numXTicksAnimal; i++)
            {
                float t = startTime + (timeSpan * i / numXTicksAnimal);
                float xPos = animalGraphRect.x + animalGraphRect.width * i / numXTicksAnimal;
                GUI.Label(new Rect(xPos - 20, animalGraphRect.y + animalGraphRect.height + 5, 40, 25), t.ToString("F1"), labelStyle);
            }

            // Étiquette de l'axe X
            GUI.Label(new Rect(
                animalGraphRect.x + animalGraphRect.width / 2 - 40,
                animalGraphRect.y + animalGraphRect.height + 35,
                80, 25),
                "Temps (s)",
                labelStyle);

            // Traces et étiquettes pour l'axe Y
            int numYTicksAnimal = adjustedMaxAnimal / fixedYAxisInterval;
            for (int j = 0; j <= numYTicksAnimal; j++)
            {
                int val = j * fixedYAxisInterval;
                float yPos = animalGraphRect.y + animalGraphRect.height - (val / (float)adjustedMaxAnimal) * animalGraphRect.height;
                GUI.Label(new Rect(animalGraphRect.x - 45, yPos - 10, 40, 25), val.ToString(), labelStyle);
            }

            // Étiquette de l'axe Y (rotation)
            Matrix4x4 oldMatrixAnimal = GUI.matrix;
            Vector2 pivotPointAnimal = new Vector2(animalGraphRect.x - 55, animalGraphRect.y + animalGraphRect.height / 2);
            GUIUtility.RotateAroundPivot(-90, pivotPointAnimal);
            GUI.Label(new Rect(pivotPointAnimal.x - 80, pivotPointAnimal.y - 20, 200, 40), "Nb Animaux", labelStyle);
            GUI.matrix = oldMatrixAnimal;

            // Légende pour les animaux
            float legendX2 = animalGraphRect.x + animalGraphRect.width + 25;
            float legendY2 = animalGraphRect.y + 10;
            float lineHeight2 = 30;
            
            GUI.Label(new Rect(legendX2, legendY2 + lineHeight2 * 0, 200, 25), "Jaune: Cerfs Sain", labelStyle);
            GUI.Label(new Rect(legendX2, legendY2 + lineHeight2 * 1, 200, 25), "Rouge: Cerfs Malades", labelStyle);
            GUI.Label(new Rect(legendX2, legendY2 + lineHeight2 * 2, 200, 25), "Noir: Cerfs Morts", labelStyle);
            GUI.Label(new Rect(legendX2, legendY2 + lineHeight2 * 3, 200, 25), "Marron: Ours Sain", labelStyle);
            GUI.Label(new Rect(legendX2, legendY2 + lineHeight2 * 4, 250, 25), "Rose pâle: Ours Malades", labelStyle);
            GUI.Label(new Rect(legendX2, legendY2 + lineHeight2 * 5, 200, 25), "Gris foncé: Ours Morts", labelStyle);
        }
    }

    /* Trace une ligne de données sur le graphique */
    void DrawStateGraph(Rect graphRect, System.Func<DataPoint, int> getCount, Color color, float startTime, float xScale, float yScale)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();

        for (int i = 1; i < dataPoints.Count; i++)
        {
            DataPoint previous = dataPoints[i - 1];
            DataPoint current = dataPoints[i];
            Vector2 prevPoint = new Vector2(
                graphRect.x + (previous.time - startTime) * xScale,
                graphRect.y + graphRect.height - getCount(previous) * yScale);
            Vector2 currPoint = new Vector2(
                graphRect.x + (current.time - startTime) * xScale,
                graphRect.y + graphRect.height - getCount(current) * yScale);
            DrawLine(prevPoint, currPoint, tex, 2.0f);
        }
    }

    /* Trace une ligne entre deux points avec une largeur donnée */
    void DrawLine(Vector2 pointA, Vector2 pointB, Texture2D tex, float width)
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        float angle = Vector3.Angle(pointB - pointA, Vector2.right);
        if (pointA.y > pointB.y)
            angle = -angle;
        float length = (pointB - pointA).magnitude;
        GUIUtility.RotateAroundPivot(angle, pointA);
        GUI.DrawTexture(new Rect(pointA.x, pointA.y - (width / 2), length, width), tex);
        GUI.matrix = oldMatrix;
    }
}
