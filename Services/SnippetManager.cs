using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using SuikaTextExpander.Models;

namespace SuikaTextExpander.Services
{
    public class SnippetManager
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SuikaTextExpander");
        
        private static readonly string FilePath = Path.Combine(AppDataPath, "data.json");

        public ObservableCollection<SnippetNode> RootNodes { get; private set; } = new ObservableCollection<SnippetNode>();
        public AppConfig Config { get; private set; } = new AppConfig();

        public void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var data = JsonConvert.DeserializeObject<StorageData>(json);
                    if (data != null)
                    {
                        RootNodes = new ObservableCollection<SnippetNode>(data.Snippets ?? new List<SnippetNode>());
                        Config = data.Config ?? new AppConfig();
                        return;
                    }
                }
                
                CreateDefaultData();
            }
            catch
            {
                CreateDefaultData();
            }
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }

                var data = CreateStorageData();
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save data: {ex.Message}");
            }
        }

        public void Export(string path)
        {
            var data = CreateStorageData();
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public void Import(string path)
        {
            var json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<StorageData>(json);
            if (data != null)
            {
                RootNodes = new ObservableCollection<SnippetNode>(data.Snippets ?? new List<SnippetNode>());
                Config = data.Config ?? new AppConfig();
                Save();
            }
        }

        private StorageData CreateStorageData()
        {
            return new StorageData
            {
                Config = this.Config,
                Snippets = new List<SnippetNode>(this.RootNodes)
            };
        }

        private void CreateDefaultData()
        {
            RootNodes = new ObservableCollection<SnippetNode>
            {
                new SnippetNode { Title = "挨拶", Type = NodeType.Folder },
                new SnippetNode { Title = "署名", Content = "---\nSuika Text Expander User", Type = NodeType.Snippet }
            };
            RootNodes[0].Children.Add(new SnippetNode { Title = "お疲れ様です", Content = "お疲れ様です。Suikaです。", Type = NodeType.Snippet });
            RootNodes[0].Children.Add(new SnippetNode { Title = "よろしくお願いします", Content = "よろしくお願いいたします。", Type = NodeType.Snippet });

            Config = new AppConfig();
            Save();
        }
    }

    public class StorageData
    {
        public AppConfig Config { get; set; } = new AppConfig();
        public List<SnippetNode> Snippets { get; set; } = new List<SnippetNode>();
    }
}
