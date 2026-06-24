# Fastech.MarkDown.App — Istruzioni progetto

## Panoramica
Applicazione WPF (.NET 10) per la visualizzazione di file Markdown con rendering HTML via WebView2.

## Stack tecnologico
- **Framework**: .NET 10, WPF (`net10.0-windows`)
- **Rendering Markdown**: [Markdig](https://github.com/xoofx/markdig) v1.2.0 con `UseAdvancedExtensions()`
- **Viewer**: `Microsoft.Web.WebView2` — rendering HTML con virtual host `assets.local` → `wwwroot/`
- **Editor**: [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) v6.3.1.120 — editor testo con numeri di riga e syntax highlighting Markdown (`wwwroot/Markdown.xshd`)
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
├── wwwroot/                     # Asset statici copiati in output (highlight.js, mermaid.js, CSS, Markdown.xshd)
├── MainWindow.xaml              # UI principale: toolbar, TreeView, WebView2, editor AvalonEdit, status bar
└── MainWindow.xaml.cs           # Logica: apertura file/cartella, watcher, render, modifica, sessione
```

## Convenzioni di codice
- Lingua UI e commenti: **italiano**
- Codice (nomi variabili, metodi, classi): **inglese**
- Nullable enable, ImplicitUsings enable
- Stile brand Fastech: arancione `#F0A000` / `#C87D00`, sfondo caldo `#FFF4E0`, testo scuro `#2C1A00`

## Comportamenti chiave
- **Modalità Anteprima/Modifica**: toggle a tutta finestra tra rendering (WebView2) ed editor (AvalonEdit). Bottoni toolbar 👁 Anteprima / ✏ Modifica / 💾 Salva
- **Salvataggio**: esplicito con 💾 o **Ctrl+S**; indicatore modifiche non salvate `*` su titolo/status bar; prompt di conferma al cambio file o chiusura con modifiche pendenti
- **Auto-refresh**: `FileSystemWatcher` sul folder aperto; ri-renderizza se il file corrente cambia (sospeso durante la modifica e il salvataggio interno per evitare loop)
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
