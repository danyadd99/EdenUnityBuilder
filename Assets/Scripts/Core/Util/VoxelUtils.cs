using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper class for working with voxels.
/// </summary>
public class VoxelUtils
{

    public static VoxelUtils I; // Instance

    private World _world;

    public VoxelUtils(World w)
    {
        I = this;
        _world = w;
    }
    /// <summary>
    /// Set block. Don't use this if you want to create many blocks in one go, it can be slow.
    /// </summary>
    public void SetBlock(int x, int y, int z, BlockType blocktype, bool refreshChunk)
    {
        _world.SetBlock(x, y, z, blocktype);
        if (refreshChunk)
        {
            Chunk chunk = _world.FindChunk(x, y, z);
            chunk.RefreshAsync();
            chunk.SetDirty();
            _world.RefreshNearChunks(chunk, x, y, z);
        }
    }

    /// <summary>
    /// Set block. Don't use this if you want to create many blocks in one go, it can be slow.
    /// </summary>
    public void SetBlock(Vector3 pos, BlockType blocktype, bool refreshChunk)
    {
        SetBlock((int)pos.x, (int)pos.y, (int)pos.z, blocktype, refreshChunk);
    }

    /// <summary>
    /// Returns a blocktype at these coordinates.
    /// </summary>
    public BlockType GetBlockType(int x, int y, int z)
    {
        return _world.GetBlock(x, y, z).BlockType;
    }

    /// <summary>
    /// Returns a blocktype at these coordinates.
    /// </summary>
    public BlockType GetBlockType(Vector3 pos)
    {
        return _world.GetBlock((int)pos.x, (int)pos.y, (int)pos.z).BlockType;
    }

    /// <summary>
    /// Returns a block at these coordinates. You will receive ALL information about the block. Use GetBlockType() to get only block type.
    /// </summary>
    public Block GetBlock(int x, int y, int z)
    {
        return _world.GetBlock(x, y, z);
    }

    /// <summary>
    /// Returns a block at these coordinates. You will receive ALL information about the block. Use GetBlockType() to get only block type.
    /// </summary>
    public Block GetBlock(Vector3 pos)
    {
        return _world.GetBlock((int)pos.x, (int)pos.y, (int)pos.z);
    }

    /// <summary>
    /// Creates a ray that return the block
    /// </summary>
    public Block Raycast(Vector3 start, Vector3 direction, float maxDistance)
    {
        RaycastHit hit;
        bool isHit = Physics.Raycast(start, direction, out hit, maxDistance);
        if (isHit)
        {
            return _world.GetBlock(hit.point);
        }
        else
        {
            return new Block(BlockType.Air);
        }
    }
}
