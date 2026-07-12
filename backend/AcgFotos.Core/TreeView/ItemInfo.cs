namespace AcgFotos.Core.TreeView
{
    public class ItemInfo
    {
        public long Id { get; set; }

        public string IconClass { get; set; }

        public string Nombre { get; set; }

        public string Description { get; set; }

        public long? ParentId { get; set; }

        public string Info { get; set; }

        public bool ExternalLink { get; set; }
    }
}
