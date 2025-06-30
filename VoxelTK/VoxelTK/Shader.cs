using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using System.Xml.Linq;

namespace VoxelTK {
    internal class Shader: IDisposable {

        public int Handle;

        private bool disposedValue = false;

        /// <summary>
        /// Takes in filepaths and compiles a shader program into the Handle property
        /// </summary>
        /// <param name="vertexPath">Filepath string for vertex shader program</param>
        /// <param name="fragmentPath">Filepath string for fragment shader program</param>
        public Shader(string vertexPath, string fragmentPath) {
            int vertexShader;
            int fragmentShader;

            string vertexShaderSource = File.ReadAllText(vertexPath);
            string fragmentShaderSource = File.ReadAllText(fragmentPath);

            vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);

            fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);

            int success; // used to check for errors
            GL.CompileShader(vertexShader);

            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out success);
            if (success == 0) {
                string infoLog = GL.GetShaderInfoLog(vertexShader);
                Debug.WriteLine(infoLog);
            }

            GL.CompileShader(fragmentShader);

            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
            if (success == 0) {
                string infoLog = GL.GetShaderInfoLog(fragmentShader);
                Debug.WriteLine(infoLog);
            }

            Handle = GL.CreateProgram();

            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);

            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out success);
            if (success == 0) {
                string infoLog = GL.GetProgramInfoLog(Handle);
                Debug.WriteLine(infoLog);
            }

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

        }

        public void Use() {
            GL.UseProgram(Handle);
        }

        public void SetInt(string name, int value) {
            int location = GL.GetUniformLocation(Handle, name);
            if (location == -1)
                throw new Exception($"Uniform '{name}' not found.");
            GL.Uniform1(location, value);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                GL.DeleteProgram(Handle);

                disposedValue = true;
            }
        }

        ~Shader() {
            if (disposedValue == false) {
                Debug.WriteLine("GPU Resource leak! Did you forget to call Dispose()?");
            }
        }


        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
