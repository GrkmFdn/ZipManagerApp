using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace ZipApp.Services
{
    [DataContract]
    public class AppSettings
    {
        [DataMember]
        public string LastDirectoryPath { get; set; }

        [DataMember]
        public List<string> RecentPaths { get; set; } = new List<string>();
        
        [DataMember]
        public Dictionary<string, List<string>> DirectorySelections { get; set; } = new Dictionary<string, List<string>>();
    }

    public class PersistenceService
    {
        private readonly string _configPath;

        public PersistenceService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, "ZipApp");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            _configPath = Path.Combine(appFolder, "settings.json");
        }

        public void Save(AppSettings settings)
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                using (var stream = new FileStream(_configPath, FileMode.Create))
                {
                    serializer.WriteObject(stream, settings);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save Error: {ex.Message}");
            }
        }

        public AppSettings Load()
        {
            if (!File.Exists(_configPath))
                return new AppSettings();

            try
            {
                var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                using (var stream = new FileStream(_configPath, FileMode.Open))
                {
                    return (AppSettings)serializer.ReadObject(stream);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load Error: {ex.Message}");
                return new AppSettings();
            }
        }
    }
}
