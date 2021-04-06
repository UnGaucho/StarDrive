﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace SDGameTextToEnum
{
    public struct Token
    {
        public int Index;
        public string Text;
    }
    public sealed class LocalizationFile
    {
        public List<Token> TokenList;
        public IEnumerable<TextToken> GetTokens(string lang) => TokenList.Select(t => new TextToken(lang, t.Index, null, t.Text));
    }
    public sealed class ToolTip
    {
        public int TIP_ID;   // Serialized from: Tooltips.xml
        public int Data;     // Serialized from: Tooltips.xml
        public string Title; // Serialized from: Tooltips.xml
    }
    public sealed class Tooltips
    {
        public List<ToolTip> ToolTipsList;
        public IEnumerable<TextToken> GetTokens(string lang)
            => ToolTipsList.Select(t 
                => new TextToken(lang, t.TIP_ID, null, t.Title){ ToolTipData = t.Data });
    }
    
    /// <summary>
    /// Converts StarDrive GameText into C# enums
    /// </summary>
    public static class GameTextToEnum_Main
    {
        static bool UseYAMLFileAsSource = false;

        static T Deserialize<T>(string path)
        {
            var ser = new XmlSerializer(typeof(T));
            return (T)ser.Deserialize(File.OpenRead(path));
        }

        static IEnumerable<TextToken> GetGameText(string lang, string path)
        {
            Log.Write(ConsoleColor.Cyan, $"GetGameText: {lang} {path}");
            return Deserialize<LocalizationFile>(path).GetTokens(lang);
        }

        static IEnumerable<TextToken> GetToolTips(string lang, string path)
        {
            Log.Write(ConsoleColor.Cyan, $"GetToolTips: {lang} {path}");
            return Deserialize<Tooltips>(path).GetTokens(lang);
        }

        struct TextDatabases
        {
            public LocalizationDB Game; // all game localizations
            public ModLocalizationDB Mod; // all mod localizations
        }

        static TextDatabases CreateGameTextEnum(string contentDir, string modDir, string outputDir)
        {
            string enumFile = $"{outputDir}/GameText.cs";
            string yamlFile = $"{contentDir}/GameText.yaml";
            var gen = new LocalizationDB("Ship_Game", "GameText");
            gen.LoadIdentifiers(enumFile, yamlFile);
            if (UseYAMLFileAsSource)
            {
                if (gen.AddFromYaml(yamlFile))
                {
                    gen.AddFromYaml($"{contentDir}/GameText.Missing.RUS.yaml", logMerge:true);
                    gen.AddFromYaml($"{contentDir}/GameText.Missing.SPA.yaml", logMerge:true);
                }
            }
            if (gen.NumLocalizations == 0)
            {
                gen.AddLocalizations(GetGameText("ENG", $"{contentDir}/Localization/English/GameText_EN.xml"));
                gen.AddLocalizations(GetGameText("RUS", $"{contentDir}/Localization/Russian/GameText_RU.xml"));
                gen.AddLocalizations(GetGameText("SPA", $"{contentDir}/Localization/Spanish/GameText.xml"));
            }
            gen.ExportCsharp(enumFile);
            gen.ExportYaml(yamlFile);
            gen.ExportMissingTranslationsYaml("RUS", $"{contentDir}/GameText.Missing.RUS.yaml");
            gen.ExportMissingTranslationsYaml("SPA", $"{contentDir}/GameText.Missing.SPA.yaml");

            ModLocalizationDB mod = null;
            if (Directory.Exists(modDir))
            {
                mod = new ModLocalizationDB(gen, "ModGameText");
                if (UseYAMLFileAsSource)
                {
                    if (mod.AddFromModYaml($"{modDir}/GameText.yaml"))
                    {
                        mod.AddFromModYaml($"{modDir}/GameText.Missing.RUS.yaml", logMerge:true);
                        mod.AddFromModYaml($"{modDir}/GameText.Missing.SPA.yaml", logMerge:true);
                    }
                }
                if (mod.NumModLocalizations == 0)
                {
                    mod.AddModLocalizations(GetGameText("ENG", $"{modDir}/Localization/English/GameText_EN.xml"));
                    mod.AddModLocalizations(GetGameText("RUS", $"{modDir}/Localization/Russian/GameText_RU.xml"));
                }
                mod.FinalizeModLocalization();
                mod.ExportModYaml($"{modDir}/GameText.yaml");
                mod.ExportMissingModYaml("RUS", $"{modDir}/GameText.Missing.RUS.yaml");
                mod.ExportMissingModYaml("SPA", $"{modDir}/GameText.Missing.SPA.yaml");
            }
            return new TextDatabases{ Game = gen, Mod = mod };
        }

        // Tooltips is mostly a hack, because we don't use half of the EnumGenerator features
        static void CreateGameTipsEnum(string contentDir, string outputDir, LocalizationDB db)
        {
            string enumFile = $"{outputDir}/GameTips.cs";
            string yamlFile = $"{contentDir}/ToolTips.yaml";
            var gen = new LocalizationDB(db, "GameTips");
            gen.LoadIdentifiers(enumFile, yamlFile);
            if (UseYAMLFileAsSource)
            {
                gen.AddToolTips(TextToken.FromYaml(yamlFile));
            }
            if (gen.NumToolTips == 0)
            {
                gen.AddToolTips(GetToolTips("ANY", $"{contentDir}/Tooltips/Tooltips.xml"));
            }
            gen.ExportCsharp(enumFile);
            gen.ExportTipsYaml(yamlFile);

            // no tooltips for Mods
        }

        static void UpgradeGameXmls(string contentDir, LocalizationDB db)
        {
            UpgradeXmls(db, $"{contentDir}/Buildings", 
                             "NameTranslationIndex", "DescriptionIndex", "ShortDescriptionIndex");
        }

        static void UpgradeXmls(LocalizationDB db, string contentFolder, params string[] tags)
        {
            string[] xmls = Directory.GetFiles(contentFolder, "*.xml");
            foreach (string xmlFile in xmls)
                UpgradeXml(db, xmlFile, tags);
        }

        static void UpgradeXml(LocalizationDB db, string xmlFile, string[] tags)
        {
            if (!File.Exists(xmlFile))
                return;

            Log.Write(ConsoleColor.Blue, $"Upgrading XML Localizations: {xmlFile}");
            string[] lines = File.ReadAllLines(xmlFile);
            int modified = 0;
            Regex[] patterns = tags.Select(tag => new Regex($"<{tag}>.+\\d+.+<\\/{tag}>")).ToArray();
            Regex numberMatcher = new Regex($"\\d+");
            for (int i = 0; i < lines.Length; ++i)
            {
                string line = lines[i];
                foreach (Regex pattern in patterns)
                {
                    if (pattern.Match(line).Success)
                    {
                        // replace number with the new id
                        int id = int.Parse(numberMatcher.Match(line).Value);
                        string nameId = db.GetNameId(id);
                        string replacement = numberMatcher.Replace(line, nameId);
                        Log.Write(ConsoleColor.Cyan, $"replace {id} => {nameId}");
                        ++modified;
                        lines[i] = replacement;
                        break;
                    }
                }
            }

            if (modified > 0)
            {
                //Log.Write(ConsoleColor.Green, $"Modified {modified} entries");
                //File.WriteAllLines(xmlFile, lines);
            }
        }

        public static void Main(string[] args)
        {
            string workingDir = Directory.GetCurrentDirectory();
            string contentDir = $"{workingDir}/Content";
            string outputDir = $"{workingDir}/Ship_Game/Data";
            string modDir = $"{workingDir}/StarDrive/Mods/Combined Arms";
            if (!Directory.Exists(contentDir) || !Directory.Exists(outputDir))
            {
                Log.Write(ConsoleColor.Red, "WorkingDir must be BlackBox code directory with Content and Ship_Game/Data folders!");
            }
            else
            {
                TextDatabases dbs = CreateGameTextEnum(contentDir, modDir, outputDir);
                CreateGameTipsEnum(contentDir, outputDir, dbs.Game);
                UpgradeGameXmls(contentDir, dbs.Game);
                UpgradeGameXmls(modDir, dbs.Mod);
            }

            Log.Write(ConsoleColor.Gray, "Press any key to continue...");
            Console.ReadKey(false);
        }
    }

    public static class Log
    {
        public static void Write(ConsoleColor color, string message)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = original;
        }
    }
}
