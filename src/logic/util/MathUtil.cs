using System;

namespace Project.Util {
    public static class MathUtil {
        ///<summary>Ensure value is >= min and <= max.</summary>
        public static int MinMax(int value, int min, int max) {
            if (value < min)
                return min;
            if (value > max)
                return max;

            return value;
        }
    }
}