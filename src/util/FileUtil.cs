using System.IO;

namespace Project.Util {
	/// <summary> Provides miscellaneous file IO functions </summary>
	public static class FileUtil {
		/// <summary> Returns true if the file is being locked by another process </summary>
		public static bool IsFileLocked(string filePath) {
			try {
				using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None)) {
					stream.Close();
				}
			}
			catch (IOException ex) {
				return true;
			}

			return false;
		}

		/// <summary> Attempts to read a file to a string. If it fails, it returns false. If it succeeds it provides the string via an out argument </summary>
		public static bool TryReadFile(string filePath, out string contents) {
			contents = null;
			if (IsFileLocked(filePath))
				return false;

			try {
				//Open file
				FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
				StreamReader reader = new StreamReader(stream);

				//Lock file to prevent other processes from editing it, and read it
				stream.Lock(0, stream.Length);
				contents = reader.ReadToEnd();
				stream.Unlock(0, stream.Length);
				reader.Close();
			} catch (IOException ex) {
				return false;
			}

	 		return true;
		}
	}
}