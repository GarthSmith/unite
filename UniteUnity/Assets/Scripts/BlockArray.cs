using Unity.Mathematics;
using Random = UnityEngine.Random;

public struct BlockArray
{
	public BlockStruct[] Values;

	public static BlockArray CreateTestArray()
	{
		var blockArray = new BlockArray();
		blockArray.Values = new BlockStruct[100 * 100];
		var index = 0;
		for (var x = 0; x < 100; x++)
		{
			for (var y = 0; y < 100; y++)
			{
				var blockStruct = new BlockStruct
				{
					Type = (short)Random.Range(0, 5),
					GroupId = 0,
					Position = new int2(x, y),
					FrameCountLastUpdated = 0,
				};
				blockArray.Values[index++] = blockStruct;
			}
		}

		return blockArray;
	}
}