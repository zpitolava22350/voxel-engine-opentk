using OpenTK.Graphics.OpenGL4;
using SharpNoise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelTK;

internal class Chunk {

    static int ChunkSize = 24;


    static Random rnd = new Random();

    float CaveScale = 0.1f;
    float HillScale = 0.04f;


    static Chunk() {

    }

    ushort[,,] blocks;

    bool mesh = false;

    int VAO;
    int VBO;
    int EBO;

    int xPos;
    int yPos;
    int zPos;

    int indicesCount;

    public Chunk(int x, int y, int z) {

        xPos = x;
        yPos = y;
        zPos = z;

    }

    public void Render() {
        GL.BindVertexArray(VAO);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        GL.DrawElements(BeginMode.Triangles, indicesCount, DrawElementsType.UnsignedInt, 0);
    }

    public void GenerateBlocks() {

        blocks = new ushort[ChunkSize, ChunkSize, ChunkSize];

        int xOff = xPos * ChunkSize;
        int yOff = yPos * ChunkSize;
        int zOff = zPos * ChunkSize;

        for (int x = 0; x < ChunkSize; x++) {
            for (int z = 0; z < ChunkSize; z++) {
                for (int y = 0; y < ChunkSize; y++) {

                    if(NoiseGenerator.ValueCoherentNoise3D((z + zOff) * HillScale, 0, (x + xOff) * HillScale) * 5 > (y + yOff)) {
                        if (NoiseGenerator.ValueCoherentNoise3D((x + xOff) * CaveScale, (y + yOff) * CaveScale, (z + zOff) * CaveScale) < 0.5f) {
                            blocks[x, y, z] = 1;
                        } else {
                            blocks[x, y, z] = 0;
                        }
                    } else {
                        blocks[x, y, z] = 0;
                    }
                    

                }
            }
        }

    }

    public void Dispose() {
        if(mesh) {
            mesh = false;
            GL.DeleteVertexArray(VAO);
            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(EBO);
        }
    }

    public void GenerateVertices() {

        Dispose();

        mesh = true;

        List<float> tempV = new List<float>();
        List<uint> tempI = new List<uint>();

        uint sumV = 0;

        int xOff = xPos * ChunkSize;
        int yOff = yPos * ChunkSize;
        int zOff = zPos * ChunkSize;

        for (int x = 0; x < ChunkSize; x++) {
            for (int z = 0; z < ChunkSize; z++) {
                for (int y = 0; y < ChunkSize; y++) {

                    if (blocks[x, y, z] == 0)
                        continue;

                    int x2 = x + xOff;
                    int y2 = y + yOff;
                    int z2 = z + zOff;

                    float lx = 0f;
                    float ly = 0.9f;
                    float hx = 0.1f;
                    float hy = 1f;

                    // x-
                    if(x == 0 || blocks[x - 1, y, z] == 0) {
                        tempV.AddRange(new float[] {
                            // Positions                     // UVs
                            x2 - 0.5f, y2 + 0.5f, z2 - 0.5f,  lx, hy,
                            x2 - 0.5f, y2 - 0.5f, z2 - 0.5f,  lx, ly,
                            x2 - 0.5f, y2 - 0.5f, z2 + 0.5f,  hx, ly,
                            x2 - 0.5f, y2 + 0.5f, z2 + 0.5f,  hx, hy
                        });

                        tempI.AddRange(new uint[] {
                            0 + sumV, 1 + sumV, 2 + sumV,
                            0 + sumV, 2 + sumV, 3 + sumV
                        });

                        sumV += 4;
                    }

                    // x+
                    if (x == ChunkSize - 1 || blocks[x + 1, y, z] == 0) {
                        tempV.AddRange(new float[] {
                            // Positions                     // UVs
                            x2 + 0.5f, y2 + 0.5f, z2 + 0.5f,  lx, hy,
                            x2 + 0.5f, y2 - 0.5f, z2 + 0.5f,  lx, ly,
                            x2 + 0.5f, y2 - 0.5f, z2 - 0.5f,  hx, ly,
                            x2 + 0.5f, y2 + 0.5f, z2 - 0.5f,  hx, hy
                        });

                        tempI.AddRange(new uint[] {
                            0 + sumV, 1 + sumV, 2 + sumV,
                            0 + sumV, 2 + sumV, 3 + sumV
                        });

                        sumV += 4;
                    }

                    // y-
                    if (y == 0 || blocks[x, y - 1, z] == 0) {
                        tempV.AddRange(new float[] {
                            // Positions                     // UVs
                            x2 + 0.5f, y2 - 0.5f, z2 - 0.5f,  lx, hy,
                            x2 + 0.5f, y2 - 0.5f, z2 + 0.5f,  lx, ly,
                            x2 - 0.5f, y2 - 0.5f, z2 + 0.5f,  hx, ly,
                            x2 - 0.5f, y2 - 0.5f, z2 - 0.5f,  hx, hy
                        });

                        tempI.AddRange(new uint[] {
                            0 + sumV, 1 + sumV, 2 + sumV,
                            0 + sumV, 2 + sumV, 3 + sumV
                        });

                        sumV += 4;
                    }

                    // y+
                    if (y == ChunkSize - 1 || blocks[x, y + 1, z] == 0) {
                        tempV.AddRange(new float[] {
                            // Positions                     // UVs
                            x2 + 0.5f, y2 + 0.5f, z2 + 0.5f,  lx, hy,
                            x2 + 0.5f, y2 + 0.5f, z2 - 0.5f,  lx, ly,
                            x2 - 0.5f, y2 + 0.5f, z2 - 0.5f,  hx, ly,
                            x2 - 0.5f, y2 + 0.5f, z2 + 0.5f,  hx, hy
                        });

                        tempI.AddRange(new uint[] {
                            0 + sumV, 1 + sumV, 2 + sumV,
                            0 + sumV, 2 + sumV, 3 + sumV
                        });

                        sumV += 4;
                    }

                    // z-
                    if (z == 0 || blocks[x, y, z - 1] == 0) {
                        tempV.AddRange(new float[] {
                            // Positions                     // UVs
                            x2 + 0.5f, y2 + 0.5f, z2 - 0.5f,  lx, hy,
                            x2 + 0.5f, y2 - 0.5f, z2 - 0.5f,  lx, ly,
                            x2 - 0.5f, y2 - 0.5f, z2 - 0.5f,  hx, ly,
                            x2 - 0.5f, y2 + 0.5f, z2 - 0.5f,  hx, hy
                        });

                        tempI.AddRange(new uint[] {
                            0 + sumV, 1 + sumV, 2 + sumV,
                            0 + sumV, 2 + sumV, 3 + sumV
                        });

                        sumV += 4;
                    }

                    // z+
                    if (z == ChunkSize - 1 || blocks[x, y, z + 1] == 0) {
                        tempV.AddRange(new float[] {
                            // Positions                     // UVs
                            x2 - 0.5f, y2 + 0.5f, z2 + 0.5f,  lx, hy,
                            x2 - 0.5f, y2 - 0.5f, z2 + 0.5f,  lx, ly,
                            x2 + 0.5f, y2 - 0.5f, z2 + 0.5f,  hx, ly,
                            x2 + 0.5f, y2 + 0.5f, z2 + 0.5f,  hx, hy
                        });

                        tempI.AddRange(new uint[] {
                            0 + sumV, 1 + sumV, 2 + sumV,
                            0 + sumV, 2 + sumV, 3 + sumV
                        });

                        sumV += 4;
                    }

                }
            }
        }

        float[] vertices = tempV.ToArray();
        uint[] indices = tempI.ToArray();

        indicesCount = indices.Length;

        VAO = GL.GenVertexArray();
        GL.BindVertexArray(VAO);

        VBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * 4, vertices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 20, 0);
        GL.EnableVertexArrayAttrib(VAO, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 20, 12); // 5 * sizeof(float), 3 * sizeof(float)
        GL.EnableVertexArrayAttrib(VAO, 1);

        EBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * 4, indices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

        GL.BindVertexArray(0);

    }

}