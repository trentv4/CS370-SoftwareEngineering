using System;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using System.IO;

namespace Project.Render {
	/// <summary> Wrapper class for the concept of an OpenGL program. Internally, this
	/// will handle setting vertex attribs and program-wide uniforms.</summary>
	public class ShaderProgram {
		/// <summary> OpenGL-assigned ID for this program </summary>
		public readonly int ShaderProgram_ID;
		public readonly int VertexArrayObject_ID;

		/// <summary> Loads a vertex and a fragment shader from disk, compiles them, links, and creates a ShaderProgram. </summary>
		public ShaderProgram(string vertexShaderPath, string fragmentShaderPath) {
			int vertexShaderID = CreateShaderFromFile(vertexShaderPath, ShaderType.VertexShader);
			int fragmentShaderID = CreateShaderFromFile(fragmentShaderPath, ShaderType.FragmentShader);
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
			return this;
		}

		/// <summary> Assigns the pre-determined vertex attrib information to attrib pointers. This is called once after
		/// creating at least one VBO in this format. </summary>
		public virtual ShaderProgram SetVertexAttribPointers() {
			return this;
		}

		/// <summary> Creates a shader in GL from the path provided and returns the ID. </summary>
		private static int CreateShaderFromFile(string path, ShaderType type) {
			string source = new StreamReader(path).ReadToEnd();
			Debug.Assert(source.Length > 0, $"Shader at {path} is of length zero.");
			int shaderID = GL.CreateShader(type);
			GL.ShaderSource(shaderID, source);
			GL.CompileShader(shaderID);
			if (GL.GetShaderInfoLog(shaderID) != System.String.Empty)
				throw new Exception($"\tError in \"{source}\" shader: \n{GL.GetShaderInfoLog(shaderID)}");
			return shaderID;
		}
	}

	public class ShaderProgramForwardRenderer : ShaderProgram {
		public readonly int UniformModel_ID;
		public readonly int UniformView_ID;
		public readonly int UniformPerspective_ID;
		public readonly int UniformTextureAlbedo_ID;

		public ShaderProgramForwardRenderer(string vertexShaderPath, string fragmentShaderPath) : base(vertexShaderPath, fragmentShaderPath) {
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
			UniformView_ID = GL.GetUniformLocation(ShaderProgram_ID, "view");
			UniformPerspective_ID = GL.GetUniformLocation(ShaderProgram_ID, "perspective");
			UniformTextureAlbedo_ID = GL.GetUniformLocation(ShaderProgram_ID, "albedoTexture");
		}

		public override ShaderProgram SetVertexAttribPointers() {
			Use();
			GL.BindVertexArray(VertexArrayObject_ID);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.EnableVertexAttribArray(2);
			GL.EnableVertexAttribArray(3);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 12 * sizeof(float), 0 * sizeof(float)); /* xyz */
			GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 12 * sizeof(float), 3 * sizeof(float)); /* normals */
			GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 12 * sizeof(float), 6 * sizeof(float)); /* rgba */
			GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, 12 * sizeof(float), 10 * sizeof(float)); /* uv */
			return this;
		}
	}

	public class ShaderProgramInterface : ShaderProgram {
		public readonly int UniformModel_ID;

		public ShaderProgramInterface(string vertexShaderPath, string fragmentShaderPath) : base(vertexShaderPath, fragmentShaderPath) {
			UniformModel_ID = GL.GetUniformLocation(ShaderProgram_ID, "model");
		}

		public override ShaderProgram SetVertexAttribPointers() {
			Use();
			GL.BindVertexArray(VertexArrayObject_ID);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0 * sizeof(float)); /* xyz */
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float)); /* uv */
			return this;
		}
	}
}
