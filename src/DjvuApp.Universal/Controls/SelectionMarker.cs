using System;

namespace DjvuApp.Controls
{
    public struct SelectionMarker : IComparable<SelectionMarker>
    {
        public uint PageNumber { get; }

        public uint Index { get; }
        
        public SelectionMarker(uint pageNumber, uint index)
        {
            PageNumber = pageNumber;
            Index = index;
        }

        public static bool operator >(SelectionMarker left, SelectionMarker right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <(SelectionMarker left, SelectionMarker right)
        {
            return left.CompareTo(right) < 0;
        }

        public int CompareTo(SelectionMarker other)
        {
            return PageNumber == other.PageNumber ? Index.CompareTo(other.Index) : PageNumber.CompareTo(other.PageNumber);
        }
    }

    public sealed class SelectionInterval
    {
        public SelectionMarker Start { get; }

        public SelectionMarker End { get; }

        public SelectionInterval(SelectionMarker start, SelectionMarker end)
        {
            Start = start;
            End = end;
        }
    }
}