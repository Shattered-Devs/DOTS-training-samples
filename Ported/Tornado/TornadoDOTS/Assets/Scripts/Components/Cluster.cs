using Unity.Entities;
using Unity.Mathematics;

public struct Cluster : IComponentData
{
    public float3 Position;
    public int NumberOfSubClusters;
    public Entity BarPrefab;
    // Remove these probably:
    public int MinTowerHeight;
    public int MaxTowerHeight;
}

public struct GenerateCluster : IComponentData
{
}

public struct Joint : IBufferElementData
{
    public float3 Value;
    public float3 OldPos;
    public bool IsAnchored;
}

public struct Connection : IBufferElementData
{
    public int J1, J2;
    public float OriginalLength;
}

public struct Bar : IBufferElementData
{
    public Entity Value;

    public static implicit operator Entity(in Bar b) => b.Value;
    public static implicit operator Bar(in Entity e) => new Bar() {Value = e};
}

public struct BarVisualizer : IComponentData {}