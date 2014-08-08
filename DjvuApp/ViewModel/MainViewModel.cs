using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using DjvuApp.Dialogs;
using DjvuApp.Model.Books;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace DjvuApp.ViewModel
{
    public sealed class MainViewModel : ViewModelBase
    {
        private ObservableCollection<Book> _books = null;
        private bool _isProgressVisible = false;
        private IBookProvider _bookProvider = null;

        public ObservableCollection<Book> Books
        {
            get { return _books; }

            set
            {
                if (_books == value)
                {
                    return;
                }

                _books = value;
                RaisePropertyChanged();
            }
        }

        public bool IsProgressVisible
        {
            get { return _isProgressVisible; }

            set
            {
                if (_isProgressVisible == value)
                {
                    return;
                }

                _isProgressVisible = value;
                RaisePropertyChanged();
            }
        }

        public ICommand RenameBookCommand { get; private set; }

        public ICommand RemoveBookCommand { get; private set; }

        public ICommand AddBookCommand { get; private set; }

        public MainViewModel(IBookProvider bookProvider)
        {
            _bookProvider = bookProvider;

            RenameBookCommand = new RelayCommand<Book>(RenameBook);
            RemoveBookCommand = new RelayCommand<Book>(RemoveBook);
            AddBookCommand = new RelayCommand(AddBook);

            RefreshBooks();
        }

        private void AddBook()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".djvu");
            picker.PickSingleFileAndContinue();
        }

        private async void RenameBook(Book book)
        {
            var dialog = new RenameDialog(book.Title);
            var newName = await dialog.ShowAsync();

            if (newName != book.Title)
            {
                await _bookProvider.ChangeTitleAsync(book, newName);
            }
        }

        private async void RemoveBook(Book book)
        {
            var dialog = new MessageDialog("If you delete this document, you won't be able to recover it later." +
                " All the progress will also be deleted from your phone.", string.Format("Delete {0}?", book.Title));
            dialog.Commands.Add(new UICommand("delete", async command =>
            {
                await _bookProvider.RemoveBookAsync(book);
                Books.Remove(book);
            }));
            dialog.Commands.Add(new UICommand("cancel"));

            await dialog.ShowAsync();
        }

        private async void RefreshBooks()
        {
            IsProgressVisible = true;

            IEnumerable<Book> books = await _bookProvider.GetBooksAsync();
            books = books.OrderByDescending(book => book.CreationTime);
            Books = new ObservableCollection<Book>(books);

            IsProgressVisible = false;
        }
    }
}