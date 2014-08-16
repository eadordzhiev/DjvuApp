using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DjvuApp.Common;
using DjvuApp.Dialogs;
using DjvuApp.Model.Books;
using DjvuApp.Model.Outline;
using DjvuLibRT;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace DjvuApp.ViewModel
{
    public sealed class ViewerViewModel : ViewModelBase
    {
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

        public DjvuDocument CurrentDocument
        {
            get { return _currentDocument; }

            set
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
                RaisePropertyChanged();
            }
        }

        public Outline Outline
        {
            get { return _outline; }

            set
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

        private bool _isProgressVisible = false;
        private DjvuDocument _currentDocument = null;
        private uint _currentPageNumber = 0;
        private Outline _outline = null;

        public ViewerViewModel()
        {
            ShowOutlineCommand = new RelayCommand(ShowOutline);
            JumpToPageCommand = new RelayCommand(ShowJumpToPageDialog);

            MessengerInstance.Register<LoadedHandledMessage<IBook>>(this, message => LoadedHandler(message.Parameter));
        }

        private async void ShowOutline()
        {
            var dialog = new OutlineDialog(Outline);

            var pageNumber = await dialog.ShowAsync();
            if (pageNumber.HasValue)
            {
                CurrentPageNumber = pageNumber.Value;
            }
        }

        private async void ShowJumpToPageDialog()
        {
            var dialog = new JumpToPageDialog(CurrentDocument.PageCount);
            
            var pageNumber = await dialog.ShowAsync();
            if (pageNumber.HasValue)
            {
                CurrentPageNumber = pageNumber.Value;
            }
        }

        private async void LoadedHandler(IBook book)
        {
            IsProgressVisible = true;

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

            var djvuOutline = await document.GetBookmarksAsync();
            if (djvuOutline != null)
            {
                Outline = new Outline(djvuOutline);
            }

            await Task.Delay(1);
            CurrentDocument = document;

            await Task.Delay(20);
            IsProgressVisible = false;
        }
    }
}
