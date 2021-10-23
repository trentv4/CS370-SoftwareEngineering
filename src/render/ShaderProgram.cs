using System;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Project.Util;

namespace Project.Render {
	/// <summary> Wrapper class for the concept of an OpenGL program. Internally, this
	/// will handle setting vertex attribs and program-wide uniforms. </summary>
	public class ShaderProgram {
		public int ShaderProgram_ID { get; private set; } = -1;
		public readonly int VertexArrayObject_ID;
		private ShaderData _shaders;

		/// <summary> Loads a vertex and a fragment shader from disk, compiles them, links, and creates a ShaderProgram. </summary>
		public ShaderProgram(string vertexShaderPath, string fragmentShaderPath) {
			_shaders = new ShaderData(vertexShaderPath, fragmentShaderPath);
			ShaderProgram_ID = GL.CreateProgram();
			VertexArrayObject_ID = GL.GenVertexArray();
			LoadShaders(true);
		}

		/// <summary> Loads a unified shader (vertex + fragment shaders in same file split by a split keyword), compiles them,
		/// links, and creates the ShaderProgram. </summary>
		public ShaderProgram(string unifiedPath) {
			_shaders = new ShaderData(unifiedPath);
			ShaderProgram_ID = GL.CreateProgram();
			VertexArrayObject_ID = GL.GenVertexArray();
			LoadShaders(true);
		}

		/// <summary> Create vertex and fragment shaders from strings </summary>
		private void LoadShaders(bool firstLoad = false) {
			if(!_shaders.Load())
				return;

			//Link shaders on first load
			if (firstLoad) {
				foreach (int shaderID in _shaders.IDs) {
					Debug.Assert(shaderID != 0, "Null shader ID during shader program linking.");
					GL.AttachShader(ShaderProgram_ID, shaderID);
				}
			}
			GL.LinkProgram(ShaderProgram_ID);

			//Check for link errors
			if (GL.GetProgramInfoLog(ShaderProgram_ID) != System.String.Empty) {
				Console.WriteLine($"Error while linking shaders: {GL.GetProgramInfoLog(ShaderProgram_ID)}");
				if (firstLoad) //Throw if a valid fallback shader isn't already loaded
					throw new Exception($"\tError in shader program linkage: \n{GL.GetProgramInfoLog(ShaderProgram_ID)}");

				return;
			}

			//Success. Bind program and update uniforms
			GL.UseProgram(ShaderProgram_ID);
			SetUniforms();
		}

		/// <summary> Chainable method to change state to this program, and bind appropriate state or uniforms. </summary>
		public ShaderProgram Use() {
			TryReload();
			GL.UseProgram(ShaderProgram_ID);
			GL.BindVertexArray(VertexArrayObject_ID);
			Renderer.INSTANCE.CurrentProgram = this;
			return this;
		}

		protected virtual void SetUniforms() { }

		/// <summary> Reload the shader if its file(s) were changed </summary>
		private bool TryReload() {
			if(_shaders.Changed()) {
				LoadShaders();
				return true;
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

		/// <summary> Additional data for the shaders that make up an OpenGL program. </summary>
		private class ShaderData {
			public readonly string[] Paths;
			public DateTime[] WriteTimes;
			public readonly int[] IDs;
			///<summary> If true, there's only one shader file </summary>
			public readonly bool Unified;

			public ShaderData(string vertexShaderPath, string fragmentShaderPath) {
				Paths = new string[] { vertexShaderPath, fragmentShaderPath };
				WriteTimes = new DateTime[] { File.GetLastWriteTime(vertexShaderPath), File.GetLastWriteTime(fragmentShaderPath) };
				IDs = new int[] { GL.CreateShader(ShaderType.VertexShader), GL.CreateShader(ShaderType.FragmentShader) };
				Unified = false;
				Load(true);
			}

			public ShaderData(string unifiedPath) {
				Paths = new string[] { unifiedPath };
				WriteTimes = new DateTime[] { File.GetLastWriteTime(unifiedPath) };
				IDs = new int[] { GL.CreateShader(ShaderType.VertexShader), GL.CreateShader(ShaderType.FragmentShader) };
				Unified = true;
				Load(true);
			}

			~ShaderData() {
				foreach (int id in IDs)
					GL.DeleteShader(id);
			}

			/// <summary> Returns true if any of the shader files have changed since last reload </summary>
			public bool Changed() {
				for (int i = 0; i < Paths.Length; i++)
					if (WriteTimes[i] != File.GetLastWriteTime(Paths[i]))
						return true;

				return false;
			}

			/// <summary> Load and compile shader files </summary>
			public bool Load(bool firstLoad = false) {
				//Load shader sources
				if (!LoadShaderSources(out string[] sources))
					return false;

				//Update write times
				for (int i = 0; i < Paths.Length; i++)
					WriteTimes[i] = File.GetLastWriteTime(Paths[i]);

				//Compile shader source
				for (int i = 0; i < sources.Length; i++) {
					string source = sources[i];
					int id = IDs[i];
					Debug.Assert(source.Length > 0, $"File for shader {i} is empty.");
					GL.ShaderSource(id, source);
					GL.CompileShader(id);

					//Check for errors
					if (GL.GetShaderInfoLog(id) != System.String.Empty) {
						//Throw if no existing shader is loaded
						if (firstLoad)
							throw new Exception($"\tError in \"{source}\" shader: \n{GL.GetShaderInfoLog(id)}");

						//Otherwise log the error
						Console.WriteLine($"Error compiling shader {Paths[0]}:{i}: {GL.GetShaderInfoLog(id)}");
						return false;
					}
				}

				return true;
			}

			/// <summary> Load shader source strings from their files </summary>
			private bool LoadShaderSources(out string[] sources) {
				sources = null;

				List<string> sourceList = new List<string>();
				if (Unified) {
					//Load unified file and split into individual shaders
					if (!FileUtil.TryReadFile(Paths[0], out string source)) {
						Console.WriteLine($"Failed to read shader file '{Paths[0]}'. File is locked.");
						return false;
					}
					sourceList.AddRange(source.Split("<split>"));

					//Ensure required shaders are present
					if (sourceList.Count != 2) {
						Console.WriteLine($"Shader at {Paths[0]} is not a unified glsl file. Halting load.");
					 	return false;
					}
					//Todo: ^^Fix this. This check exists because sometimes the shader file is empty when loaded. 
					//		I suspect this happens when VSCode saves while the game is loading the shader. 
					//		However, FileUtil.TryReadFile() locks the file before reading it so this shouldn't happen
				}  else {
					//Load separate shader files
					if (!FileUtil.TryReadFile(Paths[0], out string vertSource) || !FileUtil.TryReadFile(Paths[1], out string fragSource)) {
						Console.WriteLine($"Failed to read '{Paths[0]}' and '{Paths[1]}'. Files are locked.");
						return false;
					}
					sourceList.Add(vertSource);
					sourceList.Add(fragSource);
				}

				sources = sourceList.ToArray();
				return true;
			}
		}
	}

	/// <summary> ShaderProgram for foward geometry rendering.<br/>
	/// Uniforms:  mat4 model, mat4 view, mat4 perspective, sampler2D albedoTexture<br/>
	/// Attribs: vec3 _position, vec3 _normal, vec4 _albedo, vec2 _uv </summary>
	public class ShaderProgramForwardRenderer : ShaderProgram {
		public ShaderProgramForwardRenderer(string unifiedPath) : base(unifiedPath) { }
		public ShaderProgramForwardRenderer(string vertexShader, string fragmentShader) : base(vertexShader, fragmentShader) { }

		public int UniformModel_ID { get; private set; }
		public int UniformView_ID { get; private set; }
		public int UniformPerspective_ID { get; private set; }
		public int UniformTextureAlbedo_ID { get; private set; }

		/// <summary> Creates a ShaderProgram with vertex attribs and uniforms configured for src/render/shaders/ForwardShader.
		/// The purpose of this shader is a simpistic forward-renderer (as opposed to a deferred). </summary>
		protected override void SetUniforms() {
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
		public ShaderProgramInterface(string unifiedPath) : base(unifiedPath) { }
		public ShaderProgramInterface(string vertexShader, string fragmentShader) : base(vertexShader, fragmentShader) { }

		public int UniformModel_ID { get; private set; }
		public int UniformPerspective_ID { get; private set; }
		public int UniformTextureAlbedo_ID { get; private set; }
		public int UniformIsFont { get; private set; }
		public int UniformOpacity { get; private set; }

		/// <summary> Creates a ShaderProgram with vertex attribs and uniforms configured for src/render/shaders/InterfaceShader.
		/// The purpose of this shader is a simpistic interface renderer. Primarily operates on textured quads. Can also support
		/// advanced font rendering with signed distance fields when configured correctly and provided MDSDF textures. </summary>
		protected override void SetUniforms() {
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");
			UniformTextureAlbedo_ID = GL.GetUniformLocation(ShaderProgram_ID, "albedoTexture");
			UniformIsFont = GL.GetUniformLocation(ShaderProgram_ID, "isFont");
			UniformOpacity = GL.GetUniformLocation(ShaderProgram_ID, "opacity");
		}
	}

	/// <summary> ShaderProgram for fog rendering. Found at src/render/shaders/FogShader. 
	/// The purpose of this shader is a fancy fog shader, calculating fog depth via backface to frontface distance. <br/>
	/// Uniforms:  mat4 model, mat4 view, mat4 perspective, sampler2D depth, vec2 screenSize<br/>
	/// Attribs: vec3 _position </summary>
	public class ShaderProgramFog : ShaderProgram {
		public ShaderProgramFog(string unifiedPath) : base(unifiedPath) { }
		public ShaderProgramFog(string vertexShader, string fragmentShader) : base(vertexShader, fragmentShader) { }

		public int UniformModel_ID { get; private set; }
		public int UniformView_ID { get; private set; }
		public int UniformPerspective_ID { get; private set; }
		public int UniformDepth_ID { get; private set; }

		protected override void SetUniforms() {
			GL.Uniform2(GL.GetUniformLocation(ShaderProgram_ID, "screenSize"), (float)Renderer.INSTANCE.Size.X, (float)Renderer.INSTANCE.Size.Y);
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformView_ID = GL.GetUniformLocation(ShaderProgram_ID, "view");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");
			UniformDepth_ID = GL.GetUniformLocation(ShaderProgram_ID, "depth");
		}
	}

	/// <summary> ShaderProgram for the vignette full-screen effect. Found at src/render/shaders/VignetteShader. <br/>
	/// Uniforms: vec2 screenSize, float vignetteStrength <br/>
	/// Attribs: none </summary>
	public class ShaderProgramVignette : ShaderProgram {
		public ShaderProgramVignette(string unifiedPath) : base(unifiedPath) { }
		public ShaderProgramVignette(string vertexShader, string fragmentShader) : base(vertexShader, fragmentShader) { }

		public int UniformVignetteStrength_ID { get; private set; }

		protected override void SetUniforms() {
			UniformVignetteStrength_ID = GL.GetUniformLocation(ShaderProgram_ID, "vignetteStrength");
			GL.Uniform2(GL.GetUniformLocation(ShaderProgram_ID, "screenSize"), (float)Renderer.INSTANCE.Size.X, (float)Renderer.INSTANCE.Size.Y);
		}
	}

	public class ShaderProgramDeferredRenderer : ShaderProgram {
		public ShaderProgramDeferredRenderer(string unifiedPath) : base(unifiedPath) { }
		public ShaderProgramDeferredRenderer(string vertexShader, string fragmentShader) : base(vertexShader, fragmentShader) { }

		public int UniformModel_ID { get; private set; }
		public int UniformView_ID { get; private set; }
		public int UniformPerspective_ID { get; private set; }
		public int UniformTextureAlbedo_ID { get; private set; }

		/// <summary> Creates a ShaderProgram with vertex attribs and uniforms configured for src/render/shaders/ForwardShader.
		/// The purpose of this shader is a simpistic forward-renderer (as opposed to a deferred). </summary>
		protected override void SetUniforms() {
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformView_ID = GL.GetUniformLocation(ShaderProgram_ID, "view");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");
			UniformTextureAlbedo_ID = GL.GetUniformLocation(ShaderProgram_ID, "albedoTexture");
		}
	}

	public class ShaderProgramCompositor : ShaderProgram {
		public ShaderProgramCompositor(string unifiedPath) : base(unifiedPath) { }
		public ShaderProgramCompositor(string vertexShader, string fragmentShader) : base(vertexShader, fragmentShader) { }

		protected override void SetUniforms() {
			GL.Uniform1(GL.GetUniformLocation(ShaderProgram_ID, "sampler_world"), 0);
			GL.Uniform1(GL.GetUniformLocation(ShaderProgram_ID, "sampler_fog"), 1);
			GL.Uniform1(GL.GetUniformLocation(ShaderProgram_ID, "sampler_lighting"), 2);
			GL.Uniform1(GL.GetUniformLocation(ShaderProgram_ID, "sampler_interface"), 3);
		}

		public void Draw(Texture framebufferTexture) {
			framebufferTexture.Bind(0);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
		}
	}
}
