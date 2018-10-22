using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

[TestFixture]
public class ConnectedBlockPerformanceTests
{
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

	public class Board
	{
		public Board()
		{
			for (var x = 0; x < 100; x++)
			{
				for (var y = 0; y < 100; y++)
				{
					_positions.Add(new int2(x, y));
				}
			}
		}

		public List<int2> GetAllPositions()
		{
			return new List<int2>(_positions);
		}

		private readonly List<int2> _positions = new List<int2>();
	}

	[Test]
	public void ConnectedBlocksSpeedTest()
	{
		// Arrange
		const int totalLoops = 1000;
		
		// Act
		var arrayMilliseconds = FindConnectedBlocksArray(totalLoops);

		var refMilliseconds = FindConnectedBlocksRef(totalLoops);
		
		Debug.Log($"{nameof(totalLoops)}: {totalLoops}. {nameof(arrayMilliseconds)}: {arrayMilliseconds}. {nameof(refMilliseconds)}: {refMilliseconds}.");
		Assert.IsTrue(arrayMilliseconds < refMilliseconds);
	}

	private long FindConnectedBlocksRef(int loopCount)
	{
		var board = new Board();
		var blockManager = new BlockManager();
		
		var visited = new HashSet<int2>();
		var frontier = new Stack<int2>();
		var connectedGroupsOfPositions = new List<List<int2>>();
		var debugLoops = 0;
		var positionsAlreadyInGroup = new HashSet<int2>();

		var watch = Stopwatch.StartNew();
		for (var loopIndex = 0; loopIndex < loopCount; loopIndex++)
		{
			connectedGroupsOfPositions.Clear();
			var connected = new List<int2>();
			positionsAlreadyInGroup.Clear();
			
			foreach (var position in board.GetAllPositions())
			{
				// if (visited.Contains(position)) continue;
				var alreadyInGroup = false;
				if (positionsAlreadyInGroup.Contains(position)) continue;
				frontier.Push(position);
				while (frontier.Count > 0)
				{
					debugLoops++;
					var currentPosition = frontier.Pop();
					connected.Add(currentPosition);
					positionsAlreadyInGroup.Add(currentPosition);
					foreach (var neighbor in GetNeighbors(currentPosition))
					{
						if (!visited.Contains(neighbor) &&
						    blockManager.GetBlockTypeAt(neighbor) == blockManager.GetBlockTypeAt(currentPosition))
							frontier.Push(neighbor);
						visited.Add(neighbor);
					}
				}

				connectedGroupsOfPositions.Add(connected);
				connected.Clear();
				visited.Clear();
			}
		}
		watch.Stop();
		Debug.Log($"{nameof(debugLoops)}: {debugLoops} for {nameof(FindConnectedBlocksRef)}");
		return watch.ElapsedMilliseconds;
	}

	public struct BlockGroupInfo
	{
		public short visitedIndex;
		public short groupId;
	}
	
	private long FindConnectedBlocksArray(int loopCount)
	{
		// Arrange
		var blocks = BlockArray.CreateTestArray();
		var frontier = new Stack<int2>();
		var groupId = (short) 1;
		var blockByPosition = new BlockStruct[100, 100];
		var visitedByPosition = new short[100, 100];
		var frameCount = Random.Range(10, 100);
		var arrayWatch = Stopwatch.StartNew();
		var debugLoops = 0;
		for (var loopIndex = 0; loopIndex < loopCount; loopIndex++)
		{
			frameCount++;
			
			for (var i = 0; i < blocks.Values.Length; i++)
			{
				var blockPosition = blocks.Values[i].Position;
				blockByPosition[blockPosition.x, blockPosition.y] = blocks.Values[i];
			}

			
			for (var x = 0; x < 100; x++)
			{
				for (var y = 0; y < 100; y++)
				{
					if (blockByPosition[x, y].FrameCountLastUpdated >= frameCount)
						continue; // Already updated group id.
					groupId++;
					frontier.Push(new int2(x, y));
					while (frontier.Count > 0)
					{
						debugLoops++;
						var currentPosition = frontier.Pop();
						blockByPosition[currentPosition.x, currentPosition.y].GroupId = groupId;
						blockByPosition[x, y].FrameCountLastUpdated = frameCount;

						// up
						var neighborPosition = currentPosition + new int2(0, 1);
						if (ShouldCheck(neighborPosition, visitedByPosition, groupId, blockByPosition, frameCount))// && visitedByPosition[neighborPosition.x, neighborPosition.y] != groupId)
						{
							if (blockByPosition[neighborPosition.x, neighborPosition.y].Type ==
							    blockByPosition[currentPosition.x, currentPosition.y].Type)
								frontier.Push(neighborPosition);
							visitedByPosition[neighborPosition.x, neighborPosition.y] = groupId;
						}

						// down
						neighborPosition = currentPosition + new int2(0, -1);
						if (ShouldCheck(neighborPosition, visitedByPosition, groupId, blockByPosition, frameCount))// && visitedByPosition[neighborPosition.x, neighborPosition.y] != groupId)
						{
							if (blockByPosition[neighborPosition.x, neighborPosition.y].Type ==
							    blockByPosition[currentPosition.x, currentPosition.y].Type)
								frontier.Push(neighborPosition);
							visitedByPosition[neighborPosition.x, neighborPosition.y] = groupId;
						}

						// left
						neighborPosition = currentPosition + new int2(-1, 0);
						if (ShouldCheck(neighborPosition, visitedByPosition, groupId, blockByPosition, frameCount))// && visitedByPosition[neighborPosition.x, neighborPosition.y] != groupId)
						{
							if (blockByPosition[neighborPosition.x, neighborPosition.y].Type ==
							    blockByPosition[currentPosition.x, currentPosition.y].Type)
								frontier.Push(neighborPosition);
							visitedByPosition[neighborPosition.x, neighborPosition.y] = groupId;
						}

						// right
						neighborPosition = currentPosition + new int2(1, 0);
						if (ShouldCheck(neighborPosition, visitedByPosition, groupId, blockByPosition, frameCount))// && visitedByPosition[neighborPosition.x, neighborPosition.y] != groupId)
						{
							if (blockByPosition[neighborPosition.x, neighborPosition.y].Type ==
							    blockByPosition[currentPosition.x, currentPosition.y].Type)
								frontier.Push(neighborPosition);
							visitedByPosition[neighborPosition.x, neighborPosition.y] = groupId;
						}
					}
				}
			}

			for (var i = 0; i < blocks.Values.Length; i++)
			{
				var position = blocks.Values[i].Position;
				blocks.Values[i] = blockByPosition[position.x, position.y];
			}
		}
		arrayWatch.Stop();
		
		Debug.Log($"{nameof(debugLoops)}: {debugLoops}");
		return arrayWatch.ElapsedMilliseconds;
	}

	private bool ShouldCheck(int2 neighborPosition, short[,] visitedByPosition, short groupId, BlockStruct[,] blockByPosition, int frameCount)
	{
		if (!IsValid(neighborPosition)) return false;
		if (blockByPosition[neighborPosition.x, neighborPosition.y].FrameCountLastUpdated >= frameCount) return false;
		return visitedByPosition[neighborPosition.x, neighborPosition.y] != groupId;
	}

	private bool IsValid(int2 position)
	{
		return position.x >= 0 && position.x < 100 && position.y >= 0 && position.y < 100;
	}

	[Test]
	public void ContainsTest()
	{
		// Arrange
		const int totalLoops = 1000;
		const int arrayLength = 1000000;
		var originalFloats = new float[arrayLength];
		var originalReferences = new List<ClassThatContainsBoxedFloat>();
		var componentArray = new FloatComponent[arrayLength];
		var referenceHashSet = new HashSet<ClassThatContainsBoxedFloat>();

		for (var index = 0; index < arrayLength; index++)
		{
			var value = Random.Range(-100f, 100f);
			originalFloats[index] = value;
			componentArray[index].Value = value;
			var reference = new ClassThatContainsBoxedFloat(value);
			originalReferences.Add(reference);
			referenceHashSet.Add(reference);
		}

		// Act
		var structArrayWatch = Stopwatch.StartNew();
		for (var loopCount = 0; loopCount < totalLoops; loopCount++)
		{
			var searchValue = originalFloats[Random.Range(0, originalFloats.Length)];
			var foundIt = false;
			for (var i = 0; i < componentArray.Length; i++)
			{
				if (!componentArray[i].Value.Equals(searchValue)) continue;
				foundIt = true;
				break;
			}

			if (!foundIt) Assert.Fail();
		}

		structArrayWatch.Stop();

		var referenceHashSetWatch = Stopwatch.StartNew();
		for (var loopCount = 0; loopCount < totalLoops; loopCount++)
		{
			var searchReference = originalReferences[Random.Range(0, originalReferences.Count)];
			var found = referenceHashSet.Contains(searchReference);
			if (!found) Assert.Fail();
		}

		referenceHashSetWatch.Stop();

		// Assert
		Debug.Log("Array of structs took total " + structArrayWatch.ElapsedMilliseconds +
		          " milliseconds to run 1000 times.");
		Debug.Log("HashSet of references took total " + referenceHashSetWatch.ElapsedMilliseconds +
		          " milliseconds to run 1000 times.");
		Assert.True(structArrayWatch.ElapsedMilliseconds < referenceHashSetWatch.ElapsedMilliseconds);
	}

	[Test]
	public void ConnectedGroupsAlgorithmWithDictionaries()
	{
		// Arrange
		const int loops = 1000;
		var connectedGroupCount = 0;

		// Act
		var watch = Stopwatch.StartNew();
		for (var loopCount = 0; loopCount < loops; loopCount++)
		{
			var blocksByPosition = GenerateBlocksByPosition();
			var connectedGroupsOfPositions = new List<List<int2>>();
			var visited = new HashSet<int2>();
			var alreadyAssignedGroup = new HashSet<int2>();
			var frontier = new Stack<int2>();
			foreach (var position in blocksByPosition.Keys)
			{
				if (alreadyAssignedGroup.Contains(position)) continue;
				var connected = new List<int2>();
				frontier.Push(position);
				while (frontier.Count > 0)
				{
					var currentPosition = frontier.Pop();
					connected.Add(currentPosition);
					alreadyAssignedGroup.Add(position);
					foreach (var neighbor in GetNeighbors(currentPosition))
					{
						if (!visited.Contains(neighbor))
						{
							Block neighborBlock, currentBlock;
							if (blocksByPosition.TryGetValue(neighbor, out neighborBlock) &&
							    blocksByPosition.TryGetValue(currentPosition, out currentBlock) &&
							    neighborBlock.BlockType == currentBlock.BlockType)
								frontier.Push(neighbor);
						}

						visited.Add(neighbor);
					}
				}

				connectedGroupsOfPositions.Add(connected);
				visited.Clear();
			}

			connectedGroupCount += connectedGroupsOfPositions.Count;
		}

		watch.Stop();

		// Assert
		Assert.Pass("Finished! Found " + connectedGroupCount + " connected groups over " + loops + " loops in " +
		            watch.ElapsedMilliseconds + " milliseconds.");
	}

	private static IEnumerable<int2> GetNeighbors(int2 center)
	{
		return new[]
		{
			center + new int2(1, 0),
			center + new int2(-1, 0),
			center + new int2(0, 1),
			center + new int2(0, -1),
		};
	}

	private static Dictionary<int2, Block> GenerateBlocksByPosition()
	{
		var blocksByPosition = new Dictionary<int2, Block>();
		for (var x = 0; x < 10; x++)
		{
			for (var y = 0; y < 10; y++)
			{
				var position = new int2(x, y);
				var blockType = (short) Random.Range(0, 4);
				blocksByPosition[position] = new Block(blockType);
			}
		}

		return blocksByPosition;
	}

	/*
	[Test]
	public void ConnectedGroupsAlgorithmDirectArrayConversion()
	{
		// Arrange
		const int loops = 1000;
		var connectedGroupCount = 0;

		// Act
		var watch = Stopwatch.StartNew();
		for (var loopCount = 0; loopCount < loops; loopCount++)
		{
			var blocksByPosition = GenerateBlockArray();
			var connectedGroupsOfPositions = new int2[][];
			var visited = new HashSet<int2>();
			var frontier = new Stack<int2>();
			foreach (var position in blocksByPosition.Keys)
			{
				if (visited.Contains(position)) continue;
				var connected = new List<int2>();
				frontier.Push(position);
				while (frontier.Count > 0)
				{
					var currentPosition = frontier.Pop();
					connected.Add(currentPosition);
					foreach (var neighbor in GetNeighbors(currentPosition))
					{
						if (!visited.Contains(neighbor))
						{
							Block neighborBlock, currentBlock;
							if (blocksByPosition.TryGetValue(neighbor, out neighborBlock) &&
							    blocksByPosition.TryGetValue(currentPosition, out currentBlock) &&
							    neighborBlock.BlockType == currentBlock.BlockType)
								frontier.Push(neighbor);
						}

						visited.Add(neighbor);
					}
				}

				connectedGroupsOfPositions.Add(connected);
			}

			connectedGroupCount += connectedGroupsOfPositions.Count;
		}

		watch.Stop();
		
		// Assert
		Assert.Pass("Finished! Found " + connectedGroupCount + " connected groups over " + loops + " loops in " + watch.ElapsedMilliseconds + " milliseconds.");
	}
	/*
	
	/*	
	[Test]
	public void ConnectedGroupsAlgorithmECSFriendlyArrays()
	{
		// Arrange
		// Act
		// Assert
	}
	*/
}

public struct PositionArray
{
	public int2[] Values;
}

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

public struct BlockStruct
{
	public short Type;
	public short GroupId;
	public int2 Position;
	public int FrameCountLastUpdated;
}

public class Block
{
	public Block(short blockType)
	{
		BlockType = blockType;
	}

	public Vector2Int Position { get; set; }
	public short BlockType { get; private set; }
}