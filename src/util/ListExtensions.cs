using System.Collections.Generic;
using System;

namespace Project.Util {
    public static class ListExtensions {
        /// <summary>
        /// Override for List<T>.Contains() that uses a predicate instead of comparing each item with a value.
        /// Useful when checking if a list contains an object with specific member values.
        /// </summary>
        public static bool Contains<T>(this List<T> list, Predicate<T> match) {
            //List<T>.Find() returns null if the predicate doesn't match any value
            return list.Find(match) != null;
        }
    }
}