using Unity.Entities;
using Unity.Mathematics;

public struct BlockStruct : IComponentData
{
	public short Type;
	public short GroupId;
	public int2 Position;
	public int FrameCountLastUpdated;
}