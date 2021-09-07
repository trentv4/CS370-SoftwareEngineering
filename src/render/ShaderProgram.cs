using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace Project.Render {
	public class ShaderProgram {
		/// <summary> OpenGL-assigned ID for this program </summary>
		public readonly int ShaderProgramID;

		public ShaderProgram(string vertexShaderPath, string fragmentShaderPath) {
			int vertexShaderID = CreateShaderFromFile(vertexShaderPath, ShaderType.VertexShader);
			int fragmentShaderID = CreateShaderFromFile(fragmentShaderPath, ShaderType.FragmentShader);
			Debug.Assert(vertexShaderID != 0 && fragmentShaderID != 0, "Failure during shader loading with creating vert/frag shaders");
			ShaderProgramID = GL.CreateProgram();
			GL.AttachShader(vertexShaderID, 0);
			GL.AttachShader(fragmentShaderID, 1);
			GL.LinkProgram(ShaderProgramID);
			GL.DeleteShader(vertexShaderID);
			GL.DeleteShader(fragmentShaderID);
		}

		/// <summary> Chainable method to change state to this program, and bind appropriate state or uniforms. </summary>
		public ShaderProgram use() {
			GL.UseProgram(ShaderProgramID);
			return this;
		}

		/// <summary> Creates a shader in GL from the path provided and returns the ID. </summary>
		private static int CreateShaderFromFile(string path, ShaderType type) {
			string source = new StreamReader(path).ReadToEnd();
			Debug.Assert(source.Length > 0, $"Shader at {path} is of length zero.");
			int shaderID = GL.CreateShader(type);
			GL.CompileShader(shaderID);
			if (GL.GetShaderInfoLog(shaderID) != System.String.Empty)
				throw new Exception($"\tError in \"{source}\" shader: \n{GL.GetShaderInfoLog(shaderID)}");
			return shaderID;
		}
	}
}
