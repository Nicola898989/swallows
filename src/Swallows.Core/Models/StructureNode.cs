using System.Collections.ObjectModel;

namespace Swallows.Core.Models;

public class StructureNode
{
    public string Name { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public ObservableCollection<StructureNode> Children { get; set; } = new();
    public int PageCount { get; set; }
}
