# Pierwsza gra online — host i kod Relay

## Planowane lobby — ustalone wymagania

Docelowa gra jest wyłącznie wieloosobowa dla 2–4 graczy, licząc hosta. Host może czekać sam w lobby, ale nie może rozpocząć rozgrywki w pojedynkę. Działanie komponentów bez adapterów sieciowych pozostaje przydatne do testów i nie oznacza dostępnego trybu singleplayer.

Lobby jest przestrzenią dla 2–4 graczy z czterema otwartymi pokojami: po dwa z lewej i prawej strony wspólnego korytarza. Przednia część holu ma mały salon, a z tyłu znajduje się osobny zielony pokój gotowości. Mapa ma około 22.8 × 15 m. Spawny pozostają osobnymi znacznikami; każdy uczestnik dostaje pierwszy wolny pokój, może odwiedzać cudze pokoje i zachowuje przypisanie przy powrocie z rundy. Wyposażenie jest dekoracyjne i nie dodaje interakcji.

Po porównaniu z referencjami użytkownik upoważnił również poprawę prefabu i całej oprawy. LobbyCamera.prefab używa teraz łagodnej perspektywy: kąt X = 45°, Y/Z = 0° i pionowe Field of View = 15°. LobbyCameraFraming dopasowuje odległość do stałych granic planszy i proporcji okna, zostawiając miejsce na HUD. Nie śledzi gracza ani nie obraca kamery podczas gry; po zmianie rozmiaru okna koryguje wyłącznie kadr. Jest to świadoma zmiana wcześniejszej kamery ortograficznej, zgodna z najnowszą zgodą użytkownika.

Osobne pomieszczenie służy do rozpoczęcia gry: wszyscy obecni gracze muszą do niego wejść i pozostać w nim przez pełne 5 sekund. Przy czterech graczach obecność trzech w tym pomieszczeniu nie wystarcza. Wyjście choć jednej osoby ze strefy, dołączenie nowej osoby lub rozłączenie uczestnika zeruje odliczanie. Po zmianie składu sprawdzamy ponownie wszystkich aktualnych uczestników: jeśli jest ich co najmniej dwóch i wszyscy są w strefie, rozpoczyna się nowe pełne odliczanie. Jedna osoba nigdy nie uruchamia rozgrywki. Warunki i odliczanie rozstrzyga serwer.

Dołączanie jest dostępne wyłącznie w lobby; po rozpoczęciu przejścia do rozgrywki należy je zablokować. Kod dołączenia jest widoczny na dole ekranu. Na górze znajduje się angielski komunikat o stanie pomieszczenia startowego, liczbie obecnych w nim uczestników i odliczaniu.

Po odliczaniu wszyscy przechodzą do sceny `Kuba`; nazwa sceny docelowej jest konfigurowalna, ponieważ zostanie później zmieniona. Po zakończeniu rundy uczestnicy wracają do lobby. Jeśli podczas rozgrywki zostanie tylko jeden gracz, również wraca do lobby. Wyjście hosta nadal kończy sesję zgodnie z istniejącym modelem bez przejmowania hosta. Docelowo koniec rundy obsłuży przyszły ekran podsumowania. Obecny CountdownTimer pokazuje czas i sam nie przełącza scen.

Powyżej opisano ustalone wymagania. Zakres przygotowanego prototypu jest opisany poniżej, a rzeczywiście wykonane testy w Validation.md.

## Prosta scena lobby

Generator `Networking/Editor/Lobby/LobbySceneSetup.cs` korzysta ze wspólnych ścieżek w `Editor/ProjectStructure/ProjectAssetPaths.cs`. Grywalne sceny są w `Assets/Project/Scenes/Debug/Kuba/`, prefaby w `Assets/Project/Prefabs/`, a zastępcza oprawa w `Assets/Project/Placeholders/`. Wcześniejsze sceny pozostają osobno w `Scenes/Debug/Kuba/Prototypes/`; pełny podział opisuje [mapa folderów](../ProjectStructure.md).

- `Lobby.unity`: cztery umeblowane pokoje wokół wspólnego korytarza i salonu, zielony pokój gotowości, oświetlenie oraz znaczniki. Oprawa znajduje się pod `LobbyPresentation`, a jej materiały w `Placeholders/Lobby/Materials/`.
- `Kuba.unity`: obecna scena rozgrywki online, utworzona jako kopia OnlineTest bez jego NetworkManagera i z punktami wejścia do rozgrywki. Nie aktualizuje się automatycznie po zmianach sceny prototypowej.
- `LobbyPlayer.prefab`: kopia gracza online z zachowanym ruchem i rigem, dopasowaną skalą grafiki, wydzielonymi materiałami stroju i pierścieniem przy stopach. `PlayerNetworkAppearance` otrzymuje od serwera paletę odpowiadającą pokojowi. Barwi właściwy renderer przez MaterialPropertyBlock, bez zmieniania wspólnego materiału innych graczy.
- `LobbyCamera.prefab`: wspólna definicja projekcji, kadrowania i lokalnego profilu rysowania lobby.
- `LobbySession.prefab`: ustawienia scen i pięciosekundowego oczekiwania.
- `LobbyConnection.prefab` oraz `LobbyNetworkPrefabs.asset`: połączenie Relay, menu prototypowe i rejestr prefabów.

`LobbyEntry` tworzy połączenie tylko wtedy, gdy nie ma istniejącego NetworkManagera. Powrót do lobby zachowuje bieżącą sesję i kod. `LobbySessionBootstrap` tworzy serwerowy obiekt sesji i odrzuca dołączenia poza lobby oraz powyżej czterech graczy. `LobbyLayout` przechowuje znaczniki i sprawdza obecność punktu gracza w prostopadłościennej strefie; nie wymaga Rigidbody ani zdarzeń fizycznych triggera. Liczy się pozycja głównego obiektu postaci, nie sam kontakt jej brzegu ze strefą.

`LobbyRoomAssignments` na serwerze przydziela pierwszy wolny pokój, zachowuje przypisanie podczas przejść między scenami i ustawia pozycje przy wejściu do sceny. `LobbySession` sprawdza wszystkich uczestników i prowadzi wspólne odliczanie. Obecność jednej osoby, brak któregokolwiek uczestnika w strefie albo zmiana składu zeruje odliczanie. Zmiany scen wykonuje NGO, a ruch serwera jest wstrzymany podczas ich ładowania. Po wejściu do sceny gracz zostaje raz umieszczony przy swoim znaczniku; normalny ruch nadal wykonuje tylko PlayerMotor. `LobbyConnectionView` pokazuje angielską gotowość u góry i kod dołączenia u dołu również klientom. Panel IMGUI prototypu ma skalowane karty: gotowość i liczbę uczestników u góry oraz kod i przyciski połączenia na dole. Widok nie decyduje o gotowości ani zmianie scen.

Przyszły ekran podsumowania powinien wywołać `LobbySession.ReturnToLobby()` na serwerze. Zwykły klient nie może wymusić tego przejścia. Brak podsumowania oznacza, że teraz nie ma automatycznego powrotu po upływie czasu rundy. Powrót po utracie pozostałych graczy działa niezależnie.

### Otwórz i dostosuj scenę

1. Zapisz własną scenę przez **File > Save**. W oknie **Project** otwórz `Assets > Project > Scenes > Debug > Kuba`, a następnie kliknij dwukrotnie `Lobby`.
2. W **Hierarchy > LobbyMarkers** znajdziesz `Player1_Spawn_MoveMe` do `Player4_Spawn_MoveMe`. Przesuwaj je narzędziem **Move (W)** do docelowych pokojów. Punkt oznacza pozycję głównego obiektu gracza; przy obecnej kapsule wysokość Y wynosi `1.1`, aby jej dolna część znajdowała się nad podłogą.
3. Zaznacz `StartZone_MoveAndResizeMe`. **Transform > Position** przesuwa strefę, a **Box Collider > Size** ustawia jej rozmiar w metrach. Pozostaw **Is Trigger** włączone. Zielona podłoga i oznaczenia miejsc znajdują się w `LobbyPresentation > ReadyRoomDecor`; po przeniesieniu strefy przenieś również tę grupę wizualną.
4. Zaznacz `LobbyCamera` lub otwórz `Assets/Project/Prefabs/Cameras/LobbyCamera.prefab`. **Camera > Projection: Perspective > Field of View** ustawia delikatność perspektywy. **Lobby Camera Framing > Board Bounds** określa środek i rozmiar stałej planszy w metrach; na tej podstawie dobierana jest odległość kamery. Kamera nie śledzi postaci. **Lobby Camera Rendering > Shadow Distance** określa zasięg cieni w metrach.
5. W oknie **Project** zaznacz `LobbySession.prefab`. Pola **Lobby Scene** i **Game Scene** zawierają pełne ścieżki scen. Po zmianie nazwy lub miejsca sceny popraw odpowiednią ścieżkę. **Ready Seconds** domyślnie wynosi `5`.
6. Zapisz przez **Ctrl+S**, kliknij **Play**, a następnie **Host lobby**. Host pojawi się w swoim pokoju, kod będzie na dole, a napis u góry poprosi o drugiego gracza. Samotny host nie uruchomi rozgrywki.
7. Drugi gracz uruchamia tę samą wersję gry, wpisuje kod w dolnym panelu i wybiera **Join lobby**. Do testu dwóch okien potrzebny jest build zawierający sceny `Assets/Project/Scenes/Debug/Kuba/Lobby.unity` i `Assets/Project/Scenes/Debug/Kuba/Kuba.unity`, ze sceną Lobby jako pierwszą w tym osobnym teście. W zwykłym buildzie pierwszą sceną pozostaje Main Menu. Generator dodaje obie do wspólnej **File > Build Profiles > Open Scene List**, zachowując wcześniejsze pozycje; jeśli profil ma własną listę scen, dodaj je także tam. Dla prototypu zaznacz w tym profilu wyłącznie te dwie sceny, w tej kolejności.

Materiały oprawy używają standardowego URP Simple Lit i miękkich cieni. LobbyCameraRendering stosuje osobne wywołania rysowania i odpowiedni zasięg cieni wyłącznie podczas rysowania kamery lobby oraz Scene View aktywnego lobby. Przy zakończeniu rysowania i wyłączeniu komponentu przywraca poprzednie wartości. Globalne assety pipeline pozostają bez zmian. W testowanym trybie SRP Batcher mieszał kolory materiałów; lokalny profil kamery usuwa ten problem. Nie wprowadzono własnego shadera jako zależności.

Wygląd buduje `Networking/Editor/Lobby/LobbyPresentationBuilder.cs`, a sam prefab wizualny przygotowuje `LobbyPlayerVisualSetup.cs`. Skala jest na rodzicu PlayerVisualScale poza Animatorem; oryginalne kości, wierzchołki, kontroler i ścieżki klipów są zachowane. Materiały rozdzielają strój, kapelusz/rękawiczki, twarz i buty. Serwerowy adapter PlayerNetworkAppearance powiela kolor pokoju na pozostałe klienty. Po tej zmianie prefabu wszystkie okna testowe i gracze muszą używać nowego buildu. Polecenie **Tools > Kucharze > Online > Refresh Lobby Appearance** odświeża wyłącznie generowaną oprawę sceny, kamerę i światło; zachowuje znaczniki spawnów, strefę logiczną, LobbyEntry i konfigurację sieciową. Zapisz scenę i wyjdź z Play Mode przed jego użyciem. Polecenie zastępuje również wcześniejsze nazwane bryły prototypu; własne dodatki trzymaj poza grupą `LobbyPresentation`.

Scena jest już wygenerowana. Polecenie **Tools > Kucharze > Online > Create Simple Lobby** jest przeznaczone do pierwszego utworzenia zestawu i odmawia nadpisywania istniejących scen i prefabów. **Validate Simple Lobby** sprawdza podstawowe referencje i wspólną listę scen; nie jest testem rozgrywki. **Rebuild Lobby Room Layout** dodatkowo ustawia nowy układ 2×2, przesuwa cztery spawny oraz granice pokoju gotowości; **Refresh Lobby Appearance** zachowuje ręcznie przesunięte znaczniki i strefę.

Do przygotowania programu z właściwymi scenami wybierz **Tools > Kucharze > Online > Build Lobby Test** po wyjściu z Play Mode. Narzędzie sprawdza konfigurację, czeka na kompilację i buduje wariant Windows Development ze scenami Lobby i Kuba, w tej kolejności. Wynik znajduje się w `%TEMP%/KucharzeLobbyTest/KucharzeLobbyTest.exe`; cały folder można skopiować poza katalog tymczasowy. Obie osoby muszą używać aktualnej wersji. Osobne polecenie **Build Windows Test** nadal buduje wcześniejszą scenę OnlineTest.

`Networking/Tests/LobbySmokeProbe` jest uruchamiany wyłącznie jawnym argumentem `-kucharze-lobby-probe` w edytorze lub buildzie Development. Używa lokalnego transportu i technicznych przestawień postaci do sprawdzania reguł strefy; nie testuje klawiatury, pada ani kolizji. Test sprawdza też niezależne, replikowane kolory i bloki materiałów czterech postaci. Ta część wymaga aktywnego urządzenia graficznego: nie dodawaj `-nographics`. Wyniki rzeczywiście wykonanych sprawdzeń są zapisywane w `Documentation/Movement/Validation.md`.

## Konfiguracja przygotowana automatycznie w tym projekcie

Scena testowa: `Assets/Project/Scenes/Debug/Kuba/Prototypes/OnlineTest.unity`.
Prefab online: `Assets/Project/Development/Networking/OnlinePlayer.prefab`.
Lista prefabów sieciowych: `Assets/Project/Development/Networking/OnlineNetworkPrefabs.asset`.
Scena jest kopią sceny Kuba; oryginalna scena i prefab Player pozostają osobnymi assetami. Jeśli oryginalna scena ma niezapisane zmiany, automat pozostawia ją otwartą. Zapisz swoją pracę przed otwarciem sceny OnlineTest dwuklikiem w Project.

Pakiety NGO 2.14.0 i Multiplayer Services 2.3.3 są już w manifest.json. KUCHARZE_ONLINE jest włączone dla Standalone i Android. Project ID w Player Settings wskazuje projekt używany przez grę. Osobno edytor musi być zalogowany i powiązany z projektem oraz organizacją przez Unity Connect. Sam zapis Project ID nie tworzy tego pełnego powiązania.

Narzędzia edytora znajdują się w **Tools > Kucharze > Online**:

- **Prepare Test Scene** przygotowuje nową scenę i prefab; odmawia nadpisania istniejącej sceny testowej.
- **Configure Shared Wallet And Timer** dodaje adaptery do portfela i timera wyłącznie w scenie OnlineTest.
- **Build Windows Test** kolejkuje build: odświeża assety, żąda kompilacji skryptów i czeka na przeładowanie zestawów edytora. Nie wywołuje BuildPlayer w tym samym kroku co odświeżenie. Błąd kompilacji zatrzymuje kolejkę. Narzędzie sprawdza pełne powiązanie usług i w razie potrzeby wiąże istniejący projekt wskazany w Player Settings, korzystając z już zalogowanego konta edytora. Nie loguje użytkownika, nie tworzy projektu i nie zmienia uprawnień. Jeśli nie da się zakończyć powiązania, build nie startuje. Kolejka przetrwa przeładowanie skryptów dzięki SessionState.

Build obejmuje wyłącznie scenę OnlineTest, bez zmiany globalnej listy scen. Kolejne programy zapisuje w `%TEMP%/KucharzeOnlineTest/KucharzeOnlineTest.exe`. Aktualny program po poprawkach ostrzeżeń z 2026-11-24 znajduje się w `Builds/OnlineTest-Fixed/KucharzeOnlineTest.exe` w katalogu głównym projektu. Starszy program pozostawiono w `Builds/OnlineTest`.

Pierwszy program można uruchomić obok edytora: w jednym oknie Host, w drugim kod i Join. W każdym oknie jest jedna własna postać; przed Host nie powinno być postaci utworzonej przez Netcode.

W buildach Development dostępny jest test techniczny z argumentami `-kucharze-smoke-host` i `-kucharze-smoke-client`. Używa lokalnego portu 17837 i sprawdza oddzielne sterowanie dwóch postaci oraz autorytet wspólnych danych. Wariant `-kucharze-smoke-relay-host` i `-kucharze-smoke-relay-client` sprawdza te same warunki przez rzeczywiste Relay i kod dołączenia, korzystając z oddzielnych testowych profili anonimowego logowania. Bez tych argumentów test się nie uruchamia. Poza edytorem instalacja testu wymaga runtime `Debug.isDebugBuild`; nie używa przestarzałego symbolu DEVELOPMENT_BUILD. W zwykłym buildzie bez Development test się nie uruchamia. Oba warianty przeszły test dwóch procesów na jednym komputerze. Nie jest to pomiar gry na dwóch różnych łączach internetowych.

Komunikaty URP wymieniające assety oraz informacje Performance Testing o tworzeniu danych buildu są normalnymi informacjami. Narzędzie ich nie wycisza. Ostrzeżenia o nieprzekompilowanych skryptach i brakującym powiązaniu usług należy usuwać przez poprawną kolejność budowania i powiązanie edytora, a nie przez wyłączenie logowania pakietów.

## Jak działa przygotowany wariant

Host uruchamia grę na swoim komputerze i jednocześnie jest graczem oraz serwerem. Druga osoba wpisuje kod. Relay przekazuje dane między uczestnikami przez internet; nadal to komputer hosta oblicza grę. Host musi pozostać w grze. Zamknięcie hosta kończy połączenie pozostałych osób. Nie ma przejmowania roli hosta.

Pierwszy test wykonujemy w jednej wspólnej scenie rozgrywki. Panel połączenia jest prostym panelem testowym. Nowe [menu główne](../UI/MainMenu.md) znajduje się w Assets/Project/Scenes/Debug/Kuba/Main Menu.unity. Korzysta z istniejącego prefabu LobbyConnection: HostInScene przygotowuje Relay i ładuje lobby przed StartHost, a Join dołącza klienta i pozwala Netcode zsynchronizować scenę. Wejście przez menu ustawia Main Menu jako scenę powrotu po rozłączeniu; bezpośredni test lobby zachowuje poprzedni powrót do lobby. Ruch, autorytet serwera i gotowość 2–4 graczy pozostają niezależne od menu.

Poniższe kroki pozostają instrukcją konfiguracji od początku. W tym projekcie wykonano już lokalną konfigurację opisaną powyżej i sprawdzono Relay. Bez symbolu KUCHARZE_ONLINE pliki sieciowe są pomijane, a dotychczasowa gra bez adapterów sieciowych zachowuje tryb lokalny.

## Szybki test obecnego Main Menu — jeden komputer

Do tego testu użyj jednego edytora i jednego aktualnego buildu (samodzielnego programu gry), z internetem. Kod używa osobnych profili Authentication „editor” oraz „player”. Nie uruchamiaj dwóch kopii tego samego buildu na jednym komputerze jako pierwszego testu — współdzielą profil „player”.

1. Zapisz scenę i poczekaj na zakończenie kompilacji. Otwórz **File > Build Profiles**, wybierz **Windows**, następnie **Open Scene List**. Jeżeli profil ma własną listę, sprawdź jego listę zamiast wspólnej.
2. Pierwsza włączona scena ma być **Assets/Project/Scenes/Debug/Kuba/Main Menu.unity**. Włączone muszą być też **Assets/Project/Scenes/Debug/Kuba/Lobby.unity** i **Assets/Project/Scenes/Debug/Kuba/Kuba.unity**. Pozostałe wpisy mogą zostać.
3. Wybierz **Build** i nowy folder poza Assets, np. **Builds/MainMenuTest**. W tym teście nie używaj **Build Lobby Test** ani **Build Windows Test** — te narzędzia budują osobne sceny testowe z pominięciem Main Menu.
4. W edytorze otwórz Main Menu, naciśnij **Play**, następnie w grze **Play > Host Game**. Oczekuj załadowania lobby, jednej postaci i kodu lobby.
5. Uruchom .exe z nowego folderu buildu. Wybierz **Play**, wpisz kod hosta do **Lobby code** i kliknij **Join Game**. Oczekuj tego samego lobby i dwóch postaci w obu oknach.
6. Przełączaj aktywne okno i poruszaj graczem. Każde okno ma sterować wyłącznie swoją postacią; pozycje obu postaci powinny być widoczne po obu stronach.
7. Wejdź obiema postaciami do zielonego pokoju gotowości. Po wspólnych pięciu sekundach powinny przejść do rozgrywki. Sam host nie rozpoczyna gry. Wyjście jednej postaci ze strefy przed upływem czasu ma resetować odliczanie.
8. Rozłącz klienta, a następnie zakończ hostowanie i sprawdź reakcję pozostałego okna. Kolejne hostowanie tworzy nowy kod.

Do testu z kolegą na drugim komputerze wyślij cały folder tego samego aktualnego buildu, najlepiej ZIP. Jedna osoba wybiera Host Game i podaje kod, druga Join Game. Jeśli host nie dostaje kodu, odczytaj komunikat Connection status i czerwony błąd Console. Sprawdź internet oraz **Edit > Project Settings > Services** — projekt musi być powiązany z właściwym Unity Cloud Project, z przygotowanym Relay/Authentication. Nie przebudowuj istniejącego lobby przez generator tylko po to, aby wykonać ten test.

## 1. Zainstaluj pakiety — potrzebne teraz

1. Zapisz scenę: **File > Save**. Wykonaj zwykłą kopię projektu lub commit własnych zmian przed konfiguracją sceny.
2. Otwórz **Window > Package Management > Package Manager**.
3. Z listy źródeł wybierz **Unity Registry**.
4. Wyszukaj **Netcode for GameObjects**, zaznacz go i kliknij **Install**. Identyfikator: `com.unity.netcode.gameobjects`. Używamy aktualnej stabilnej wersji zgodnej z tym edytorem, co najmniej 2.7, z obsługą RpcInvokePermission.
5. Tak samo zainstaluj **Multiplayer Services**, identyfikator `com.unity.services.multiplayer`.
6. Poczekaj na zakończenie instalacji i kompilacji. Pakiety Unity Transport, Authentication i Core powinny zostać pobrane jako zależności. Jeśli Unity Transport nie ma na liście **In Project**, doinstaluj `com.unity.transport` z Unity Registry.

Jeśli wyszukiwanie nie pokazuje pakietu: przycisk **+ > Install package by name**; wklej podany identyfikator i kliknij **Install**. W niektórych wersjach Package Manager ten przycisk nazywa się **Add package by name**.

Nie instaluj starego samodzielnego pakietu Relay ani Netcode for Entities do tej konfiguracji. Unity dla Unity 6 udostępnia Relay w Multiplayer Services. [Oficjalna integracja Relay i NGO](https://docs.unity.com/en-us/mps-sdk/tutorials/relay-and-ngo).

## 2. Powiąż projekt z Unity Services — potrzebne teraz

1. Zaloguj się na konto Unity w Unity Hub i edytorze.
2. Otwórz **Edit > Project Settings > Services**.
3. Wybierz swoją organizację i istniejący projekt Unity Cloud albo użyj **Create project ID**, jeśli projekt nie jest jeszcze powiązany. **Link project ID** służy do wybrania istniejącego projektu.
4. Otwórz ten sam projekt w [Unity Dashboard](https://cloud.unity.com/).
5. W sekcji usług multiplayer projektu odszukaj **Relay** i zakończ konfigurację, jeśli panel pokazuje **Get started** lub **Enable**. W **Authentication** zakończ podstawową konfigurację usługi, jeśli panel o nią prosi. Kod używa logowania anonimowego — gracze w tym teście nie wpisują loginów.

Nazwy grup w internetowym Dashboard mogą się zmieniać; ważne jest, aby Relay i Authentication dotyczyły tego samego Project ID co w edytorze. [Łączenie projektu z UGS](https://docs.unity.com/en-us/services/getting-started).

## 3. Włącz przygotowany kod — dopiero po instalacji pakietów

1. Otwórz **Edit > Project Settings > Player**.
2. Rozwiń **Other Settings**, następnie **Script Compilation**.
3. Przy **Scripting Define Symbols** dodaj nową pozycję: `KUCHARZE_ONLINE`.
4. Kliknij **Apply**, jeśli przycisk jest widoczny, i zaczekaj na kompilację.
5. Otwórz **Window > General > Console**. Przed konfiguracją sceny usuń przyczynę każdego czerwonego błędu. Błąd dotyczący Unity.Netcode lub Unity.Services oznacza zwykle brak pakietu albo nieukończoną instalację.

Symbol jest ustawieniem danej platformy. Gdy później zmienisz platformę, np. Windows na Android, sprawdź go również tam. Usunięcie symbolu wyłącza kod online; użyj wtedy lokalnej sceny bez komponentów sieciowych.

## 4. Przygotuj scenę testową

1. Otwórz obecną scenę rozgrywki. Wybierz **File > Save As...** i zapisz kopię jako `OnlineTest.unity` obok swoich scen. Pracuj w tej kopii.
2. Zachowaj podłoże z Colliderem, oświetlenie i kamerę nad planszą. Kamera powinna widzieć kilka metrów wzdłuż osi X i Z wokół startu.
3. W **Hierarchy** wybierz gracza, który już poprawnie chodzi. Jeśli model lub efekty nie są gotowe, do testu wystarczy chodząca postać z CharacterController i PlayerWalk. Widoczna grafika powinna być dzieckiem głównego obiektu, aby PlayerRotation obracał sam model.
4. Ustaw jego pozycję startową nad podłożem. To będzie początek siatki startowej. Przy domyślnym Spacing kolejni gracze pojawiają się co 2 metry. Zostaw podłoże i widok kamery w tym obszarze.
5. W **Project** utwórz folder na prefab testowy: prawy przycisk myszy **Create > Folder**, nazwa np. `OnlinePrefabs`. Przeciągnij obiekt gracza z Hierarchy do tego folderu. Prefab to zapisany wzorzec obiektu, z którego gra tworzy osobny egzemplarz dla każdej osoby.
6. Otwórz prefab dwuklikiem. Na **głównym obiekcie gracza**, przyciskiem **Add Component**, dodaj:
   - `NetworkObject`;
   - `NetworkTransform`;
   - `PlayerNetworkInput`;
   - `PlayerNetworkState`;
   - `PlayerNetworkSpawn`.
7. W **NetworkTransform** wybierz autorytet **Server**, jeśli wersja pakietu pokazuje opcję Authority Mode. Pozostaw synchronizację pozycji X/Y/Z i włączone **Interpolate**. Wyłącz synchronizację skali i obrotu głównego obiektu, jeżeli jest widoczna; obrót modelu przesyła PlayerNetworkState. Domyślny NetworkTransform jest serwerowy. [Opis autorytetu](https://mp-docs.dl.it.unity3d.com/netcode/current/components/networktransform/).
8. Sprawdź **PlayerInputActions > Input Action Asset**: powinien zawierać `PlayerControls` z folderu obok PlayerInputActions.cs. Zapisana referencja jest potrzebna w zbudowanej grze.
9. Zachowaj istniejące Walk/Run/Dash, animację, audio i VFX, jeżeli są używane. Nie dodawaj NetworkAnimator ani drugiego skryptu przesuwającego gracza.
10. Wróć strzałką do sceny. Usuń **tylko egzemplarz gracza z Hierarchy kopii sceny OnlineTest**, którego właśnie zapisałeś jako prefab. W scenie nie powinien zostać gracz sterowany lokalnie — Netcode utworzy postacie uczestników.

Nie dodawaj drugiego NetworkObject do modelu wewnątrz tego prefabu. Jeden Player ma jeden główny NetworkObject.

## 5. Dodaj system połączenia do sceny

1. W **Hierarchy** wybierz prawym przyciskiem **Create Empty**, nazwij obiekt `OnlineSession`.
2. W **Inspector > Add Component** dodaj `RelayConnectionMenu`. Wymagane RelayConnection, NetworkManager i UnityTransport powinny zostać dodane automatycznie. Jeżeli któregoś brakuje, dodaj go na tym samym obiekcie.
3. W **RelayConnection > Maximum Players** pozostaw `4`. Ta liczba uwzględnia hosta.
4. W **NetworkManager** ustaw **Network Transport** na komponent **UnityTransport** z tego obiektu, jeżeli pole jest puste. Skrypt ustawia je również w runtime.
5. Jeśli NetworkManager pokazuje **Network Topology**, wybierz **Client Server**. Pozostaw **Enable Scene Management** włączone i **Connection Approval** wyłączone.
6. W sekcji **Player Prefab / Default Player Prefab** przeciągnij przygotowany prefab gracza z okna Project. Jeśli w tej wersji jest opcja **Auto Spawn Player Prefab Client Side**, pozostaw tworzenie gracza włączone.
7. Jeśli Inspector udostępnia listę **Network Prefabs**, sprawdź, czy prefab jest na niej zarejestrowany. Część wersji rejestruje Player Prefab automatycznie; jeśli go nie ma, dodaj prefab do listy przez **+**.
8. W **UnityTransport** wybierz **Relay Unity Transport**, jeżeli jest widoczne pole Protocol Type. Nie wpisuj adresu IP ani portu hosta. RelayConnection ustawia dane Relay przed startem.
9. Zapisz scenę przez **Ctrl+S**.

W scenie powinien być jeden NetworkManager. Nie dodawaj równolegle gotowego Multiplayer Building Block uruchamiającego inną sesję.

## 6. Pierwszy test: edytor i zbudowana gra

Build to samodzielny program gry uruchamiany bez edytora. Do pierwszego testu potrzebujemy edytora i jednego takiego programu.

1. Otwórz **File > Build Profiles**. Wybierz profil dla swojego komputera, np. Windows. Jeśli trzeba, dodaj go przez **Add Build Profile**, wybierz Windows i użyj **Switch Profile**.
2. W **Scene List** dodaj otwartą scenę przez **Add Open Scenes**. Jeżeli profil używa wspólnej listy, wejdź w **Open Scene List**. Na pierwszy test zaznacz wyłącznie `OnlineTest`, aby program uruchomił tę scenę bez przechodzenia przez menu główne.
3. Kliknij **Build**, wybierz nowy folder wyjściowy poza Assets, np. `Builds/OnlineTest`, i poczekaj na zakończenie. Nie zapisuj buildu do folderu skryptów.
4. W edytorze otwórz tę samą scenę OnlineTest i kliknij **Play**. W lewym górnym rogu pojawi się panel **Online play**.
5. Kliknij **Host**. Oczekiwany wynik: komunikat o hostowaniu, kod oraz jedna postać. Przycisk **Copy code** kopiuje kod.
6. Uruchom zbudowany program. W jego panelu wpisz kod z edytora w pole tekstowe i kliknij **Join**.
7. Oczekiwany wynik: komunikat **Connected to the host**, dwie postacie widoczne w obu oknach. Klawiatura lub pad steruje postacią właściciela aktywnego okna. Po zmianie okna własna postać powinna przestać otrzymywać sterowanie; postać drugiej osoby nie powinna dziedziczyć tych klawiszy.
8. Sprawdź chodzenie, Run, Dash oraz ścianę. Oba okna powinny pokazywać tę samą pozycję po kolizji. Obrót, animacje i dostępne efekty powinny być widoczne dla obu graczy.
9. Kliknij **Disconnect / Cancel** u klienta. Postać klienta powinna zniknąć z gry hosta. Spróbuj ponownie dołączyć bieżącym kodem.
10. Zakończ hostowanie. Klient powinien otrzymać informację o rozłączeniu. Nowe hostowanie tworzy nowy kod.

Edytor i build korzystają z różnych profili anonimowego logowania. Pierwszy test na jednym komputerze wykonuj jako **jeden edytor + jeden build**. Kilka kopii tego samego buildu na jednym komputerze wymaga osobnych profili Authentication i nie jest objęte tym panelem testowym.

Do testu internetowego wyślij drugiej osobie **cały folder gotowego buildu**, najlepiej w ZIP, nie samo .exe. Obie osoby uruchamiają tę samą wersję gry, host tworzy grę i przekazuje kod. Internet jest potrzebny także podczas testu na jednym komputerze. Ten wariant używa DTLS dla programu desktopowego; konfiguracja przeglądarki Web wymaga później WSS i nie jest częścią pierwszego testu.

## 7. Monety i wspólny timer — po sprawdzeniu ruchu

**Monety:** CoinWalletNetwork dodaj razem z CoinWallet na NetworkObject, który ma posiadać wynik. Dla wspólnej puli może to być obiekt sceny z NetworkObject; dla osobistego wyniku — główny obiekt prefabu gracza. Nie dodawaj jednocześnie obu wariantów, jeśli nie chcesz dwóch oddzielnych portfeli. CoinDisplay dalej wskazuje wybrany portfel. HUD powiązany ze starym obiektem sceny nie przełączy się automatycznie na portfel dynamicznie utworzonego gracza; takie powiązanie wymaga osobnego adaptera po ustaleniu docelowego HUD.

Serwer wywołuje istniejące TryAddCoins. Kod nie dodaje nowego sposobu zarabiania ani przycisku przyznającego monety klientowi. Wartości są synchronizowane również dla późno dołączającej osoby. Próba TryAddCoins na zwykłym kliencie zwraca false.

**Timer:** na obiekcie sceny z CountdownTimer dodaj NetworkObject i CountdownTimerNetwork. Obiekt musi być obecny w zapisanej scenie po obu stronach; nie umieszczaj go pod innym NetworkObject. Start sesji restartuje ten timer na serwerze. Tylko serwer może go zatrzymać, wznowić lub zrestartować. Klienci wyświetlają przesyłany czas, aktualizowany do 10 razy na sekundę; nie prowadzą własnego odliczania. Wygląd i format tekstu pozostają w CountdownTimer.

Dodawaj te adaptery wyłącznie do obiektów online. Sam CoinWallet i CountdownTimer bez adapterów zachowują dotychczasowe działanie lokalne. Nowa sesja w tej samej, niewczytanej ponownie scenie nie resetuje automatycznie portfela; rozpoczęcie nowej rundy i reset ekonomii to osobne zasady gry.

## Podział odpowiedzialności i ograniczenia

- `Networking/RelayConnection`: logowanie anonimowe, utworzenie i dołączenie Relay, stan połączenia, anulowanie oraz timeout po rozpoczęciu połączenia z hostem.
- `Networking/RelayConnectionMenu`: panel pierwszego testu, niezależny od docelowego menu głównego.
- `Player/Networking/PlayerNetworkInput`: przesyłanie wyłącznie kontroli właściciela do serwera, odrzucanie innego nadawcy, wygaszenie sterowania po 0,5 s bez danych. Sterowanie jest wysyłane do 30 razy na sekundę. Komunikaty są niezawodne; aktualna wersja nie implementuje predykcji ani kompensowania opóźnienia kolejki.
- `PlayerNetworkState`: serwer zapisuje zmienne sieciowe stanu i obrotu modelu; klienci blokują CharacterController oraz lokalne obliczanie obrotu. Parametry animacji wynikają z replikowanego stanu; zaakceptowany dash jest niezawodnym sygnałem Audio/VFX. Kroki pochodzą z lokalnych markerów animacji i nie wymagają osobnego RPC dla każdego kontaktu stopy.
- `PlayerNetworkSpawn`: jednorazowe rozmieszczenie graczy względem pozycji prefabu. Siatka używa identyfikatora klienta, więc kolejne dołączenia mogą zajmować dalsze pola. To proste rozmieszczenie testowe; mapa musi obejmować pola startowe.
- `NetworkTransform`: replikacja pozycji głównego obiektu z autorytetem serwera i interpolacją. Jedynie PlayerMotor wykonuje locomotion przez CharacterController.Move.
- `CoinWalletNetwork` i `CountdownTimerNetwork`: autorytet serwera i bieżący stan dla późnego dołączenia.

Repliki dostają stan prezentacji, a nie pełny lokalny stan symulacji providerów. Ich PlayerDash.IsDashing/CooldownRemaining i PlayerMotor.LastResult nie są serwerowym odczytem — debug tych pól na kliencie może pokazywać lokalne wartości początkowe. Aktualna animacja korzysta z osobno replikowanego postępu. Sieć nie odtwarza pozostałych zdarzeń przejść PlayerBus na klientach.

Brak predykcji oznacza, że klient widzi reakcję po podróży danych do hosta i z powrotem. Do pierwszego testu współpracy to świadomie prosty fundament; płynność przy większym opóźnieniu wymaga pomiarów po uruchomieniu. Nie dodano zapisu wyników, dobierania losowych graczy, listy lobby, przejmowania hosta ani nowego gameplayu.

## Gdy coś nie działa

- Brak skryptów sieciowych w Add Component: sprawdź instalację pakietów, symbol KUCHARZE_ONLINE i czerwone błędy Console.
- Host nie otrzymuje kodu: sprawdź internet, powiązanie Project ID, Relay/Authentication oraz treść błędu na panelu.
- Kod nie działa: host musi nadal działać, a obie osoby muszą mieć zgodny build. Spróbuj utworzyć nową sesję i przepisać aktualny kod.
- Dodatkowa postać albo dwie postacie sterowane jednym wejściem: usuń lokalnego Playera z kopii sceny; postacie ma tworzyć NetworkManager.
- Brak postaci: sprawdź Player Prefab, rejestrację Network Prefabs, NetworkObject i widok kamery na pola startowe.
- Postać klienta stoi: sprawdź PlayerNetworkInput, PlayerNetworkState oraz zapisane PlayerControls w prefabie. Replika klienta ma mieć lokalny resolver z wyłączoną symulacją — to poprawne; ruch wykonuje jej odpowiednik na serwerze.
- Różne sceny: przebuduj grę i sprawdź listę scen; pierwsza scena testowa musi być taka sama.

Po zmianach konfiguracji prefabu lub sceny wykonaj nowy build przed porównywaniem edytora z programem.

## Bieżący zakres Androida

Celem mobilnym jest zwykły Android, nie gogle Android XR. Symbol KUCHARZE_ONLINE musi być obecny dla Androida niezależnie od Standalone; inaczej mobilny build pominie MainMenuOnlinePanel, RelayConnection i adaptery graczy. Został dodany do wspólnych Player Settings. Przy własnych nadpisaniach symboli w profilu sprawdź także ten profil. Wspólna lista zawiera Main Menu jako pierwszą scenę oraz Lobby i Kuba.

Pełna obsługa dotyku gry nie jest jeszcze ukończona: w Lobby brakuje FloatingJoystick, który istnieje w Kuba, a wygenerowane sceny nie zawierają ekranowych przycisków Run/Dash. Zewnętrzny pad może dostarczać te wejścia; sam ekran dotykowy nie zapewnia obecnie całego przepływu lobby i wszystkich akcji. Brak tych kontrolek nie jest błędem Main Menu ani protokołu Relay. Dokładne wyniki aktualnego audytu i ograniczenia sprawdzeń znajdują się w Documentation/Movement/Validation.md.
