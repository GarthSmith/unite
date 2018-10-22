using UnityEngine;

public class BlockClass
{
	public BlockClass(short blockType)
	{
		BlockType = blockType;
	}

	public Vector2Int Position { get; set; }
	public short BlockType { get; private set; }
}