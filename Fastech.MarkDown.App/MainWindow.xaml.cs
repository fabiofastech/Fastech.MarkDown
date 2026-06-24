using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private bool _editMode;
    private bool _isDirty;
    private bool _savingInternally;
    private AppSettings _settings = new();

    public MainWindow(string? startupFilePath = null)
    {
        InitializeComponent();
        _startupFilePath = startupFilePath;
        FileTreeView.ItemsSource = _rootItems;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        PreviewKeyDown += MainWindow_PreviewKeyDown;
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

            LoadEditorHighlighting();

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
        if (!ConfirmDiscardChanges())
        {
            e.Cancel = true;
            return;
        }

        _settings.WindowWidth  = Width;
        _settings.WindowHeight = Height;
        _settings.TreeColumnWidth = TreeColumn.Width.Value;
        _settingsService.Save(_settings);
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (_editMode && _isDirty)
                SaveCurrentFile();
            e.Handled = true;
        }
    }

    private void LoadEditorHighlighting()
    {
        try
        {
            var xshdPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "Markdown.xshd");
            if (File.Exists(xshdPath))
            {
                using var reader = System.Xml.XmlReader.Create(xshdPath);
                Editor.SyntaxHighlighting =
                    ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(
                        reader, ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
            }
        }
        catch { /* Editor resta senza evidenziazione se la definizione non è caricabile */ }
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
        _currentFilePath = null;
        _editMode = false;
        _isDirty = false;
        Editor.Visibility = Visibility.Collapsed;
        WebView.Visibility = Visibility.Visible;
        BtnEdit.IsEnabled = false;
        BtnSave.IsEnabled = false;
        WebView.NavigateToString(_markdownService.RenderToHtml(welcome));
    }

    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges()) return;
        var dialog = new OpenFolderDialog { Title = "Seleziona cartella Markdown" };
        if (dialog.ShowDialog() == true)
            LoadFolder(dialog.FolderName);
    }

    private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges()) return;
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
        // In modifica il refresh non ricarica da disco (non sovrascrive l'editor)
        if (_editMode) return;
        if (_currentFilePath != null)
            RenderFile(_currentFilePath);
    }

    private void BtnToggleTree_Click(object sender, RoutedEventArgs e)
    {
        _treeVisible = !_treeVisible;
        TreeColumn.Width = _treeVisible ? new GridLength(250) : new GridLength(0);
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e) => EnterEditMode();

    private void BtnPreview_Click(object sender, RoutedEventArgs e) => EnterPreviewMode();

    private void BtnSave_Click(object sender, RoutedEventArgs e) => SaveCurrentFile();

    private void EnterEditMode()
    {
        if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
            return;

        if (!_editMode)
        {
            try
            {
                Editor.Text = File.ReadAllText(_currentFilePath);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Errore apertura in modifica: {ex.Message}";
                return;
            }

            _isDirty = false;
            _editMode = true;
            WebView.Visibility = Visibility.Collapsed;
            Editor.Visibility = Visibility.Visible;
            BtnSave.IsEnabled = false;
            Editor.Focus();
            UpdateTitleAndStatus();
        }
    }

    private void EnterPreviewMode()
    {
        if (_editMode)
        {
            // Renderizza il contenuto corrente dell'editor (anche se non salvato)
            WebView.NavigateToString(_markdownService.RenderToHtml(Editor.Text));
            _editMode = false;
            Editor.Visibility = Visibility.Collapsed;
            WebView.Visibility = Visibility.Visible;
            UpdateTitleAndStatus();
        }
    }

    private void Editor_TextChanged(object? sender, EventArgs e)
    {
        if (!_editMode) return;
        _isDirty = true;
        BtnSave.IsEnabled = true;
        UpdateTitleAndStatus();
    }

    private void SaveCurrentFile()
    {
        // Il salvataggio funziona sia in modalità Modifica che Anteprima (purché ci sia un file)
        if (!_isDirty || string.IsNullOrEmpty(_currentFilePath)) return;

        try
        {
            _savingInternally = true;
            File.WriteAllText(_currentFilePath, Editor.Text);
            _isDirty = false;
            BtnSave.IsEnabled = false;
            UpdateTitleAndStatus();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Errore salvataggio: {ex.Message}";
        }
        finally
        {
            _savingInternally = false;
        }
    }

    /// <summary>
    /// Se ci sono modifiche non salvate, chiede conferma. Ritorna false se l'utente annulla.
    /// </summary>
    private bool ConfirmDiscardChanges()
    {
        if (!_isDirty) return true;

        var result = MessageBox.Show(
            "Ci sono modifiche non salvate. Salvare prima di continuare?",
            "Fastech Markdown Viewer",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

        switch (result)
        {
            case MessageBoxResult.Yes:
                SaveCurrentFile();
                return !_isDirty;
            case MessageBoxResult.No:
                _isDirty = false;
                return true;
            default:
                return false;
        }
    }

    private void UpdateTitleAndStatus()
    {
        var name = string.IsNullOrEmpty(_currentFilePath)
            ? string.Empty
            : Path.GetFileName(_currentFilePath);
        var dirtyMark = _isDirty ? "*" : string.Empty;
        var modeLabel = _editMode ? " [Modifica]" : string.Empty;

        Title = string.IsNullOrEmpty(name)
            ? "Fastech Markdown Viewer"
            : $"Fastech Markdown Viewer — {dirtyMark}{name}{modeLabel}";

        if (!string.IsNullOrEmpty(_currentFilePath))
            StatusText.Text = $"{dirtyMark}{_currentFilePath}{modeLabel}";
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
        {
            if (!ConfirmDiscardChanges()) return;
            RenderFile(file.FullPath);
        }
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
                // Apertura di un nuovo file: torna sempre in Anteprima
                _editMode = false;
                _isDirty = false;
                Editor.Visibility = Visibility.Collapsed;
                WebView.Visibility = Visibility.Visible;
                BtnEdit.IsEnabled = true;
                BtnSave.IsEnabled = false;
                WebView.NavigateToString(html);
                UpdateTitleAndStatus();
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
        // Ignora le modifiche generate dal salvataggio interno o mentre si è in modifica,
        // per non sovrascrivere il contenuto dell'editor o innescare loop di refresh.
        if (_savingInternally || _editMode) return;
        if (e.FullPath == _currentFilePath)
            RenderFile(e.FullPath);
    }

    protected override void OnClosed(EventArgs e)
    {
        _watcher?.Dispose();
        base.OnClosed(e);
    }
}