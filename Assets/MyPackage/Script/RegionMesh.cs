using UnityEngine;

public class RegionMesh : MonoBehaviour
{
    [SerializeField] MeshLayer meshLayerPrefab;
    private MeshLayer roomFloorLayer;
    private MeshLayer roomCeilingLayer;
    private MeshLayer roomWallLayer;
    
    private MeshLayer corridorFloorLayer;
    private MeshLayer corridorCeilingLayer;
    private MeshLayer corridorWallLayer;

    public void Initialize(Region region)
    {
        if (region.name != "")
            gameObject.name = region.name;
        
        roomFloorLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        roomFloorLayer.gameObject.name = "Room Floor";
        roomFloorLayer.SetMaterial(region.roomFloorMaterial);
        roomCeilingLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        roomCeilingLayer.gameObject.name = "Room Ceiling";
        roomCeilingLayer.SetMaterial(region.roomCeilingMaterial);
        roomWallLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        roomWallLayer.gameObject.name = "Room Wall";
        roomWallLayer.SetMaterial(region.roomWallMaterial);
        
        corridorFloorLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        corridorFloorLayer.gameObject.name = "Corridor Floor";
        corridorFloorLayer.SetMaterial(region.corridorFloorMaterial);
        corridorCeilingLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        corridorCeilingLayer.gameObject.name = "Corridor Ceiling";
        corridorCeilingLayer.SetMaterial(region.corridorCeilingMaterial);
        corridorWallLayer = Instantiate(meshLayerPrefab).GetComponent<MeshLayer>();
        corridorWallLayer.gameObject.name = "Corridor Wall";
        corridorWallLayer.SetMaterial(region.corridorWallMaterial);
    }

    public void AddRoomFloorGeometry(Vector3Int pos, Vector3Int size, Vector2 uvScale, Vector3 worldScale)
    {
        roomFloorLayer.AddFloorGeometryToMesh(pos, size, uvScale, worldScale);
    }

    public void AddRoomCeilingGeometry(Vector3Int pos, Vector3Int size, Vector2 uvScale, Vector3 worldScale)
    {
        roomCeilingLayer.AddCeilingGeometryToMesh(pos, size, uvScale, worldScale);
    }
    
    public void AddRoomWallGeometry(Vector3Int pos, Vector3Int size, Vector2 uvScale, Vector3 worldScale, bool isVertical, bool flip)
    {
        roomWallLayer.AddWallGeometryToMesh(pos, size, uvScale, worldScale, isVertical, flip);
    }
    
    public void AddCorridorFloorGeometry(Vector3Int pos, Vector3Int size, Vector2 uvScale, Vector3 worldScale)
    {
        corridorFloorLayer.AddFloorGeometryToMesh(pos, size, uvScale, worldScale);
    }

    public void AddCorridorCeilingGeometry(Vector3Int pos, Vector3Int size, Vector2 uvScale, Vector3 worldScale)
    {
        corridorCeilingLayer.AddCeilingGeometryToMesh(pos, size, uvScale, worldScale);
    }
    
    public void AddCorridorWallGeometry(Vector3Int pos, Vector3Int size, Vector2 uvScale, Vector3 worldScale, bool isVertical, bool flip)
    {
        corridorWallLayer.AddWallGeometryToMesh(pos, size, uvScale, worldScale, isVertical, flip);
    }

    public void UpdateMeshes()
    {
        roomFloorLayer.UpdateMesh();
        roomCeilingLayer.UpdateMesh();
        roomWallLayer.UpdateMesh();
        
        corridorFloorLayer.UpdateMesh();
        corridorCeilingLayer.UpdateMesh();
        corridorWallLayer.UpdateMesh();
    }
}
