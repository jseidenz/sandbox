using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;
using System.IO;
struct ChunkInfo
{
    public int m_version_number;
    public int m_positon;
}

public class ChunkSerializer
{
    public ChunkSerializer(int max_chunks = 100)
    {
        m_max_chunks = max_chunks;

        m_memory_stream = new MemoryStream();
        m_writer = new BinaryWriter(m_memory_stream);

        int placeholder_chunk_count = 0;
        m_writer.Write(placeholder_chunk_count);
        for(int i = 0; i < max_chunks; ++i)
        {
            var placerholder_chunk_id = 0;
            var placeholder_chunk_offset = 0;
            var placerholder_chunk_version = 0;
            m_writer.Write(placerholder_chunk_id);
            m_writer.Write(placeholder_chunk_offset);
            m_writer.Write(placerholder_chunk_version);
        }
    }

    public BinaryWriter BeginChunk(Hash chunk_id, int version_number = 0)
    {
        if(m_chunks.Count == m_max_chunks)
        {
            throw new Exception($"Too many chunks. MaxChunks={m_max_chunks}");
        }

        if(m_current_chunk.IsValid())
        {
            throw new Exception($"Need to call EndChunk on chunk {m_current_chunk} before calling BeginChunk again.");
        }

        if(m_chunks.ContainsKey(chunk_id))
        {
            throw new Exception($"Chunk '{chunk_id}' already exists.");
        }

        m_chunks[chunk_id] = new ChunkInfo
        {
            m_positon = (int)m_writer.BaseStream.Position,
            m_version_number = version_number
        };

        m_current_chunk = chunk_id;

        return m_writer;
    }

    public void EndChunk()
    {
        if(!m_current_chunk.IsValid())
        {
            throw new Exception("Called EndChunk when no chunk was valid.");
        }

        m_current_chunk = default;
    }

    public MemoryStream Finalize()
    {
        m_memory_stream.Position = 0;

        m_memory_stream.Seek(0, SeekOrigin.Begin);

        m_writer.Write(m_chunks.Count);
        foreach(var kvp in m_chunks)
        {
            m_writer.Write(kvp.Key.m_value);
            m_writer.Write(kvp.Value.m_positon);
            m_writer.Write(kvp.Value.m_version_number);
        }

        return m_memory_stream;
    }

    Dictionary<Hash, ChunkInfo> m_chunks = new Dictionary<Hash, ChunkInfo>();
    MemoryStream m_memory_stream;
    BinaryWriter m_writer;
    Hash m_current_chunk;
    int m_max_chunks;
}

public class ChunkDeserializer
{
    public ChunkDeserializer(MemoryStream stream)
    {
        m_reader = new BinaryReader(stream);
        int chunk_count = m_reader.ReadInt32();
        for(int i = 0; i < chunk_count; ++i)
        {
            var chunk_id = m_reader.ReadInt32();
            var chunk_pos = m_reader.ReadInt32();
            var chunk_version = m_reader.ReadInt32();

            m_chunks[chunk_id] = new ChunkInfo
            {
                m_positon = chunk_pos,
                m_version_number = chunk_version
            };
        }
    }

    public bool TryGetChunk(Hash chunk_id, out BinaryReader reader)
    {
        if(m_chunks.TryGetValue(chunk_id.m_value, out var chunk_info))
        {
            m_reader.BaseStream.Position = chunk_info.m_positon;
            reader = m_reader;
            return true;
        }
        else
        {
            reader = default;
            return false;
        }
    }

    Dictionary<int, ChunkInfo> m_chunks = new Dictionary<int, ChunkInfo>();

    BinaryReader m_reader;
    MemoryStream m_memory_stream;
}