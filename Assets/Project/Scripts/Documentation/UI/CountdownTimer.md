# CountdownTimer — licznik TextMeshPro

Skrypt: `Assets/Project/Scripts/UI/Timers/CountdownTimer.cs`.

Odlicza czas w dół i wyświetla go jako `m:ss`: `5:00`, `1:05`, `0:09`, `0:00`.
Minuty nie mają zera wiodącego, a sekundy zawsze mają dwie cyfry.
Po zakończeniu licznik zatrzymuje się na `0:00`.

## Konfiguracja krok po kroku

1. Poczekaj, aż Unity skompiluje skrypt.
2. W Hierarchy wybierz **Create → UI → Text - TextMeshPro**. Unity utworzy Canvas, jeśli go brakuje.
3. Jeśli pojawi się okno importu TMP, kliknij **Import TMP Essentials**.
4. Nazwij obiekt tekstu `TimerText`. Ustaw jego pozycję, rozmiar, kolor i wielkość czcionki.
5. Zaznacz `TimerText`, kliknij **Add Component** i wyszukaj **Countdown Timer**.
6. W **Duration Seconds** wpisz czas w sekundach, np. `120` dla dwóch minut.
7. Zostaw włączone **Start Automatically**, aby odliczanie ruszyło przy uruchomieniu obiektu.
8. **Timer Text** jest pobierany automatycznie z tego samego obiektu. Możesz też przypisać inny tekst z Hierarchy.
9. Włącz **Play**. Dla `120` zobaczysz `2:00`, następnie `1:59`, aż do `0:00`.

## Sterowanie

- `PauseTimer()` zatrzymuje odliczanie.
- `ResumeTimer()` wznawia je lub uruchamia, jeśli Start Automatically jest wyłączone.
- `RestartTimer()` przywraca Duration Seconds i natychmiast rozpoczyna odliczanie.

Metody można wywoływać z innego skryptu lub przypisać w **Button → On Click()**,
przeciągając obiekt z komponentem i wybierając odpowiednią metodę CountdownTimer.

Licznik używa czasu gry: `Time.timeScale = 0` wstrzymuje odliczanie.
Wyłączenie komponentu lub obiektu również je wstrzymuje; ponowne włączenie zachowuje pozostały czas.
Bez przypisanego tekstu nadal odlicza, ale niczego nie wyświetla.
Każdy komponent przechowuje własny czas; dla niezależnych liczników przypisz osobne teksty.

## Inspector

Edytor znajduje się w `UI/Timers/Editor/CountdownTimerEditor.cs` i działa automatycznie
po zaznaczeniu obiektu z licznikiem. Nie dodawaj go jako komponentu.
Korzysta ze stylu istniejących Inspectorów PlayerGravity i PlayerAudio: ciemnego panelu,
karty ustawień i kolorowego nagłówka. Licznik otrzymał bursztynowy akcent oraz jednostkę `s`
przy polu Duration. Pole nadal zapisuje tę samą wartość Duration Seconds.
Wspólne helpery edytora zapewniają etykiety o szerokości 145 px, podpowiedzi tylko nad
nazwami ustawień i dotychczasowy wygląd checkboxów. Pola korzystają z powiązań Unity,
aby zachować Undo, nadpisania prefabów i edycję wielu zaznaczonych obiektów.

## Weryfikacja — 2026-11-10

- Kompilacja C# przeciwko zainstalowanym bibliotekom Unity 6000.6.3f1 i TextMeshPro: poprawna.
- Sprawdzono obliczenie formatu dla 300, 65, 9, 0, 600 i 0,1 sekundy: odpowiednio
  `5:00`, `1:05`, `0:09`, `0:00`, `10:00`, `0:01`.
- Przegląd kodu: czas nie schodzi poniżej zera, brak globalnego stanu i zależności od ruchu Playera.
- Nie uruchomiono Play Mode ani nie zweryfikowano wyglądu tekstu w scenie.
- Tymczasowe pliki kompilacji usunięto po sprawdzeniu.

## Przegląd komponentu — 2026-11-10

- Dodano krótkie komentarze po angielsku i podpowiedzi Inspector dla wszystkich trzech ustawień.
- Zachowano zachowanie i publiczne metody. Wyszukiwanie w skryptach C# nie znalazło zewnętrznych
  wywołań tych metod; pozostają przydatne dla przycisków Unity i przyszłego sterowania.
- Komponent ma jedną odpowiedzialność: obsługę widocznego licznika UI. Oddzielny model czasu
  będzie uzasadniony dopiero wtedy, gdy czas stanie się niezależną regułą rozgrywki.
- Brak RequireComponent jest celowy: tekst może należeć do innego obiektu, a TMP_Text jest
  abstrakcyjną klasą wspólną dla tekstu UI i tekstu 3D. Wymuszanie konkretnego wariantu
  tworzyłoby niepotrzebną zależność. Brak tekstu jest bezpiecznie obsługiwany.
- Domyślne 300 sekund i automatyczny start pozostawiono jako przykładową konfigurację.
- Nie zmieniano skryptów ruchu ani innych systemów. Nie stwierdzono potrzeby dodatkowej optymalizacji.
- Ponowna kompilacja po dodaniu komentarzy i podpowiedzi: poprawna. Pliki tymczasowe usunięto.
  Nie uruchomiono Play Mode ani nie sprawdzono podpowiedzi wizualnie w Inspectorze.

## Weryfikacja edytora — 2026-11-10

- Kompilacja licznika, nowego edytora i używanych wspólnych helperów przeciwko bibliotekom
  zainstalowanego Unity 6000.6.3f1 i TextMeshPro: poprawna.
- Przegląd powiązań: wszystkie trzy nazwy pól odpowiadają istniejącym polom serializowanym.
- Edytor znajduje się poza assembly systemu ruchu, w folderze Editor, i korzysta z jego
  publicznych helperów. Nie zmienia kodu działającego w grze ani ustawień istniejących komponentów.
- Nie wykonano kontroli wizualnej w Unity ani testu Undo, prefabów lub wielokrotnego zaznaczenia
  w działającym Inspectorze. Tymczasowe pliki kompilacji usunięto.
