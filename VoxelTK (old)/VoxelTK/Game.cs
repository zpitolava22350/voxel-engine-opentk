using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using System.Timers;

namespace VoxelTK;

internal partial class Game: GameWindow {

    int WindowWidth;
    int WindowHeight;

    Texture texture;
    Shader shader;

    Camera camera;

    System.Timers.Timer timer;

    int fps = 0;

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

        timer = new System.Timers.Timer();

        timer.Interval = 1000;
        timer.Elapsed += (s, e) => { Console.WriteLine($"{fps} fps"); fps = 0; };

        timer.Start();

        camera = new Camera(WindowWidth, WindowHeight, new Vector3(0, 0, 0));

        CursorState = CursorState.Grabbed;

        Chunks = new Dictionary<int, Dictionary<int, Dictionary<int, Chunk>>>();

        texture = Texture.LoadFromFile("texsheet.png");
        shader = new Shader("shaders/vertex.vert", "shaders/pixel.frag");
        shader.Use();

    }

    protected override void OnLoad() {
        base.OnLoad();

        for (int x = -2; x < 3; x++) {
            Chunks[x] = new Dictionary<int, Dictionary<int, Chunk>>();
            for (int y = -2; y < 3; y++) {
                Chunks[x][y] = new Dictionary<int, Chunk>();
                for (int z = -2; z < 3; z++) {
                    Chunks[x][y][z] = new Chunk(x, y, z);
                }
            }
        }

        Stopwatch sw = Stopwatch.StartNew();

        foreach (var x in Chunks) {
            foreach (var y in x.Value) {
                foreach (var z in y.Value) {
                    z.Value.GenerateBlocks();
                }
            }
        }

        sw.Stop();
        Console.WriteLine($"Generate Blocks: {sw.ElapsedMilliseconds}ms");
        sw.Restart();

        foreach (var x in Chunks) {
            foreach (var y in x.Value) {
                foreach (var z in y.Value) {
                    z.Value.GenerateVertices();
                }
            }
        }

        sw.Stop();
        Console.WriteLine($"Generate Mesh: {sw.ElapsedMilliseconds}ms");
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
            Console.WriteLine($"Deltatime: {1.0 / args.Time}");
        }

        if(CursorState == CursorState.Grabbed)
            camera.Update(KeyboardState, MouseState, args);
    }

    protected override void OnRenderFrame(FrameEventArgs args) {

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

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

        SwapBuffers();

        base.OnRenderFrame(args);

        fps++;

    }

}
