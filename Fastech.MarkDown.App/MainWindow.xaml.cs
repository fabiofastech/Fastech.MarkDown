using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Fastech.MarkDown.App.Models;
using Fastech.MarkDown.App.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace Fastech.MarkDown.App;

public partial class MainWindow : Window
{
    private readonly MarkdownRenderService _markdownService = new();
    private readonly SettingsService _settingsService = new();
    private readonly ObservableCollection<FileTreeItem> _rootItems = new();
    private readonly string? _startupFilePath;
    private FileSystemWatcher? _watcher;
    private string? _currentFilePath;
    private bool _treeVisible = true;
    private AppSettings _settings = new();

    public MainWindow(string? startupFilePath = null)
    {
        InitializeComponent();
        _startupFilePath = startupFilePath;
        FileTreeView.ItemsSource = _rootItems;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Fastech", "MarkdownViewer", "WebView2");

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await WebView.EnsureCoreWebView2Async(env);

            var wwwroot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "assets.local", wwwroot, CoreWebView2HostResourceAccessKind.Allow);

            _settings = _settingsService.Load();
            RestoreWindowState();
            RestoreLastSession();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore durante l'avvio:\n\n{ex.Message}", "Fastech Markdown Viewer",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreWindowState()
    {
        if (_settings.WindowWidth > 0)  Width  = _settings.WindowWidth;
        if (_settings.WindowHeight > 0) Height = _settings.WindowHeight;
        if (_settings.TreeColumnWidth > 0)
            TreeColumn.Width = new GridLength(_settings.TreeColumnWidth);
    }

    private void RestoreLastSession()
    {
        // Priorità al file passato da riga di comando ("Apri con" da Esplora risorse)
        if (!string.IsNullOrEmpty(_startupFilePath) && File.Exists(_startupFilePath))
        {
            var folder = Path.GetDirectoryName(_startupFilePath);
            if (!string.IsNullOrEmpty(folder))
            {
                LoadFolder(folder);
                SelectFileInTree(_startupFilePath);
            }
            RenderFile(_startupFilePath);
            return;
        }

        if (!string.IsNullOrEmpty(_settings.LastFolderPath) &&
            Directory.Exists(_settings.LastFolderPath))
        {
            LoadFolder(_settings.LastFolderPath);

            // Riapre l'ultimo file dopo il caricamento dell'albero
            if (!string.IsNullOrEmpty(_settings.LastFilePath) &&
                File.Exists(_settings.LastFilePath))
            {
                RenderFile(_settings.LastFilePath);
                SelectFileInTree(_settings.LastFilePath);
                return;
            }
        }
        else if (!string.IsNullOrEmpty(_settings.LastFilePath) &&
                 File.Exists(_settings.LastFilePath))
        {
            RenderFile(_settings.LastFilePath);
            return;
        }

        ShowWelcomePage();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _settings.WindowWidth  = Width;
        _settings.WindowHeight = Height;
        _settings.TreeColumnWidth = TreeColumn.Width.Value;
        _settingsService.Save(_settings);
    }

    private void ShowWelcomePage()
    {
        const string welcome = """
            # Fastech Markdown Viewer

            Benvenuto nel viewer Markdown.

            **Come iniziare:**
            - 📁 **Apri Cartella** — naviga tutti i file `.md` di una directory
            - 📄 **Apri File** — apri un singolo file Markdown

            **Funzionalità supportate:**
            - Markdown esteso (tabelle, task list, footnotes…)
            - Diagrammi **Mermaid**
            - Syntax highlighting del codice
            - Auto-refresh al salvataggio del file
            - Ricorda l'ultima cartella e file aperti

            ---

            **Esempio Mermaid:**

            ```mermaid
            graph LR
                A[Apri file] --> B{Tipo?}
                B -->|.md| C[Render Markdown]
                B -->|altro| D[Ignora]
                C --> E[Visualizza]
            ```
            """;
        WebView.NavigateToString(_markdownService.RenderToHtml(welcome));
    }

    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Seleziona cartella Markdown" };
        if (dialog.ShowDialog() == true)
            LoadFolder(dialog.FolderName);
    }

    private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Markdown|*.md;*.markdown|Tutti i file|*.*",
            Title = "Apri file Markdown"
        };
        if (dialog.ShowDialog() == true)
            RenderFile(dialog.FileName);
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        if (_currentFilePath != null)
            RenderFile(_currentFilePath);
    }

    private void BtnToggleTree_Click(object sender, RoutedEventArgs e)
    {
        _treeVisible = !_treeVisible;
        TreeColumn.Width = _treeVisible ? new GridLength(250) : new GridLength(0);
    }

    private void LoadFolder(string folderPath)
    {
        _rootItems.Clear();
        SetupWatcher(folderPath);

        var root = BuildTreeItem(folderPath);
        if (root != null)
        {
            root.IsExpanded = true;
            _rootItems.Add(root);
        }

        _settings.LastFolderPath = folderPath;
        StatusText.Text = $"Cartella: {folderPath}";
    }

    private static FileTreeItem? BuildTreeItem(string path)
    {
        var info = new DirectoryInfo(path);
        if (!info.Exists) return null;

        var item = new FileTreeItem { Name = info.Name, FullPath = path, IsDirectory = true };

        foreach (var dir in info.GetDirectories()
                                .Where(d => !d.Name.StartsWith('.'))
                                .OrderBy(d => d.Name))
        {
            var child = BuildTreeItem(dir.FullName);
            if (child != null && child.Children.Count > 0)
                item.Children.Add(child);
        }

        foreach (var file in info.GetFiles()
                                  .Where(f => f.Extension is ".md" or ".markdown")
                                  .OrderBy(f => f.Name))
        {
            item.Children.Add(new FileTreeItem
            {
                Name = file.Name,
                FullPath = file.FullName,
                IsDirectory = false
            });
        }

        return item;
    }

    private void SelectFileInTree(string filePath)
    {
        static bool FindAndSelect(IEnumerable<FileTreeItem> items, string path)
        {
            foreach (var item in items)
            {
                if (!item.IsDirectory && item.FullPath == path)
                    return true;
                if (item.IsDirectory && FindAndSelect(item.Children, path))
                {
                    item.IsExpanded = true;
                    return true;
                }
            }
            return false;
        }
        FindAndSelect(_rootItems, filePath);
    }

    private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is FileTreeItem { IsDirectory: false } file)
            RenderFile(file.FullPath);
    }

    private void RenderFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            _currentFilePath = filePath;
            _settings.LastFilePath = filePath;
            var html = _markdownService.RenderToHtml(content);

            void UpdateUi()
            {
                WebView.NavigateToString(html);
                StatusText.Text = filePath;
                Title = $"Fastech Markdown Viewer — {Path.GetFileName(filePath)}";
            }

            if (Dispatcher.CheckAccess())
                UpdateUi();
            else
                Dispatcher.BeginInvoke(UpdateUi);
        }
        catch (Exception ex)
        {
            var msg = $"Errore: {ex.Message}";
            if (Dispatcher.CheckAccess())
                StatusText.Text = msg;
            else
                Dispatcher.BeginInvoke(() => StatusText.Text = msg);
        }
    }

    private void SetupWatcher(string folderPath)
    {
        _watcher?.Dispose();
        _watcher = new FileSystemWatcher(folderPath)
        {
            Filter = "*.md",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFileChanged;
        _watcher.Created += (_, _) => Dispatcher.Invoke(() => LoadFolder(folderPath));
        _watcher.Deleted += (_, _) => Dispatcher.Invoke(() => LoadFolder(folderPath));
        _watcher.Renamed += (_, _) => Dispatcher.Invoke(() => LoadFolder(folderPath));
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath == _currentFilePath)
            RenderFile(e.FullPath);
    }

    protected override void OnClosed(EventArgs e)
    {
        _watcher?.Dispose();
        base.OnClosed(e);
    }
}