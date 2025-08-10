using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelTK2 {
    public class Mesh {

        public static int ModelLocation;

        public int VAO;
        int VBO;
        int EBO;

        public int indicesCount;

        Vector3 position = Vector3.Zero;
        public Vector3 Position { get { return position; } set { position = value; RecalculateMatrix(); } }

        Vector3 rotation = Vector3.Zero;
        public Vector3 Rotation { get { return rotation; } set { rotation = value; RecalculateMatrix(); } }


        Matrix4 model = Matrix4.Identity;
        public ref Matrix4 Model { get { return ref model; } }

        public bool Loaded { get; private set; }

        public Mesh() {

            Loaded = false;

        }

        public Mesh(float[] vertices, uint[] indices) : this() {

            if(vertices.Length == 0 ||  indices.Length == 0) {
                Loaded = false;
            } else {
                Load(vertices, indices);
                Loaded = true;
            }

        }

        public void Load(float[] vertices, uint[] indices) {

            Dispose();

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * 4, vertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 32, 0);  // Positions
            GL.EnableVertexArrayAttrib(VAO, 0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 32, 12); // Normals
            GL.EnableVertexArrayAttrib(VAO, 1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 32, 24); // UVs
            GL.EnableVertexArrayAttrib(VAO, 2);

            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * 4, indices, BufferUsageHint.StaticDraw);
            indicesCount = indices.Length;

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            GL.BindVertexArray(0);

        }

        private void RecalculateMatrix() {
            model = Matrix4.Identity;
            model *= Matrix4.CreateRotationZ(rotation.Z);
            model *= Matrix4.CreateRotationX(rotation.X);
            model *= Matrix4.CreateRotationY(rotation.Y);
            model *= Matrix4.CreateTranslation(position);
        }

        public void Dispose() {

            if (Loaded) {
                Loaded = false;
                GL.DeleteVertexArray(VAO);
                GL.DeleteBuffer(VBO);
                GL.DeleteBuffer(EBO);
            }

        }

        public void Render() {

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.UniformMatrix4(ModelLocation, true, ref Model);

            GL.DrawElements(BeginMode.Triangles, indicesCount, DrawElementsType.UnsignedInt, 0);

        }

    }
}
