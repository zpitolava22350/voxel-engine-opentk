using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelTK2 {
    /// <summary>
    /// Handles world generation, the clients generate the meshes
    /// </summary>
    internal static class Server {

        static Random rnd;
        static int seed;

        static Dictionary<int, Dictionary<int, Dictionary<int, byte[,,]>>> blocks;

        static Vector2? playerPos;
        
        static Server() {
            rnd = new Random();
            blocks = new Dictionary<int, Dictionary<int, Dictionary<int, byte[,,]>>>();
            playerPos = null;
        }

        public static void CreateWorld() {
            CreateWorld(rnd.Next(int.MaxValue));
        }

        public static void CreateWorld(int seed) {

        }

        public static void UpdatePlayerPos(Vector2 position) {
            if (playerPos == null) { // First

            } else { // Update

            }
        }

    }
}
