using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FriendlyTurretCoverage
{
    /// <summary>
    /// The mod's own UI strings, translated with the game's language setting.
    ///
    /// Files use the game's <c>Language/&lt;language&gt;_main.txt</c> format: entries are
    /// "<c>NNN text|</c>", lines starting with <c>#</c> are comments, and <c>&lt;par1&gt;</c>,
    /// <c>&lt;par2&gt;</c>... are replaced with values. They live in the plugin's
    /// <c>Language</c> folder and are named after <see cref="Lang.GetLanguageName"/>
    /// (english, german, chinese...). Unlike the game, entries are looked up by their number,
    /// not their position, so a translation may leave entries out; missing entries fall back to
    /// the English text built into the DLL.
    /// </summary>
    internal static class Loc
    {
        // The on-screen toggles
        public const int TurretCoverage = 0;
        // One per damage type, in DamageKind order: Beam + (int)kind
        public const int Beam = 1;
        public const int Kinetic = 2;
        public const int Missile = 3;
        public const int PointDefence = 4;
        public const int Repair = 5;
        public const int ColorByType = 6;

        private const string EnglishResource = "FriendlyTurretCoverage.Language.english_main.txt";

        private static Dictionary<int, string> english;
        private static Dictionary<int, string> current;
        private static int loadedLanguage = -1;

        public static string Get(int code)
        {
            Validate();
            if (current.TryGetValue(code, out string text) || english.TryGetValue(code, out text))
            {
                return text;
            }
            return "LANG. ERROR on [" + Plugin.Name + "] " + code;
        }

        public static string Get(int code, params object[] pars)
        {
            string text = Get(code);
            for (int i = 0; i < pars.Length; i++)
            {
                text = text.Replace("<par" + (i + 1) + ">", pars[i]?.ToString() ?? "");
            }
            return text;
        }

        /// <summary>Reloads the translation if the game language changed since the last load.</summary>
        public static void Validate()
        {
            int language = Lang.current;
            if (language == loadedLanguage && current != null)
            {
                return;
            }
            loadedLanguage = language;
            if (english == null)
            {
                english = LoadEnglish();
            }
            string name = Lang.GetLanguageName(language);
            current = name == "english" ? english : LoadFile(name) ?? english;
        }

        private static Dictionary<int, string> LoadEnglish()
        {
            // The English text is embedded so the mod still works if the Language folder is missing.
            // Entries in a file on disk override it, which lets people fix wording without rebuilding.
            Dictionary<int, string> entries;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EnglishResource))
            using (var reader = new StreamReader(stream))
            {
                entries = Parse(reader.ReadToEnd());
            }
            Dictionary<int, string> onDisk = LoadFile("english");
            if (onDisk != null)
            {
                foreach (KeyValuePair<int, string> entry in onDisk)
                {
                    entries[entry.Key] = entry.Value;
                }
            }
            return entries;
        }

        private static Dictionary<int, string> LoadFile(string languageName)
        {
            string dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Language");
            if (!Directory.Exists(dir))
            {
                return null;
            }
            // The game ships "Japanese_main.txt" but asks for "japanese", so match case-insensitively
            // (Linux file systems are case-sensitive).
            string wanted = languageName + "_main.txt";
            string path = Directory.GetFiles(dir, "*_main.txt")
                .FirstOrDefault(f => string.Equals(Path.GetFileName(f), wanted, StringComparison.OrdinalIgnoreCase));
            if (path == null)
            {
                return null;
            }
            try
            {
                return Parse(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not read translation {path}: {e.Message}");
                return null;
            }
        }

        private static Dictionary<int, string> Parse(string contents)
        {
            var entries = new Dictionary<int, string>();
            foreach (string raw in contents.Split('|'))
            {
                string entry = raw.TrimStart('﻿', '\r', '\n');
                if (entry.Length == 0 || entry[0] == '#' || entry[0] == '[')
                {
                    continue;
                }
                int digits = 0;
                while (digits < entry.Length && char.IsDigit(entry[digits]))
                {
                    digits++;
                }
                if (digits == 0 || !int.TryParse(entry.Substring(0, digits), out int code))
                {
                    continue;
                }
                // "NNN text": skip the single separator space after the number.
                entries[code] = digits < entry.Length && entry[digits] == ' ' ? entry.Substring(digits + 1) : entry.Substring(digits);
            }
            return entries;
        }
    }
}
