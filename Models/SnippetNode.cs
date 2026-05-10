using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace SuikaTextExpander.Models
{
    public enum NodeType
    {
        Snippet,
        Folder,
        Separator
    }

    public partial class SnippetNode : ObservableObject
    {
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _content = string.Empty;

        [ObservableProperty]
        private NodeType _type = NodeType.Snippet;

        public ObservableCollection<SnippetNode> Children { get; set; } = new ObservableCollection<SnippetNode>();

        // For TreeView binding and easy identification
        [JsonIgnore]
        public bool IsFolder => Type == NodeType.Folder;
        [JsonIgnore]
        public bool IsSnippet => Type == NodeType.Snippet;
        [JsonIgnore]
        public bool IsSeparator => Type == NodeType.Separator;
    }
}
