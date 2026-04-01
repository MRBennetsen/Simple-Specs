# Simple-Specs

En elegant og brugervenlig Windows-applikation, der viser detaljerede maskinvarespecifikationer for din computer.

## Om projektet

**Simple-Specs** er en WPF-baseret applikation designet til at præsentere vigtige systemoplysninger om din Windows-computer. Appen bruger Windows Management Instrumentation (WMI) til at indsamle data om hardware og viser det i en moderne, Dark-Mode-inspireret brugergrænseflade med animated glow-effekter.

## Features

- 🖥️ **Systemoplysninger** - Producent, model og serienummer
- 🎮 **CPU & GPU Info** - Processortype og grafikkort detaljer
- 💾 **RAM & Lagerplads** - Hukommelse og diskpladsberegninger
- 🔋 **Batteri Status** - Batteri sundhedstilstand (for bærbare computere)
- ✨ **Moderne UI** - Mørk tema med animated glow-effekter
- 📊 **Asynkron indlæsning** - Responsiv UI uden frysning

## Systemkrav

- Windows 10 eller nyere
- .NET 9.0 (Windows Desktop Runtime)
- Der kræves adminadgang for at få adgang til nogle WMI-data (f.eks. batteri sundhed)

## Installation & Brug

### Fra kilt kode

1. Klon repositoriet:
```bash
git clone https://github.com/din-bruger/Simple-Specs.git
cd Simple-Specs
```

2. Byg og kør applikationen:
```bash
dotnet run
```

3. Eller byg som eksekverbar:
```bash
dotnet publish -c Release -o ./output
```

## Teknologier

- **Framework**: .NET 9.0 WPF
- **Sprog**: C#
- **System API**: Windows Management Instrumentation (WMI)
- **UI**: XAML med DataBinding
- **Asynkron programmering**: Task-baseret async/await

## Projektstruktur

```
Simple-Specs/
├── App.xaml                 # Applikationsressourcer
├── App.xaml.cs             # Applikationslogik
├── MainWindow.xaml         # Brugergrænseflademarkup
├── MainWindow.xaml.cs      # GUI-logik og systemhentning
├── Simple-Specs.csproj     # Projektfil
└── README.md               # Denne fil
```

## Hvordan det fungerer

1. **Appstart**: MainWindow initialiserer og kalder `LoadHardwareSpecsAsync()`
2. **Dataindsamling**: Bakgrundstråd bruger WMI til at indsamle hardwaredata
3. **UI-opdatering**: Data returneres til UI-tråd og vises i stilfulde kort
4. **Dynamisk layout**: Kort skjules hvis data ikke er tilgængelig (f.eks. ingen batteri på desktop)

## Licens

Dette projekt er open source - brug det gerne til personlige og kommercielle formål.

---

Lavet med ❤️ til Windows-brugere der gerne vil se deres systemspecifikationer på et sekund.
