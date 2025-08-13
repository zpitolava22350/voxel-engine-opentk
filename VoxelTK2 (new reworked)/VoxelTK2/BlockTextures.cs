using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VoxelTK2 {

    /// <summary>
    /// Block faces
    /// </summary>
    public enum Face {
        Top = 0,
        Bottom = 1,
        Left = 2,
        Right = 3,
        Front = 4,
        Back = 5
    }

    /// <summary>
    /// Used to store what texture will be used for what block, and also procedurally creates a texture atlas holding all of the used textures during program initialization
    /// </summary>
    static class BlockTextures {

        // Handle for GL texture
        public static int TextureHandle;


        // A single texture is TextureSize x TextureSize
        static int TextureSize = 16;

        // The atlas contains AtlasSize x AtlasSize block textures
        static int AtlasSize = 16;

        // 16 for both means the atlas is 256x256

        private static Image<Rgba32> Atlas;
        public static Dictionary<string, Dictionary<Face, Vector2>> UVs { get; private set; } // UVs["Grass"][Face.Top] = a Vector2 that is the UV coords on the atlas for the top of a grass block
        private static Dictionary<string, Vector2> TexturePositions; // used to ensure there are no duplicate textures on the atlas

        public static List<string> BlockIDs { get; private set; } // BlockIDs[0] = air, BlocksIDs[1] = Grass or whatever the first defined block is

        // Keeps track of how many textures have been loaded, used to position them on the atlas
        private static int LoadedCount = 0;

        static BlockTextures() {
            Atlas = new Image<Rgba32>(TextureSize * AtlasSize, TextureSize * AtlasSize);
            UVs = new Dictionary<string, Dictionary<Face, Vector2>>();
            TexturePositions = new Dictionary<string, Vector2>();
            BlockIDs = new List<string>();
            BlockIDs.Add("Air");
        }

        /// <summary>
        /// Save the current atlas to disk, mainly for debugging purposes
        /// </summary>
        /// <param name="filename">File name for the saved image</param>
        public static void SaveToDisk(string filename) {
            Atlas.Save(filename, new PngEncoder());
        }

        /// <summary>
        /// Put the texture atlas on a certain GL TextureUnit slot
        /// </summary>
        /// <param name="unit">TextureUnit slot to load the Atlas onto</param>
        public static void Use(TextureUnit unit) {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, TextureHandle);
        }

        /// <summary>
        /// Use the atlas to create the texture OpenGL will use. Run this AFTER you have defined all of the blocks
        /// </summary>
        public static void CreateGLTexture() {
            Atlas.Configuration.PreferContiguousImageBuffers = true;

            // Get underlying memory
            if (!Atlas.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory))
                throw new Exception("Image pixel memory is not contiguous.");

            // Convert to byte[]
            byte[] bytes = MemoryMarshal.AsBytes(memory.Span).ToArray();

            TextureHandle = GL.GenTexture();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, TextureHandle);

            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            GL.TexImage2D(TextureTarget.Texture2D,
                level: 0, // LODs
                internalformat: PixelInternalFormat.Rgba8,
                width: TextureSize * AtlasSize,
                height: TextureSize * AtlasSize,
                border: 0,
                format: PixelFormat.Rgba,
                type: PixelType.UnsignedByte,
                pixels: bytes
            );

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Helper method to make sure the texture is on the atlas, and returns the UV position
        /// </summary>
        /// <param name="texture">Texture to put on the atlas / retrieve from the atlas</param>
        /// <returns>Vector2 UV position on the atlas (0.0 to 1.0)</returns>
        private static Vector2 getTexturePosition(string texture) {
            if (TexturePositions.ContainsKey(texture)) {
                // Just store UVs in dictionary
                return TexturePositions[texture];
            } else {
                // Put onto atlas
                int x = LoadedCount % AtlasSize;
                int y = LoadedCount / AtlasSize;
                (x, y) = (x * TextureSize, y * TextureSize);
                using var tex = Image.Load<Rgba32>($"Textures/{texture}.png");
                Atlas.Mutate(img => img.DrawImage(tex, new Point(x, y), 1.0f));

                // Store UVs in dictionary
                float uvX = (float)x / (float)(TextureSize * AtlasSize);
                float uvY = (float)y / (float)(TextureSize * AtlasSize);
                TexturePositions[texture] = new Vector2(uvX, uvY);
                LoadedCount++;
                return TexturePositions[texture];
            }
        }

        /// <summary>
        /// Define a block and give it a texture
        /// </summary>
        /// <param name="blockName">Name that you give the block, like "grass" or something, used to access it later</param>
        /// <param name="texture">Filename for the texture that covers the block, for ex: "grass" would cover the block with Textures/grass.png</param>
        public static void DefineBlock(string blockName, string texture) {

            UVs[blockName] = new Dictionary<Face, Vector2>();
            foreach (Face f in Enum.GetValues(typeof(Face))) {
                UVs[blockName][f] = getTexturePosition(texture);
            }

            BlockIDs.Add(blockName);

        }

        /// <summary>
        /// Define a block and give the top, bottom, and sides their own textures
        /// </summary>
        /// <param name="blockName">Name that you give the block, like "grass" or something, used to access it later</param>
        /// <param name="top">Filename for the texture that covers the top of the block, for ex: "grass" would cover the top with Textures/grass.png</param>
        /// <param name="sides">Filename for the texture that covers the sides of the block, for ex: "grass" would cover the sides with Textures/grass.png</param>
        /// <param name="bottom">Filename for the texture that covers the bottom of the block, for ex: "grass" would cover the bottom with Textures/grass.png</param>
        public static void DefineBlock(string blockName, string top, string sides, string bottom) {

            Vector2 topUVs = getTexturePosition(top);
            Vector2 sideUVs = getTexturePosition(sides);
            Vector2 bottomUVs = getTexturePosition(bottom);

            UVs[blockName] = new Dictionary<Face, Vector2>();
            var block = UVs[blockName];

            block[Face.Top] = topUVs;

            block[Face.Front] = sideUVs;
            block[Face.Back] = sideUVs;
            block[Face.Left] = sideUVs;
            block[Face.Right] = sideUVs;

            block[Face.Bottom] = bottomUVs;

            BlockIDs.Add(blockName);

        }

        /// <summary>
        /// Define a block and give each face a texture
        /// </summary>
        /// <param name="blockName">Name that you give the block, like "grass" or something, used to access it later</param>
        /// <param name="top">Filename for the texture that covers the top of the block, for ex: "grass" would cover the top with Textures/grass.png</param>
        /// <param name="bottom">Filename for the texture that covers the bottom of the block, for ex: "grass" would cover the bottom with Textures/grass.png</param>
        /// <param name="front">Filename for the texture that covers the front of the block, for ex: "grass" would cover the front with Textures/grass.png</param>
        /// <param name="back">Filename for the texture that covers the back of the block, for ex: "grass" would cover the back with Textures/grass.png</param>
        /// <param name="left">Filename for the texture that covers the left of the block, for ex: "grass" would cover the left with Textures/grass.png</param>
        /// <param name="right">Filename for the texture that covers the right of the block, for ex: "grass" would cover the right with Textures/grass.png</param>
        public static void DefineBlock(string blockName, string top, string bottom, string front, string back, string left, string right) {

            Vector2 topUVs = getTexturePosition(top);
            Vector2 frontUVs = getTexturePosition(front);
            Vector2 backUVs = getTexturePosition(back);
            Vector2 leftUVs = getTexturePosition(left);
            Vector2 rightUVs = getTexturePosition(right);
            Vector2 bottomUVs = getTexturePosition(bottom);

            UVs[blockName] = new Dictionary<Face, Vector2>();
            var block = UVs[blockName];

            block[Face.Top] = topUVs;

            block[Face.Front] = frontUVs;
            block[Face.Back] = backUVs;
            block[Face.Left] = leftUVs;
            block[Face.Right] = rightUVs;

            block[Face.Bottom] = bottomUVs;

            BlockIDs.Add(blockName);

        }

    }

}