using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using DjvuApp.Annotations;

namespace DjvuApp.Model.Books
{
    [DataContract]
    public sealed class Book : INotifyPropertyChanged, IEquatable<Book>
    {
        private string _title;

        [DataMember]
        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public Guid Guid { get; set; }
        [DataMember]
        public DateTime LastOpeningTime { get; set; }
        [DataMember]
        public DateTime CreationTime { get; set; }
        [DataMember]
        public uint PageCount { get; set; }
        [DataMember]
        public ulong Size { get; set; }
        [DataMember]
        public string Path { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Equals(Book other)
        {
            return other != null && this.Guid == other.Guid;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Book);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
    }
}
