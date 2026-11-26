# Układ folderów projektu

Główny poziom `Assets` ma trzy foldery:

```text
Assets/
  Project/                     # Nasza gra, jej sceny, grafiki, skrypty i ustawienia
    Settings/BuildProfiles/    # Nasze profile budowania Windows i Android
    Prefabs/Networking/        # Także DefaultNetworkPrefabs.asset
  ThirdParty/                  # Zewnętrzne dodatki spoza Unity i zespołu
  Unity/                       # Oficjalne zasoby i konfiguracja pakietów Unity
    Settings/                  # Ustawienia i profile renderowania URP
    TextMesh Pro/              # Zasoby TMP, domyślna czcionka, shadery i licencje
    XR/                        # Ustawienia XR, OpenXR i symulacji
      CompositionLayers/      # Ustawienia pakietu Composition Layers
```

TextMesh Pro jest oficjalnym pakietem Unity, dlatego jego importowane zasoby należą do `Unity`, a nasze czcionki do `Project/UI/Fonts`. `DefaultNetworkPrefabs.asset` zawiera listę naszych prefabów, dlatego pozostaje w `Project`. Netcode ma zapisaną nową ścieżkę tej listy w swoich ustawieniach projektu. Nie trzeba ponownie generować prefabów ani konfiguracji XR po zmianie folderów.

Poniższa szczegółowa mapa dotyczy `Assets/Project/`:

```text
Scenes/Debug/Kuba/
  Main Menu.unity              # Pierwsza scena gry
  Lobby.unity                  # Lobby hosta i klientów
  Kuba.unity                   # Obecna rozgrywka online
  Prototypes/                  # Wcześniejsze sceny do pracy i testów

Prefabs/
  Networking/                  # Połączenie, sesja, lista prefabów lobby
  Characters/                  # Postać lobby
  Cameras/                     # Prefaby kamer

Placeholders/
  Lobby/Materials/
    Architecture/              # Ściany, podłogi i pozostała zabudowa
    Furnishings/               # Wyposażenie i jego materiały
    RoomColors/                # Kolory pokoi i oznaczenia gotowości
  Characters/
    Materials/                 # Materiały zastępczej postaci
    Meshes/                    # Siatki postaci i znacznika

Development/Networking/        # Starszy prefab online, lista i wynik konfiguracji

UI/
  MainMenu/
    Artwork/                   # Tło, logo i MainMenu_Group
    Buttons/                   # Gotowe grafiki przycisków
  HUD/
    Controls/                  # Grafiki sterowania
    Panels/                    # Tła paneli
    Icons/                     # Ikony

UI/Fonts/
  Sources/                     # Oryginalne pliki czcionek
  TextMeshPro/                 # Font Assets używane przez teksty TMP
Animations/Controllers/        # Kontrolery Animatora

Scripts/
  Player/                      # Moduły gracza; dotychczasowy podział zachowany
  Networking/
    Connection/                # Relay i połączenie z menu
    Lobby/                     # Zasady lobby, gotowość i przypisywanie pokoi
    Tests/                     # Narzędzia do sprawdzania sieci
    Editor/Lobby/              # Generowanie i wygląd lobby
    Editor/Online/             # Konfiguracja usług i wcześniejszego testu
  UI/
    MainMenu/
      Audio/                   # Muzyka, głośność i dźwięki przycisków
      Display/                 # Ustawienia obrazu i rozpoznawanie platformy
      Interaction/             # Animacje i obsługa kontrolek
      Views/                   # Nawigacja, Credits i wygląd paneli
      Editor/                  # Przygotowanie sceny menu
    Controls/                  # Sterowanie ekranowe
    HUD/                       # Licznik czasu i wyświetlanie monet
      Editor/                  # Inspektory tych elementów
  Editor/ProjectStructure/     # Wspólne ścieżki i kontrola układu
  Documentation/               # Instrukcje i wykonane sprawdzenia
```

`Placeholders` zawiera aktualnie używane, zastępcze elementy wizualne. Nie są to pliki przeznaczone do usunięcia: lobby i postać nadal ich potrzebują. Docelowe grafiki dostarczone do menu pozostają w `UI/MainMenu`. `Development` i `Prototypes` oddzielają wcześniejsze narzędzia oraz sceny od obecnego wejścia do gry.

Sceny innych członków zespołu zachowują swoje foldery w `Project`. Zasoby zewnętrzne grupujemy według dostawcy w `ThirdParty`, a oficjalne zasoby Unity w `Unity`. Pliki `.meta` przechodzą razem z assetami, aby Unity zachowało referencje. Zmiana folderów nie scala prefabów ani nie zmienia reguł rozgrywki.

## Jak znaleźć scenę i sprawdzić listę

1. W oknie **Project** otwórz **Assets > Project > Scenes > Debug > Kuba**.
2. Kliknij dwukrotnie **Main Menu**, aby otworzyć wejście do gry.
3. Otwórz **File > Build Profiles > Open Scene List**. Pierwszą włączoną sceną powinno być **Main Menu**, a kolejnymi **Lobby** i **Kuba**. Folder **Prototypes** nie należy do zwykłego buildu gry.
4. Wybierz **Tools > Kucharze > Project > Validate Folder Layout**. Poprawny wynik pojawia się w **Console** jako `Project folder layout and playable scene entries are valid.` Narzędzie sprawdza także trzy foldery główne i ścieżkę listy Netcode. To sprawdzenie ścieżek i podstawowych referencji, nie test multiplayera.

Przy dodawaniu kolejnych paczek kieruj ich zasoby do odpowiedniego folderu, jeśli importer pozwala wybrać lokalizację. Importer może utworzyć nowy folder bezpośrednio w `Assets`; wtedy sprawdź zależności przed jego przeniesieniem. Wymagane przez pakiety podfoldery `Resources` zachowują nazwy i względne ścieżki.

Generatory korzystają ze wspólnych ścieżek w `Scripts/Editor/ProjectStructure/ProjectAssetPaths.cs`. Przy kolejnych przenosinach aktualizuj je razem z zapisanymi ścieżkami scen w komponentach i listą scen buildu. Same referencje do assetów Unity zwykle zachowuje dzięki plikom `.meta`; pola tekstowe ze ścieżkami wymagają osobnej aktualizacji.
