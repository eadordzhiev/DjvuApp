using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using DjvuApp.Djvu;
using JetBrains.Annotations;
using DjvuApp.ViewModel.Messages;
using DjvuApp.Common;
using DjvuApp.Dialogs;
using DjvuApp.Model.Books;
using DjvuApp.Model.Outline;
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

        public DjvuAsyncDocument CurrentDocument
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

        public IEnumerable<IOutlineItem> Outline
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

        public RelayCommand GoToNextPageCommand { get; private set; }

        public RelayCommand GoToPreviousPageCommand { get; private set; }

        public ICommand ShareCommand { get; private set; }

        private bool _isProgressVisible;
        private bool _isCurrentPageBookmarked;
        private DjvuAsyncDocument _currentDocument;
        private uint _currentPageNumber;
        private IEnumerable<IOutlineItem> _outline;

        private readonly DataTransferManager _dataTransferManager;
        private readonly IBookProvider _provider;
        private readonly INavigationService _navigationService;
        private ObservableCollection<IBookmark> _bookmarks;
        private IBook _book;

        public ViewerViewModel(IBookProvider provider, INavigationService navigationService)
        {
            _dataTransferManager = DataTransferManager.GetForCurrentView();
            _provider = provider;
            _navigationService = navigationService;
            ShowOutlineCommand = new RelayCommand(ShowOutline);
            JumpToPageCommand = new RelayCommand(ShowJumpToPageDialog);
            AddBookmarkCommand = new RelayCommand(AddBookmark);
            RemoveBookmarkCommand = new RelayCommand(RemoveBookmark);
            ShowBookmarksCommand = new RelayCommand(ShowBookmarks);
            ShareCommand = new RelayCommand(DataTransferManager.ShowShareUI);

            GoToNextPageCommand = new RelayCommand(
                () => CurrentPageNumber++, 
                () => CurrentPageNumber < CurrentDocument.PageCount);
            GoToPreviousPageCommand = new RelayCommand(
                () => CurrentPageNumber--, 
                () => CurrentPageNumber > 1);

            MessengerInstance.Register<LoadedHandledMessage<IBook>>(this, message => LoadedHandler(message.Parameter));

            MessengerInstance.Register<OnNavigatedFromMessage>(this,
                message => _dataTransferManager.DataRequested -= DataRequestedHandler);
            MessengerInstance.Register<OnNavigatedToMessage>(this,
                message => _dataTransferManager.DataRequested += DataRequestedHandler);
        }

        private async void DataRequestedHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            var deferral = e.Request.GetDeferral();

            e.Request.Data.Properties.Title = _book.Title;
            var file = await StorageFile.GetFileFromPathAsync(_book.Path);
            e.Request.Data.SetStorageItems(new IStorageItem[] {file}, true);

            deferral.Complete();
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

        private async void ShowFileOpeningError(IBook book)
        {
            var dialog = new MessageDialog("This file cannot be opened because it is damaged. You should remove this file.", "Can't open file");
            dialog.Commands.Add(new UICommand("remove"));
            await dialog.ShowAsync();
            await _provider.RemoveBookAsync(book);
            _navigationService.GoBack();
        }

        private async void LoadedHandler(IBook book)
        {
            IsProgressVisible = true;

            _book = book;
            DjvuAsyncDocument document;

            try
            {
                document = await DjvuAsyncDocument.LoadFileAsync(book.Path);
            }
            catch (DjvuDocumentException ex)
            {
                IsProgressVisible = false;
                ShowFileOpeningError(book);
                return;
            }

            Outline = document.GetOutline();

            _bookmarks = new ObservableCollection<IBookmark>(await _provider.GetBookmarksAsync(book));
            _bookmarks.CollectionChanged += (sender, e) => UpdateIsCurrentPageBookmarked();

            CurrentDocument = document;

            IsProgressVisible = false;
        }
        
        private void OnCurrentPageNumberChanged()
        {
            UpdateIsCurrentPageBookmarked();
            GoToNextPageCommand.RaiseCanExecuteChanged();
            GoToPreviousPageCommand.RaiseCanExecuteChanged();
        }

        private void UpdateIsCurrentPageBookmarked()
        {
            IsCurrentPageBookmarked = _bookmarks.Any(bookmark => bookmark.PageNumber == CurrentPageNumber);
        }
    }
}
