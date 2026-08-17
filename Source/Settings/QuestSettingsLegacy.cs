using System;
using System.Security.Cryptography;
using System.Text;

namespace Ustas.RimAI.Quests;

/// <summary>Legacy English default detection. No Verse types.</summary>
public static class QuestSettingsLegacy
{
    public const string UnchangedLegacyDefaultHash =
        "EA578CB442CB6F7675B5815147B859CAD25A69D1362A15F0D068AC2BAEB6E36D";

    public static bool IsUnchangedLegacyDefault(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        string normalized = value.Replace("\r\n", "\n").Trim();
        using (var sha = SHA256.Create())
        {
            string hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(normalized))).Replace("-", "");
            return string.Equals(hash, UnchangedLegacyDefaultHash, StringComparison.Ordinal);
        }
    }
}
