using System.Collections.Generic;

namespace TeritamaLauncher.Models
{
    public class StorageData
    {
        public AppConfig Config { get; set; } = new AppConfig();
        public List<SnippetNode> Snippets { get; set; } = new List<SnippetNode>();
    }
}
