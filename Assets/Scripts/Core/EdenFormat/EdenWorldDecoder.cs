using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Loading/Converting and Saving .eden files (File Format for Eden World Builder)
/// </summary>
public class EdenWorldDecoder : MonoBehaviour // Based on https://mrob.com/pub/vidgames/eden-file-format.html
{

    public string Name;

    public Dictionary<int, Vector2Int> Chunks;

    //public static int skyColor;
    public static string worldName;

    [HideInInspector]
    public byte[] Bytes;

    private World world;

    public static EdenWorldDecoder Instance;

    public string CurrentPathWorld;

    public Vector4 WorldArea;

    public WorldFileHeader Header;

    void Start()
    {
        Instance = this;
        world = World.Instance;
    }

    [Serializable]
    public class WorldFileHeader // Header of world file
    {
        // This part is used from 1.1.1 to New Dawn versions
        public int level_seed;
        public Vector3 pos = new Vector3(0, 0, 0);
        public Vector3 home = new Vector3(0, 0, 0);
        public float yaw;
        public ulong directory_offset;
        public char[] name = new char[50];

        // Not used because wrong values ​​are shown idk how to fix
        public int version;
        public char[] hash = new char[36];
        public byte[] skycolors = new byte[16];
        public int goldencubes;
    }

    public void LoadWorld(string path)
    {
        CurrentPathWorld = path;
        //List<int> skyColors = new List<int>();
        world.Name = Path.GetFileName(CurrentPathWorld);

        Header = new WorldFileHeader();
        Stream streamHeader = File.Open(path, FileMode.Open);
        using (BinaryReader reader = new BinaryReader(streamHeader))
        {
            Header.level_seed = reader.ReadInt32();
            Header.pos.x = reader.ReadSingle();
            Header.pos.y = reader.ReadSingle();
            Header.pos.z = reader.ReadSingle();

            Header.home.x = reader.ReadSingle();
            Header.home.y = reader.ReadSingle();
            Header.home.z = reader.ReadSingle();

            Header.yaw = reader.ReadSingle();

            Header.directory_offset = reader.ReadUInt32();

            Header.name = reader.ReadChars(50);

            //Header.version = reader.ReadInt32();

            //Header.hash = reader.ReadChars(36);

            //Header.skycolors = reader.ReadBytes(16);

            //Header.goldencubes = reader.ReadInt32();
            streamHeader.Close();
        }

        using (FileStream stream = File.Open(path, FileMode.Open))
        {
            Bytes = new byte[stream.Length];
            stream.Read(Bytes, 0, Bytes.Length);
            stream.Close();
        }

        // Get Sky Color
        /*
        for (int i = 132; i <= 148; i++)
        {
            if (bytes[i] != 14) skyColors.Add(bytes[i]);
        }

        if (skyColors.Count == 0) skyColor = 14;
        skyColor = skyColors.GroupBy(v => v).OrderByDescending(g => g.Count()).First().Key;

        SkyManager.Instance.Set((Paintings)skyColor);
        SkyManager.Instance.FastUpdateSky();
        */
        int chunkPointerStartIndex = Bytes[35] * 256 * 256 * 256 + Bytes[34] * 256 * 256 + Bytes[33] * 256 + Bytes[32];

        byte[] nameArray = Bytes.TakeWhile((b, i) => ((i < 40 || b != 0) && i <= 75)).ToArray();
        worldName = Encoding.ASCII.GetString(nameArray, 40, nameArray.Length - 40);
        Vector4 worldArea = new Vector4(0, 0, 0, 0);
        Dictionary<int, Vector2Int> chunks = new Dictionary<int, Vector2Int>();
        // create array of chunk points and addresses
        int currentChunkPointerIndex = chunkPointerStartIndex;
        do
        {
            chunks.Add(
                Bytes[currentChunkPointerIndex + 11] * 256 * 256 * 256 + Bytes[currentChunkPointerIndex + 10] * 256 * 256 + Bytes[currentChunkPointerIndex + 9] * 256 + Bytes[currentChunkPointerIndex + 8],// address
                new Vector2Int(Bytes[currentChunkPointerIndex + 1] * 256 + Bytes[currentChunkPointerIndex], Bytes[currentChunkPointerIndex + 5] * 256 + Bytes[currentChunkPointerIndex + 4])); // position
        } while ((currentChunkPointerIndex += 16) < Bytes.Length);

        // get max size of the world
        worldArea.x = chunks.Values.Min(p => p.x);
        worldArea.y = chunks.Values.Min(p => p.y);
        worldArea.z = chunks.Values.Max(p => p.x) - worldArea.x + 1;
        worldArea.w = chunks.Values.Max(p => p.y) - worldArea.y + 1;
        Chunks = chunks;
        WorldArea = worldArea;
    }

    public bool HasChunk(Vector2Int Pos)
    {
        ;
        Vector2Int ConvertedPosNew = new Vector2Int((Pos.y / 16) + (int)WorldArea.x, (Pos.x / 16) + (int)WorldArea.y);
        if (Chunks.ContainsValue(ConvertedPosNew))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public Vector2Int ConvertToOldPosition(Vector2Int Pos)
    {
        return new Vector2Int((Pos.y / 16) + (int)WorldArea.x, (Pos.x / 16) + (int)WorldArea.y);
    }

    public Vector2Int ConvertToNewPosition(Vector2Int Pos)
    {
        return new Vector2Int((Pos.y * 16) - (int)WorldArea.y * 16, (Pos.x * 16) - (int)WorldArea.x * 16);
    }

    public Vector2 ConvertToNewPositionFloat(Vector2Int Pos)
    {
        return new Vector2((Pos.y * 16) - (int)WorldArea.y * 16, (Pos.x * 16) - (int)WorldArea.x * 16);
    }

    public Vector3 ConvertToNewPosition3(Vector2Int Pos)
    {
        return new Vector3((Pos.y * 16) - (int)WorldArea.y * 16, 0, (Pos.x * 16) - (int)WorldArea.x * 16);
    }

    public void LoadChunk(Vector3 Pos)
    {
        //  foreach (int address in Chunks.Keys)
        if (HasChunk(new Vector2Int((int)Pos.x, (int)Pos.z)))
        {
            Vector2Int chunk = ConvertToOldPosition(new Vector2Int((int)Pos.x, (int)Pos.z));
            int address = Chunks.Single(s => s.Value == chunk).Key;

            int globalChunkPosX = chunk.x;

            int globalChunkPosY = chunk.y;

            var realChunkPosX = (globalChunkPosX * 16) * 100;

            var realChunkPosY = (globalChunkPosY * 16) * 100;

            var baseX = (chunk.x - WorldArea.x) * 16;

            var baseY = (chunk.y - WorldArea.y) * 16;

            for (int BaseHeight = 0; BaseHeight < 4; BaseHeight++)
            {

                if (world.ChunkExists(new Vector3((int)baseX, 16 * BaseHeight, (int)baseY)) && world.FindChunk(new Vector3((int)baseX, 16 * BaseHeight, (int)baseY)).isConverted == false)
                {
                    Chunk c = world.CreateChunk(new Vector3((int)baseX, 16 * BaseHeight, (int)baseY));
                    c.InitData();
                    for (int x = 0; x < 16; x++)
                    {
                        for (int z = 0; z < 16; z++)
                        {
                            for (int y = 0; y < 16; y++)
                            {
                                var id = Bytes[address + BaseHeight * 8192 + x * 256 + y * 16 + z];
                                var color = Bytes[address + BaseHeight * 8192 + x * 256 + y * 16 + z + 4096];
                                var RealX = (x + (globalChunkPosX * 16));
                                var RealY = (y + (globalChunkPosY * 16));
                                var RealZ = (z + (16 * BaseHeight));

                                var Position = new Vector3Int(RealY - (int)WorldArea.y * 16, RealZ, RealX - (int)WorldArea.x * 16);

                                //Block spawn
                                c.SetBlock(x, z, y, (BlockType)id);
                                c.SetColor(x, z, y, (Paintings)color);
                            }
                        }
                    }

                    if (c != null && c.isConverted == false)
                    {
                        c.isConverted = true;
                        c.isDirty = true;
                        c.RefreshAsync();
                    }
                }
            }
        }
    }

    // This does not work as it requires chunk pointers which are not used in my version. I'm not sure how to fix this yet.
    // I think it's worth looking into this https://mrob.com/pub/vidgames/eden-file-format.html
    public void SaveWorld(string path)
    {
        CurrentPathWorld = path;
        if (!path.Contains(".eden")) // protection to prevent accidental overwriting of other files
        {
            Debug.Log("Wrong path for save: " + path);
            return;
        }

        foreach (int address in Chunks.Keys)
        {
            Vector2Int chunk = Chunks[address];
            var baseX = (chunk.x - WorldArea.x) * 16;

            var baseY = (chunk.y - WorldArea.y) * 16;
            //Vector2Int pos;
            //  Chunks.TryGetValue(address,out )
            for (int BaseHeight = 0; BaseHeight < 4; BaseHeight++)
            {
                Vector3 posChunk = new Vector3((int)ConvertToNewPositionFloat(chunk).y, (int)(BaseHeight * 16), (int)ConvertToNewPositionFloat(chunk).x);
                Chunk chunkForSave = world.FindChunk(posChunk);

                if (chunkForSave != null)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        for (int z = 0; z < 16; z++)
                        {
                            for (int y = 0; y < 16; y++)
                            {
                                Bytes[address + BaseHeight * 8192 + x * 256 + y * 16 + z] = (byte)chunkForSave.GetBlock(x, z, y).BlockType;
                                Bytes[address + BaseHeight * 8192 + x * 256 + y * 16 + z + 4096] = (byte)chunkForSave.GetBlock(x, z, y).Painting;
                            }
                        }
                    }
                }
            }
        }

        if (File.Exists(path))
        {
            File.Delete(path);
        }


        using (FileStream stream = new FileStream(path, FileMode.CreateNew))
        {
            //Save File with changed world name or original world name
            byte[] name = Encoding.ASCII.GetBytes(worldName);
            for (int i = 0; i < Bytes.Length; i++)
            {
                if (i >= 40 && i <= (75))
                {
                    if (i - 40 < name.Length)
                    {
                        stream.WriteByte(name[i - 40]);
                    }
                    else
                    {
                        stream.WriteByte(0);
                    }

                }
                else
                {
                    stream.WriteByte(Bytes[i]);
                }

            }
        }
    }

}
