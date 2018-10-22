using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

[TestFixture]
public class IncrementTests
{
	[Test]
	public void ReferenceVsComponentIncrementSpeedTest()
	{
		// Arrange
		const int totalTestLoops = 100;
		const int elementQuantity = 1000000;
		var originals = new float[elementQuantity];
		var componentArray = new FloatComponent[elementQuantity];
		var referenceHashSet = new HashSet<ClassThatContainsBoxedFloat>();

		for (var index = 0; index < elementQuantity; index++)
		{
			var value = Random.Range(-100f, 100f);
			originals[index] = value;
			componentArray[index].Value = value;
			while (Random.value < 0.5f) new ClassThatContainsBoxedFloat(Random.value);
			referenceHashSet.Add(new ClassThatContainsBoxedFloat(value));
		}

		// Act
		var structArrayWatch = Stopwatch.StartNew();
		for (var loopCount = 0; loopCount < totalTestLoops; loopCount++)
		{
			for (var i = 0; i < componentArray.Length; i++)
				componentArray[i].Value++;
		}

		structArrayWatch.Stop();

		var referenceHashSetWatch = Stopwatch.StartNew();
		for (var loopCount = 0; loopCount < totalTestLoops; loopCount++)
		{
			foreach (var ClassThatContainsBoxedFloat in referenceHashSet)
				ClassThatContainsBoxedFloat.Value++;
		}

		referenceHashSetWatch.Stop();

		// Assert
		Debug.Log("Array of structs took on average " + (structArrayWatch.ElapsedMilliseconds / (float) totalTestLoops) +
		          " milliseconds.");
		Debug.Log("HashSet of references took on average " +
		          (referenceHashSetWatch.ElapsedMilliseconds / (float) totalTestLoops) + " milliseconds.");
		Assert.True(structArrayWatch.ElapsedMilliseconds < referenceHashSetWatch.ElapsedMilliseconds);
	}
	
	[Test]
	public void IncrementTestWithJobs()
	{
		// Arrange
		const int totalTestLoops = 100;
		const int elementQuantity = 1000000;
		var originals = new float[elementQuantity];
		var componentArray = new FloatComponent[elementQuantity];
		var nativeComponentArray = new NativeArray<FloatComponent>(elementQuantity, Allocator.Temp);
		var nativeComponentArrayForBurst = new NativeArray<FloatComponent>(elementQuantity, Allocator.Temp);
		var referenceHashSet = new HashSet<ClassThatContainsBoxedFloat>();

		for (var index = 0; index < elementQuantity; index++)
		{
			var value = Random.Range(-100f, 100f);
			originals[index] = value;
			componentArray[index].Value = value;
			var floatComponent = new FloatComponent {Value = value};
			nativeComponentArray[index] = floatComponent;
			nativeComponentArrayForBurst[index] = floatComponent;
			while (Random.value < 0.5f) new ClassThatContainsBoxedFloat(Random.value);
			referenceHashSet.Add(new ClassThatContainsBoxedFloat(value));
		}

		// Act
		
		// ComponentArray
		var structArrayWatch = Stopwatch.StartNew();
		for (var loopCount = 0; loopCount < totalTestLoops; loopCount++)
		{
			for (var i = 0; i < componentArray.Length; i++)
				componentArray[i].Value++;
		}
		structArrayWatch.Stop();

		// Reference HashSet
		var referenceHashSetWatch = Stopwatch.StartNew();
		for (var loopCount = 0; loopCount < totalTestLoops; loopCount++)
		{
			foreach (var ClassThatContainsBoxedFloat in referenceHashSet)
				ClassThatContainsBoxedFloat.Value++;
		}
		referenceHashSetWatch.Stop();

		// ParallelFor
		var jobWatch = Stopwatch.StartNew();
		var job = new NonBurstParallelIncrementJob
		{
			FloatComponents = nativeComponentArray,
		};
		var jobHandle = job.Schedule(elementQuantity, 128);
		jobHandle.Complete();
		jobWatch.Stop();
		
		// ParallelFor Job with Burst
		var burstWatch = Stopwatch.StartNew();
        var burst = new BurstParallelIncrementJob
        {
	        FloatComponents = nativeComponentArrayForBurst,
        };
		var burstJobHandle = burst.Schedule(elementQuantity, 128);
		burstJobHandle.Complete();
		burstWatch.Stop();
		
		// Assert
		Debug.Log("Array of structs took on average " + (structArrayWatch.ElapsedMilliseconds / (float) totalTestLoops) +
		          " milliseconds.");
		Debug.Log("HashSet of references took on average " +
		          (referenceHashSetWatch.ElapsedMilliseconds / (float) totalTestLoops) + " milliseconds.");
		Debug.Log("JobParallelFor took on average " +
		          (jobWatch.ElapsedMilliseconds / (float) totalTestLoops) + " milliseconds.");
		Debug.Log("JobParallelFor with Burst took on average " +
		          (burstWatch.ElapsedMilliseconds / (float) totalTestLoops) + " milliseconds.");
		Assert.True(burstWatch.ElapsedMilliseconds < referenceHashSetWatch.ElapsedMilliseconds);
		
		// Cleanup
		nativeComponentArray.Dispose();
		nativeComponentArrayForBurst.Dispose();
	}
	
	[Test]
	public void IncrementTestBurstVsNonBurst()
	{
		// Arrange
		const int totalTestLoops = 10000;
		const int elementQuantity = 100000000;
		var originals = new float[elementQuantity];
		var nativeComponentArray = new NativeArray<FloatComponent>(elementQuantity, Allocator.Temp);
		var nativeComponentArrayForBurst = new NativeArray<FloatComponent>(elementQuantity, Allocator.Temp);

		for (var index = 0; index < elementQuantity; index++)
		{
			var value = Random.Range(-100f, 100f);
			originals[index] = value;
			var floatComponent = new FloatComponent {Value = value};
			nativeComponentArray[index] = floatComponent;
			nativeComponentArrayForBurst[index] = floatComponent;
		}

		// Act
		
		// ParallelFor
		var jobWatch = Stopwatch.StartNew();
		var job = new NonBurstParallelIncrementJob
		{
			FloatComponents = nativeComponentArray,
		};
		var jobHandle = job.Schedule(elementQuantity, 128);
		jobHandle.Complete();
		jobWatch.Stop();
		
		// ParallelFor Job with Burst
		var burstWatch = Stopwatch.StartNew();
        var burst = new BurstParallelIncrementJob
        {
	        FloatComponents = nativeComponentArrayForBurst,
        };
		var burstJobHandle = burst.Schedule(elementQuantity, 128);
		burstJobHandle.Complete();
		burstWatch.Stop();
		
		// Assert
		Debug.Log("JobParallelFor took on average " +
		          (jobWatch.ElapsedMilliseconds / (float) totalTestLoops) + " milliseconds.");
		Debug.Log("JobParallelFor with Burst took on average " +
		          (burstWatch.ElapsedMilliseconds / (float) totalTestLoops) + " milliseconds.");
		Assert.True(burstWatch.ElapsedMilliseconds < jobWatch.ElapsedMilliseconds);
		
		// Cleanup
		nativeComponentArray.Dispose();
		nativeComponentArrayForBurst.Dispose();
	}

	[BurstCompile]
	public struct BurstParallelIncrementJob : IJobParallelFor
	{
		public NativeArray<FloatComponent> FloatComponents;
		
		public void Execute(int index)
		{
			var floatComponent = FloatComponents[index];
			floatComponent.Value++;
			FloatComponents[index] = floatComponent;
		}
	}
	
	public struct NonBurstParallelIncrementJob : IJobParallelFor
	{
		public NativeArray<FloatComponent> FloatComponents;
		
		public void Execute(int index)
		{
			var floatComponent = FloatComponents[index];
			floatComponent.Value++;
			FloatComponents[index] = floatComponent;
		}
	}
}

