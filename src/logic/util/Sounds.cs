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
		public static bool IsAudioEnabled = false;

		/// <summary>Initializes OpenAL context for audio playback.</summary>
		public static void Init() {
			try {
				//Initialize OpenAL
				var contextAttributes = new int[] { 0 };
				_device = ALC.OpenDevice(null);
				_context = ALC.CreateContext(_device, contextAttributes);
				ALC.MakeContextCurrent(_context);
				IsAudioEnabled = true;
			} catch (DllNotFoundException e) {
				Console.WriteLine("OpenAL.dll unable to be loaded. Disabling sounds.");
				Console.WriteLine(e.ToString());
			}
		}

		/// <summary>Cleanup OpenAL resources</summary>
		public static void Cleanup() {
			if (!IsAudioEnabled) return;
			if (_context != ALContext.Null) {
				ALC.MakeContextCurrent(ALContext.Null);
				ALC.DestroyContext(_context);
			}
			_context = ALContext.Null;

			if (_device != ALDevice.Null) {
				ALC.CloseDevice(_device);
			}
			_device = ALDevice.Null;
		}

		/// <summary>Play sound. Loads it into memory the first time it's played and keeps it cached for future playback.</summary>
		public static void PlaySound(string filePath) {
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

			//Todo: Use a different source each time a sound is played so one sound can be played several times at once instead of being restarted
			//		Can have a pool of OpenAL sources and cycle between them
			//Play the sound
			sound.Play();
		}
	}

	/// <summary>Instance of an audio file loaded into memory</summary>
	class SoundInstance {
		public readonly string Name;
		/// <summary>OpenAL buffer for audio data</summary>
		private int _buffer;
		private int _source;

		public SoundInstance(string name, byte[] data, ALFormat format, int frequency) {
			Name = name;

			//Create OpenAL buffer and copy sound data to it
			_buffer = AL.GenBuffer();
			AL.BufferData(_buffer, format, ref data[0], data.Length, frequency);

			//Create and config OpenAL source to play the sound from
			_source = AL.GenSource();
			AL.Source(_source, ALSourcei.Buffer, _buffer); //Set buffer to play from
			AL.Source(_source, ALSourceb.Looping, false); //Not looping
		}

		public SoundInstance() {
			AL.DeleteSource(_source);
			AL.DeleteBuffer(_buffer);
		}

		/// <summary>Play the sound</summary>
		public void Play() {
			AL.SourcePlay(_source);
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