namespace DjvuApp.Controls
{
    public struct SelectionMarker
    {
        public uint PageNumber { get; }

        public uint Index { get; }
        
        public SelectionMarker(uint pageNumber, uint index)
        {
            PageNumber = pageNumber;
            Index = index;
        }
    }
}