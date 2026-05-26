namespace RageNAdrenaline.Data;

/// <summary>
/// Extension methods for hashing.
/// </summary>
public static class HashExtensions
{
    /// <summary>
    /// Returns a stable hash code for a string (for Valheim).
    /// </summary>
    /// <param name="str">string to be hashed</param>
    /// <returns>hashed string</returns>
    public static int GetStableHashCode(this string str)
    {
        if (str == null) return 0;

        unchecked
        {
            var hash1 = 5381;
            var hash2 = hash1;
            
            for (var i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                
                if (i + 1 >= str.Length)
                    break;

                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }
}