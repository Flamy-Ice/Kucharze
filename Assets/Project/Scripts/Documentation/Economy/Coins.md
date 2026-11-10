# Coin System

System pokazuje całkowitą liczbę monet zarobionych w bieżącej sesji. Domyślna wartość to `0`.
Kwota jest wyświetlana z `$` na końcu, bez spacji, np. `0$` i `125$`.
Waluta jest wyłącznie wizualnym wynikiem: nie można jej wydawać ani wymieniać na cokolwiek.
Nie ma mechaniki nagród. Nie zależy od licznika czasu ani od ruchu Playera.

## Skrypty i odpowiedzialności

- `Economy/Coins/CoinWallet.cs` — przechowuje liczbę zarobionych monet.
- `UI/Coins/CoinDisplay.cs` — wyświetla liczbę z wybranego portfela w TextMeshPro.
- `Economy/Coins/Editor/CoinWalletEditor.cs` i `CoinDisplayEditor.cs` — Inspectory w ciemnym
  stylu projektu ze złotym akcentem. `CoinInspectorPanel.cs` współdzieli ich wygląd.

Wallet nie zna tekstu, zasad zarabiania pieniędzy ani Playera. Display wyłącznie wyświetla liczbę.
Nazwa CoinWallet pozostaje dla zgodności z istniejącymi komponentami i przypisaniami;
nie oznacza portfela z obsługą płatności.
Każdy portfel ma własny stan. Nie ma globalnego singletona ani wyszukiwania portfela w całej scenie.

## Konfiguracja krok po kroku

1. Poczekaj na kompilację skryptów w Unity.
2. Utwórz pusty obiekt `GameSession` w Hierarchy.
3. Dodaj do niego **Economy → Coin Wallet** przez Add Component.
4. Zostaw **Starting Coins = 0**. Na potrzeby sprawdzenia możesz ustawić np. `125` przed Play.
5. Utwórz **UI → Text - TextMeshPro**, nazwij go `CoinText` i ustaw pozycję oraz wygląd tekstu.
6. Jeśli Unity o to poprosi, wybierz **Import TMP Essentials**.
7. Dodaj do `CoinText` komponent **UI → Coin Display**.
8. Przeciągnij `GameSession` do pola **Wallet** w Coin Display.
9. Pole **Coin Text** automatycznie korzysta z tekstu na tym samym obiekcie. Możesz przypisać inny tekst.
10. Włącz Play. Zobaczysz początkową liczbę monet. Przyszłe zwiększenie zarobionej kwoty automatycznie odświeży tekst.

Jeżeli portfel znajduje się na tym samym obiekcie co Coin Display albo na jego rodzicu,
pole Wallet może pozostać puste. W innym układzie przypisz je ręcznie przed włączeniem widoku.
Dla wspólnego wyniku drużyny przypisz ten sam portfel do kilku widoków.
Dla osobnych wyników graczy użyj osobnych portfeli i odpowiednich przypisań.

## Podłączenie przyszłej mechaniki

Jeżeli przyszła mechanika ma zwiększać zarobioną kwotę, może mieć lokalną referencję:

```csharp
[SerializeField] private CoinWallet coinWallet;
```

Żeby zwiększyć wyświetlaną kwotę, może wywołać:

```csharp
if (coinWallet != null && coinWallet.TryAddCoins(25))
{
    // The displayed total was increased successfully.
}
```

Fragmenty to przykład integracji, nie gotowy dodatkowy komponent.
Bezpośredni odczyt: `coinWallet.Coins`. Powiadomienie o nowej sumie: `CoinsChanged(int)`.
Metoda TryAddCoins zwraca true po dopisaniu monet. Jest prostym wejściem do licznika,
nie mechaniką nagród. Bez jej wywołania liczba pozostaje na wartości początkowej.
Przyszła mechanika określi, kiedy zwiększyć wynik; sam licznik nie podejmuje tej decyzji.

## Kontrakt i ograniczenia

- Monety są całkowite. Nie ma groszy ani wartości ujemnych.
- Zwiększenia muszą być dodatnie. Zero, kwoty ujemne i operacje przekraczające
  `2 147 483 647` monet są odrzucane w całości, bez zdarzenia i bez zmiany wyniku.
- Starting Coins obowiązuje przy pierwszej inicjalizacji. Zmiana tego pola w Play Mode nie zmienia aktualnej sumy.
- Kwota dopisana przed Awake nie zostanie nadpisana podczas późniejszej inicjalizacji.
- CoinDisplay pobiera początkową liczbę i później aktualizuje się przez zdarzenia, bez Update.
- Wyłączenie widoku odłącza nasłuchiwanie. Ponowne włączenie odczytuje aktualną liczbę.
- Referencje skonfiguruj przed włączeniem widoku. Po ich zmianie w trakcie gry wyłącz i włącz komponent.
- Bez portfela widok pokazuje `0$`; bez tekstu niczego nie wyświetla. Nie powoduje to błędów dostępu do pustej referencji.
- Brak RequireComponent jest celowy: portfel i tekst mogą znajdować się na innych obiektach.
- Waluta służy wyłącznie do wyświetlania. Nagrody, płatności, zakupy i wymiana waluty nie należą do tego systemu.
- Nie ma zapisu między uruchomieniami gry, zbierania monet, przeliczania wyniku na monety ani powiązania z końcem czasu.

## Weryfikacja — 2026-11-10

- Kompilacja runtime, obu edytorów i helperów przeciwko bibliotekom Unity 6000.6.3f1 i TextMeshPro: poprawna.
- 17 sprawdzeń logiki z minimalnymi atrapami Unity/TMP: poprawne. Objęły domyślną i początkową
  sumę, nagrodę przed Awake, jedno zdarzenie po nagrodzie, odrzucanie nieprawidłowych kwot,
  limit int, niezależne portfele, odświeżanie tekstu, odłączanie zdarzeń, powrót ukrytego widoku
  i brak referencji. To sprawdzenia kodu poza silnikiem, nie testy Unity.
- Przegląd: rozdzielono przechowywanie monet, prezentację i kod edytora; brak zmian w ruchu i CountdownTimer.
- Nie uruchomiono Play Mode, sceny z dwoma graczami ani kontroli wizualnej Inspectorów,
  Undo i prefabów w Unity. Poprawność powiązań Inspectorów sprawdzono w kodzie.
- Tymczasowe pliki sprawdzające i kompilacyjne usunięto.

## Doprecyzowanie zakresu — 2026-11-10

- Zgodnie z decyzją użytkownika waluta jest wyłącznie wizualnym licznikiem zarobionej kwoty;
  nie przewiduje nagród ani możliwości wydawania lub wymiany pieniędzy.
- Dostosowano komentarze, opisy Inspectorów i instrukcję. Zachowano nazwy klas, pola serializowane
  i interfejs TryAddCoins, aby istniejące przypisania pozostały zgodne.
- Nie zmieniono logiki runtime. Wcześniejsze sprawdzenia odnoszą się do tej samej logiki.

## Format waluty

- Dodano stały sufiks `$` w CoinDisplay. Wcześniejsze sprawdzenia porównujące samą liczbę
  w tekście dotyczyły poprzedniego formatu, bez sufiksu.
- Przegląd kodu: sufiks jest dodawany przy każdym odświeżeniu, także dla wartości zerowej.
- Nie uruchomiono Play Mode ani ponownej kompilacji dla tej zmiany tekstu.

## Przegląd CoinWallet i CoinDisplay — 2026-11-10

- Uzupełniono krótkie komentarze: inicjalizacja, odrzucanie nieprawidłowych kwot,
  powiadamianie widoku, włączanie i odłączanie widoku oraz stały format waluty.
  Istniejące podpowiedzi wszystkich pól pozostawiono bez powielania.
- SRP: CoinWallet przechowuje sumę, CoinDisplay tylko ją prezentuje. Inne systemy nie wymagają zmian.
- Nie znaleziono martwego kodu. TryAddCoins jest przygotowanym wejściem dla przyszłej mechaniki;
  aktualne wyszukiwanie w C# nie znalazło jego zewnętrznych wywołań. Nie sprawdzano przypisań scen i prefabów.
- Domyślne 0 i całkowite kwoty pozostawiono. Limit int chroni przed przepełnieniem.
- RequireComponent nie jest potrzebny: CoinWallet nie wymaga innych komponentów,
  a CoinDisplay może czytać portfel i tekst z osobnych obiektów. Nie wymusza się lokalnych kopii.
- Nie zmieniano logiki. Widok nadal odświeża tekst przez zdarzenia zamiast sprawdzać go co klatkę.
- Kompilacja obu komponentów przeciwko zainstalowanym bibliotekom Unity i TextMeshPro: poprawna.
  Sprawdzenie wyrażenia formatującego: `0$`, `125$`, `2147483647$`.
- Nie uruchomiono Play Mode ani kontroli wizualnej w Unity. Tymczasowe pliki kompilacji usunięto.
