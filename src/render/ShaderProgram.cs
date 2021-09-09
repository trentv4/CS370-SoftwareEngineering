using System;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using System.IO;

namespace Project.Render {
	/// <summary> Wrapper class for the concept of an OpenGL program. Internally, this
	/// will handle setting vertex attribs and program-wide uniforms.</summary>
	public class ShaderProgram {
		/// <summary> OpenGL-assigned ID for this program </summary>
		public readonly int ShaderProgramID;
		public readonly int VertexArrayObject_ID;

		public ShaderProgram(string vertexShaderPath, string fragmentShaderPath) {
			int vertexShaderID = CreateShaderFromFile(vertexShaderPath, ShaderType.VertexShader);
			int fragmentShaderID = CreateShaderFromFile(fragmentShaderPath, ShaderType.FragmentShader);
			Debug.Assert(vertexShaderID != 0 && fragmentShaderID != 0, "Failure during shader loading with creating vert/frag shaders");
			ShaderProgramID = GL.CreateProgram();
			GL.AttachShader(ShaderProgramID, vertexShaderID);
			GL.AttachShader(ShaderProgramID, fragmentShaderID);
			GL.LinkProgram(ShaderProgramID);
			if (GL.GetProgramInfoLog(ShaderProgramID) != System.String.Empty)
				throw new Exception($"\tError in shader program linkage: \n{GL.GetProgramInfoLog(ShaderProgramID)}");
			GL.UseProgram(ShaderProgramID);
			GL.DeleteShader(vertexShaderID);
			GL.DeleteShader(fragmentShaderID);

			VertexArrayObject_ID = GL.GenVertexArray();
		}

		/// <summary> Chainable method to change state to this program, and bind appropriate state or uniforms. </summary>
		public ShaderProgram Use() {
			GL.UseProgram(ShaderProgramID);
			return this;
		}

		public ShaderProgram SetVertexAttribPointers() {
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
}