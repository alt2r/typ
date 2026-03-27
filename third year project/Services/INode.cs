using System.Collections.Generic;

namespace third_year_project.Services
{
    //interface for sandbox pages nodes, which are used to build the tree diagrams in the sandbox page
    public interface INode
    {
        List<INode> GetChildren();
        int GetTotalChildrenCount();
    }
}