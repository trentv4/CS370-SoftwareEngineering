using OpenTK.Windowing.GraphicsLibraryFramework;
using Project.Render;

namespace Project.Util {
	///<summary>Utility class to track user input. Easier to access since it's static</summary>
	public static class Input {
		///<summary>The number of consecutive frames each key has been down.</summary>
		private static int[] _keyDownStates = new int[(int)Keys.LastKey];

		///<summary>
		///Returns true if the key was pressed this frame, but not last frame. For non-repeating input.
		///Use this instead of OpenTKs IsKeyPressed(), as the OpenTK version isn't working correctly. 
		///</summary>
		public static bool IsKeyPressed(Keys key) {
			int keyVal = (int)key;
			if (keyVal >= _keyDownStates.Length)
				return false;

			return _keyDownStates[keyVal] == 1 ? true : false;
		}

		///<summary>Returns true if the key is currently down</summary>
		public static bool IsKeyDown(Keys key) {
			var keyboardState = Renderer.INSTANCE.KeyboardState;
			return keyboardState.IsKeyDown(key);
		}

		///<summary>Returns true if the key isn't down</summary>
		public static bool IsKeyReleased(Keys key) {
			var keyboardState = Renderer.INSTANCE.KeyboardState;
			return keyboardState.IsKeyReleased(key);
		}

		///<summary>Updates key states</summary>
		public static void Update() {
			var keyboardState = Renderer.INSTANCE.KeyboardState;
			for (int i = 0; i < _keyDownStates.Length; i++)
				if (keyboardState.IsKeyDown((Keys)i))
					_keyDownStates[i]++;
				else
					_keyDownStates[i] = 0;
		}
	}
}