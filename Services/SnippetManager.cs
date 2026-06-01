using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using TeritamaLauncher.Models;

namespace TeritamaLauncher.Services
{
    public class SnippetManager
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TeritamaLauncher");
        
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
                        RootNodes.Clear();
                        if (data.Snippets != null)
                        {
                            foreach (var node in data.Snippets)
                            {
                                RootNodes.Add(node);
                            }
                        }
                        Config = data.Config ?? new AppConfig();
                        return;
                    }
                }
                
                CreateDefaultData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load data: {ex.Message}");
                try
                {
                    if (File.Exists(FilePath))
                    {
                        string bakPath = FilePath + ".bak";
                        File.Copy(FilePath, bakPath, true);
                        System.Diagnostics.Debug.WriteLine($"Corrupted data backed up to: {bakPath}");
                    }
                }
                catch (Exception backupEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to create backup: {backupEx.Message}");
                }
                CreateDefaultData();
            }
        }

        /// <summary>
        /// データを保存します。保存に成功した場合は true を、失敗した場合は false を返します。
        /// </summary>
        public bool Save()
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
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save data: {ex.Message}");
                return false;
            }
        }

        public void Export(string path)
        {
            try
            {
                var data = CreateStorageData();
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to export data: {ex.Message}");
                throw;
            }
        }

        public void Import(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<StorageData>(json);
                if (data != null)
                {
                    RootNodes.Clear();
                    if (data.Snippets != null)
                    {
                        foreach (var node in data.Snippets)
                        {
                            RootNodes.Add(node);
                        }
                    }
                    Config = data.Config ?? new AppConfig();
                    if (!Save())
                    {
                        throw new IOException("インポート後のデータ保存に失敗しました。");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to import data: {ex.Message}");
                throw;
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
            RootNodes.Clear();
            var toolFolder = new SnippetNode { Title = "便利ツール", Type = NodeType.Folder };
            toolFolder.Children.Add(new SnippetNode { Title = "メモ帳", Content = "notepad.exe", Type = NodeType.Snippet });
            toolFolder.Children.Add(new SnippetNode { Title = "電卓", Content = "calc.exe", Type = NodeType.Snippet });

            RootNodes.Add(toolFolder);
            RootNodes.Add(new SnippetNode { Title = "Google (ウェブサイト)", Content = "https://google.com", Type = NodeType.Snippet });

            Config = new AppConfig();
            Save();
        }
    }
}
