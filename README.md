# 🚀 Simple-Specs

![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%7C%2010-blue)
![Architecture](https://img.shields.io/badge/Arkitektur-x64%20%7C%20ARM64-success)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)

**Simple-Specs** er et lynhurtigt, letvægts og 100% portabelt hardware-dashboard til Windows. 

Bygget af frustration over langsomme, reklamefyldte spec-programmer. Simple-Specs giver IT-supportere, systemadministratorer og almindelige brugere det gyldne overblik over deres maskines vitale dele på et splitsekund – pakket ind i et moderne, mørkt OLED-optimeret design.

## ✨ Højdepunkter

- **🔋 Zero Install (Portabelt):** Hele programmet er pakket i én enkelt `.exe`-fil. Kræver ingen installation eller eksterne frameworks. Lige til at smide på en USB-nøgle.
- **⚡ Ægte Hastigheder:** Læser RAM-hastighed korrekt i MT/s i stedet for at forvirre med forældede MHz-tal.
- **🧠 Dynamisk Brugerflade:** Skjuler automatisk kort (som f.eks. Batteri og System/BIOS info), hvis hardwaren ikke understøtter det.
- **🌐 Indbygget Netværks-tjek:** Viser øjeblikkeligt aktivt netværkskort, Privat IP, MAC-adresse og Offentlig IP, hvis knappen trykkes.
- **📋 1-Click Eksport:** En dedikeret kopier-knap formaterer alle specs pænt til udklipsholderen – perfekt til sagsbehandlingssystemer eller salgsannoncer.
- **💪 Native ARM64 Support:** Kører fejlfrit og ufiltreret på moderne Snapdragon X processorer uden brug af emulering.

---

## 📥 Download & Brug

Du behøver ikke at kompilere koden selv for at bruge programmet. 

1. Gå til [Releases-siden](../../releases) her på GitHub.
2. Download den version, der passer til dit system:
   - `Simple-Specs-x64.exe` (Til standard Intel/AMD maskiner)
   - `Simple-Specs-ARM64.exe` (Til Snapdragon / ARM maskiner)
3. Dobbeltklik på filen og kør programmet!

---

## 🛠️ For Udviklere (Byg det selv)

Vil du pille i koden, ændre farverne eller tilføje nye funktioner? Projektet er utrolig nemt at sætte op.

### Forudsætninger
- [Visual Studio Code](https://code.visualstudio.com/)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- C# Dev Kit (VS Code Extension)

### Kørsel lokalt
Klon projektet og kør det direkte i din terminal:

```bash
git clone [https://github.com/MRBennetsen/Simple-Specs.git](https://github.com/MRBennetsen/Simple-Specs.git)
cd Simple-Specs
dotnet run
