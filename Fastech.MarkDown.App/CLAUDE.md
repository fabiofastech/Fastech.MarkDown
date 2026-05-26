# Fastech.MarkDown.App — Istruzioni progetto

## Panoramica
Applicazione WPF (.NET 10) per la visualizzazione di file Markdown con rendering HTML via WebView2.

## Stack tecnologico
- **Framework**: .NET 10, WPF (`net10.0-windows`)
- **Rendering Markdown**: [Markdig](https://github.com/xoofx/markdig) v1.2.0 con `UseAdvancedExtensions()`
- **Viewer**: `Microsoft.Web.WebView2` — rendering HTML con virtual host `assets.local` → `wwwroot/`
- **Diagrammi**: Mermaid.js (locale in `wwwroot/`)
- **Syntax highlighting**: highlight.js (locale in `wwwroot/`)

## Struttura del progetto
```
Fastech.MarkDown.App/
├── Models/
│   └── FileTreeItem.cs          # Nodo dell'albero file (INotifyPropertyChanged)
├── Services/
│   ├── MarkdownRenderService.cs # Converte .md → HTML completo (template inline)
│   └── SettingsService.cs       # Persistenza impostazioni in %AppData%\Fastech\MarkdownViewer\settings.json
├── Assets/
│   ├── fastech.ico
│   └── logo.png
├── wwwroot/                     # Asset statici copiati in output (highlight.js, mermaid.js, CSS)
├── MainWindow.xaml              # UI principale: toolbar, TreeView, WebView2, status bar
└── MainWindow.xaml.cs           # Logica: apertura file/cartella, watcher, render, sessione
```

## Convenzioni di codice
- Lingua UI e commenti: **italiano**
- Codice (nomi variabili, metodi, classi): **inglese**
- Nullable enable, ImplicitUsings enable
- Stile brand Fastech: arancione `#F0A000` / `#C87D00`, sfondo caldo `#FFF4E0`, testo scuro `#2C1A00`

## Comportamenti chiave
- **Auto-refresh**: `FileSystemWatcher` sul folder aperto; ri-renderizza se il file corrente cambia
- **Persistenza sessione**: all'avvio riapre l'ultima cartella e l'ultimo file; salva al `Closing`
- **Toggle pannello**: nasconde/mostra la colonna TreeView impostando `Width=0`
- **Asset locali**: WebView2 mappa `assets.local` → `wwwroot/` tramite `SetVirtualHostNameToFolderMapping`

## Build & Run
```powershell
cd Fastech.MarkDown.App
dotnet build
dotnet run
```

## Note
- I file `wwwroot/` devono essere inclusi nel `.csproj` con `CopyToOutputDirectory=PreserveNewest`
- Le directory nascoste (`.git`, ecc.) sono escluse dall'albero file
- Solo file `.md` e `.markdown` sono visibili nel TreeView
