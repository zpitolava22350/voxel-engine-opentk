using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace VoxelTK;

internal partial class Game: GameWindow {

    Dictionary<int, Dictionary<int, Dictionary<int, Chunk>>> Chunks;

}