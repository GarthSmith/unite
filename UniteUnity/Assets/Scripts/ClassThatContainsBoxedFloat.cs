public class ClassThatContainsBoxedFloat
{
	public ClassThatContainsBoxedFloat(float value)
	{
		Value = value;
	}

	public float Value
	{
		get => (float) _boxed;
		set => _boxed = value;
	}

	private object _boxed;
}