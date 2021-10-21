using System;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Project.Render {
	/// <summary> Wrapper class for the concept of an OpenGL program. Internally, this
	/// will handle setting vertex attribs and program-wide uniforms. </summary>
	public class ShaderProgram {
		public int ShaderProgram_ID { get; private set; } = -1;
		public readonly int VertexArrayObject_ID;
		private readonly string _shaderPath0 = null;
		private readonly string _shaderPath1 = null;
		private DateTime _lastWriteTime0;
		private DateTime _lastWriteTime1;

		/// <summary> Loads a vertex and a fragment shader from disk, compiles them, links, and creates a ShaderProgram. </summary>
		public ShaderProgram(string vertexShaderPath, string fragmentShaderPath) {
			_shaderPath0 = vertexShaderPath;
			_shaderPath1 = fragmentShaderPath;
			_lastWriteTime0 = File.GetLastWriteTime(vertexShaderPath);
			_lastWriteTime1 = File.GetLastWriteTime(fragmentShaderPath);

			LoadShaders(new StreamReader(vertexShaderPath).ReadToEnd(), new StreamReader(fragmentShaderPath).ReadToEnd());
			VertexArrayObject_ID = GL.GenVertexArray();
		}

		/// <summary> Loads a unified shader (vertex + fragment shaders in same file split by a split keyword), compiles them,
		/// links, and creates the ShaderProgram. </summary>
		public ShaderProgram(string unifiedPath) {
			_shaderPath0 = unifiedPath;
			_lastWriteTime0 = File.GetLastWriteTime(unifiedPath);

			string unified = new StreamReader(unifiedPath).ReadToEnd();
			string[] sources = unified.Split("<split>");
			Debug.Assert(sources.Length == 2, $"Shader at {unifiedPath} is not a unified glsl file.");
			LoadShaders(sources[0], sources[1]);
			VertexArrayObject_ID = GL.GenVertexArray();
		}

		/// <summary> Create vertex and fragment shaders from strings </summary>
		private void LoadShaders(string vertexShader, string fragmentShader) {
			int vertexShaderID = CreateShaderFromSource(vertexShader, ShaderType.VertexShader);
			int fragmentShaderID = CreateShaderFromSource(fragmentShader, ShaderType.FragmentShader);
			Debug.Assert(vertexShaderID != 0 && fragmentShaderID != 0, "Failure during shader loading with creating vert/frag shaders");
			int newShaderProgram_ID = GL.CreateProgram();
			GL.AttachShader(newShaderProgram_ID, vertexShaderID);
			GL.AttachShader(newShaderProgram_ID, fragmentShaderID);
			GL.LinkProgram(newShaderProgram_ID);
			if (GL.GetProgramInfoLog(newShaderProgram_ID) != System.String.Empty)
			{
				//Log error
				Console.WriteLine($"Error while linking shaders: {GL.GetProgramInfoLog(newShaderProgram_ID)}");

				//If it fails and a valid program isn't already loaded, throw
				if (ShaderProgram_ID == -1)
					throw new Exception($"\tError in shader program linkage: \n{GL.GetProgramInfoLog(newShaderProgram_ID)}");

				//Otherwise just keep using the existing program and delete the new shaders
				GL.DeleteShader(vertexShaderID);
				GL.DeleteShader(fragmentShaderID);
				return;
			}
			GL.UseProgram(newShaderProgram_ID);
			GL.DeleteShader(vertexShaderID);
			GL.DeleteShader(fragmentShaderID);

			//Successfully loaded. Update shader ID and delete old programs
			if(ShaderProgram_ID != -1) //Delete existing shader when reloading
				GL.DeleteProgram(ShaderProgram_ID);
			ShaderProgram_ID = newShaderProgram_ID;
		}

		/// <summary> Chainable method to change state to this program, and bind appropriate state or uniforms. </summary>
		public ShaderProgram Use() {
			GL.UseProgram(ShaderProgram_ID);
			GL.BindVertexArray(VertexArrayObject_ID);
			Renderer.INSTANCE.CurrentProgram = this;
			return this;
		}

		/// <summary> Reload the shader if its file(s) were changed </summary>
		public bool TryReload() {
			bool twoFiles = _shaderPath0 != null && _shaderPath1 != null;
			if (twoFiles) { //Separate shader files
				if (_lastWriteTime0 != File.GetLastWriteTime(_shaderPath0) || _lastWriteTime1 != File.GetCreationTime(_shaderPath1)) {
					Thread.Sleep(250); //Wait for a bit to ensure the text editor has released the file. Otherwise can crash.
					_lastWriteTime0 = File.GetLastWriteTime(_shaderPath0);
					_lastWriteTime1 = File.GetLastWriteTime(_shaderPath1);
					LoadShaders(new StreamReader(_shaderPath0).ReadToEnd(), new StreamReader(_shaderPath1).ReadToEnd());
					Console.WriteLine($"Reloaded '{_shaderPath0}' and '{_shaderPath1}'");
					return true;
				}
			}
			else { //Unified shader file
				if (_lastWriteTime0 != File.GetLastWriteTime(_shaderPath0)) {
					Thread.Sleep(250); //Wait for a bit to ensure the text editor has released the file. Otherwise can crash.
					_lastWriteTime0 = File.GetLastWriteTime(_shaderPath0);

					string unified = new StreamReader(_shaderPath0).ReadToEnd();
					string[] sources = unified.Split("<split>");
					Debug.Assert(sources.Length == 2, $"Shader at {_shaderPath0} is not a unified glsl file.");
					LoadShaders(sources[0], sources[1]);
					Console.WriteLine($"Reloaded '{_shaderPath0}'");
					return true;
				}
			}
			return false;
		}

		/// <summary> Assigns the pre-determined vertex attrib information to attrib pointers. This is called once after
		/// creating at least one VBO in this format. Provide the attribs as a series of ints specifying attrib size.
		/// For example, [vec3, vec4, vec3, vec2] would be int[] { 3, 4, 3, 2 }. </summary>
		public virtual ShaderProgram SetVertexAttribPointers(int[] attribs) {
			Use();
			int stride = attribs.Sum() * sizeof(float);
			int runningTotal = 0;
			for (int i = 0; i < attribs.Length; i++) {
				GL.EnableVertexAttribArray(i);
				GL.VertexAttribPointer(i, attribs[i], VertexAttribPointerType.Float, false, stride, runningTotal);
				runningTotal += attribs[i] * sizeof(float);
			}
			return this;
		}

		/// <summary> Creates a shader in GL from the path provided and returns the ID. </summary>
		private int CreateShaderFromSource(string source, ShaderType type) {
			Debug.Assert(source.Length > 0, $"Shader of type {type} is of length zero.");
			int shaderID = GL.CreateShader(type);
			GL.ShaderSource(shaderID, source);
			GL.CompileShader(shaderID);
			if (GL.GetShaderInfoLog(shaderID) != System.String.Empty)
			{
				//Log error
				Console.WriteLine($"Error compiling {type.ToString()} shader: {GL.GetShaderInfoLog(shaderID)}");

				//Throw if no existing shader is loaded
				if (ShaderProgram_ID == -1)
					throw new Exception($"\tError in \"{source}\" shader: \n{GL.GetShaderInfoLog(shaderID)}");
			}
			return shaderID;
		}
	}

	/// <summary> ShaderProgram for foward geometry rendering.<br/>
	/// Uniforms:  mat4 model, mat4 view, mat4 perspective, sampler2D albedoTexture<br/>
	/// Attribs: vec3 _position, vec3 _normal, vec4 _albedo, vec2 _uv </summary>
	public class ShaderProgramForwardRenderer : ShaderProgram {
		public readonly int UniformModel_ID;
		public readonly int UniformView_ID;
		public readonly int UniformPerspective_ID;
		public readonly int UniformTextureAlbedo_ID;

		/// <summary> Creates a ShaderProgram with vertex attribs and uniforms configured for src/render/shaders/ForwardShader.
		/// The purpose of this shader is a simpistic forward-renderer (as opposed to a deferred). </summary>
		public ShaderProgramForwardRenderer(string unifiedPath) : base(unifiedPath) {
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformView_ID = GL.GetUniformLocation(ShaderProgram_ID, "view");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");
			UniformTextureAlbedo_ID = GL.GetUniformLocation(ShaderProgram_ID, "albedoTexture");
		}
	}

	/// <summary> ShaderProgram for interface rendering.<br/>
	/// Uniforms:  mat4 model, mat4 perspective, sampler2D albedoTexture, bool isFont, float opacity<br/>
	/// Attribs: vec2 _position, vec2 _uv </summary>
	public class ShaderProgramInterface : ShaderProgram {
		public readonly int UniformModel_ID;
		public readonly int UniformPerspective_ID;
		public readonly int UniformTextureAlbedo_ID;
		public readonly int UniformIsFont;
		public readonly int UniformOpacity;

		/// <summary> Creates a ShaderProgram with vertex attribs and uniforms configured for src/render/shaders/InterfaceShader.
		/// The purpose of this shader is a simpistic interface renderer. Primarily operates on textured quads. Can also support
		/// advanced font rendering with signed distance fields when configured correctly and provided MDSDF textures. </summary>
		public ShaderProgramInterface(string unifiedPath) : base(unifiedPath) {
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");
			UniformTextureAlbedo_ID = GL.GetUniformLocation(ShaderProgram_ID, "albedoTexture");
			UniformIsFont = GL.GetUniformLocation(ShaderProgram_ID, "isFont");
			UniformOpacity = GL.GetUniformLocation(ShaderProgram_ID, "opacity");
		}
	}

	/// <summary> ShaderProgram for fog rendering.<br/>
	/// Uniforms:  mat4 model, mat4 view, mat4 perspective, sampler2D depth, vec2 screenSize<br/>
	/// Attribs: vec3 _position </summary>
	public class ShaderProgramFog : ShaderProgram {
		public readonly int UniformModel_ID;
		public readonly int UniformView_ID;
		public readonly int UniformPerspective_ID;
		public readonly int UniformDepth_ID;
		public readonly int UniformScreenSize_ID;

		/// <summary> Creates a ShaderProgram with vertex attribs and uniforms configured for src/render/shaders/FogShader.
		/// The purpose of this shader is a fancy fog shader, calculating fog depth via backface to frontface distance. </summary>
		public ShaderProgramFog(string unifiedPath) : base(unifiedPath) {
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformView_ID = GL.GetUniformLocation(ShaderProgram_ID, "view");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");
			UniformDepth_ID = GL.GetUniformLocation(ShaderProgram_ID, "depth");
			UniformScreenSize_ID = GL.GetUniformLocation(ShaderProgram_ID, "screenSize");
		}
	}

	/// <summary> ShaderProgram for the vignette full-screen effect. <br/>
	/// Uniforms: vec2 screenSize, float vignetteStrength <br/>
	/// Attribs: none </summary>
	public class ShaderProgramVignette : ShaderProgram {
		public readonly int UniformScreenSize_ID;
		public readonly int UniformVignetteStrength_ID;

		/// <summary> Creates a ShaderProgram with uniforms configured for src/render/shaders/VignetteShader.
		/// The purpose of this shader is to create the fullscreen vignette effect, at configurable at runtime strength. </summary>
		public ShaderProgramVignette(string unifiedPath) : base(unifiedPath) {
			UniformScreenSize_ID = GL.GetUniformLocation(ShaderProgram_ID, "screenSize");
			UniformVignetteStrength_ID = GL.GetUniformLocation(ShaderProgram_ID, "vignetteStrength");
		}
	}
}
