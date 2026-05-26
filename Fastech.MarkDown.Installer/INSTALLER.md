# Fastech Markdown Viewer — Build Installer MSI

## Prerequisiti

| Strumento | Installazione |
|---|---|
| .NET 10 SDK | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0) |
| WiX Toolset v7 | `dotnet tool install --global wix` |

> **Nota:** WiX v7 richiede l'accettazione della EULA OSMF (una volta sola per macchina):
> ```powershell
> wix eula accept wix7
> ```
> Lo script `build-installer.ps1` installa WiX automaticamente se assente, ma la EULA va accettata manualmente.

---

## Generare l'MSI

```powershell
cd Fastech.MarkDown.Installer

# Build con versione di default (1.0.0.0)
.\build-installer.ps1

# Build con versione custom
.\build-installer.ps1 -Version 1.2.0.0

# Build con cartella di output custom
.\build-installer.ps1 -Version 1.2.0.0 -OutputDir C:\dist
```

L'MSI viene creato in `output\FastechMarkdownViewer-<Versione>.msi`.

---

## Cosa fa l'installer

| Azione | Dettaglio |
|---|---|
| **Directory di installazione** | `%ProgramFiles%\Fastech\MarkDown` |
| **Shortcut menu Start** | `Fastech → Fastech Markdown Viewer` |
| **Associazione file** | `.md` e `.markdown` impostati come app predefinita |
| **Registro** | Chiavi in `HKCR` per ProgID `Fastech.MarkdownFile` |
| **Upgrade** | Upgrade automatico (rimuove versione precedente) |
| **Runtime** | Self-contained win-x64 — nessun .NET richiesto sul PC target |

---

## Struttura del progetto installer

```
Fastech.MarkDown.Installer/
├── Package.wxs            ← Definizione WiX: directory, shortcut, associazioni
└── build-installer.ps1    ← Script di build completo (publish + harvest + wix build)
```

---

## Come funziona lo script

```
build-installer.ps1
│
├── 1. Verifica / installa WiX v7 (dotnet tool)
│
├── 2. dotnet publish
│       -c Release
│       -r win-x64
│       --self-contained true
│       → _publish/
│
├── 3. Genera _AppFiles.wxs
│       Scansiona _publish/ ricorsivamente
│       Crea ComponentGroup "AppFiles" con struttura directory completa
│
├── 4. wix build Package.wxs _AppFiles.wxs
│       → output/FastechMarkdownViewer-<Version>.msi
│
└── 5. Cleanup: rimuove _publish/ e _AppFiles.wxs
```

---

## File associations

Dopo l'installazione i file `.md` e `.markdown` vengono aperti automaticamente con **Fastech Markdown Viewer**.

Per ripristinare l'app precedente: *Impostazioni Windows → App → App predefinite → scegli un'altra app per `.md`*.

---

## Disinstallazione

Tramite il pannello di controllo standard di Windows:
**Impostazioni → App → App installate → Fastech Markdown Viewer → Disinstalla**

La disinstallazione rimuove anche la shortcut dal menu Start.
Le associazioni file vengono rimosse insieme al ProgID `Fastech.MarkdownFile`.
