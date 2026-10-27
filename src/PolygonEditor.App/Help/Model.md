ZAŁOŻENIA I MODEL DANYCH

ZAKRES PROJEKTU

Zdefiniowana długość, równoległość i domyślne G1 przy nowym Bézierze. 
Program pozwala edytować dowolny wielokąt sceny; tworzenie i usuwanie całych wielokątów przez użytkownika nie jest wymagane. Dowolną liczbę figur dodaje się w DemoSceneFactory.Create przez dopisanie polygons.Add(CreatePolygon(...)).

Współrzędne: X rośnie w prawo, Y w dół. Jednostki sceny odpowiadają jednostkom logicznym interfejsu. Komórka Bresenhama ma rozmiar 1 × 1 takiej jednostki; przy skalowaniu ekranu nie musi odpowiadać jednemu fizycznemu pikselowi.

Krawędzie długości zerowej są dopuszczone jako odcinki. Nie można nadać im równoległości ani zamienić ich w krzywą przed rozsunięciem końców. Geometria samoprzecinająca się jest dozwolona. Wnętrze wybierane jest regułą parzystości, a obwód nie jest wypełniany kolorem.

MODELE

Point2D: niemutowalna para współrzędnych double X, Y.
ScenePoint: wspólna baza punktów, stałe Guid Id i pozycja; pozycję zmienia warstwa Core.
Vertex: wierzchołek obwodu, klasa G0/G1/C1 oraz informacja, czy użytkownik wybrała ją jawnie.
ControlPoint: jeden z dwóch uchwytów Béziera.
ArcCenter: środek okręgu, pomocnicza niewidoczna zmienna geometrii, bez uchwytu myszy.
Edge: stałe Id, referencje Start i End do istniejących wierzchołków. Ma opcjonalny BezierSegment albo ArcSegment; brak obu oznacza odcinek.
BezierSegment: referencje do dwóch punktów kontrolnych. Końce pochodzą z Edge.
ArcSegment: środek i znak kierunku obiegu okręgu (+1 lub −1). Końce pochodzą z Edge; promień i kąty są obliczane.
Polygon: uporządkowane listy wierzchołków i krawędzi. Ostatnia krawędź wraca do pierwszego wierzchołka. Sąsiednie krawędzie współdzielą ten sam obiekt Vertex.
Scene: lista wielokątów i lista ograniczeń. Widoki kolekcji są tylko do odczytu, a edytory wykonują kontrolowane zmiany.
GeometricConstraint: Id i identyfikatory krawędzi. LengthConstraint dodaje długość docelową, ParallelConstraint odnosi się do dwóch krawędzi.
HitResult: wynik wskazania elementu: wielokąt i opcjonalnie wierzchołek, uchwyt albo krawędź. Nie jest częścią przechowywanej geometrii.

PODZIAŁ ODPOWIEDZIALNOŚCI

PolygonEditor.Core zawiera dane, geometrię, rasteryzację, edytory i solver. Nie odwołuje się do Avalonii.
PolygonEditor.App zależy od Core. Tworzy okna, odbiera zdarzenia i rysuje scenę. MainWindow łączy panele ze SceneCanvas. SceneRenderer wyłącznie odczytuje geometrię.

PRZEPŁYW KLIKNIĘCIA I RUCHU

SceneCanvas.OnPointerPressed → EditorInteraction.BeginDrag → HitTesting.Pick → HitResult i kopia pozycji.
SceneCanvas.OnPointerMoved → EditorInteraction.Move → SceneEditor.TryMove → ConstraintSolver.TrySolve → odświeżenie panelu i SceneCanvas.Render → SceneRenderer.Draw.
OnPointerReleased kończy gest i zwalnia przechwycenie myszy. 

ZMIANA TOPOLOGII I TYPU

Podział zastępuje jedną krawędź dwiema i usuwa jej relacje. Usunięcie wierzchołka zastępuje dwie krawędzie jedną i usuwa ich relacje, także te łączące z innymi wielokątami. Id pozostałych krawędzi się nie zmieniają, więc relacje sąsiadów nadal wskazują poprawne obiekty.

Podział Béziera w t = 0,5 korzysta z de Casteljau i początkowo zachowuje kształt. Nowe złącze ma G1. Przy wymaganiu C1 na zewnętrznych końcach solver może dopasować uchwyty, bo parametryzacja dwóch połówek zmienia długości pochodnych.

Podział łuku G0/G0 zachowuje okrąg: nowy wierzchołek ma G1, a każda połowa po jednym końcu G1. Jeżeli oryginał miał już G1, nowe złącze ma G0. Wtedy dziecko z G0/G0 staje się półokręgiem i kształt tej części może się zmienić. To przyjęta polityka pogodzenia podziału z zakazem dwóch G1 na jednym łuku.

Usunięcie wierzchołka między odcinkami tworzy odcinek. Jeżeli przynajmniej jedna zastępowana krawędź była krzywą, powstaje Bézier z początkowo zachowanymi zewnętrznymi kierunkami stycznych. Nie obiecujemy zachowania całego usuwanego obwodu.

Operacje typu, klasy i topologii zapisują SceneEditSnapshot: pozycje, oryginalne obiekty obwodu, typy, kierunki łuków, klasy i relacje. W razie niepowodzenia przywracany jest cały zapis, a nie tylko współrzędne.

GRANICE NUMERYCZNE

Solver jest lokalną metodą iteracyjną. Brak zbieżności nie dowodzi matematycznej sprzeczności relacji. Zamiast zapisywać niepoprawny wynik aplikacja odrzuca taką próbę i wyświetla komunikat.

Odrzucamy łuki o promieniu ponad 100 000 razy większym niż długość cięciwy i odcinki z kierunkiem o długości poniżej 10⁻⁸. Chroni to przed praktycznie nieskończonym okręgiem i dzieleniem przez zero. Punkty są przechowywane jako double; obraz krzywej to przybliżenie łamaną. 
