using System;
using System.Collections.Generic;
using System.Linq;

namespace AcgFotos.Core.TreeView
{
    public class HierarchicalManager
    {
        public List<HierarchicalItem<long>> GetItems(List<ItemInfo> sourceList)
        {
            var targetList = new List<HierarchicalItem<long>>();

            var rootItems = sourceList.Where(x => x.ParentId == null).ToList();

            foreach (var item in rootItems)
            {
                var rootItem = CreateItem(item, false);
                targetList.Add(rootItem);

                this.AddItemToParent(rootItem, sourceList);
            }

            return targetList;
        }
        private void AddItemToParent(HierarchicalItem<long> parentItem, List<ItemInfo> sourceList)
        {

            var childItems = sourceList.Where(x => x.ParentId == parentItem.Id).ToList();

            if (childItems != null && childItems.Count > 0)
            {
                foreach (var item in childItems)
                {
                    var hierarchicalItem = CreateItem(item, false);
                    parentItem.Children.Add(hierarchicalItem);

                    this.AddItemToParent(hierarchicalItem, sourceList);
                }
            }
        }

        private static HierarchicalItem<long> CreateItem(ItemInfo child, bool selected)
        {
            var newItem = new HierarchicalItem<long>
            {
                Id = child.Id,
                Name = child.Nombre,
                IconClass = child.IconClass,
                Info = child.Info,
                IsExpanded = true,
                IsSelected = selected,
                ExternalLink = child.ExternalLink,
                Children = new List<HierarchicalItem<long>>()
            };

            return newItem;
        }
    }
}
