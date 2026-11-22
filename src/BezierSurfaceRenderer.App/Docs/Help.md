# Bézier Surface Renderer

Program wyświetla powierzchnie Béziera trzeciego stopnia wczytane z pliku tekstowego jako siatki trójkątów, wypełniane programowo z oświetleniem wyznaczanym dla każdego piksela, buforem głębokości (Z-buffer), teksturą i mapą wektorów normalnych. Klawisz **F1** lub **Esc** zamyka to okno.

## 1. Szybki start

- Powierzchnie są wczytywane podczas uruchamiania programu z pliku `Assets/surfaces.txt` (obok pliku programu). Inny plik można podać jako pierwszy argument wiersza poleceń: `BezierSurfaceRenderer.exe moje_powierzchnie.txt`.
- **Przeciąganie lewym przyciskiem myszy** w obszarze rysowania obraca scenę.
- Panel po prawej stronie służy do zmiany dokładności triangulacji, rysowanych elementów, materiału, koloru obiektu, mapy wektorów normalnych i światła.

## 2. Sterowanie myszą

| Akcja | Efekt |
|---|---|
| Przeciąganie lewym przyciskiem | Swobodny obrót (wirtualna kula, trackball) — punkt pod kursorem podąża za kursorem |
| Przeciąganie przy krawędzi / poza kulą | Obrót wokół osi patrzenia (oś z) |
| Shift + lewy przycisk lub prawy przycisk | Wyłącznie obrót wokół osi patrzenia |
| Przeciąganie punktu krzywej światła | Przesuwa punkt kontrolny (tylko w trybie „Edit light path”) |

Kąty obrotu nie są w żaden sposób ograniczane — scenę można obracać dowolnie wiele razy w dowolnym kierunku.

## 3. Skróty klawiaturowe

| Klawisz | Działanie |
|---|---|
| F1 | Otwiera dokumentację |
| Spacja | Uruchamia / zatrzymuje animację światła |
| E | Włącza / wyłącza tryb edycji krzywej światła (obrót myszą jest wtedy wyłączony) |
| R | Przywraca początkową orientację sceny |
| 1 | Pokazuje / ukrywa wieloboki Béziera (siatki kontrolne) |
| 2 | Pokazuje / ukrywa siatkę trójkątów |
| 3 | Pokazuje / ukrywa wypełnione trójkąty |
| S | Wybiera następną powierzchnię, której kolor obiektu IO jest edytowany |
| T | Przełącza kolor obiektu wybranej powierzchni: jednolity kolor / tekstura |
| N | Włącza / wyłącza mapę wektorów normalnych |
| [ oraz ] | Zmniejsza / zwiększa dokładność triangulacji |
| Ctrl+T | Wczytuje teksturę dla wybranej powierzchni |
| Ctrl+N | Wczytuje mapę wektorów normalnych |
| Ctrl+L | Wybór koloru światła IL |
| Ctrl+K | Wybór jednolitego koloru obiektu IO wybranej powierzchni |
| Strzałki, PgUp/PgDn, Home/End, kółko myszy | Zmiana wartości aktywnego suwaka |

## 4. Panel sterowania

### Geometry (geometria)
- **Triangulation** — liczba komórek siatki (u, v) na bok, od 1 do 64. Każda komórka jest dzielona na dwa trójkąty, więc jedna powierzchnia ma 2·n² trójkątów.
- **Bézier control net** — wielobok Béziera (siatka 4×4 punktów kontrolnych) każdej powierzchni (kolor pomarańczowy).
- **Triangle mesh** — krawędzie trójkątów (kolor cyjanowy).
- **Filled triangles** — wypełnione, oświetlone trójkąty z buforem głębokości.

### Material (materiał)
- **kd**, **ks** (0–1) oraz **m** (1–100) — współczynniki modelu oświetlenia, jednakowe dla wszystkich trójkątów.

### Object color (kolor obiektu IO)
- **Object color IO** — wybierany **niezależnie dla każdej powierzchni**: najpierw wybieramy powierzchnię z listy (lub klawiszem S), a potem **jednolity kolor** (przycisk „Color…”, który pokazuje bieżący kolor) albo **teksturę**. Tekstura jest przeskalowana do dziedziny parametrów: punkt P(u, v) przyjmuje kolor teksela (u·szerokość, v·wysokość). Domyślna tekstura jest wczytywana przy starcie (druga powierzchnia startuje z teksturą, pierwsza z jednolitym kolorem); inną można wczytać przyciskiem „Load texture…”.

### Normal map (mapa wektorów normalnych)
- **Normal map** — modyfikacja wektora normalnego na podstawie wczytanej mapy wektorów normalnych (domyślnie: cegły; w katalogu `Assets/normalmaps` jest też mapa ze sferą).

### Light (światło)
- **Light z** — współrzędna z płaszczyzny, w której porusza się światło.
- **Light color…** — kolor światła IL, domyślnie biały (kolor przycisku pokazuje bieżący kolor).
- **Animate light** — światło porusza się po zamkniętej krzywej Béziera; odznaczenie pola (lub Spacja) zatrzymuje animację.
- **Edit light path** — pokazuje wielobok kontrolny krzywej (punkty V0 … V7; większy kwadrat to V0 = V8). Każdy punkt można przeciągnąć myszą. Żółty punkt na krzywej to aktualne położenie źródła światła.

## 5. Format pliku wejściowego

Każda powierzchnia to 16 kolejnych wierszy `x y z` (liczby rzeczywiste, kropka jako separator dziesiętny) w kolejności V00, V01, V02, V03, V10, …, V33: wiersz k zawiera punkt V(i, j), gdzie i = k / 4 (odpowiada parametrowi u), a j = k mod 4 (odpowiada parametrowi v). Kolejne powierzchnie występują po sobie w grupach po 16 wierszy; wymagane są co najmniej dwie. Puste wiersze są pomijane. Dołączony plik zawiera „kopułę” i falujące „siodło”; obie powierzchnie są funkcjami względem płaszczyzny xy, a ich punkty kontrolne wypełniają sześcian [−1, 1]³ o środku w punkcie (0, 0, 0).

## 6. Przekształcenie ruchu myszy na obrót obiektu (opis techniczny)

### 6.1 Narastające składanie orientacji
Scena przechowuje jedną macierz orientacji R o wymiarach 3×3 (konwencja wektorów kolumnowych, P' = R·P), początkowo równą macierzy jednostkowej. Każde zdarzenie ruchu myszy podczas przeciągania wyznacza mały obrót R_przyrost, obliczany wyłącznie na podstawie **poprzedniego** i **bieżącego** położenia kursora, i składa go z aktualną orientacją:

```
R_nowa = R_przyrost · R_aktualna
R_nowa = ortonormalizacja(R_nowa)        (Gram–Schmidt)
```

Orientacja nigdy nie jest więc wyznaczana na podstawie położenia, w którym rozpoczęto przeciąganie, a kąty nie są ograniczane. Ponieważ R_przyrost jest mnożona z lewej strony, działa w układzie ekranu (obserwatora): ten sam ruch myszy zawsze obraca obiekt tak samo względem ekranu, niezależnie od tego, jak jest już obrócony. Tysiące mnożeń zmiennoprzecinkowych stopniowo psują ortogonalność macierzy, dlatego po każdym kroku jest ona ortonormalizowana (pierwsza kolumna jest normalizowana, druga ortogonalizowana względem pierwszej i normalizowana, trzecia to ich iloczyn wektorowy).

Wierzchołki siatki są zawsze obracane z oryginalnych, nieobróconych danych za pomocą złożonej macierzy R: P' = R·P, Pu' = R·Pu, Pv' = R·Pv, N' = normalize(R·N). Punkty są obracane wokół środka sceny (0, 0, 0), a wektory styczne i normalne — jako wektory kierunkowe.

### 6.2 Wirtualna kula (przeciąganie lewym przyciskiem)
Obszar rysowania traktujemy tak, jakby w jego środku znajdowała się szklana kula o promieniu r = ½·min(szerokość, wysokość), której powierzchnię użytkownik przesuwa kursorem. Położenie kursora względem środka obszaru (oś y w górę) p = (x, y) jest rzutowane na kulę:

```
q = p / r
jeżeli |q|² ≤ 1:   s = (qx, qy, sqrt(1 − |q|²))     punkt na przedniej półkuli
w przeciwnym razie: s = (qx, qy, 0) / |q|           punkt na brzegu (konturze) kuli
```

Dla dwóch kolejnych położeń kursora p0 → p1, rzutowanych na punkty kuli s0, s1:

```
oś    = normalize(s0 × s1)
kąt   = acos(clamp(s0 · s1, −1, 1))
R_przyrost = obrót wokół osi o kąt               (wzór Rodriguesa)
```

(Jeżeli |s0 × s1| = 0, mysz się nie przesunęła i R_przyrost = I). Jest to najkrótszy obrót przeprowadzający s0 w s1, więc punkt kuli znajdujący się pod kursorem dokładnie podąża za kursorem — dzięki temu sterowanie jest intuicyjne:

- ruch poziomy przez środek: s0 × s1 jest równoległy do osi y → obiekt obraca się w lewo/w prawo wokół osi pionowej;
- ruch pionowy: obrót wokół poziomej osi x → obiekt pochyla się do obserwatora lub od niego;
- ruch ukośny: obrót wokół osi prostopadłej do kierunku ruchu;
- ruch wzdłuż zewnętrznej części obszaru: oba punkty leżą na konturze kuli (z = 0), więc s0 × s1 jest równoległy do osi z → obiekt obraca się w płaszczyźnie ekranu, jak kierownica.

Wzór Rodriguesa dla wersora osi a = (x, y, z), c = cos θ, s = sin θ, t = 1 − c:

```
      | t·x·x + c     t·x·y − s·z   t·x·z + s·y |
R  =  | t·x·y + s·z   t·y·y + c     t·y·z − s·x |
      | t·x·z − s·y   t·y·z + s·x   t·z·z + c   |
```

Dla a równego osi x, y lub z wzór sprowadza się dokładnie do macierzy Rx, Ry, Rz podanych w treści zadania.

### 6.3 Obrót wokół osi patrzenia (Shift + lewy przycisk lub prawy przycisk)
Kąt, o jaki kursor obrócił się wokół środka obszaru rysowania między dwoma kolejnymi położeniami, jest stosowany jako obrót wokół osi z (osi patrzenia):

```
kąt = atan2(p0 × p1, p0 · p1)       (dwuwymiarowy iloczyn wektorowy i skalarny)
R_przyrost = Rz(kąt)
```

Ruch myszy przeciwny do ruchu wskazówek zegara obraca scenę w tym samym kierunku.

### 6.4 Przykład obliczeniowy
Obszar rysowania 800×800 (r = 400). Kursor przesuwa się ze środka p0 = (0, 0) do p1 = (40, 0):
s0 = (0, 0, 1), s1 = (0,1; 0; 0,995); s0 × s1 = (0; 0,1; 0), zatem oś to +y, a kąt to acos(0,995) ≈ 5,7°. R_przyrost = Ry(5,7°): przód obiektu przesuwa się w prawo razem z kursorem.

## 7. Potok renderowania (opis techniczny)

- **Powierzchnia**: P(u,v) = Σ Σ V(i,j)·B(i,3)(u)·B(j,3)(v); Pu i Pv to dokładne pochodne cząstkowe (postać Bernsteina stopnia 2); N = normalize(Pu × Pv).
- **Triangulacja**: regularna siatka (n+1)×(n+1) w dziedzinie (u, v), dwa trójkąty na komórkę; każdy wierzchołek przechowuje P, Pu, Pv, N przed obrotem i po obrocie oraz parametry (u, v).
- **Rzutowanie**: rzut prostokątny na płaszczyznę xy; początek układu współrzędnych znajduje się w środku obszaru rysowania, oś y jest skierowana w górę. Skala jest dobrana tak, aby sfera zawierająca wszystkie punkty kontrolne mieściła się w obszarze rysowania, więc powierzchnie mieszczą się przy każdym obrocie.
- **Wypełnianie**: każdy trójkąt jest wypełniany niezależnie algorytmem przeglądania liniami poziomymi z kubełkowym sortowaniem krawędzi (tablica krawędzi ET z kubełkami indeksowanymi pierwszą linią skanowania; tablica krawędzi aktywnych AET sortowana po x w każdej linii). Próbkowane są środki pikseli z regułą „górna-lewa krawędź”, więc sąsiednie trójkąty nie nachodzą na siebie i nie zostawiają szczelin.
- **Kolor piksela**: dla każdego piksela wyznaczane są współrzędne barycentryczne w rzutowanym trójkącie (dokładne dla rzutu prostokątnego) i na ich podstawie interpolowane są z, N, Pu, Pv, u oraz v. Wektor N jest ponownie normalizowany.
- **Bufor głębokości (Z-buffer)**: obserwator patrzy z kierunku +z (V = [0, 0, 1]); piksel jest zapisywany tylko wtedy, gdy jego z jest większe (bliżej obserwatora) niż wartość zapisana w buforze.
- **Mapa wektorów normalnych** (opcjonalnie): N_tekstury = 2·RGB/255 − 1 (znormalizowany), M = [Pu, Pv, N] (kolumny; Pu i Pv znormalizowane), N = normalize(M · N_tekstury).
- **Oświetlenie**, osobno dla składowych R, G, B, wartości z przedziału 0–1:

```
I = kd·IL·IO·cos(N, L) + ks·IL·IO·cos^m(V, R)
L = normalize(światło − P),  V = [0, 0, 1],  R = 2⟨N, L⟩N − L
```

  uwzględniane są **obie strony powierzchni**: jeżeli interpolowany wektor normalny jest odwrócony od obserwatora (N_z < 0), jest zastępowany przez −N, więc spód powierzchni jest oświetlany tak samo jak wierzch. Ujemny cosinus oznacza, że światło jest po drugiej stronie powierzchni — dany składnik wynosi wtedy 0. Dopiero wynik końcowy jest mnożony przez 255 i ograniczany do 255.
- **Światło**: punktowe źródło światła poruszające się po zamkniętej krzywej Béziera stopnia 8 (punkty kontrolne V0 … V8, V8 = V0, czyli 9 punktów, w tym 8 różnych) w płaszczyźnie z = const, w układzie obserwatora (krzywa nie obraca się razem ze sceną). Punkt krzywej dla parametru t jest wyznaczany algorytmem de Casteljau; t rośnie z prędkością 0,1 okrążenia na sekundę.
- **Wydajność**: poziome pasy obrazu są renderowane równolegle; każdy pas ma na wyłączność swoje wiersze bufora koloru i bufora głębokości.
