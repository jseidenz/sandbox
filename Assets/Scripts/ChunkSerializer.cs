﻿using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;

struct ChunkInfo
{
    public int m_position;
    public int m_version_number;    
}

public class ChunkSerializer
{
    public ChunkSerializer(int max_chunks = 100)
    {
        m_max_chunks = max_chunks;

        m_buffer = new byte[50 * 1024 * 1024];

        var placeholdedr_major_version = 0;
        var placeholder_minor_version = 0;
        var placeholder_chunk_count = 0;

        Write(placeholdedr_major_version);
        Write(placeholder_minor_version);
        Write(placeholder_chunk_count);


        for(int i = 0; i < max_chunks; ++i)
        {
            var placerholder_chunk_id = 0;
            var placerholder_chunk_position = 0;
            var placerholder_chunk_version = 0;

            Write(placerholder_chunk_id);
            Write(placerholder_chunk_position);
            Write(placerholder_chunk_version);
        }
    }

    public void BeginChunk(Hash chunk_id, int version_number = 0)
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
            m_position = m_position,
            m_version_number = version_number
        };

        m_current_chunk = chunk_id;
    }

    public void EndChunk()
    {
        if(!m_current_chunk.IsValid())
        {
            throw new Exception("Called EndChunk when no chunk was valid.");
        }

        m_current_chunk = default;
    }

    public void Write(int data)
    {
        var data_length_in_bytes = 4;

        if (m_position + data_length_in_bytes > m_buffer.Length)
        {
            throw new Exception($"No more room in buffer. m_position={m_position}, BufferLength={m_buffer.Length}, data_length_in_bytes={data_length_in_bytes}");
        }

        unsafe
        {
            fixed (byte* my_bytes = &m_buffer[m_position])
            {
                var my_int = (int*)my_bytes;

                *my_int = data;
            }
        }

        m_position += data_length_in_bytes;
        m_length += data_length_in_bytes;
    }

    public void Write(float[] data)
    {
        var data_length_in_floats = data.Length;
        var data_length_in_bytes = 4 * data_length_in_floats;

        if(m_position + data_length_in_bytes > m_buffer.Length)
        {
            throw new Exception($"No more room in buffer. m_position={m_position}, BufferLength={m_buffer.Length}, data_length_in_bytes={data_length_in_bytes}");
        }

        unsafe
        {
            fixed(byte* my_bytes = &m_buffer[m_position])
            {
                fixed(float* data_floats = data)
                {
                    var my_floats = (float*)my_bytes;

                    for(int i = 0; i < data.Length; ++i)
                    {
                        my_floats[i] = data_floats[i];
                    }
                }
            }
        }

        m_position += data_length_in_bytes;
        m_length += data_length_in_bytes;
    }

    public void Finalize(out byte[] buffer, out int length)
    { 
        m_position = 0;

        var placeholder_major_version = 0;
        var placeholder_minor_version = 0;

        Write(placeholder_major_version);
        Write(placeholder_minor_version);
        Write(m_chunks.Count);

        foreach(var kvp in m_chunks)
        {
            Write(kvp.Key.m_value);
            Write(kvp.Value.m_position);
            Write(kvp.Value.m_version_number);
        }

        m_position = 0;

        buffer = m_buffer;
        length = m_length;
    }

    Dictionary<Hash, ChunkInfo> m_chunks = new Dictionary<Hash, ChunkInfo>();
    Hash m_current_chunk;
    int m_max_chunks;
    int m_position;
    int m_length;
    byte[] m_buffer;
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
                m_position = chunk_pos,
                m_version_number = chunk_version
            };
        }
    }

    public bool TryGetChunk(Hash chunk_id, out BinaryReader reader)
    {
        if(m_chunks.TryGetValue(chunk_id.m_value, out var chunk_info))
        {
            m_reader.BaseStream.Position = chunk_info.m_position;
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