using UnityEngine;

[System.Serializable]
public struct Region
{
    public string name;
    
    public Material roomWallMaterial;
    public Material roomFloorMaterial;
    public Material roomCeilingMaterial;
    
    public Material corridorWallMaterial;
    public Material corridorFloorMaterial;
    public Material corridorCeilingMaterial;

    public GameObject doorPrefab;
    public GameObject lightSconcePrefab;
    public GameObject lightCeilingPrefab;

    public float maxEntropy;
    public float minEntropy;
    
    public Region(float maxEntropy = 1.0f, float minEntropy = 0.0f)
    {
        this.name = "";
        this.roomWallMaterial = null;
        this.roomFloorMaterial = null;
        this.roomCeilingMaterial = null;
        this.corridorWallMaterial = null;
        this.corridorFloorMaterial = null;
        this.corridorCeilingMaterial = null;
        this.doorPrefab = null;
        this.lightSconcePrefab = null;
        this.lightCeilingPrefab = null;
        this.maxEntropy = maxEntropy;
        this.minEntropy = minEntropy;
    }
}


