INSTRUKCJA OBSŁUGI

Po uruchomieniu powstaje scena demonstracyjna: wielokąt z Bézierem po lewej, wielokąt z samoprzecięciem po prawej oraz wielokąt z łukiem niżej. Górny odcinek pierwszego wielokąta ma zadaną długość 180 i jest równoległy do poziomego odcinka drugiego wielokąta.

MYSZ I ZAZNACZENIE

• Lewy przycisk wybiera element. Samo kliknięcie nie zmienia geometrii.
• Przeciągnięcie niebieskiego wierzchołka przesuwa go pod kursor. Próg rozpoczęcia ruchu wynosi 3 jednostki sceny.
• Przeciągnięcie fioletowego kwadratu przesuwa punkt kontrolny Béziera. Przerywane linie tworzą pomocniczy wielobok kontrolny.
• Przeciągnięcie wnętrza lub krawędzi przesuwa cały wielokąt.
• Ctrl + klik drugiego odcinka wybiera parę do równoległości. Pierwszy jest czerwony, drugi turkusowy. Mogą należeć do różnych wielokątów.
• Przy nakładaniu elementów pierwszeństwo ma wierzchołek, potem uchwyt, potem krawędź i wnętrze. W ramach tego samego rodzaju wybierany jest wielokąt narysowany później. Tolerancja trafienia wynosi 8 jednostek.

KLAWISZE I PRZYCISKI

Insert / „Dodaj wierzchołek”: podział wybranej krawędzi. Dla odcinka jest to środek; dla Béziera i łuku punkt parametru t = 0,5.
Delete / „Usuń wierzchołek”: usunięcie wybranego wierzchołka i połączenie jego sąsiadów. Wielokąt musi zachować co najmniej 3 wierzchołki.
Escape na scenie: koniec przeciągania i wyczyszczenie zaznaczenia, bez cofania wykonanych zmian.
F1 / „Pomoc / dokumentacja”: otwarcie tego okna. Escape w pomocy zamyka pomoc.
Enter w oknie długości: zapis. Escape: anulowanie.
Insert, Delete i Escape sceny działają po ustawieniu fokusu na scenie, np. kliknięciu płótna.

OGRANICZENIA ODCINKÓW

Wybierz odcinek i kliknij przycisk zadania długości w panelu relacji. Okno podpowiada aktualną długość. Wpisz dodatnią liczbę; akceptowany jest separator bieżącego języka systemu i kropka. Powtórne zadanie długości zastępuje poprzednią.

Wybierz pierwszy odcinek, Ctrl + klik drugi, następnie dodaj równoległość. Przeciwne zwroty odcinków też są równoległe. Można połączyć jedną krawędź kilkoma relacjami, np. długością i równoległością do kilku innych odcinków.

Oznaczenia przy krawędziach i na liście: L z numerem i długością oraz ∥ z numerem relacji. Ten sam numer ∥ wskazuje jej obie krawędzie. Aby usunąć ograniczenie, wybierz je na liście i użyj przycisku usunięcia relacji.

KRZYWE I CIĄGŁOŚĆ

Po zaznaczeniu krawędzi użyj „Zamień na Béziera” albo „Zamień na łuk”. Odpowiedni przycisk pozwala wrócić do odcinka. Można również zmienić jedną krzywą w drugą. Relacje długości i równoległości tej krawędzi są wtedy usuwane.

Kliknij wierzchołek, wybierz klasę i naciśnij „Ustaw ciągłość wierzchołka”:
• G0: segmenty mają wspólny punkt; kierunki stycznych są dowolne.
• G1: styczne mają ten sam kierunek i zwrot, a ich długości mogą się różnić.
• C1: styczne mają również tę samą długość; dostępne przy Bézierze, jeżeli obok nie ma łuku.

Nowy Bézier ustawia domyślne G1 na końcach. Wcześniejszy jawny wybór użytkownika ma pierwszeństwo. Zmiana klasy zachowuje wierzchołki, dopasowując wolne uchwyty i środki łuków; niemożliwa próba jest cofana.

Łuk może mieć G1 na najwyżej jednym końcu. Przy G0 na obu końcach jest półokręgiem o promieniu połowy długości cięciwy; przycisk „Zmień stronę półokręgu” wybiera drugą stronę. Przy G1 stronę wyznacza styczna sąsiada i przycisk jest niedostępny. C1 przy łuku jest niedozwolone.

Podczas ruchu solver dopasowuje inne elementy do relacji. Aktywny punkt jest przypięty do żądanej pozycji. Przy uchwycie Béziera w pierwszej próbie chronione są także końce jego segmentu. Jeśli dodatkowe relacje to uniemożliwiają, druga próba może poruszyć te końce. Jeśli żadna próba nie da poprawnego wyniku, zostaje ostatnia poprawna scena i pojawia się komunikat.

RYSOWANIE

Zaznaczenie „Biblioteczne” wybiera DrawLine Avalonii. Zaznaczenie „Bresenham” wybiera własny algorytm wyznaczania komórek rastra. Ten wybór zmienia wyłącznie obraz. Punkty krzywych są w obu trybach obliczane tymi samymi algorytmami; ich kolejne próbki łączą wybrane odcinki.

Scena jest przechowywana w pamięci. Ponowne uruchomienie odtwarza scenę demonstracyjną.
