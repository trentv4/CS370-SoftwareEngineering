namespace Project.Util {
	/// <summary> Provides minor useful math functions. </summary>
	public static class MathUtil {
		/// <summary> Ensure value is >= min, and <= max. </summary>
		public static int MinMax(int value, int min, int max) {
			if (value < min)
				return min;
			if (value > max)
				return max;

			return value;
		}

		/// <summary> Ensure value is >= min, and <= max. </summary>
		public static float MinMax(float value, float min, float max) {
			if (value < min)
				return min;
			if (value > max)
				return max;

			return value;
		}
	}
}