using System.Collections.Generic;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class BlockManager
{
	public short GetBlockTypeAt(int2 position)
	{
		if (_blocksByPosition.TryGetValue(position, out var blockType))
			return blockType;
		return -1;
	}
		
	public BlockManager()
	{
		for (var x = 0; x < 100; x++)
		{
			for (var y = 0; y < 100; y++)
			{
				_blocksByPosition.Add(new int2(x, y), (short)Random.Range(0, 5));
			}
		}
	}

                                     
	private readonly Dictionary<int2, short> _blocksByPosition = new Dictionary<int2, short>();
}