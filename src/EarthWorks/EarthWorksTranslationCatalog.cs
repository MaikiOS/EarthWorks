using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;

namespace OstrixMods.EarthWorks
{
    internal static class EarthWorksTranslationCatalog
    {
        private const string ResourcePrefix = "OstrixMods.EarthWorks.Translations.";

        internal static Dictionary<string, string> LoadBuiltIn(string language)
        {
            string resourceName = ResourcePrefix + language + ".translations.json";
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Missing localization resource " + resourceName);
                }
                using (StreamReader reader = new StreamReader(stream))
                {
                    return Parse(reader.ReadToEnd(), resourceName);
                }
            }
        }

        internal static void ApplyExternalOverrides(
            string language,
            Dictionary<string, string> translations)
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(
                directory ?? string.Empty,
                "Translations",
                "EarthWorks",
                language,
                "translations.json");
            if (!File.Exists(path))
            {
                return;
            }
            foreach (KeyValuePair<string, string> entry in Parse(File.ReadAllText(path), path))
            {
                translations[entry.Key] = entry.Value;
            }
        }

        internal static Dictionary<string, string> Parse(string json, string source)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, string>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            Dictionary<string, string> result;
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                result = serializer.ReadObject(stream) as Dictionary<string, string>;
            }
            if (result == null || result.Count == 0)
            {
                throw new InvalidOperationException("No translations found in " + source);
            }
            return result;
        }
    }
}
