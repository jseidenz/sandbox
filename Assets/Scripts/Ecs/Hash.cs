using System;
using System.Collections.Generic;

[System.Diagnostics.DebuggerDisplay("{ToString()}")]
public struct Hash : IComparable<Hash>, IEquatable<Hash>
{
    public int m_hash;
    static Dictionary<int, string> m_hash_to_strings = new Dictionary<int, string>();

    public Hash(string str)
    {
        if (str == null) { throw new Exception("String is null."); }

        int hash = 0;
        for (int i = 0; i < str.Length; ++i)
        {
            char c = Char.ToLower(str[i]);
            hash = c + (hash << 6) + (hash << 16) - hash;
        }

        m_hash = hash;

        m_hash_to_strings[hash] = str;
    }

    public bool IsValid()
    {
        return m_hash != 0;
    }

    public int CompareTo(Hash other)
    {
        return m_hash - other.m_hash;
    }

    public int CompareTo(object other)
    {
        return m_hash - ((Hash)other).m_hash;
    }

    public override bool Equals(object other)
    {
        return m_hash == ((Hash)other).m_hash;
    }

    public bool Equals(Hash other)
    {
        return m_hash == other.m_hash;
    }

    public override int GetHashCode()
    {
        return m_hash;
    }

    public static implicit operator Hash(string str)
    {
        return new Hash(str);
    }

    public override string ToString()
    {
        if(m_hash_to_strings.TryGetValue(m_hash, out var str))
        {
            return str;
        }

        throw new Exception($"Could not find string for hash {m_hash}");
    }

    public static bool operator !=(Hash a, Hash b)
    {
        return a.m_hash != b.m_hash;
    }

    public static bool operator ==(Hash a, Hash b)
    {
        return a.m_hash == b.m_hash;
    }
}