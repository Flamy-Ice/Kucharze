# Układ folderów projektu

Uzgodniony układ docelowy oddziela zasoby scen, wspólne pliki i zastępcze elementy wizualne. Model Player.fbx i klipy rig_*.anim są własnymi zasobami zespołu. TextMesh Pro jest oficjalnym pakietem Unity; jego zasoby, shadery i licencje trafiają do Unity, bez zmiany podfolderów Resources.

```text
Assets/
  Project/
    MainMenu/UI/
      Artwork/                 # Tło, logo i grafika panelu
      Buttons/                 # Grafiki przycisków
    Lobby/Prefabs/
      Networking/              # Połączenie, sesja i lista prefabów lobby
      Characters/              # LobbyPlayer
      Cameras/                 # LobbyCamera
    Gameplay/
      Prefabs/
        Characters/            # Player
        Cameras/               # Kamera rozgrywki
      UI/HUD/
        Controls/              # Grafiki sterowania ekranowego
        Panels/                # Tła paneli graczy, monet i czasu
        Icons/                 # Ikony HUD
    Shared/
      Characters/
        Models/                # Nasz Player.fbx
        Animations/
          Clips/               # Nasze animacje rig_*
          Controllers/         # Player.controller
      UI/Fonts/
        Sources/               # Pliki TTF używane przez grę
        TextMeshPro/           # Font Assets utworzone dla gry
      Networking/              # DefaultNetworkPrefabs.asset
    Placeholders/Lobby/
      Materials/
        Architecture/          # Zabudowa i podłogi
        Furnishings/           # Wyposażenie
        RoomColors/            # Kolory graczy, pokoi i gotowości
      Characters/
        Materials/             # Zastępcze materiały postaci lobby
        Meshes/                # Wygenerowana siatka i pierścień
    Development/Networking/    # Wcześniejsza konfiguracja testowa
    Settings/BuildProfiles/    # Profile budowania projektu
    Scenes/Debug/
      Kuba/
        Main Menu.unity        # Pierwsza scena buildu
        Lobby.unity
        Kuba.unity
        Prototypes/            # Wcześniejsze sceny poza zwykłym buildem
      Ada/                     # Zachowane foldery zespołu
      Mati/
      Szymon/
    Scripts/
      AGENTS.md
      Player/                  # Istniejący podział modułów bez zmian
      Networking/              # Connection, Lobby, Editor i Tests
      UI/                      # MainMenu, Controls i HUD
      Economy/
      Editor/ProjectStructure/ # Wspólne ścieżki, organizator i walidator
      Documentation/
  ThirdParty/                  # Dodatki od zewnętrznych autorów
  Unity/
    Settings/                  # Oficjalne ustawienia renderowania
    TextMesh Pro/              # Zasoby oficjalnego pakietu TMP
```

Zasób używany przez kilka scen należy do Shared. Prefab połączenia używany z menu pozostaje w grupie Lobby, ponieważ należy do przepływu sesji lobby. Zastępcza postać i materiały lobby są w Placeholders, ale działające prefaby pozostają przy scenie. Wasz model oraz animacje nie są oznaczane jako placeholder bez takiej decyzji zespołu.

Nie tworzymy pustych folderów Placeholders/MainMenu, Gameplay ani Shared, dopóki nie mają zasobów. ThirdParty pozostaje przygotowany jako jeden z trzech głównych katalogów. Istniejące puste foldery innych systemów nie oznaczają nowych funkcji. Sceny i modularne skrypty zachowują dotychczasowy podział; przenosiny nie zmieniają rozgrywki.

## Zastosowanie układu w Unity

Stan na 2026-11-26: użytkownik wykonał organizację w Unity. Wynik: 17 przenosin, 135 kontroli GUID-ów i zawartości zakończonych powodzeniem; lista scen buildu zachowana. Sprawdzono też docelowe lokalizacje na dysku, trzy główne foldery oraz zapisaną ścieżkę listy Netcode. Poniższe kroki opisują użycie narzędzia; nie trzeba ponownie uruchamiać migracji. Nie ma automatycznego polecenia wykonywanego przy starcie edytora.

1. Wyjdź z Play Mode, jeśli przycisk Play na górze jest aktywny. Zapisz otwarte sceny przez **File > Save**, a otwarty prefab przyciskiem **Save** w jego widoku. Niezapisana pusta scena Untitled może zostać zamknięta przez otwarcie zapisanej sceny, ale najpierw zachowaj własne zmiany.
2. Wybierz **Assets > Refresh**. Poczekaj, aż Unity skończy import i kompilację; ikona postępu w dolnym prawym rogu przestanie pracować.
3. Wybierz **Tools > Kucharze > Project > Organize Scene Assets**. Polecenie przenosi zasoby przez AssetDatabase razem z ich metadanymi. Nie nadpisuje istniejących celów. Zmienia też zapisaną ścieżkę listy prefabów Netcode, aby pakiet nie odtwarzał jej w głównym Assets.
4. Otwórz **Window > General > Console**. Oczekiwany wynik: `Scene asset organization complete: ... GUID/content checks passed; build scene entries preserved.` Jeżeli pojawi się błąd, przekaż jego treść; nie przenoś ręcznie kolidujących plików.
5. Wybierz **Tools > Kucharze > Project > Validate Folder Layout**. Oczekiwany wynik: `Project folder layout and playable scene entries are valid.`
6. Otwórz **File > Build Profiles > Open Scene List**. Włączone sceny pozostają w kolejności Main Menu, Lobby, Kuba. Prototypes pozostaje poza normalnym buildem. Wyłączony wpis nieistniejącej SampleScene został usunięty podczas sprzątania.

Walidacja układu sprawdza ścieżki i podstawowe referencje. Nie zastępuje Play Mode, testu Relay ani buildu Windows/Android. Uruchomienie organizatora ponownie pomija już przeniesione źródła; jeśli istnieje jednocześnie stara i nowa lokalizacja, zatrzymuje się bez nadpisywania.

Nowe zasoby przypisuj najpierw do sceny lub Shared, a następnie do typu pliku. Tymczasowe grafiki umieszczaj w analogicznej grupie w Placeholders. Zachowuj pliki .meta oraz Resources-relative paths oficjalnych pakietów. Nie edytuj PackageCache. Generatory korzystają z ProjectAssetPaths.cs; przy zmianie ścieżek aktualizuj również dokumentację i tekstowe odwołania. Computer Use nie jest używane; czynności w edytorze wykonuje użytkownik według instrukcji.

## Android na zwykły telefon — bez XR

Cel mobilny to zwykłe telefony Android, bez AR, VR i Android XR. Użytkownik usunął Unity OpenXR Android XR przez Package Manager. Kontrola manifest.json i packages-lock.json potwierdziła brak pakietów com.unity.xr.*, w tym OpenXR, AR Foundation, XR Hands, XR Plug-in Management oraz Composition Layers.

Usunięto pozostawione ustawienia Assets/XR i Assets/Unity/XR, ich metadane oraz trzy osierocone wpisy XR w EditorBuildSettings. Walidator nie wymaga zasobów XR, a organizator nie przenosi dawnych folderów XR. Profil Project/Settings/BuildProfiles/Android.asset zachował swój GUID i ustawienia zwykłej platformy Android; zmieniła się nazwa pliku i etykieta Android XR na Android.

Android Build Support oraz Device Simulator pozostają dostępne. Wbudowany moduł silnika com.unity.modules.xr i standardowe pola XR w ustawieniach Unity nie są pakietem OpenXR ani włączeniem AR/VR. Nie edytowano PackageCache ani wspólnych ustawień silnika tylko z powodu występowania w nich liter XR.

Po zakończeniu zmian:
1. Zapisz swoją pracę i zamknij edytor Unity.
2. W Unity Hub ponownie otwórz projekt Kucharze. Ponowne otwarcie pozwoli odczytać oczyszczone ustawienia projektu i zaktualizować zaimportowane zasoby.
3. Poczekaj na koniec kompilacji. Wybierz Tools > Kucharze > Project > Validate Folder Layout. Oczekiwany komunikat w Window > General > Console: `Project folder layout and playable scene entries are valid.`
4. Otwórz File > Build Profiles. Profil Android jest profilem telefonu; Main Menu, Lobby i Kuba pozostają na liście scen w dotychczasowej kolejności.

Kompilacja zmienionych narzędzi edytora przeszła kompilatorem Roslyn z Unity 6000.6.3f1. Kontrole plików potwierdziły trzy główne foldery, brak pakietów i konfiguracji XR oraz zachowanie zwykłych ustawień Androida. Nie wykonano w tej zmianie ponownego uruchomienia Unity, walidacji w jego API, Play Mode ani buildu APK. Jeżeli XR wróci, trzeba sprawdzić, czy ponownie zainstalowano pakiet tworzący ten folder; nie należy maskować go automatycznym przenoszeniem.
