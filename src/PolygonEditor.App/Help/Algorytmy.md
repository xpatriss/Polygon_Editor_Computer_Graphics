ALGORYTMY

ODCINKI — BRESENHAM

Wybieramy oś dominującą, czyli tę, w której zmiana współrzędnej jest większa. W każdej iteracji przesuwamy się o jedną komórkę w tej osi. Zmienna decision rozstrzyga, czy również wykonać krok w drugiej osi.

major = większa z wartości |dx| i |dy|, minor = mniejsza. Początkowo decision = 2·minor − major. Jeśli decision >= 0, wykonujemy krok w osi pobocznej i odejmujemy 2·major. W każdym obrocie dodajemy 2·minor. Znaki dx i dy określają zwroty kroków. Gdy końce są równe, wynikiem jest pojedyncza komórka.


BÉZIER TRZECIEGO STOPNIA

P0 i P3 są końcami, P1 i P2 uchwytami. Parametr t przechodzi od 0 do 1. Uchwyty zwykle nie leżą na krzywej; wskazują kierunki i siłę odchylenia.

B(t) = (1−t)³P0 + 3(1−t)²tP1 + 3(1−t)t²P2 + t³P3.

Przechodzimy do bazy potęgowej B(t) = a·t³ + b·t² + c·t + d:
a = −P0 + 3P1 − 3P2 + P3
b = 3P0 − 6P1 + 3P2
c = −3P0 + 3P1
d = P0.

Dla kroku h = 1/n inicjujemy osobno X i Y:
value = d
first = a·h³ + b·h² + c·h
second = 6a·h³ + 2b·h²
third = 6a·h³.

Kolejne punkty wyznaczamy tylko dodawaniem, w tej kolejności: value += first; first += second; second += third. third jest stałe. Ostatni punkt ustawiamy dokładnie na P3, aby nie zostawić szczeliny przez zaokrąglenia.

Liczba kroków wynika z długości wieloboku kontrolnego (około jeden krok na 2 jednostki), z granicami 16–2048. To własna praktyczna heurystyka, a nie ścisła gwarancja błędu aproksymacji. Evaluate używa wzoru Bernsteina jako niezależnego odniesienia do sprawdzeń; rysowanie korzysta z Sample i różnic progresywnych.


STYCZNE I CIĄGŁOŚĆ

Pochodne Béziera: B′(0) = 3(P1−P0), B′(1) = 3(P3−P2). Dla odcinka parametryzowanego od 0 do 1 pochodna wynosi End−Start. To dlatego C1 na złączu odcinka i Béziera wymaga odpowiedniego dzielenia uchwytu przez 3.

G0 jest zapewnione przez wspólny obiekt Vertex. G1 porównuje pełny kąt stycznych i wymaga zgodnego zwrotu; same równoległe styczne o przeciwnych zwrotach nie wystarczają. C1 zeruje różnicę wektorów stycznych osobno w osi X i Y. Zerowa styczna przy wymaganym G1 powoduje odrzucenie próby.

PODZIAŁ BÉZIERA 

Algorytm de Casteljau: wyznaczamy środki kolejnych par P0P1, P1P2, P2P3; następnie środki otrzymanych par, na końcu wspólny punkt krzywej. Te punkty tworzą dwa nowe wieloboki kontrolne. Używamy go wyłącznie do podziału krzywej, nie do jej rysowania.

ŁUKI

Przy G0/G0 środek O = (A+B)/2, promień r = |B−A|/2. Wybór kierunku obiegu określa stronę półokręgu.

Przy G1 środek jest dodatkową zmienną solvera. Wymagamy |A−O| − |B−O| = 0 oraz zgodnej stycznej z sąsiednim segmentem. Styczna łuku jest obrotem promienia o 90° ze znakiem obiegu: direction·(−ry, rx).

Początkowy środek przy zmianie klasy wyznaczamy z jednostkowej normalnej n do sąsiedniej stycznej. Dla końca A i drugiego końca B:
O = A + s·n, gdzie s = |B−A|² / (2·(B−A)·n).
Iloczyn skalarny w mianowniku bliski zeru oznacza brak stabilnej skończonej konstrukcji. Solver sprawdza także wszystkie pozostałe relacje, bo sąsiednia krzywa może wymagać dopasowania.

Rysowanie: z atan2 obliczamy kąt początkowy i skierowany zakres łuku. Promień obracamy o stały mały kąt, korzystając z macierzy obrotu. Sinus i cosinus kroku są liczone raz, a nie dla każdej próbki. Próbkowanie ograniczamy do 8–4096 kroków, zależnie od długości i kąta łuku. Ostatnia próbka jest dokładnym końcem.


SOLVER — WŁASNA IMPLEMENTACJA SEKWENCYJNYCH KOREKT
 
1. SceneEditor odtwarza geometrię z początku gestu, nakłada żądaną pozycję i przypina aktywny punkt. Dzięki temu kolejne ruchy nie sumują błędów poprzednich zdarzeń.
2. Solver oblicza błąd każdej relacji i jej gradient, czyli wpływ małego ruchu każdego punktu na błąd.
3. Dla wolnych punktów wykonuje korektę Δp = −error·gradient / sumę kwadratów wszystkich wolnych gradientów. Punkty przypięte nie są przesuwane. Wkłady wspólnego punktu w tej samej relacji sumujemy.
4. Kolejno koryguje długości i równoległość, zależne środki półokręgów, równość promieni łuków i klasy ciągłości. Każda poprawka może naruszyć poprzednią relację, dlatego sprawdza je ponownie i powtarza cały przebieg.
5. Kończy po spełnieniu tolerancji albo najwyżej 400 przebiegach. Niepowodzenie przywraca pozycje z wejścia do solvera; edytor odtwarza ostatnią poprawną scenę lub pełny zapis sprzed polecenia.

Błąd długości: |End−Start| − wymaganaDługość.
Błąd równoległości: kąt atan2(wyznacznik, iloczyn skalarny), sprowadzony do zakresu [−π/2, π/2]. Przeciwne zwroty są dozwolone.
Błąd G1: pełny kąt pomiędzy stycznymi, bez utożsamiania przeciwnych zwrotów.
Błąd C1: różnica współrzędnych wektorów stycznych.
Błąd okręgu: różnica odległości końców od środka.

Tolerancja długości i składowych C1: 0,0001 jednostki. Tolerancja kąta: 0,000001 radiana. Jednorazową korektę błędu kątowego ograniczamy do ±0,25 radiana, aby zmniejszyć gwałtowne skoki.

Metoda jest lokalna, a nie symbolicznym rozwiązaniem całego układu. Może odrzucić również układ matematycznie rozwiązywalny, jeśli nie znajdzie rozwiązania z bieżącego stanu w zadanym limicie. 

TRAFIANIE MYSZĄ 

DistanceSquared liczy kwadrat odległości punktów, bez zbędnego pierwiastka. DistanceToSegmentSquared wyznacza rzut na prostą przez parametr t = ((P−A)·(B−A))/|B−A|². Ogranicza t do [0,1], aby dostać najbliższy punkt odcinka, i zwraca kwadrat odległości od niego. Jeśli A = B, porównuje z A.

Krzywe sprawdzamy jako tę samą łamaną próbek, która jest rysowana. Wnętrze określa liczba przecięć półprostej z obwodem: nieparzysta oznacza wnętrze, parzysta zewnętrze. Dzięki temu obsługiwane są także samoprzecięcia.



