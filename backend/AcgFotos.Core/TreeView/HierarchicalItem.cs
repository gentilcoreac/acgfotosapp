using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Core.TreeView
{
    public class HierarchicalItem<T>
    {
        public T Id { get; set; }

        public string Name { get; set; }

        public string IconClass { get; set; }

        public virtual List<HierarchicalItem<T>> Children { get; set; }

        public bool IsExpanded { get; set; }

        public bool IsSelected { get; set; }

        public bool ExternalLink { get; set; }

        public string Info { get; set; }

        public HierarchicalItem()
        {
            this.Children = new List<HierarchicalItem<T>>();
        }
    }
}
