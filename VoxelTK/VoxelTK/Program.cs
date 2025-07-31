using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;

namespace VoxelTK;

internal class Program {
    static void Main(string[] args) {

        var g = new GameWindowSettings();
        var n = new NativeWindowSettings();

        n.Size = new Vector2i(1920, 1080);
        n.Title = "Slopcraft";

        //n.Vsync = OpenTK.Windowing.Common.VSyncMode.On;

        using (Game game = new Game(g, n)) {
            game.Run();
        }
    }
}