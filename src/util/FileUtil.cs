using System.IO;

namespace Project.Util {
	/// <summary> Provides miscellaneous file IO functions </summary>
	public static class FileUtil {
		/// <summary> Attempts to read a file to a string. If it fails, it returns false. If it succeeds it provides the string via an out argument </summary>
		public static bool TryReadFile(string filePath, out string contents) {
			contents = null;
			try {
				//Open file
				FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Write);
				StreamReader reader = new StreamReader(stream);

				//Lock file to prevent other processes from editing it, and read it
				stream.Lock(0, stream.Length);
				contents = reader.ReadToEnd();
				stream.Unlock(0, stream.Length);
				reader.Close();
			} catch (IOException) {
				return false;
			}
	 		return true;
		}
	}
}