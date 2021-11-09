using System.Collections.Generic;
using OpenTK.Mathematics;
using System;

namespace Project.Util {
    public static class Extensions {
        /// <summary>
        /// Override for List<T>.Contains() that uses a predicate instead of comparing each item with a value.
        /// Useful when checking if a list contains an object with specific member values.
        /// </summary>
        public static bool Contains<T>(this List<T> list, Predicate<T> match) {
            //List<T>.Find() returns null if the predicate doesn't match any value
            return list.Find(match) != null;
        }

		/// <summary>
        /// Returns a random float in range [0.0f, 1.0f) with optional arguments to define a custom range.
        /// </summary>
        public static float NextFloat(this Random random, float min = 0.0f, float max = 1.0f) {
			float value = (float)random.NextDouble();
			return (value * (max - min)) + min;
		}

        /// <summary>
        /// Returns a random Vector2. By default returns a normalized vector. Has optional arguments for custom min/max x and y values.
        /// </summary>
		public static Vector2 NextVec2(this Random random, float xMin = 0.0f, float xMax = 1.0f, float yMin = 0.0f, float yMax = 1.0f) {
			float x = random.NextFloat(xMin, xMax);
			float y = random.NextFloat(yMin, yMax);
			return new Vector2(x, y);
		}

		/// <summary> Distance between two points. </summary>
		public static float Distance(this Vector2 a, Vector2 b) {
			return (b - a).Length;
		}
    }
}