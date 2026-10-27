# Edytor wielokątów

Projekt na przedmiot Grafika komputerowa. C# / .NET 10 / Avalonia.

## Instalacja i uruchomienie od zera

Instrukcja dotyczy budowania i uruchamiania aplikacji z kodu źródłowego.

### Co trzeba zainstalować

| Składnik | Sposób przygotowania |
| --- | --- |
| **SDK .NET 10** | Instalacja ręczna według instrukcji poniżej. SDK zawiera narzędzia kompilacji i runtime; sam runtime nie wystarczy do `dotnet run`. |
| NuGet | Obsługa pakietów jest dostępna przez `dotnet` z SDK. Osobny program `nuget.exe` ani konto NuGet nie są potrzebne. |
| Avalonia | Pakiety zadeklarowano już w `.csproj`; pobierze je `dotnet restore`. Nie trzeba instalować szablonów Avalonii ani dodawać pakietów ręcznie. |
| Biblioteki systemowe Linuksa | Instalacja menedżerem pakietów według przykładu niżej. |
| Edytor i Git | Opcjonalne do uruchomienia. Wystarczy terminal i pobrany ZIP projektu. |

Bezpośrednie zależności projektu to `Avalonia.Desktop` i `Avalonia.Themes.Fluent` **12.1.4**. NuGet pobiera również ich zależności. Core nie korzysta z pakietów zewnętrznych.

### Windows

1. Pobierz instalator **.NET 10 SDK** z [oficjalnej strony Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), z sekcji **Build apps — SDK**. Wybierz Windows **x64** dla typowego komputera Intel/AMD albo **Arm64** dla Windows na ARM. Zainstaluj SDK.
2. Alternatywnie, jeśli masz WinGet, zainstaluj SDK w PowerShell:

   ```powershell
   winget install --id Microsoft.DotNet.SDK.10 --exact
   ```

   Wybierz jeden z tych sposobów instalacji.

3. Otwórz nowe okno PowerShell i sprawdź instalację:

   ```powershell
   dotnet --list-sdks
   ```

   Na liście powinna być wersja **10.0.xxx**. Visual Studio, osobny .NET Desktop Runtime i dodatkowe workloady nie są wymagane do uruchomienia tego projektu z terminala.

Szczegóły: [instalacja .NET na Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows).

### Linux — przykład dla Ubuntu 24.04 LTS i 26.04 LTS

Uruchamiaj aplikację w sesji z pulpitem graficznym. Ten projekt korzysta z domyślnego backendu X11 Avalonii; w sesji Wayland potrzebny jest XWayland. Instrukcja dla Linuksa opiera się na dokumentacji dostawców; aplikację dotychczas sprawdzano w rzeczywistym oknie na Windows.

W terminalu zainstaluj SDK i biblioteki systemowe:

```bash
sudo apt update
sudo apt install dotnet-sdk-10.0 libx11-6 libice6 libsm6 libfontconfig1
dotnet --list-sdks
```

Na liście SDK powinna być wersja **10.0.xxx**. Jeżeli pracujesz na Waylandzie i XWayland nie jest zainstalowany:

```bash
sudo apt install xwayland
```

Jeżeli `apt` nie znajduje `dotnet-sdk-10.0`, sprawdź wersję dystrybucji i odpowiednią sekcję [instrukcji Microsoft dla Ubuntu](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install). Dla innych dystrybucji użyj [instrukcji instalacji .NET na Linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux) oraz odpowiedników bibliotek z [dokumentacji zależności Avalonii](https://docs.avaloniaui.net/docs/deployment/linux). Konfigurację X11/XWayland opisuje [przewodnik Avalonii dla Linuksa](https://docs.avaloniaui.net/docs/platform-specific-guides/linux).

### Pobranie projektu i start — oba systemy

1. Pobierz repozytorium: na GitHub wybierz **Code → Download ZIP** i rozpakuj archiwum albo sklonuj je Gitem, jeśli go używasz.
2. Otwórz terminal w folderze zawierającym **`PolygonEditor.sln`** i katalog `src`. Jeśli pobrano cały folder przedmiotu, przejdź do `projekt1_codex`.
3. Wykonaj kolejno te same polecenia w PowerShell lub terminalu Linuksa:

   ```text
   dotnet restore PolygonEditor.sln
   dotnet build PolygonEditor.sln --no-restore
   dotnet run --project src/PolygonEditor.App --no-build
   ```

`restore` pobiera pakiety z NuGet, `build` kompiluje oba projekty, a `run` otwiera aplikację. Pierwsze odtworzenie wymaga internetu; pakiety trafiają do lokalnej pamięci podręcznej. Zwykle wystarczy później `dotnet run --project src/PolygonEditor.App`, które w razie potrzeby także odtwarza pakiety i kompiluje. Mechanizm opisuje [dokumentacja `dotnet restore`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore).

Po starcie automatycznie pojawiają się trzy figury: wielokąt z Bézierem, wielokąt z samoprzecięciem i wielokąt z łukiem. Scena ma ograniczenie długości i równoległość między dwiema figurami. Pomoc otwiera **F1**.

### Gdy uruchomienie się nie udaje

- **`dotnet` nie jest rozpoznawany:** po instalacji otwórz nowy terminal; sprawdź instalację SDK i dostępność `dotnet` w `PATH`.
- **Brak SDK 10 na liście:** zainstaluj SDK 10; instalacja wyłącznie runtime albo starszego SDK nie wystarczy.
- **Nie znaleziono rozwiązania/projektu:** przejdź do folderu z `PolygonEditor.sln`.
- **Nie można pobrać pakietów:** sprawdź internet i wynik `dotnet nuget list source`. Jeśli na liście brakuje źródła nuget.org, dodaj je poleceniem poniżej i powtórz `restore`. Jeśli istnieje, ale jest wyłączone, włącz je przez `dotnet nuget enable source NAZWA`, podstawiając jego nazwę z listy.

  ```text
  dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org
  ```

- **Linux: błąd biblioteki `.so` albo połączenia z ekranem:** sprawdź instalację bibliotek wymienionych wyżej oraz sesję graficzną X11/XWayland. Sam terminal przez SSH bez przekazywania obrazu nie wystarczy do pokazania okna.

## Obsługa

| Działanie | Obsługa |
| --- | --- |
| Zaznaczenie | Kliknięcie lewym przyciskiem |
| Ruch wierzchołka lub uchwytu | Przeciągnięcie niebieskiego punktu lub fioletowego kwadratu |
| Ruch całej figury | Przeciągnięcie wnętrza lub krawędzi |
| Drugi odcinek do relacji | Ctrl + klik, po zwykłym wybraniu pierwszego |
| Dodanie wierzchołka | Wybrana krawędź → Insert lub przycisk |
| Usunięcie wierzchołka | Wybrany wierzchołek → Delete lub przycisk |
| Typ krawędzi i ciągłość | Panel „Krzywe i ciągłość” |
| Długość, równoległość, usuwanie relacji | Panel relacji; długość w osobnym oknie |
| Algorytm odcinków | Radio „Biblioteczne” / „Bresenham” |
| Wyczyszczenie zaznaczenia | Escape na scenie |
| Dokumentacja | **F1 lub „Pomoc / dokumentacja”** |

Panel po prawej jest przewijany. Skróty edycji działają, gdy scena ma fokus. Pomoc jest wbudowana w aplikację i dostępna offline; zawiera instrukcję, założenia, model, algorytmy i źródła. Jej pliki źródłowe są w [`src/PolygonEditor.App/Help`](src/PolygonEditor.App/Help).

## Architektura

```text
PolygonEditor.sln
src/
  PolygonEditor.Core/        model i algorytmy, bez zależności od UI
    Model/                  scena, obwód, wierzchołki, uchwyty, łuki
    Geometry/               trafianie, próbkowanie i styczne
    Rasterization/          własny Bresenham
    Constraints/            długość i równoległość
    Editing/                polecenia, ruch i cofanie nieudanych prób
    Solving/                wspólny solver ograniczeń i ciągłości
    DemoSceneFactory.cs     czytelna definicja sceny startowej
  PolygonEditor.App/         aplikacja Avalonia, zależy od Core
    Views/, Controls/       okna, panele i scena
    Interaction/            zaznaczenie i gest przeciągania
    Rendering/              rysowanie istniejącej geometrii
    Help/                   dokumentacja osadzona w pliku aplikacji
```

`.sln` grupuje dwa projekty tworzące **jedną aplikację**: App używa biblioteki Core. Listy modelu są udostępniane tylko do odczytu. Sąsiednie krawędzie współdzielą wierzchołki, a stabilne identyfikatory krawędzi pozwalają utrzymywać relacje podczas zmian obwodu.

Kolejny wielokąt dodaje się w [`DemoSceneFactory.Create`](src/PolygonEditor.Core/DemoSceneFactory.cs), dopisując `polygons.Add(CreatePolygon(...))`. Relacje i typy ustawia się za pomocą edytorów Core.

## Algorytmy i źródła

Główne źródło stanowią wykłady z przedmiotu, przechowywane lokalnie poza tym repozytorium:

| Element | Implementacja |
| --- | --- |
| Bresenham | własne uogólnienie osi dominującej i znaków na wszystkie kierunki |
| Bézier sześcienny | baza potęgowa i różnice progresywne; kolejne próbki wyznaczane dodawaniem |
| G0/G1/C1 | G1 wymaga zgodnego zwrotu, C1 równych wektorów stycznych |
| Podział Béziera | de Casteljau w t=0,5, źródło : [materiały C.-K. Shene, MTU](https://pages.mtu.edu/~shene/COURSES/cs3621/NOTES/spline/Bezier/bezier-sub.html) |
| Łuki | własna konstrukcja analityczna i próbkowanie przez obrót promienia |
| Solver | własna implementacja sekwencyjnych korekt błędów według gradientów, wspólna dla relacji i ciągłości |
| Trafianie i wnętrze |rzut na odcinek i reguła parzystości |
| UI | oficjalna dokumentacja Avalonii: [rysowanie](https://docs.avaloniaui.net/docs/graphics-animation/custom-rendering), [zdarzenia wskaźnika](https://docs.avaloniaui.net/docs/input-interaction/pointer) |

Solver przypina przeciągany punkt, kolejno koryguje pozostałe i ponownie sprawdza wszystkie relacje. Limit wynosi 400 przebiegów; tolerancja długości 0,0001 jednostki, kąta 0,000001 radiana. Nieudana próba jest wycofywana. Brak zbieżności lokalnej metody nie jest dowodem sprzeczności układu.

## Przyjęte zasady edycji

- Wielokąt ma co najmniej trzy wierzchołki; samoprzecięcia są dozwolone.
- Długość i równoległość dotyczą odcinków. Zamiana typu, podział i usunięcie krawędzi usuwają związane z nią ograniczenia. Pozostałe krawędzie zachowują Id i relacje.
- Jawny wybór klasy ma pierwszeństwo przed domyślnym G1 Béziera. Łuk pozwala na G0/G1 i najwyżej jeden koniec G1; próba C1 jest odrzucana.
- Łuk G0/G0 jest półokręgiem. Podział takiego łuku nadaje nowemu złączu G1 i zachowuje okrąg. Przy podziale łuku z istniejącym G1 nowe złącze ma G0; część G0/G0 staje się półokręgiem, więc jej kształt może się zmienić.
- Usunięcie wierzchołka przy krzywej tworzy łączący Bézier i zachowuje zewnętrzne klasy ciągłości, jeżeli solver znajdzie rozwiązanie.
- Krzywe są przybliżane łamanymi. Liczba próbek ma praktyczny limit; nie jest to ścisła gwarancja błędu. Łuki o promieniu większym niż 100 000 długości cięciwy są odrzucane ze względów numerycznych.
- Raster ma jednostki logiczne UI. Scena pozostaje w pamięci.


