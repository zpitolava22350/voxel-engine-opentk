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

        World world;
        Shader shader;
        Camera camera;

        public Game(GameWindowSettings g, NativeWindowSettings n) : base(g, n) {

            WindowWidth = n.Size.X;
            WindowHeight = n.Size.Y;

            CenterWindow(new Vector2i(WindowWidth, WindowHeight));
            GL.Viewport(0, 0, WindowWidth, WindowHeight);

            GL.ClearColor(0.56f, 0.9f, 1f, 1f);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Cw);

            CursorState = CursorState.Grabbed;

            camera = new Camera(WindowWidth, WindowHeight, new Vector3(0, 0, 0));

            shader = new Shader("shaders/vertex.vert", "shaders/pixel.frag");
            shader.Use();

            world = new World();

        }

        protected override void OnLoad() {
            base.OnLoad();
            DefineAllBlocks();
            RawMouseInputReader.SetMoveCallback(MouseMove);
            RawMouseInputReader.SetWheelCallback(MouseScroll);
            RawMouseInputReader.Initialize(RawMouseInputReader.GetActiveWindow());
            BlockTextures.CreateGLTexture();
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

            camera.Update(KeyboardState, MouseState, args);
            world.Update(camera.position);
            //Console.WriteLine($"blocks {world.blocks.Count}");
            //Console.WriteLine($"meshes {world.meshes.Count}");

            //if (CursorState == CursorState.Grabbed)
            //camera.Update(KeyboardState, MouseState, args);
        }

        protected override void OnRenderFrame(FrameEventArgs args) {

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.Use();
            BlockTextures.Use(TextureUnit.Texture0);

            Matrix4 model = Matrix4.Identity;
            Matrix4 view = camera.getViewMatrix();
            Matrix4 projection = camera.getProjectionMatrix();

            int modelLocation = GL.GetUniformLocation(shader.Handle, "model");
            int viewLocation = GL.GetUniformLocation(shader.Handle, "view");
            int projectionLocation = GL.GetUniformLocation(shader.Handle, "projection");

            Mesh.ModelLocation = modelLocation;

            GL.UniformMatrix4(viewLocation, true, ref view);
            GL.UniformMatrix4(projectionLocation, true, ref projection);

            world.Render(camera.position);

            SwapBuffers();

            base.OnRenderFrame(args);

        }

        private void MouseMove(int deltaX, int deltaY) {
            // Update yaw and pitch based on mouse movement
            camera.MouseController(deltaX, deltaY);
        }

        private void MouseScroll(int delta) {

        }

    }
}
