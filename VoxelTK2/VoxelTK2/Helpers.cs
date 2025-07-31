using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelTK2 {
    internal partial class Game: GameWindow {

        /// <summary>
        /// Helper method to run all of the BlockTextures.DefineBlock calls. This should be on program startup.
        /// </summary>
        private void DefineAllBlocks() {
            // BlockTextures class will create the Texture atlas procedurally
            // We just need to name blocks and put textures onto them

            // All textures are inside the "Textures" folder

            BlockTextures.DefineBlock(blockName: "Grass", // The "Grass" block has the following textures:
                top: "grass",        // "grass.png" on the top
                sides: "grass_side", // "grass_side.png" on the sides
                bottom: "dirt"       // "dirt.png" on the bottom
            );

            BlockTextures.DefineBlock(blockName: "Dirt",
                texture: "dirt"
            );

            BlockTextures.DefineBlock(blockName: "Stone",
                texture: "stone"
            );
        }

    }
}
