﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ship_Game.Tools.Localization
{
    public partial class LocalizationDB
    {
        readonly string Namespace;
        readonly string Name;
        readonly LocUsageDB Usages;
        readonly Dictionary<int, TextToken> ExistingIds = new Dictionary<int, TextToken>();
        readonly List<LocText> LocalizedText = new List<LocText>();
        readonly List<LocText> ModText = new List<LocText>();
        readonly HashSet<string> EnumNames = new HashSet<string>();
        readonly string[] WordSeparators = { " ", "\t", "\r", "\n", "\"",
                                                 "\\t","\\r","\\n", "\\\"" };

        public string Prefix;
        public string ModPrefix;

        public int NumModLocalizations => ModText.Count;
        public int NumLocalizations => LocalizedText.Count;

        public LocalizationDB(string enumNamespace, string enumName, string gameContent, string modContent)
        {
            Namespace = enumNamespace;
            Name = enumName;
            Prefix = "BB";
            ModPrefix = MakeModPrefix(modContent);
            Usages = new LocUsageDB(gameContent, modContent, Prefix, ModPrefix);
        }

        public LocalizationDB(LocalizationDB gen, string newName) // copy
        {
            Namespace = gen.Namespace;
            Name = newName;
            Usages = gen.Usages;
            ExistingIds = new Dictionary<int, TextToken>(gen.ExistingIds);
            EnumNames = new HashSet<string>(gen.EnumNames);
            foreach (LocText loc in gen.LocalizedText)
                LocalizedText.Add(new LocText(loc));
        }
        
        static string MakeModPrefix(string modDir)
        {
            if (modDir.IsEmpty())
                return "";
            string dir = Path.GetDirectoryName(modDir);
            if (modDir.Last() != '/' && modDir.Last() != '\\')
                dir = Path.GetFileName(modDir);
            string[] words = dir.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", words.Select(word => char.ToUpper(word[0])));
        }

        // Load existing identifiers
        // First from the Csharp enum file, then supplement with entries from yaml file
        public void LoadIdentifiers(string enumFile, string yamlFile)
        {
            ExistingIds.Clear();
            EnumNames.Clear();

            if (File.Exists(enumFile))
                ReadIdentifiersFromCsharp(enumFile);

            if (File.Exists(yamlFile))
                ReadIdentifiersFromYaml(yamlFile);
        }

        void ReadIdentifiersFromCsharp(string enumFile)
        {
            string fileName = Path.GetFileName(enumFile);
            List<TextToken> tokens = TextToken.FromCSharp(enumFile);
            foreach (TextToken t in tokens)
            {
                if (ExistingIds.TryGetValue(t.Id, out TextToken e))
                    Log.Write(ConsoleColor.Red, $"{Name} ID CONFLICT:"
                                               +$"\n  existing at {fileName}: {e.NameId} = {t.Id}"
                                               +$"\n  addition at {fileName}: {t.NameId} = {t.Id}");
                else
                    ExistingIds.Add(t.Id, t);
            }
        }

        void ReadIdentifiersFromYaml(string yamlFile)
        {
            List<TextToken> tokens = TextToken.FromYaml(yamlFile);
            foreach (TextToken token in tokens)
                if (!ExistingIds.ContainsKey(token.Id))
                    ExistingIds.Add(token.Id, token);
        }

        string GetCapitalizedIdentifier(string word)
        {
            var sb = new StringBuilder();
            foreach (char c in word)
            {
                if (char.IsLetter(c))
                {
                    sb.Append(sb.Length == 0 ? char.ToUpper(c) : char.ToLower(c));
                }
            }
            return sb.ToString();
        }

        string CreateNameId(string nameIdPrefix, int id, string[] words)
        {
            string name = nameIdPrefix + "_" ?? "";
            if (ExistingIds.TryGetValue(id, out TextToken existing) &&
                !string.IsNullOrWhiteSpace(existing.NameId))
            {
                name = existing.NameId;
            }
            else
            {
                int maxWords = 5;
                for (int i = 0; i < maxWords && i < words.Length; ++i)
                {
                    string identifier = GetCapitalizedIdentifier(words[i]);
                    if (identifier == "") // it was some invalid token like " + "
                    {
                        ++maxWords; // discount this word
                        continue;
                    }
                    name += identifier;
                }
            }

            if (string.IsNullOrWhiteSpace(name))
                return "";

            if (EnumNames.Contains(name))
            {
                for (int suffix = 2; suffix < 100; ++suffix)
                {
                    if (!EnumNames.Contains(name + suffix))
                    {
                        name = name + suffix;
                        break;
                    }
                }
            }
            return name;
        }

        protected LocText AddNewLocalization(List<LocText> localizations, 
                                                  TextToken token, string nameIdPrefix)
        {
            string[] words = token.Text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
            const int maxCommentWords = 10;
            string comment = "";

            for (int i = 0; i < maxCommentWords && i < words.Length; ++i)
            {
                comment += words[i];
                if (i != (words.Length - 1) && i != (maxCommentWords - 1))
                    comment += " ";
            }

            if (Usages.Contains(token.Id))
                token.NameId = nameIdPrefix + "_" + Usages.Get(token.Id).NameId;
            else if (string.IsNullOrEmpty(token.NameId))
                token.NameId = CreateNameId(nameIdPrefix, token.Id, words);

            if (!string.IsNullOrEmpty(token.NameId))
            {
                EnumNames.Add(token.NameId);
                var loc = new LocText(token, comment);
                localizations.Add(loc);
                return loc;
            }
            else
            {
                Log.Write(ConsoleColor.Yellow, 
                    $"{Name}: skipping empty enum entry {token.Lang} {token.NameId} {token.Id}: '{token.Text}'");
                return null;
            }
        }

        protected bool GetLocalization(List<LocText> localizedText, int id, out LocText loc)
        {
            loc = localizedText.FirstOrDefault(x => x.Id == id);
            return loc != null;
        }

        public bool AddFromYaml(string yamlFile, bool logMerge = false)
        {
            List<TextToken> tokens = TextToken.FromYaml(yamlFile);
            if (tokens.Count == 0)
                return false;
            AddLocalizations(tokens, logMerge:logMerge);
            return true;
        }

        protected void AddLocalization(List<LocText> localizations, TextToken token, string nameIdPrefix, bool logMerge)
        {
            if (string.IsNullOrEmpty(token.Text))
                return;

            if (GetLocalization(localizations, token.Id, out LocText loc))
            {
                if (logMerge)
                    Log.Write(ConsoleColor.Green, $"Merged {token.Lang} {token.Id}: {token.Text}");
                loc.AddTranslation(new Translation(token.Id, token.Lang, token.Text));
            }
            else
            {
                AddNewLocalization(localizations, token, nameIdPrefix);
            }
        }

        public void AddLocalizations(IEnumerable<TextToken> localizations, bool logMerge = false)
        {
            foreach (TextToken token in localizations)
            {
                AddLocalization(LocalizedText, token, Prefix, logMerge);
            }
        }
        
        public string GetModNameId(int id)
        {
            if (GetLocalization(ModText, id, out LocText mod))
                return mod.NameId;
            return GetNameId(id);
        }

        public string GetNameId(int id)
        {
            if (GetLocalization(LocalizedText, id, out LocText loc))
                return loc.NameId;

            Log.Write(ConsoleColor.Red, $"{Name}: failed to find tooltip data with id={id}");
            return id.ToString();
        }

        public bool AddFromModYaml(string yamlFile, bool logMerge = false)
        {
            List<TextToken> tokens = TextToken.FromYaml(yamlFile);
            if (tokens.Count == 0)
                return false;
            AddModLocalizations(tokens, logMerge);
            return true;
        }

        public void AddModLocalizations(IEnumerable<TextToken> localizations, bool logMerge = false)
        {
            // build ModTexts
            var uniqueToMod = new List<TextToken>();
            foreach (TextToken token in localizations)
            {
                if (GetLocalization(LocalizedText, token.Id, out LocText vanilla))
                    token.NameId = vanilla.NameId; // keep NameId from vanilla
                else
                    uniqueToMod.Add(token); // this is unique to the mod
                AddLocalization(ModText, token, ModPrefix, logMerge);
            }
        }

        public void FinalizeModLocalization()
        {
            // add in missing translations
            foreach (LocText mod in ModText)
            {
                if (GetLocalization(LocalizedText, mod.Id, out LocText vanilla))
                {
                    foreach (Translation tr in vanilla.Translations)
                        if (!mod.HasLang(tr.Lang))
                            mod.AddTranslation(tr);
                }
            }

            // NOTE: not really worth it actually
            bool shouldRemoveDuplicates = false;
            if (shouldRemoveDuplicates)
            {
                // then remove ModTexts which are complete duplicates from vanilla
                int numRemoved = ModText.RemoveAll(mod =>
                {
                    LocText dup = LocalizedText.FirstOrDefault(vanilla => vanilla.Equals(mod));
                    if (dup == null)
                        return false;
                    Log.Write(ConsoleColor.Gray, $"{Name}: remove duplicate {mod.Id} {mod.NameId}"
                                                +$"\n  mod: {mod.Translations[0].Text}"
                                                +$"\n  dup: {dup.Translations[0].Text}");
                    return true;
                });
                if (numRemoved > 0)
                    Log.Write(ConsoleColor.Gray, $"{Name}: removed {numRemoved} text entries that already matched vanilla text");
            }
        }
    }
}