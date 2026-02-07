using GorillaAvatarCatalog.Models;
using GorillaAvatarCatalog.Tools;
using GorillaNetworking;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace GorillaAvatarCatalog.Behaviours
{
    internal class AvatarPreferences : MonoBehaviour
    {
        public static AvatarPreferences Instance;

        private readonly string _avatarPath = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "Avatars");

        private JsonSerializerSettings serializeSettings, deserializeSettings;

        private readonly string _legacyPath = Path.Combine(BepInEx.Paths.ConfigPath, $"Avatars ({Constants.Name}).json");

        private readonly Dictionary<int, PlayerAvatar> _avatarFiles = [];

        public async void Awake()
        {
            Instance = this;

            serializeSettings = new JsonSerializerSettings
            {
                //TypeNameHandling = TypeNameHandling.All,
                //CheckAdditionalContent = true,
                Formatting = Formatting.Indented
            };

            deserializeSettings = new JsonSerializerSettings
            {
                //TypeNameHandling = TypeNameHandling.All
            };

            if (!Directory.Exists(_avatarPath)) Directory.CreateDirectory(_avatarPath);

            if (File.Exists(_legacyPath))
            {
                string text = await File.ReadAllTextAsync(_legacyPath);

                foreach (var (key, value) in JsonConvert.DeserializeObject<Dictionary<string, object>>(text, deserializeSettings))
                {
                    Logging.Error(key[7..]);
                    if (key.StartsWith("Avatar") && int.TryParse(key[7..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                    {
                        string serializedText = JsonConvert.SerializeObject(value, serializeSettings);

                        List<string> lines = [];

                        using StringReader reader = new(serializedText);

                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains("$type")) continue;
                            lines.Add(line);
                        }

                        await File.WriteAllTextAsync(Path.Combine(_avatarPath, $"{result}.json"), string.Join('\n', lines));
                    }
                }

                File.Delete(_legacyPath);
            }

            string[] filePathArray = Directory.GetFiles(_avatarPath, "*.json", SearchOption.AllDirectories);

            foreach (string path in filePathArray)
            {
                if (int.TryParse(Path.GetFileNameWithoutExtension(path), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                {
                    string text = await File.ReadAllTextAsync(path);
                    PlayerAvatar avatar = JsonConvert.DeserializeObject<PlayerAvatar>(text, deserializeSettings);
                    if (avatar != null) _avatarFiles.AddOrUpdate(result, avatar);
                }
            }
        }

        public PlayerAvatar GetAvatar(int index) => _avatarFiles.TryGetValue(index, out PlayerAvatar avatar) ? avatar : null;

        public bool HasAvatar(int index) => _avatarFiles.ContainsKey(index);

        public void DeleteAvatar(int index) => SetAvatar(index, null);

        public void SetAvatar(int index, PlayerAvatar avatar)
        {
            string path = Path.Combine(_avatarPath, $"{index}.json");

            if (avatar == null)
            {
                _avatarFiles.Remove(index);
                if (File.Exists(path)) File.Delete(path);
                return;
            }

            if (_avatarFiles.ContainsKey(index)) _avatarFiles[index] = avatar;
            else _avatarFiles.Add(index, avatar);

            File.WriteAllText(path, JsonConvert.SerializeObject(avatar, serializeSettings));
        }
    }
}
