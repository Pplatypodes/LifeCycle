using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct TerrainType {
	public string name;
	public float height;
	public Color colour;
}

[System.Serializable]
public struct MapData {
	public readonly float[,] heightMap;
	public readonly Color[] colourMap;

	public MapData (float[,] heightMap, Color[] colourMap)
	{
		this.heightMap = heightMap;
		this.colourMap = colourMap;
	}
}
