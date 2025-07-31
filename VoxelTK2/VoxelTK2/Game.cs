using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelTK2 {
    internal partial class Game : GameWindow {

        int WindowWidth;
        int WindowHeight;

        public Game(GameWindowSettings g, NativeWindowSettings n) : base(g, n) {

            WindowWidth = n.Size.X;
            WindowHeight = n.Size.Y;

            CenterWindow(new Vector2i(WindowWidth, WindowHeight));
            GL.Viewport(0, 0, WindowWidth, WindowHeight);

            GL.ClearColor(0.1f, 0.7f, 0.4f, 1f);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Ccw);

            CursorState = CursorState.Grabbed;

        }

        protected override void OnLoad() {
            base.OnLoad();
            DefineAllBlocks();
        }

        protected override void OnResize(ResizeEventArgs e) {
            base.OnResize(e);
            WindowWidth = e.Width;
            WindowHeight = e.Height;
            GL.Viewport(0, 0, WindowWidth, WindowHeight);
        }

        protected override void OnUpdateFrame(FrameEventArgs args) {
            base.OnUpdateFrame(args);

            if (!KeyboardState.WasKeyDown(Keys.Escape) && KeyboardState.IsKeyDown(Keys.Escape)) {
                if (CursorState == CursorState.Normal)
                    Close();
                else
                    CursorState = CursorState.Normal;
            }

            if (!MouseState.WasButtonDown(MouseButton.Left) && MouseState[MouseButton.Left]) {
                CursorState = CursorState.Grabbed;
            }

            //if (CursorState == CursorState.Grabbed)
                //camera.Update(KeyboardState, MouseState, args);
        }

        protected override void OnRenderFrame(FrameEventArgs args) {

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            /*

            shader.Use();
            texture.Use(0);

            Matrix4 model = Matrix4.Identity;
            Matrix4 view = camera.getViewMatrix();
            Matrix4 projection = camera.getProjectionMatrix();

            model *= Matrix4.CreateTranslation(0f, 0f, -3f);

            int modelLocation = GL.GetUniformLocation(shader.Handle, "model");
            int viewLocation = GL.GetUniformLocation(shader.Handle, "view");
            int projectionLocation = GL.GetUniformLocation(shader.Handle, "projection");

            GL.UniformMatrix4(modelLocation, true, ref model);
            GL.UniformMatrix4(viewLocation, true, ref view);
            GL.UniformMatrix4(projectionLocation, true, ref projection);

            foreach (var x in Chunks) {
                foreach (var y in x.Value) {
                    foreach (var z in y.Value) {
                        z.Value.Render();
                    }
                }
            }

            */

            SwapBuffers();

            base.OnRenderFrame(args);

        }

    }
}
