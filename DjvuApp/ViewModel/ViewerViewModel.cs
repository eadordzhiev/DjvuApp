using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using JetBrains.Annotations;
using DjvuApp.ViewModel.Messages;
using DjvuApp.Common;
using DjvuApp.Dialogs;
using DjvuApp.Model.Books;
using DjvuApp.Model.Outline;
using DjvuLibRT;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace DjvuApp.ViewModel
{
    [UsedImplicitly]
    public sealed class ViewerViewModel : ViewModelBase
    {
        public bool IsProgressVisible
        {
            get { return _isProgressVisible; }

            private set
            {
                if (_isProgressVisible == value)
                {
                    return;
                }

                _isProgressVisible = value;
                RaisePropertyChanged();
            }
        }

        public bool IsCurrentPageBookmarked
        {
            get { return _isCurrentPageBookmarked; }

            private set
            {
                if (_isCurrentPageBookmarked == value)
                {
                    return;
                }

                _isCurrentPageBookmarked = value;
                RaisePropertyChanged();
            }
        }

        public DjvuDocument CurrentDocument
        {
            get { return _currentDocument; }

            private set
            {
                if (_currentDocument == value)
                {
                    return;
                }

                _currentDocument = value;
                RaisePropertyChanged();
            }
        }

        public uint CurrentPageNumber
        {
            get { return _currentPageNumber; }

            set
            {
                if (_currentPageNumber == value)
                {
                    return;
                }

                _currentPageNumber = value;
                OnCurrentPageNumberChanged();
                RaisePropertyChanged();
            }
        }

        public Outline Outline
        {
            get { return _outline; }

            private set
            {
                if (_outline == value)
                {
                    return;
                }

                _outline = value;
                RaisePropertyChanged();
            }
        }

        public ICommand ShowOutlineCommand { get; private set; }

        public ICommand JumpToPageCommand { get; private set; }

        public ICommand AddBookmarkCommand { get; private set; }

        public ICommand RemoveBookmarkCommand { get; private set; }

        public ICommand ShowBookmarksCommand { get; private set; }

        private bool _isProgressVisible;
        private bool _isCurrentPageBookmarked;
        private DjvuDocument _currentDocument;
        private uint _currentPageNumber;
        private Outline _outline;

        private readonly IBookProvider _provider;
        private ObservableCollection<IBookmark> _bookmarks;
        private IBook _book;

        public ViewerViewModel(IBookProvider provider)
        {
            _provider = provider;
            ShowOutlineCommand = new RelayCommand(ShowOutline);
            JumpToPageCommand = new RelayCommand(ShowJumpToPageDialog);
            AddBookmarkCommand = new RelayCommand(AddBookmark);
            RemoveBookmarkCommand = new RelayCommand(RemoveBookmark);
            ShowBookmarksCommand = new RelayCommand(ShowBookmarks);

            MessengerInstance.Register<LoadedHandledMessage<IBook>>(this, message => LoadedHandler(message.Parameter));
        }

        private async void RemoveBookmark()
        {
            if (!IsCurrentPageBookmarked)
                return;

            var bookmark = _bookmarks.First(item => item.PageNumber == CurrentPageNumber);
            await _provider.RemoveBookmarkAsync(bookmark);
            _bookmarks.Remove(bookmark);
            IsCurrentPageBookmarked = false;
        }

        private async void AddBookmark()
        {
            if (IsCurrentPageBookmarked) 
                return;

            var title = await CreateBookmarkDialog.ShowAsync();
            if (title == null)
                return;
            var bookmark = await _provider.CreateBookmarkAsync(_book, title, CurrentPageNumber);
            _bookmarks.Add(bookmark);
            IsCurrentPageBookmarked = true;
        }

        private async void ShowBookmarks()
        {
            var bookmark = await SelectBookmarkDialog.ShowAsync(_bookmarks);
            if (bookmark == null)
                return;

            CurrentPageNumber = bookmark.PageNumber;
        }

        private async void ShowOutline()
        {
            var pageNumber = await OutlineDialog.ShowAsync(Outline);
            if (pageNumber.HasValue)
            {
                CurrentPageNumber = pageNumber.Value;
            }
        }

        private async void ShowJumpToPageDialog()
        {
            var pageNumber = await JumpToPageDialog.ShowAsync(CurrentDocument.PageCount);
            if (pageNumber.HasValue)
            {
                CurrentPageNumber = pageNumber.Value;
            }
        }

        private async void LoadedHandler(IBook book)
        {
            IsProgressVisible = true;

            _book = book;
            DjvuDocument document;

            try
            {
                document = await DjvuDocument.LoadAsync(book.Path);
            }
            catch (Exception ex)
            {
                // TODO: Добавить диалог удаления файла
                throw;
            }

            var djvuOutline = document.GetBookmarks();
            if (djvuOutline != null)
            {
                Outline = new Outline(djvuOutline);
            }

            _bookmarks = new ObservableCollection<IBookmark>(await _provider.GetBookmarksAsync(book));
            _bookmarks.CollectionChanged += (sender, e) => UpdateIsCurrentPageBookmarked();

            CurrentDocument = document;

            IsProgressVisible = false;
        }
        
        private void OnCurrentPageNumberChanged()
        {
            UpdateIsCurrentPageBookmarked();
        }

        private void UpdateIsCurrentPageBookmarked()
        {
            IsCurrentPageBookmarked = _bookmarks.Any(bookmark => bookmark.PageNumber == CurrentPageNumber);
        }
    }
}
