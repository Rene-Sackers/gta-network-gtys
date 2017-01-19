using GTANetworkShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace GoTruckYourself.Server.Models
{
    [Serializable]
    public class Config
    {
        public List<SpawnPoint> SpawnPoints { get; set; }

        public Config()
        {
            SpawnPoints = new List<SpawnPoint>();
        }

        private static XmlSerializer GetSerializer()
        {
            return new XmlSerializer(typeof(Config));
        }

        public void Save(string configPath)
        {
            Main.Log("Saving config.");

            try
            {
                using (var writer = File.CreateText(configPath))
                {
                    GetSerializer().Serialize(writer, this);
                    writer.Flush();
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                Main.Log("Failed to save config: " + ex);
            }
        }

        private static Config CreateNewConfig(string configPath)
        {
            var emptyConfig = new Config();
            emptyConfig.Save(configPath);

            return emptyConfig;
        }

        public static Config LoadConfig(string configPath)
        {
            Main.Log("Loading config: " + configPath);

            if (!File.Exists(configPath)) return CreateNewConfig(configPath);

            using (var reader = File.OpenRead(configPath))
            {
                try
                {
                    return (Config)GetSerializer().Deserialize(reader) ?? CreateNewConfig(configPath);
                }
                catch (Exception ex)
                {
                    Main.Log("Failed to load config: " + ex);
                }
            }

            return CreateNewConfig(configPath);
        }
    }
}
