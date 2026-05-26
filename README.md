# Fastech Markdown Viewer

<p align="center">
  <img src="Fastech.MarkDown.App/Assets/logo.png" alt="Fastech Logo" height="60"/>
</p>

<p align="center">
  <strong>Visualizzatore Markdown desktop per Windows</strong><br/>
  WPF · .NET 10 · WebView2 · Markdig · Mermaid.js
</p>

<p align="center">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet"/>
  <img alt="Platform" src="https://img.shields.io/badge/platform-Windows-0078D4?logo=windows"/>
  <img alt="License" src="https://img.shields.io/badge/license-MIT-green"/>
</p>

---

## Funzionalità

- 📄 **Rendering Markdown completo** — supporto tabelle, task list, footnotes, strikethrough e tutte le estensioni avanzate di [Markdig](https://github.com/xoofx/markdig)
- 🌊 **Diagrammi Mermaid** — flowchart, sequence, gantt, class diagram e altri renderizzati inline
- 🎨 **Syntax highlighting** — via [highlight.js](https://highlightjs.org/) con tema GitHub
- 📁 **File Explorer integrato** — naviga una intera cartella di file `.md` tramite TreeView laterale
- 🔄 **Auto-refresh** — rileva le modifiche ai file in tempo reale tramite `FileSystemWatcher`
- 💾 **Persistenza sessione** — riapre automaticamente l'ultima cartella e l'ultimo file all'avvio
- 🔗 **"Apri con" da Esplora risorse** — supporto argomenti da riga di comando per aprire file `.md` direttamente
- 🪟 **Salvataggio layout** — ricorda posizione e dimensioni della finestra e del pannello file

---

## Screenshot

> _Aggiungere screenshot dell'applicazione_

---

## Requisiti

- Windows 10/11
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (incluso in Windows 11, scaricabile per Windows 10)

---

## Build

```bash
git clone https://github.com/fabiofastech/Fastech.MarkDown.git
cd Fastech.MarkDown
dotnet build
dotnet run --project Fastech.MarkDown.App
```

---

## Associare i file `.md` all'applicazione

1. Tasto destro su un file `.md` in Esplora risorse
2. **Apri con** → **Scegli un'altra app**
3. Seleziona `Fastech.MarkDown.App.exe`
4. Spunta **"Usa sempre questa app"**

---

## Build installer MSI

Vedi [`Fastech.MarkDown.Installer/INSTALLER.md`](Fastech.MarkDown.Installer/INSTALLER.md) per le istruzioni complete.

```powershell
cd Fastech.MarkDown.Installer
.\build-installer.ps1
```

---

## Struttura del progetto

```
Fastech.MarkDown/
├── Fastech.MarkDown.App/
│   ├── Models/
│   │   └── FileTreeItem.cs          # Nodo albero file (INotifyPropertyChanged)
│   ├── Services/
│   │   ├── MarkdownRenderService.cs # Markdown → HTML
│   │   └── SettingsService.cs       # Persistenza impostazioni (%AppData%)
│   ├── Assets/                      # Icona e logo brand Fastech
│   ├── wwwroot/                     # highlight.js, mermaid.js, CSS (copiati in output)
│   ├── MainWindow.xaml              # UI principale
│   └── MainWindow.xaml.cs           # Logica applicazione
└── Fastech.MarkDown.slnx
```

---

## Tecnologie

| Componente | Tecnologia |
|---|---|
| Framework | WPF .NET 10 (`net10.0-windows`) |
| Parsing Markdown | [Markdig](https://github.com/xoofx/markdig) |
| Rendering HTML | Microsoft WebView2 |
| Diagrammi | [Mermaid.js](https://mermaid.js.org/) |
| Syntax highlight | [highlight.js](https://highlightjs.org/) |

---

## Licenza

MIT — © Fastech
