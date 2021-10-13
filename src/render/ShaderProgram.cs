using System;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Project.Render {
	/// <summary> Wrapper class for the concept of an OpenGL program. Internally, this
	/// will handle setting vertex attribs and program-wide uniforms.</summary>
	public class ShaderProgram {
		public readonly int ShaderProgram_ID;
		public readonly int VertexArrayObject_ID;

		/// <summary> Loads a vertex and a fragment shader from disk, compiles them, links, and creates a ShaderProgram. </summary>
		public ShaderProgram(string vertexShaderPath, string fragmentShaderPath) {
			int vertexShaderID = CreateShaderFromSource(new StreamReader(vertexShaderPath).ReadToEnd(), ShaderType.VertexShader);
			int fragmentShaderID = CreateShaderFromSource(new StreamReader(fragmentShaderPath).ReadToEnd(), ShaderType.FragmentShader);
			Debug.Assert(vertexShaderID != 0 && fragmentShaderID != 0, "Failure during shader loading with creating vert/frag shaders");
			ShaderProgram_ID = GL.CreateProgram();
			GL.AttachShader(ShaderProgram_ID, vertexShaderID);
			GL.AttachShader(ShaderProgram_ID, fragmentShaderID);
			GL.LinkProgram(ShaderProgram_ID);
			if (GL.GetProgramInfoLog(ShaderProgram_ID) != System.String.Empty)
				throw new Exception($"\tError in shader program linkage: \n{GL.GetProgramInfoLog(ShaderProgram_ID)}");
			GL.UseProgram(ShaderProgram_ID);
			GL.DeleteShader(vertexShaderID);
			GL.DeleteShader(fragmentShaderID);

			VertexArrayObject_ID = GL.GenVertexArray();
		}

		public ShaderProgram(string unifiedPath) {
			string unified = new StreamReader(unifiedPath).ReadToEnd();
			string[] sources = unified.Split("<split>");
			Debug.Assert(sources.Length == 2, $"Shader at {unifiedPath} is not a unified glsl file.");
			int vertexShaderID = CreateShaderFromSource(sources[0], ShaderType.VertexShader);
			int fragmentShaderID = CreateShaderFromSource(sources[1], ShaderType.FragmentShader);
			Debug.Assert(vertexShaderID != 0 && fragmentShaderID != 0, "Failure during shader loading with creating vert/frag shaders");
			ShaderProgram_ID = GL.CreateProgram();
			GL.AttachShader(ShaderProgram_ID, vertexShaderID);
			GL.AttachShader(ShaderProgram_ID, fragmentShaderID);
			GL.LinkProgram(ShaderProgram_ID);
			if (GL.GetProgramInfoLog(ShaderProgram_ID) != System.String.Empty)
				throw new Exception($"\tError in shader program linkage: \n{GL.GetProgramInfoLog(ShaderProgram_ID)}");
			GL.UseProgram(ShaderProgram_ID);
			GL.DeleteShader(vertexShaderID);
			GL.DeleteShader(fragmentShaderID);

			VertexArrayObject_ID = GL.GenVertexArray();
		}

		/// <summary> Chainable method to change state to this program, and bind appropriate state or uniforms. </summary>
		public ShaderProgram Use() {
			GL.UseProgram(ShaderProgram_ID);
			GL.BindVertexArray(VertexArrayObject_ID);
			Renderer.INSTANCE.CurrentProgram = this;
			return this;
		}

		/// <summary> Assigns the pre-determined vertex attrib information to attrib pointers. This is called once after
		/// creating at least one VBO in this format. Provide the attribs as a series of ints specifying attrib size.
		/// For example, [vec3, vec4, vec3, vec2] would be int[] { 3, 4, 3, 2 }.</summary>
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
		private static int CreateShaderFromSource(string source, ShaderType type) {
			Debug.Assert(source.Length > 0, $"Shader of type {type} is of length zero.");
			int shaderID = GL.CreateShader(type);
			GL.ShaderSource(shaderID, source);
			GL.CompileShader(shaderID);
			if (GL.GetShaderInfoLog(shaderID) != System.String.Empty)
				throw new Exception($"\tError in \"{source}\" shader: \n{GL.GetShaderInfoLog(shaderID)}");
			return shaderID;
		}
	}

	/// <summary> ShaderProgram for foward geometry rendering.<br/>
	/// Uniforms:  mat4 model, mat4 view, mat4 perspective, sampler2D albedoTexture<br/>
	/// Attribs: vec3 _position, vec3 _normal, vec4 _albedo, vec2 _uv</summary>
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
	/// Uniforms:  mat3 mvp (model-view-projection matrix), sampler2D albedoTexture<br/>
	/// Attribs: vec2 _position, vec2 _uv</summary>
	public class ShaderProgramInterface : ShaderProgram {
		public readonly int UniformModel_ID;
		public readonly int UniformPerspective_ID;
		public readonly int UniformTextureAlbedo_ID;
		public readonly int UniformIsFont;
		public readonly int UniformOpacity;

		/// <summary> Creates a ShaderProgram with vertex attribs and uniforms configured for src/render/shaders/InterfaceShader.
		/// The purpose of this shader is a simpistic interface renderer. Primarily operates on textured quads. </summary>
		public ShaderProgramInterface(string unifiedPath) : base(unifiedPath) {
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");
			UniformTextureAlbedo_ID = GL.GetUniformLocation(ShaderProgram_ID, "albedoTexture");
			UniformIsFont = GL.GetUniformLocation(ShaderProgram_ID, "isFont");
			UniformOpacity = GL.GetUniformLocation(ShaderProgram_ID, "opacity");
		}
	}

	/// <summary> ShaderProgram for interface rendering.<br/>
	/// Uniforms:  mat3 mvp (model-view-projection matrix), sampler2D albedoTexture<br/>
	/// Attribs: vec2 _position, vec2 _uv</summary>
	public class ShaderProgramFog : ShaderProgram {
		public readonly int UniformModel_ID;
		public readonly int UniformView_ID;
		public readonly int UniformPerspective_ID;
		public readonly int UniformDepth_ID;
		public readonly int UniformScreenSize_ID;

		/// <summary> Creates a ShaderProgram with vertex attribs and uniforms configured for src/render/shaders/InterfaceShader.
		/// The purpose of this shader is a simpistic interface renderer. Primarily operates on textured quads. </summary>
		public ShaderProgramFog(string unifiedPath) : base(unifiedPath) {
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformView_ID = GL.GetUniformLocation(ShaderProgram_ID, "view");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");
			UniformDepth_ID = GL.GetUniformLocation(ShaderProgram_ID, "depth");
			UniformScreenSize_ID = GL.GetUniformLocation(ShaderProgram_ID, "screenSize");
		}
	}

	public class ShaderProgramVignette : ShaderProgram {
		public readonly int UniformScreenSize_ID;
		public readonly int UniformVignetteStrength_ID;

		public ShaderProgramVignette(string unifiedPath) : base(unifiedPath) {
			UniformScreenSize_ID = GL.GetUniformLocation(ShaderProgram_ID, "screenSize");
			UniformVignetteStrength_ID = GL.GetUniformLocation(ShaderProgram_ID, "vignetteStrength");
		}
	}
}
