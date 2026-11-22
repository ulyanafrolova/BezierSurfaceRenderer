# Bézier Surface Renderer

Zadanie 2 — Powierzchnie Béziera. Aplikacja WinForms, która poddaje triangulacji powierzchnie Béziera
trzeciego stopnia wczytane z pliku tekstowego i wyświetla je za pomocą programowego wypełniania trójkątów
(algorytm z kubełkowym sortowaniem krawędzi), oświetlenia wyznaczanego dla każdego piksela (składowa
rozproszona Lamberta + składowa zwierciadlana), bufora głębokości, tekstury i mapy wektorów normalnych.
Pełna dokumentacja (instrukcja obsługi i opis techniczny):
[src/BezierSurfaceRenderer.App/Docs/Help.md](src/BezierSurfaceRenderer.App/Docs/Help.md)
— dostępna również w aplikacji pod klawiszem **F1**.

## Wymagania
- Windows 10/11 z .NET 8 SDK (Visual Studio 2022 17.8+ lub `dotnet` CLI).

## Kompilacja i uruchomienie
```
dotnet run --project src/BezierSurfaceRenderer.App
```
lub otwórz `BezierSurfaceRenderer.sln` w Visual Studio i uruchom projekt `BezierSurfaceRenderer.App`.

## Testy
Rdzeń niezależny od interfejsu (matematyka, wypełnianie, oświetlenie, renderowanie) jest pokryty testami
jednostkowymi, które działają na dowolnym systemie:
```
dotnet test
```

## Struktura
| Ścieżka | Zawartość |
|---|---|
| `src/BezierSurfaceRenderer.Core` | Geometria, obroty, wypełnianie (ET/AET), Z-buffer, oświetlenie, krzywa światła (bez UI) |
| `src/BezierSurfaceRenderer.App` | Interfejs WinForms, zasoby (`surfaces.txt`, tekstura, mapy normalnych), dokumentacja |
| `tests/BezierSurfaceRenderer.Core.Tests` | Testy xUnit |
