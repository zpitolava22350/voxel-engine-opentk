using OpenTK.Compute.OpenCL;
using OpenTK.Mathematics;
using SharpNoise;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace VoxelTK2 {
    internal partial class World {

        static Random rnd;

        public event Action<byte[,,], Vector3i> GenerateCallback;
        public event Action<float[], uint[], Vector3i> MeshCallback;

        int Seed;
        public int ChunkSize { get; private set; } = 32;

        private int GenerateDistance = 4;

        private int RenderDistance = 6;

        private readonly Queue<Action> callbackQueue = new();
        private readonly object callbackLock = new();

        public Dictionary<Vector3i, byte[,,]> blocks { get; private set; }
        public Dictionary<Vector3i, Mesh> meshes;

        private Vector3i playerChunk = Vector3i.Zero;

        int GenerateTaskCount = 2;
        Channel<Vector3i> GenChannel;

        int MeshTaskCount = 1;
        Channel<MeshGenerationJob> MeshChannel;

        static World() {
            rnd = new Random();
        }

        public World() {
            MeshGenerationJob.SetWorld(this);
            meshes = new Dictionary<Vector3i, Mesh>();
            blocks = new Dictionary<Vector3i, byte[,,]>();
            GenChannel = Channel.CreateUnbounded<Vector3i>();
            for (int i = 0; i < GenerateTaskCount; i++) {
                _ = GenerateTaskMethod();
            }
            GenerateCallback += GenerateCallbackMethod;
            MeshChannel = Channel.CreateUnbounded<MeshGenerationJob>();
            for (int i = 0; i < MeshTaskCount; i++) {
                _ = MeshTaskMethod();
            }
            MeshCallback += MeshCallbackMethod;
            GenerateNearby(new Vector3(0, 0, 0));
            Seed = rnd.Next();
        }

        public void Update(Vector3 position) {
            lock (callbackLock) {
                while (callbackQueue.Count > 0) {
                    var action = callbackQueue.Dequeue();
                    action.Invoke();
                }
            }
            
            Vector3i newPlayerChunk = new Vector3i((int)(Math.Ceiling(position.X / ChunkSize)), (int)(Math.Ceiling(position.Y / ChunkSize)), (int)(Math.Ceiling(position.Z / ChunkSize)));

            if(newPlayerChunk != playerChunk) {
                GenerateNearby(position);
            }
            playerChunk = newPlayerChunk;

        }

        public void Render(Vector3 position) {

            Vector3i ChunkPos = new Vector3i((int)position.X / ChunkSize, (int)position.Y / ChunkSize, (int)position.Z / ChunkSize);

            for(int x = ChunkPos.X - RenderDistance; x <= ChunkPos.X + RenderDistance; x++) {
                for (int y = ChunkPos.Y - RenderDistance; y <= ChunkPos.Y + RenderDistance; y++) {
                    for (int z = ChunkPos.Z - RenderDistance; z <= ChunkPos.Z + RenderDistance; z++) {
                        Vector3i temp = new Vector3i(x, y, z);
                        if (meshes.ContainsKey(temp) && meshes[temp].Loaded) {
                            meshes[temp].Render();
                        }
                    }
                }
            }
            
            /*
            foreach (var c in meshes) {
                if(c.Value.Loaded)
                    c.Value.Render();
            }
            */
            

        }

        private void GenerateNearby(Vector3 position) {

            Vector3i ChunkPos = new Vector3i((int)position.X / ChunkSize, (int)position.Y / ChunkSize, (int)position.Z / ChunkSize);

            for (int x = ChunkPos.X - GenerateDistance - 1; x <= ChunkPos.X + GenerateDistance + 1; x++) {
                for (int y = ChunkPos.Y - GenerateDistance - 1; y <= ChunkPos.Y + GenerateDistance + 1; y++) {
                //for (int y = -3; y <= 0; y++) {
                    for (int z = ChunkPos.Z - GenerateDistance - 1; z <= ChunkPos.Z + GenerateDistance + 1; z++) {
                        Vector3i temp = new Vector3i(x, y, z);
                        if (!blocks.ContainsKey(temp)) {
                            GenerateChunkStart(temp);
                        }
                    }
                }
            }

        }

        private void GenerateChunkStart(Vector3i chunk) {

            defBlocks(chunk);
            GenChannel.Writer.TryWrite(chunk);

        }

        private async Task GenerateTaskMethod() {
            while (await GenChannel.Reader.WaitToReadAsync()) {
                try {
                    Vector3i chunk = await GenChannel.Reader.ReadAsync();

                    Vector3i ChunkOffset = chunk * ChunkSize;
                    byte[,,] current = new byte[ChunkSize, ChunkSize, ChunkSize];
                    for (int x = 0; x < ChunkSize; x++) {
                        for (int z = 0; z < ChunkSize; z++) {
                            int GroundHeight = (int)((NoiseGenerator.ValueCoherentNoise3D((ChunkOffset.X + x) * 0.03, 0, (ChunkOffset.Z + z) * 0.03, Seed, NoiseQuality.Standard) - 1.1) * 20.0);
                            GroundHeight += (int)((NoiseGenerator.ValueCoherentNoise3D((ChunkOffset.X + x) * 0.1, 10, (ChunkOffset.Z + z) * 0.1, Seed, NoiseQuality.Standard)) * 2);

                            //GroundHeight += (int)((NoiseGenerator.ValueCoherentNoise3D((ChunkOffset.X + x) * 0.005, 60, (ChunkOffset.Z + z) * 0.005, Seed, NoiseQuality.Standard)) * 50);

                            for (int y = 0; y < ChunkSize; y++) {
                                int GlobalY = ChunkOffset.Y + y;

                                if (GlobalY > GroundHeight) {
                                    current[x, y, z] = 0;
                                } else if (GlobalY == GroundHeight) {
                                    current[x, y, z] = 1;
                                } else if (GlobalY >= GroundHeight - 3) {
                                    current[x, y, z] = 2;
                                } else {
                                    current[x, y, z] = 3;
                                }

                                var cave = NoiseGenerator.ValueCoherentNoise3D((ChunkOffset.X + x) * 0.01, (ChunkOffset.Y + y) * 0.01, (ChunkOffset.Z + z) * 0.01, Seed, NoiseQuality.Standard) * 0.4;
                                if (NoiseGenerator.ValueCoherentNoise3D((ChunkOffset.X + x) * 0.04, (ChunkOffset.Y + y) * (0.08 + (cave * 0.025)), (ChunkOffset.Z + z) * 0.04, Seed, NoiseQuality.Standard) > 0.8 + cave + Math.Max((ChunkOffset.Y + y) * 0.001, -0.3))
                                    current[x, y, z] = 0;

                            }
                        }
                    }
                    lock (callbackLock) {
                        callbackQueue.Enqueue(() => GenerateCallback.Invoke(current, chunk));
                    }
                } catch (ChannelClosedException ex) {
                    Console.WriteLine(ex.Message);
                }

            }
        }

        private async Task MeshTaskMethod() {
            while (await MeshChannel.Reader.WaitToReadAsync()) {
                try {
                    MeshGenerationJob data = await MeshChannel.Reader.ReadAsync();

                    int temp = 0;
                    foreach(var c in data.BlockData) {
                        temp += c;
                    }

                    List<float> vertices = new List<float>();
                    List<uint> indices = new List<uint>();

                    uint totalVertices = 0;

                    float cX = data.ChunkPos.X * ChunkSize;
                    float cY = data.ChunkPos.Y * ChunkSize;
                    float cZ = data.ChunkPos.Z * ChunkSize;

                    float uvSize = 0.0625f;


                    for (int x = 1; x <= ChunkSize; x++) {
                        for (int y = 1; y <= ChunkSize; y++) {
                            for (int z = 1; z <= ChunkSize; z++) {

                                if (data.BlockData[x, y, z] == 0) continue;

                                float bX = cX + x - 1;
                                float bY = cY + y - 1;
                                float bZ = cZ + z - 1;

                                var uvs = BlockTextures.UVs[BlockTextures.BlockIDs[data.BlockData[x, y, z]]];

                                // x-
                                if (data.BlockData[x - 1, y, z] == 0) {
                                    // Floats per vertex:
                                    // x, y, z, UVx, UVy
                                    vertices.AddRange(new float[] { bX - 0.5f, bY + 0.5f, bZ + 0.5f, -1f, 0f, 0f, uvs[Face.Left].X + uvSize, uvs[Face.Left].Y });
                                    vertices.AddRange(new float[] { bX - 0.5f, bY - 0.5f, bZ + 0.5f, -1f, 0f, 0f, uvs[Face.Left].X + uvSize, uvs[Face.Left].Y + uvSize });
                                    vertices.AddRange(new float[] { bX - 0.5f, bY - 0.5f, bZ - 0.5f, -1f, 0f, 0f, uvs[Face.Left].X, uvs[Face.Left].Y + uvSize });
                                    vertices.AddRange(new float[] { bX - 0.5f, bY + 0.5f, bZ - 0.5f, -1f, 0f, 0f, uvs[Face.Left].X, uvs[Face.Left].Y });

                                    indices.AddRange(new uint[] { 2 + totalVertices, 3 + totalVertices, 0 + totalVertices, 2 + totalVertices, 0 + totalVertices, 1 + totalVertices });
                                    totalVertices += 4;
                                }

                                // x+
                                if (data.BlockData[x + 1, y, z] == 0) {
                                    vertices.AddRange(new float[] { bX + 0.5f, bY + 0.5f, bZ - 0.5f, 1f, 0f, 0f, uvs[Face.Right].X, uvs[Face.Right].Y });
                                    vertices.AddRange(new float[] { bX + 0.5f, bY - 0.5f, bZ - 0.5f, 1f, 0f, 0f, uvs[Face.Right].X, uvs[Face.Right].Y + uvSize });
                                    vertices.AddRange(new float[] { bX + 0.5f, bY - 0.5f, bZ + 0.5f, 1f, 0f, 0f, uvs[Face.Right].X + uvSize, uvs[Face.Right].Y + uvSize });
                                    vertices.AddRange(new float[] { bX + 0.5f, bY + 0.5f, bZ + 0.5f, 1f, 0f, 0f, uvs[Face.Right].X + uvSize, uvs[Face.Right].Y });

                                    indices.AddRange(new uint[] { 0 + totalVertices, 1 + totalVertices, 2 + totalVertices, 0 + totalVertices, 2 + totalVertices, 3 + totalVertices });
                                    totalVertices += 4;
                                }

                                // z-
                                if (data.BlockData[x, y, z - 1] == 0) {
                                    vertices.AddRange(new float[] { bX - 0.5f, bY - 0.5f, bZ - 0.5f, 0f, 0f, -1f, uvs[Face.Back].X, uvs[Face.Back].Y + uvSize });
                                    vertices.AddRange(new float[] { bX - 0.5f, bY + 0.5f, bZ - 0.5f, 0f, 0f, -1f, uvs[Face.Back].X, uvs[Face.Back].Y });
                                    vertices.AddRange(new float[] { bX + 0.5f, bY + 0.5f, bZ - 0.5f, 0f, 0f, -1f, uvs[Face.Back].X + uvSize, uvs[Face.Back].Y });
                                    vertices.AddRange(new float[] { bX + 0.5f, bY - 0.5f, bZ - 0.5f, 0f, 0f, -1f, uvs[Face.Back].X + uvSize, uvs[Face.Back].Y + uvSize });

                                    indices.AddRange(new uint[] { 0 + totalVertices, 2 + totalVertices, 1 + totalVertices, 0 + totalVertices, 3 + totalVertices, 2 + totalVertices });
                                    totalVertices += 4;
                                }

                                // z+
                                if (data.BlockData[x, y, z + 1] == 0) {
                                    vertices.AddRange(new float[] { bX - 0.5f, bY - 0.5f, bZ + 0.5f, 0f, 0f, 1f, uvs[Face.Front].X, uvs[Face.Front].Y + uvSize });
                                    vertices.AddRange(new float[] { bX - 0.5f, bY + 0.5f, bZ + 0.5f, 0f, 0f, 1f, uvs[Face.Front].X, uvs[Face.Front].Y });
                                    vertices.AddRange(new float[] { bX + 0.5f, bY + 0.5f, bZ + 0.5f, 0f, 0f, 1f, uvs[Face.Front].X + uvSize, uvs[Face.Front].Y });
                                    vertices.AddRange(new float[] { bX + 0.5f, bY - 0.5f, bZ + 0.5f, 0f, 0f, 1f, uvs[Face.Front].X + uvSize, uvs[Face.Front].Y + uvSize });

                                    indices.AddRange(new uint[] { 0 + totalVertices, 1 + totalVertices, 2 + totalVertices, 0 + totalVertices, 2 + totalVertices, 3 + totalVertices });
                                    totalVertices += 4;
                                }

                                // y-
                                if (data.BlockData[x, y - 1, z] == 0) {
                                    vertices.AddRange(new float[] { bX - 0.5f, bY - 0.5f, bZ - 0.5f, 0f, -1f, 0f, uvs[Face.Bottom].X, uvs[Face.Bottom].Y });
                                    vertices.AddRange(new float[] { bX - 0.5f, bY - 0.5f, bZ + 0.5f, 0f, -1f, 0f, uvs[Face.Bottom].X, uvs[Face.Bottom].Y + uvSize });
                                    vertices.AddRange(new float[] { bX + 0.5f, bY - 0.5f, bZ + 0.5f, 0f, -1f, 0f, uvs[Face.Bottom].X + uvSize, uvs[Face.Bottom].Y + uvSize });
                                    vertices.AddRange(new float[] { bX + 0.5f, bY - 0.5f, bZ - 0.5f, 0f, -1f, 0f, uvs[Face.Bottom].X + uvSize, uvs[Face.Bottom].Y });

                                    indices.AddRange(new uint[] { 1 + totalVertices, 2 + totalVertices, 3 + totalVertices, 1 + totalVertices, 3 + totalVertices, 0 + totalVertices });
                                    totalVertices += 4;
                                }

                                // y+
                                if (data.BlockData[x, y + 1, z] == 0) {
                                    vertices.AddRange(new float[] { bX - 0.5f, bY + 0.5f, bZ + 0.5f, 0f, 1f, 0f, uvs[Face.Top].X, uvs[Face.Top].Y + uvSize });
                                    vertices.AddRange(new float[] { bX - 0.5f, bY + 0.5f, bZ - 0.5f, 0f, 1f, 0f, uvs[Face.Top].X, uvs[Face.Top].Y });
                                    vertices.AddRange(new float[] { bX + 0.5f, bY + 0.5f, bZ - 0.5f, 0f, 1f, 0f, uvs[Face.Top].X + uvSize, uvs[Face.Top].Y });
                                    vertices.AddRange(new float[] { bX + 0.5f, bY + 0.5f, bZ + 0.5f, 0f, 1f, 0f, uvs[Face.Top].X + uvSize, uvs[Face.Top].Y + uvSize });

                                    indices.AddRange(new uint[] { 0 + totalVertices, 2 + totalVertices, 3 + totalVertices, 0 + totalVertices, 1 + totalVertices, 2 + totalVertices });
                                    totalVertices += 4;
                                }

                            }
                        }
                    }

                    lock (callbackLock) {
                        callbackQueue.Enqueue(() => MeshCallback.Invoke(vertices.ToArray(), indices.ToArray(), data.ChunkPos));
                    }
                } catch (ChannelClosedException ex) {
                    Console.WriteLine(ex.Message);
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        // When chunk finishes generating, do the following for the current chunk as well as the 4 adjacent ones:
        // if the 4 adjacent chunks exist, attempt to create a mesh for the current chunk
        private void GenerateCallbackMethod(byte[,,] blocksArray, Vector3i chunk) {

            // Generate the chunk's blocks
            var current = blocks[chunk];
            for (int x = 0; x < ChunkSize; x++) {
                for (int y = 0; y < ChunkSize; y++) {
                    for (int z = 0; z < ChunkSize; z++) {
                        current[x, y, z] = blocksArray[x, y, z];
                    }
                }
            }

            // Make sure the correct chunks have meshes generated
            CreateMeshCheck(chunk);
        }

        private void CreateMeshCheck(Vector3i chunk, bool center = true) {
            if (IsChunkGenerated(chunk) &&
                IsChunkGenerated(chunk + new Vector3i(-1, 0, 0)) &&
                IsChunkGenerated(chunk + new Vector3i(1, 0, 0)) &&
                IsChunkGenerated(chunk + new Vector3i(0, -1, 0)) &&
                IsChunkGenerated(chunk + new Vector3i(0, 1, 0)) &&
                IsChunkGenerated(chunk + new Vector3i(0, 0, -1)) &&
                IsChunkGenerated(chunk + new Vector3i(0, 0, 1))) {
                if (!meshes.ContainsKey(chunk)) {
                    meshes[chunk] = new Mesh();
                    MeshChannel.Writer.TryWrite(new MeshGenerationJob(chunk));
                }
            }

            if (center) {
                CreateMeshCheck(chunk + new Vector3i(-1, 0, 0), false);
                CreateMeshCheck(chunk + new Vector3i(1, 0, 0), false);
                CreateMeshCheck(chunk + new Vector3i(0, -1, 0), false);
                CreateMeshCheck(chunk + new Vector3i(0, 1, 0), false);
                CreateMeshCheck(chunk + new Vector3i(0, 0, -1), false);
                CreateMeshCheck(chunk + new Vector3i(0, 0, 1), false);
            }
        }

        private void MeshCallbackMethod(float[] vertices, uint[] indices, Vector3i chunk) {
            if (meshes[chunk].Loaded) {
                meshes[chunk].Dispose();
            }
            meshes[chunk] = new Mesh(vertices, indices);

        }

        public void RunInBackgroundThenReturn<T>(Func<T> backgroundWork, Action<T> onMainThread) {
            Task.Run(() =>
            {
                T result = backgroundWork();

                lock (callbackLock) {
                    callbackQueue.Enqueue(() => onMainThread(result));
                }
            });
        }


        /// <summary>
        /// Ensure chunk exists in the blocks dictionary
        /// </summary>
        /// <param name="chunk">Chunk position</param>
        private void defBlocks(Vector3i chunk) {
            if (!blocks.ContainsKey(chunk)) {
                blocks[chunk] = new byte[ChunkSize, ChunkSize, ChunkSize];
                blocks[chunk][0, 0, 0] = 255;
            }
        }

        private bool IsChunkGenerated(Vector3i chunk) {
            return blocks.ContainsKey(chunk) && blocks[chunk][0, 0, 0] != 255;
        }

    }

    internal class MeshGenerationJob {

        static World w;
        public static void SetWorld(World world) {
            w = world;
        }

        public Vector3i ChunkPos;
        public byte[,,] BlockData;

        public MeshGenerationJob(Vector3i chunkPos) {
            BlockData = new byte[w.ChunkSize+2, w.ChunkSize+2, w.ChunkSize+2];
            ChunkPos = chunkPos;

            // Center blocks
            for (int x = 0; x < w.ChunkSize; x++) {
                for (int y = 0; y < w.ChunkSize; y++) {
                    for (int z = 0; z < w.ChunkSize; z++) {
                        BlockData[x + 1, y + 1, z + 1] = w.blocks[chunkPos][x, y, z];
                    }
                }
            }

            // x
            Vector3i nxChunk = chunkPos + new Vector3i(-1, 0, 0);
            Vector3i pxChunk = chunkPos + new Vector3i(1, 0, 0);
            for (int y = 0; y < w.ChunkSize; y++) {
                for (int z = 0; z < w.ChunkSize; z++) {
                    BlockData[0, y + 1, z + 1] = w.blocks[nxChunk][w.ChunkSize - 1, y, z];
                    BlockData[w.ChunkSize + 1, y + 1, z + 1] = w.blocks[pxChunk][0, y, z];
                }
            }

            // y
            Vector3i nyChunk = chunkPos + new Vector3i(0, -1, 0);
            Vector3i pyChunk = chunkPos + new Vector3i(0, 1, 0);
            for (int x = 0; x < w.ChunkSize; x++) {
                for (int z = 0; z < w.ChunkSize; z++) {
                    BlockData[x + 1, 0, z + 1] = w.blocks[nyChunk][x, w.ChunkSize - 1, z];
                    BlockData[x + 1, w.ChunkSize + 1, z + 1] = w.blocks[pyChunk][x, 0, z];
                }
            }

            // z
            Vector3i nzChunk = chunkPos + new Vector3i(0, 0, -1);
            Vector3i pzChunk = chunkPos + new Vector3i(0, 0, 1);
            for (int x = 0; x < w.ChunkSize; x++) {
                for (int y = 0; y < w.ChunkSize; y++) {
                    BlockData[x + 1, y + 1, 0] = w.blocks[nzChunk][x, y, w.ChunkSize - 1];
                    BlockData[x + 1, y + 1, w.ChunkSize + 1] = w.blocks[pzChunk][x, y, 0];
                }
            }

        }
    }
}
