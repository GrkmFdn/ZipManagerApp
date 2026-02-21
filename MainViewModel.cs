using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ZipApp.Models;
using ZipApp.Services;

namespace ZipApp.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly PersistenceService _persistenceService;
        private readonly ZipService _zipService;
        private string _currentPath;
        private string _zipFileName = "hetabil_sterp.zip";
        private bool _isBusy;
        private string _statusMessage;

        public MainViewModel()
        {
            _persistenceService = new PersistenceService();
            _zipService = new ZipService();
            Files = new ObservableCollection<FileItem>();
            RecentPaths = new ObservableCollection<string>();
            BrowseCommand = new RelayCommand(Browse);
            ZipCommand = new RelayCommand(Zip, CanZip);
            
            LoadLastState();
        }

        public ObservableCollection<FileItem> Files { get; private set; }

        public ObservableCollection<string> RecentPaths { get; private set; }

        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                if (_currentPath != value)
                {
                    _currentPath = value;
                    OnPropertyChanged();
                    LoadFiles(value);
                    if (Directory.Exists(value))
                    {
                        AddRecentPath(value);
                    }
                    SaveState();
                }
            }
        }

        public string ZipFileName
        {
            get => _zipFileName;
            set
            {
                if (_zipFileName != value)
                {
                    _zipFileName = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand BrowseCommand { get; }
        public ICommand ZipCommand { get; }

        private void Browse(object parameter)
        {
            try 
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    if (!string.IsNullOrWhiteSpace(CurrentPath) && Directory.Exists(CurrentPath))
                    {
                         dialog.SelectedPath = CurrentPath;
                    }
                    
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        CurrentPath = dialog.SelectedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Hata: " + ex.Message;
                MessageBox.Show("Klasör açılırken hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LoadFiles(string path)
        {
             Files.Clear();
             StatusMessage = "";
             if (!Directory.Exists(path)) return;

             try
             {
                 var settings = _persistenceService.Load();
                 List<string> selectedFiles = null;
                 if (settings.DirectorySelections != null && settings.DirectorySelections.ContainsKey(path))
                 {
                     selectedFiles = settings.DirectorySelections[path];
                 }

                 var info = new DirectoryInfo(path);
                 
                 // Add directories
                 foreach (var dir in info.GetDirectories())
                 {
                     bool isSelected = selectedFiles != null && selectedFiles.Contains(dir.Name);
                     var item = new FileItem 
                     { 
                         Name = dir.Name, 
                         FullPath = dir.FullName, 
                         Type = "Folder", 
                         Size = "",
                         IsSelected = isSelected
                     };
                     item.PropertyChanged += Item_PropertyChanged;
                     Files.Add(item);
                 }

                 // Add files
                 foreach (var file in info.GetFiles())
                 {
                     bool isSelected = selectedFiles != null && selectedFiles.Contains(file.Name);
                     var item = new FileItem 
                     { 
                         Name = file.Name, 
                         FullPath = file.FullName, 
                         Type = "File", 
                         Size = FormatSize(file.Length),
                         IsSelected = isSelected
                     };
                     item.PropertyChanged += Item_PropertyChanged;
                     Files.Add(item);
                 }
             }
             catch (Exception ex)
             {
                 StatusMessage = "Hata: " + ex.Message;
             }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileItem.IsSelected))
            {
                SaveState();
            }
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private bool CanZip(object parameter)
        {
            return !IsBusy 
                && !string.IsNullOrWhiteSpace(CurrentPath) 
                && !string.IsNullOrWhiteSpace(ZipFileName) 
                && Files.Any(f => f.IsSelected);
        }

        private async void Zip(object parameter)
        {
            IsBusy = true;
            StatusMessage = "Zipleniyor...";
            try
            {
                var filesToZip = Files.Where(f => f.IsSelected).ToList();
                var path = CurrentPath;
                var zipName = ZipFileName;

                await System.Threading.Tasks.Task.Run(() => 
                {
                    _zipService.CreateZip(path, filesToZip, zipName);
                });
                
                StatusMessage = "Zipleme Başarılı!";
                LoadFiles(CurrentPath);
                MessageBox.Show("Dosyalar başarıyla ziplendi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = "Hata: " + ex.Message;
                MessageBox.Show("Hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                StatusMessage = "Hazır";
            }
        }

        private void AddRecentPath(string path)
        {
            if (RecentPaths.Contains(path))
            {
                RecentPaths.Move(RecentPaths.IndexOf(path), 0);
            }
            else
            {
                RecentPaths.Insert(0, path);
            }

            while (RecentPaths.Count > 5)
            {
                RecentPaths.RemoveAt(RecentPaths.Count - 1);
            }
        }

        private void SaveState()
        {
            if (string.IsNullOrWhiteSpace(CurrentPath)) return;

            var settings = _persistenceService.Load(); 
            
            settings.LastDirectoryPath = CurrentPath;
            settings.RecentPaths = RecentPaths.ToList();

            if (settings.DirectorySelections == null) 
                 settings.DirectorySelections = new Dictionary<string, List<string>>();
                 
            var selectedNames = Files.Where(f => f.IsSelected).Select(f => f.Name).ToList();
            
            if (settings.DirectorySelections.ContainsKey(CurrentPath))
            {
                settings.DirectorySelections[CurrentPath] = selectedNames;
            }
            else
            {
                settings.DirectorySelections.Add(CurrentPath, selectedNames);
            }
            
            _persistenceService.Save(settings);
        }

        private void LoadLastState()
        {
            var settings = _persistenceService.Load();
            
            if (settings.RecentPaths != null)
            {
                foreach (var path in settings.RecentPaths)
                {
                    RecentPaths.Add(path);
                }
            }

            if (!string.IsNullOrWhiteSpace(settings.LastDirectoryPath) && Directory.Exists(settings.LastDirectoryPath))
            {
                CurrentPath = settings.LastDirectoryPath;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
