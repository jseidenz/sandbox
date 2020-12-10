using System.Collections.Generic;

[System.Diagnostics.DebuggerDisplay("{ToString()}")]
public struct Hash : System.IComparable<Hash>, System.IEquatable<Hash>
{
    static Dictionary<int, string> m_strings = new Dictionary<int, string>();
    public int m_value;

    public Hash(string str)
    {

        m_value = SDBMLower(str);
    }

    public bool IsValid()
    {
        return m_value != 0;
    }

    public int CompareTo(Hash other)
    {
        return m_value - other.m_value;
    }

    public int CompareTo(object other)
    {
        return m_value - ((Hash)other).m_value;
    }

    public override bool Equals(object other)
    {
        return m_value == ((Hash)other).m_value;
    }

    public bool Equals(Hash other)
    {
        return m_value == other.m_value;
    }

    public override int GetHashCode()
    {
        return m_value;
    }

    public static implicit operator Hash(string str)
    {
        return new Hash(str);
    }

    public override string ToString()
    {
        if(m_strings.TryGetValue(m_value, out var str))
        {
            return str;
        }

        throw new System.Exception($"Could not find string for hash {m_value}");
    }

    public static bool operator !=(Hash a, Hash b)
    {
        return a.m_value != b.m_value;
    }

    public static bool operator ==(Hash a, Hash b)
    {
        return a.m_value == b.m_value;
    }

    public static int SDBMLower(string s)
    {
        int hash = 0;
        for (int i = 0; i < s.Length; ++i)
        {
            char c = System.Char.ToLower(s[i]);
            hash = c + (hash << 6) + (hash << 16) - hash;
        }

        if(m_strings.TryGetValue(hash, out var existing_str))
        {
            if(existing_str != s)
            {
                throw new System.Exception($"Hash collision between {s} and {existing_str}");
            }
        }
        else
        {
            m_strings[hash] = s;
        }

        m_strings[hash] = s;

        return hash;
    }
}