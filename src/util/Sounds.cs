using System.Collections.Generic;
using OpenTK.Audio.OpenAL;
using System.IO;
using System;

namespace Project.Util {
	/// <summary>Loads and plays audio files</summary>
	public static class Sounds {
		//OpenAL state
		private static ALDevice _device = ALDevice.Null;
		private static ALContext _context = ALContext.Null;
		private static List<SoundInstance> _sounds = new List<SoundInstance>();
		/// <summary>Sources used to play sounds. If all of these are playing then no new sounds can be played.</summary>
		private static int[] _sources = new int[32];
		/// <summary>Tracks the last sound a source played</summary>
		private static Dictionary<int, string> _sourceSounds = new Dictionary<int, string>();
		public static bool IsAudioEnabled = false;

		/// <summary>Initializes OpenAL context for audio playback.</summary>
		public static void Init() {
			try {
				//Initialize OpenAL
				var contextAttributes = new int[] { 0 };
				_device = ALC.OpenDevice(null);
				_context = ALC.CreateContext(_device, contextAttributes);
				ALC.MakeContextCurrent(_context);

				//Initialize source pool
				AL.GenSources(_sources);
				foreach (int source in _sources)
					_sourceSounds[source] = null;

				IsAudioEnabled = true;
			} catch (DllNotFoundException e) {
				Console.WriteLine("OpenAL.dll unable to be loaded. Disabling sounds.");
				Console.WriteLine(e.ToString());
			}
		}

		/// <summary>Cleanup OpenAL resources</summary>
		public static void Cleanup() {
			if (!IsAudioEnabled) return;

			//Destroy sources
			for (int i = 0; i < _sources.Length; i++) {
				int source = _sources[i];
				AL.SourceStop(source);
				AL.DeleteSource(source);
			}

			//Destroy sound instances
			_sounds.Clear();

			//Destroy OpenAL context
			if (_context != ALContext.Null) {
				ALC.MakeContextCurrent(ALContext.Null);
				ALC.DestroyContext(_context);
			}
			_context = ALContext.Null;

			//Destroy OpenAL device
			if (_device != ALDevice.Null) {
				ALC.CloseDevice(_device);
			}
			_device = ALDevice.Null;
		}

		/// <summary>Play sound. Loads it into memory the first time it's played and keeps it cached for future playback.</summary>
		public static void PlaySound(string filePath, bool looping = false, float gain = 1.0f) {
			if (!IsAudioEnabled) return;

			//Find or load sound
			string name = Path.GetFileName(filePath);
			var sound = _sounds.Find(sound => sound.Name == name);
			if (sound == null) {
				bool result = SoundInstance.TryCreateFromFile(filePath, out sound);
				if (!result)
					return;

				_sounds.Add(sound);
			}

			//Find an unused source and play the sound from it
			bool foundSource = false;
			for (int i = 0; i < _sources.Length; i++) {
				int source = _sources[i];
				ALSourceState state = AL.GetSourceState(source);
				if(state == ALSourceState.Initial || state == ALSourceState.Stopped) { //Source that's never been used or not playing
					AL.Source(source, ALSourcef.Gain, gain);
					sound.Play(source, looping);

					_sourceSounds[source] = filePath; //Track which sound the source is playing
					foundSource = true;
					break;
				}
			}

			//Report error if no free source was found
			if(!foundSource)
				Console.WriteLine($"Failed to play sound \"{filePath}\". All OpenAL sources are in use.");
		}

		/// <summary>Stop every source that's playing a sound.</summary>
		public static void StopSound(string filePath) {
			//Stop all sources that are playing this sound
			foreach (var kv in _sourceSounds)
				if (kv.Value == filePath) {
					AL.SourceStop(kv.Key);
					_sourceSounds[kv.Key] = null; //Unmap sound from source
					break;
				}
		}

		/// <summary> Set loudness of an already playing sound. </summary>
		public static void SetSoundGain(string filePath, float gain = 1.0f) {
			foreach (var kv in _sourceSounds)
				if (kv.Value == filePath) {
					AL.Source(kv.Key, ALSourcef.Gain, gain);
					return;
				}
		}

		/// <summary> Returns true if any source is playing the sound </summary>
		public static bool IsSoundPlaying(string filePath) {
			foreach (var kv in _sourceSounds)
				if (kv.Value == filePath)
					return true;

			return false;
		}
	}

	/// <summary>Instance of an audio file loaded into memory</summary>
	class SoundInstance {
		public readonly string Name;
		/// <summary>OpenAL buffer for audio data</summary>
		private int _buffer;

		public SoundInstance(string name, byte[] data, ALFormat format, int frequency) {
			Name = name;

			//Create OpenAL buffer and copy sound data to it
			_buffer = AL.GenBuffer();
			AL.BufferData(_buffer, format, ref data[0], data.Length, frequency);
		}

		~SoundInstance() {
			AL.DeleteBuffer(_buffer);
		}

		/// <summary>Play the sound</summary>
		public void Play(int source, bool looping) {
			//Play buffered audio from source
			AL.Source(source, ALSourcei.Buffer, _buffer);
			AL.Source(source, ALSourceb.Looping, looping); //If true, the sound will be played repeatedly until stopped
			AL.SourcePlay(source);
		}

		/// <summary>Create a sound instance from a file. Supports .wav and .ogg</summary>
		public static bool TryCreateFromFile(string filePath, out SoundInstance sound) {
			//Ensure the sound file exists
			if (!File.Exists(filePath)) {
				Console.WriteLine($"Failed to load sound from \"{filePath}\". File doesn't exist.");
				sound = null;
				return false;
			}

			//Load the sound file if the format is supported
			string fileExt = Path.GetExtension(filePath);
			string fileName = Path.GetFileName(filePath);
			if (fileExt == ".wav") {
				BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open));

				//RIFF chunk
				string signature = new string(reader.ReadChars(4));
				if(signature != "RIFF") {
					Console.WriteLine($"Error reading \"{filePath}\". Invalid wav signature. Expected \"RIFF\", found \"{signature}\"");
					sound = null;
					return false;
				}
				int riffChunkSize = reader.ReadInt32();

				//WAVE tag
				string format = new string(reader.ReadChars(4));
				if(format != "WAVE") {
					Console.WriteLine($"Error reading \"{filePath}\". Invalid format specifier. Expected \"WAVE\", found \"{format}\"");
					sound = null;
					return false;
				}

				//Format chunk
				string fmtString = new string(reader.ReadChars(4));
				if(fmtString != "fmt ") {
					Console.WriteLine($"Error reading \"{filePath}\". Invalid chunk signature. Expected \"fmt \", found \"{fmtString}\"");
					sound = null;
					return false;
				}
				int formatChunkSize = reader.ReadInt32();
				int audioFormat = reader.ReadInt16();
				int numChannels = reader.ReadInt16();
				int sampleRate = reader.ReadInt32();
				int byteRate = reader.ReadInt32();
				int blockAlign = reader.ReadInt16();
				int bitsPerSample = reader.ReadInt16();

				//Try to locate the data chunk while skipping any optional chunks
				string dataSignature = null;
				while(reader.BaseStream.Position != reader.BaseStream.Length && dataSignature != "data")
					dataSignature = new string(reader.ReadChars(4));

				//Check if we found the data chunk
				if(dataSignature != "data") {
					Console.WriteLine($"Error reading \"{filePath}\". Failed to locate \"data\" chunk.");
					sound = null;
					return false;
				}

				//Read audio data
				int dataChunkSize = reader.ReadInt32();
				byte[] data = reader.ReadBytes(dataChunkSize);
				ALFormat alFormat = numChannels switch {
					1 => ALFormat.Mono16,
					2 => ALFormat.Stereo16,
					//Todo: Support other formats like float32
					_ => throw new NotSupportedException($"Wav files with ${numChannels} channels aren't supported!"),
				};

				//Create sound instance
				sound = new SoundInstance(fileName, data, alFormat, sampleRate);
				return true;
			} else {
				Console.WriteLine($"Failed to load sound from \"{filePath}\". Unsupported file extension.");
				sound = null;
				return false;
			}
		}
	}
}