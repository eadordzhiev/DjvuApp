using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DjvuApp.Annotations;
using SQLite;

namespace DjvuApp.Model.Books
{
    public sealed class SqliteBook : IBook
    {
        private string _title;

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public Guid Guid { get; set; }

        [MaxLength(255)]
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

        public DateTime LastOpeningTime { get; set; }

        public DateTime CreationTime { get; set; }

        public uint PageCount { get; set; }

        public uint Size { get; set; }

        [MaxLength(255)]
        public string Path { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Equals(IBook other)
        {
            return other != null && this.Guid == other.Guid;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IBook);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
    }
}