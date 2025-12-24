using System.Collections.ObjectModel;
using Swallows.Core.Models;

namespace Swallows.Core.Services;

public static class StructureBuilder
{
    public static ObservableCollection<StructureNode> BuildTree(IEnumerable<string> urls)
    {
        // Minimal stub to return a root node
        return new ObservableCollection<StructureNode>
        {
            new StructureNode { Name = "Root", IsDirectory = true, PageCount = urls.Count() }
        };
    }
}
