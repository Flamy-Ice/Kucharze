# Wykonane sprawdzenia

## 2026-11-24 — fundament host/Relay

Przygotowano RelayConnection i panel testowy, trzy adaptery Player/Networking, adaptery CoinWalletNetwork i CountdownTimerNetwork oraz niezależne od Netcode granice wejścia, symulacji i stanu prezentacji. Zmodyfikowano PlayerInputActions, PlayerMovementResolver, PlayerStates, PlayerBus, PlayerAnimation, CoinWallet i CountdownTimer. Dodano pięć testów Edit Mode i instrukcję konfiguracji. Nie zmieniono scen, prefabów, manifestu pakietów ani ustawień usług.

Wyniki faktycznie wykonanych sprawdzeń:

- Kompilator C# dostarczony z Unity 6000.6.3f1: aktualne źródła runtime PlayerMovementSystem3D oraz skrypty domyślnego assembly z Economy, UI i Networking skompilowano z KUCHARZE_ONLINE i bez tego symbolu. Końcowy wynik: brak błędów i ostrzeżeń.
- Wariant online sprawdzono względem rzeczywistych bibliotek referencyjnych NGO, Transport i Services zachowanych w Library/Bee, mimo braku pakietów w bieżącym Packages/manifest.json. Usunięto z tymczasowej konfiguracji kompilatora nieistniejące, nieużywane referencje starego cache Newtonsoft i Wire. To sprawdzenie typów C#, a nie pełny import pakietów ani wykonanie generatora RPC Netcode w Unity.
- Osobno skompilowano wariant UNITY_EDITOR runtime i istniejące Inspectory PlayerMovementSystem3D.Editor względem aktualnego runtime. Brak błędów i ostrzeżeń.
- Skompilowano źródło pięciu nowych testów względem NUnit i runtime. Testów nie uruchomiono w Unity. Obejmują niezależność dwóch źródeł sterowania, deadzone i ograniczenie przekątnej, odrzucenie NaN, ochronę stanu autorytatywnego, sygnał dasha repliki oraz odłączenie źródła zewnętrznego.
- Parser Roslyn: zero błędów składni w skryptach Player, Networking, Economy i UI z online włączonym i wyłączonym.
- Przegląd statyczny: CharacterController.Move nadal wywołuje wyłącznie PlayerMotor. Jedyny nowy zapis pozycji jest jednorazowym rozmieszczeniem nowego gracza na serwerze. Pozycje replik przesyła NetworkTransform.
- Przegląd odpowiedzialności: sterowanie właściciela i timeout należą do adaptera wejścia; stan/efekty/obrót do adaptera replikacji; rozmieszczenie do adaptera spawnu; połączenie i panel mają osobne komponenty. Logika Walk/Run/Dash, grawitacji i smoothingu nie otrzymała zależności od Netcode.
- Sprawdzono diff pod kątem błędów białych znaków. Tymczasowe wyniki kompilacji i narzędzie inspekcji usunięto po sprawdzeniach.

Nie uruchamiano Play Mode, testów Edit Mode, buildu gry ani dwóch połączonych procesów. Nie zweryfikowano jeszcze odczuwalnego opóźnienia, klawiatury/pada, przejść Walk/Run/Dash, kolizji i podłoża w sieci, efektów na replikach, dołączenia w trakcie, rozłączeń ani replikacji monet i timera w działającej sesji. Wymaga to instalacji pakietów, powiązania Unity Services oraz konfiguracji sceny i prefabu opisanej w Documentation/Networking/README.md.

Wariant zachowuje lokalne działanie przy braku adapterów. Pełne runtime cooldowny providerów i Motor.LastResult nie są replikowane do diagnostyki klienta. Nie ma predykcji, przejmowania hosta ani automatycznego powiązania dynamicznego portfela gracza z dotychczasowym HUD.

## 2026-11-24 — konfiguracja Unity i rzeczywiste testy dwóch procesów

Po instalacji pakietów przez użytkownika potwierdzono NGO 2.14.0 i Multiplayer Services 2.3.3. Włączono KUCHARZE_ONLINE dla Standalone i zapisano Project ID wskazany przez użytkownika. Nie zmieniano jego konta ani uprawnień usług.

Skrypt edytora Networking/Editor/OnlineTestSetup utworzył osobną scenę Networking/Setup/OnlineTest.unity, prefab OnlinePlayer i listę OnlineNetworkPrefabs. Zachowano ustawienia oraz model gracza z kopii aktualnej sceny Kuba, zapisano PlayerControls w prefabie, dodano serwerowy NetworkTransform i adaptery PlayerNetworkInput/State/Spawn. Scenowe instancje lokalnego gracza usunięto wyłącznie z kopii testowej. Oryginalną scenę pozostawiono otwartą ze względu na niezapisane zmiany. Do portfela GameManager i timera Time w kopii dodano NetworkObject oraz właściwe adaptery. Skrypt nie zmienia globalnej listy scen buildu.

Wykonano:

- Sprawdzenie kompilacji runtime i nowego narzędzia edytora kompilatorem Unity. Następnie rzeczywisty import i kompilacja projektu w otwartym Unity, wraz z generowaniem kodu NGO.
- Dwa udane buildy Windows Development przez Unity BuildPipeline; drugi zawiera poprawione sprawdzenie początkowego salda oraz wariant testu Relay. Gotowy program skopiowano do Builds/OnlineTest w katalogu projektu.
- Test dwóch samodzielnych procesów host/klient na lokalnym transporcie: PASS. Host poruszał się po X, klient po Z; na serwerze działała symulacja obu graczy, na kliencie była wyłączona. Obie strony odebrały zaakceptowany dash swojego gracza.
- Serwer przyznał 7 monet do skonfigurowanego salda 100. Oba procesy odczytały 107; klient nie mógł przyznać kolejnych monet. Pierwsza próba testu zakładała zerowe saldo początkowe i została poprawiona, zachowując ustawienia użytkownika.
- Wspólny timer był uruchomiony po obu stronach, a wywołanie PauseTimer na kliencie nie zatrzymało odliczania. Po zakończeniu klienta host potwierdził usunięcie jego postaci.
- Ten sam test przeprowadzono przez rzeczywiste Unity Relay: PASS. Host uzyskał kod, drugi proces dołączył kodem, a obie strony przeszły powyższe sprawdzenia ruchu, autorytetu, dasha i danych wspólnych. Testowe sesje zamknięto.

Testy wykonywał Networking/Tests/OnlineSmokeProbe, aktywny tylko z jawnymi argumentami w edytorze/buildzie Development. Zwykłe uruchomienie programu nie uruchamia testu. Logi, wyniki, kod testowej sesji i tymczasowe wyniki kompilacji usunięto po zapisaniu tego podsumowania.

Unity podczas buildu zapisało także GraphicsSettings oraz assety DefaultVolumeProfile, PC_RPAsset i UniversalRenderPipelineGlobalSettings. Próba cofnięcia tych dodatkowych zmian została odrzucona przez automatyczną kontrolę z powodu ryzyka utraty zmian niezapisanych w Git. Nie ponawiano cofania i pozostawiono stan plików użyty podczas udanego buildu. UnityConnectSettings ma włączone usługi. Nie cofano wcześniejszych zmian użytkownika.

Nie uruchamiano testów NUnit/Edit Mode ani ręcznego Play Mode. Testy sieciowe wykonano w dwóch procesach Windows na jednym komputerze, również przez internetową usługę Relay. Nie zweryfikowano ręcznie wyglądu animacji/Audio/VFX, fizycznej klawiatury/pada, mobilnego sterowania, kolizji ze ścianami, późnego dołączania ani zachowania na dwóch różnych łączach internetowych. Zachowano wcześniejsze ograniczenia dotyczące predykcji i diagnostyki providerów na replikach.

## 2026-11-24 — usunięcie przyczyn ostrzeżeń buildu

- OnlineTestSetup przekazuje budowanie do osobnej kolejki OnlineTestBuildQueue. Kolejka odświeża assety, żąda kompilacji, obsługuje błędy i czeka na przeładowanie zestawów edytora. Stan kolejki jest zachowany w SessionState. Dopiero potem wywołuje BuildPipeline.BuildPlayer.
- OnlineSmokeProbe nie używa już DEVELOPMENT_BUILD. Zwykły program nie uruchamia testu; poza edytorem wymagane są Debug.isDebugBuild oraz jawne argumenty testowe. Kod testu jest kompilowany przy KUCHARZE_ONLINE, a jego uruchamianie jest ograniczone w runtime.
- Odczytano rzeczywisty stan Unity Connect, bez zapisywania danych konta ani tokenów: konto zalogowane, ale projekt niepowiązany, brak projektu i organizacji w kontekście edytora. Wcześniejszy GUID w Player Settings nie był pełnym powiązaniem edytora.
- OnlineServicesProjectLink odnalazł wskazany wcześniej projekt w organizacjach już zalogowanego konta i powiązał go przez mechanizm Unity Connect. Nie tworzono projektu, nie zmieniano konta ani uprawnień. Status po operacji: LoggedIn=True, ProjectBound=True; identyfikator i nazwa projektu oraz identyfikator i nazwa organizacji są obecne.
- Kompilacja C# runtime i narzędzi edytora zakończona bez błędów. Rzeczywista kolejka przeszła kompilację i reload, pełne powiązanie usług oraz udany build Windows Development. W nowym fragmencie dziennika obejmującym końcowe powiązanie i build: zero UAC0009, zero ostrzeżeń o nieprzekompilowanych zmianach, zero ostrzeżeń ServicesCore o braku powiązania, zero błędów CS. Nie wyciszano logów pakietów. Informacje URP i Performance Testing pozostają normalnymi komunikatami.
- Nowy program przeszedł test dwóch procesów przez rzeczywiste Relay: host wygenerował kod, klient dołączył; oddzielne kierunki ruchu, autorytet serwera, dash, saldo monet, blokada przyznawania monet i pauzowania czasu przez klienta oraz rozłączenie klienta: PASS.
- Program zapisano oddzielnie w Builds/OnlineTest-Fixed. Starszej wersji nie nadpisano. Wyniki i logi pomocnicze oraz tymczasowe wyniki kompilacji usunięto po zapisaniu podsumowania. JSON-y Performance Testing nie pozostały w Assets/Resources po zakończeniu buildu.

Nie uruchamiano ręcznego Play Mode ani NUnit. Test dwóch procesów na jednym komputerze przez Relay nadal nie zastępuje testu na dwóch różnych łączach i ręcznej oceny prezentacji. Nie zmieniano silnika ruchu, umiejętności ani ich publicznych kontraktów.

## 2026-11-24 — usunięcie starego skryptu MainMenu

Na prośbę użytkownika usunięto UI/MainMenu.cs i jego .meta. Przed usunięciem sprawdzono pliki C#, sceny, prefaby i assety pod Assets: brak konsumentów klasy i odwołań do GUID skryptu poza samym usuwanym plikiem. Nie zmieniano sceny Main Menu, innych elementów UI ani panelu połączenia online. Zaktualizowano instrukcję sieciową, aby nie zakładała istnienia starego kontrolera menu. Wykonano sprawdzenie statyczne odwołań i diffu; nie uruchamiano Unity, kompilacji ani testów Play Mode dla tego usunięcia.

## 2026-11-24 — proste lobby przestrzenne

Przygotowano Networking/Lobby/Generated/Lobby.unity z czterema pokojami, korytarzem, strefą startową, nieruchomą kamerą i ruchomymi znacznikami spawnów. Round/Kuba.unity jest osobną kopią OnlineTest bez dodatkowego NetworkManagera; oryginalna scena Kuba i prefab OnlinePlayer nie były nadpisywane. Generator dodaje dwie sceny do wspólnej listy buildu, zachowując wcześniejsze pozycje. Logika lobby pozostaje w Networking/Lobby i pod KUCHARZE_ONLINE; LobbyLayout nie wymaga pakietów sieciowych.

Wykonano:

- Kompilację runtime i edytora kompilatorem C# Unity, a następnie import i dwa udane buildy Windows Development w izolowanej tymczasowej kopii projektu przez Unity w trybie batch, bez Computer Use. Drugi build obejmował końcowe reguły powrotu po rozłączeniu i wydzielone przypisania pokojów. Występuje wcześniejsze ostrzeżenie UAC0005 w OnlineServicesProjectLink; nowy kod nie wprowadził błędów kompilacji.
- Rzeczywisty test czterech procesów na lokalnym transporcie przez LobbySmokeProbe: host i trzej klienci zakończyli wynikiem PASS. Samotny host w strefie nie rozpoczął odliczania. Dwie osoby otrzymały oddzielne pozycje; komplet dwóch uruchomił licznik, wyjście jednej osoby go wyzerowało. Dołączenie trzeciej osoby podczas licznika wyzerowało gotowość; komplet trzech ponownie uruchomił odliczanie. Dołączenie czwartej zablokowało start: trzy osoby w strefie przez ponad 5 sekund nie uruchomiły gry. Cztery osoby uruchomiły wspólną zmianę sceny po pełnych 5 sekundach. W rozgrywce flaga przyjmowania nowych graczy była wyłączona. Po rozłączeniu trzech klientów host wrócił do lobby, do przypisanego pokoju, z zablokowanym odliczaniem w pojedynkę.
- Przegląd odpowiedzialności: LobbyLayout przechowuje znaczniki, LobbyRoomAssignments przydziela pokoje i pozycje wejścia, LobbySession prowadzi gotowość i wspólny przepływ scen, bootstrap obsługuje życie połączenia i zatwierdzanie dołączeń, a widok wyłącznie UI. Silnik ruchu nie otrzymał zależności od lobby. CharacterController.Move pozostaje wyłącznie w PlayerMotor. Wznowienie ruchu po ładowaniu dotyczy tylko resolverów, które wcześniej były włączone.
- Sprawdzenie diffu pod kątem błędów białych znaków.

Test sieciowy używał jawnych technicznych teleportów zamiast sterowania fizyczną klawiaturą/padem. Nie sprawdzono ręcznie Play Mode, kolizji w nowych pokojach, odczuwalnej płynności, animacji/Audio/VFX, dwóch różnych łączy internetowych ani połączenia nowego lobby przez Relay. Odrzucanie dołączeń w trakcie gry przejrzano w kodzie i sprawdzono flagę serwera, ale nie wykonano dodatkowej próby dołączenia w tej fazie. Rzeczywista zmiana sceny została sprawdzona z czterema graczami; dla dwóch i trzech sprawdzono warunki i odliczanie. Nie dodano ekranu podsumowania ani powrotu wyzwalanego timerem: przyszły ekran wywoła ReturnToLobby na serwerze.

Pierwsze żądanie buildu w otwartym edytorze pozostało na etapie READY bez końcowego raportu; nie uznano go za udany test i nie zamykano edytora użytkownika. Wyniki powyżej pochodzą z zakończonych procesów batch na kopii. Brakujące starsze pliki projektu, m.in. Documentation/Movement/README.md, nie były odtwarzane w ramach lobby.
Sprawdzono podgląd sceny renderowany przez API Unity na kopii projektu. Dla czytelnych kolorów prototypu zastosowano materiały URP Unlit; potwierdzono widoczność czterech pokojów, przejść, korytarza i zielonej strefy startowej. Ta zmiana dotyczy wyłącznie materiałów nowego lobby.

Usunięto tymczasową kopię projektu, buildy i logi bieżącego testu, podglądy PNG, pliki żądań i raportów wraz z .meta oraz dane Performance Testing wygenerowane przez rozpoczęty build w Assets/Resources. Pozostawiono wyłącznie sceny, materiały, prefaby, kod narzędzi/testów i trwałą dokumentację.

## 2026-11-24 — kąt kamery lobby zgodny z prefabem Core/Camera

Poprawiono wyłącznie Transform kamery LobbyCamera w scenie Lobby oraz generator LobbySceneSetup i specyfikację sieciową. Obrót sceny jest identyczny z obrotem Main Camera w Assets/Project/Prefabs/Core/Camera.prefab: X = 50°, Y = 0°, Z = 0°, zamiast wcześniejszych 90° z góry. Generator odczytuje obrót bezpośrednio z komponentu Camera w prefabie. Kamera pozostaje nieruchoma i ortograficzna; przesunięcie do (0, 22.981333, -19.283628) utrzymuje środek lobby w centrum kadru. Orthographic Size pozostaje 11.5.

Sprawdzono statycznie identyczność zapisanych kwaternionów, kierunek do środka planszy oraz objęcie geometrii lobby kadrem 16:9. Sprawdzono diff pod kątem białych znaków. Nie uruchamiano Unity, renderowania, Play Mode ani ponownych testów sieciowych dla tej korekty. Nie zmieniono ruchu, reguł lobby ani oryginalnego prefabu kamery.
## 2026-11-24 — poprawa wyglądu lobby i paneli

Dodano LobbyPresentationBuilder jako osobne narzędzie oprawy sceny; LobbySceneSetup deleguje do niego budowanie wyglądu. Zaktualizowano Lobby.unity z zachowaniem GUID, LobbyEntry, znaczników i strefy logicznej. Nowa grupa LobbyPresentation zawiera kafelkowany korytarz, drewniane podłogi, dywany w czterech kolorach, łóżka, stoliki, rośliny, ławki oraz ściany z listwami i jasnymi nakrywami. Dwadzieścia sześć materiałów prototypu znajduje się w Generated/Presentation. Kamera jest nieruchoma, ortograficzna: X = 55°, Y/Z = 0°, Size = 10.8. Jest to świadoma zmiana perspektywy dopuszczona przez użytkownika podczas poprawy wyglądu. Prefab Core/Camera pozostaje bez zmian.

LobbyConnectionView pokazuje skalowane karty gotowości i połączenia, czytelny kod, liczbę graczy i przyciski. Pozostaje wyłącznie widokiem i wywołuje te same operacje RelayConnection; nie zmieniono reguł gotowości, odliczania ani przejść scen.

Wykonano:

- Import oraz kompilację runtime i edytora w oddzielnej kopii projektu przez Unity batch, bez Computer Use. Końcową wersję runtime po drobnej poprawce wskazówki pola kodu i wyglądu fokusu sprawdzono dodatkowo kompilatorem C# Unity: zero błędów.
- Wygenerowanie i wizualne sprawdzenie renderu 1400x1000 z dwoma tymczasowymi modelami graczy. Potwierdzono czytelność pokojów, wejść, dekoracji i strefy startowej. Modele podglądu nie zostały zapisane w scenie.
- Test geometrii przez Physics.OverlapCapsule: wszystkie cztery spawny są wolne od kolizji z wyposażeniem. Sprawdzono również obecność środka pokoju startowego w strefie oraz brak gotowości w środku korytarza i brak brakujących skryptów w scenie.
- Porównanie serializowanych referencji LobbyLayout i parametrów triggera przed oraz po przebudowie: bez zmian. Przed skopiowaniem sceny sprawdzono, że nie została w międzyczasie zmieniona na dysku w oryginalnym projekcie.
- Udany build Windows Development wyłącznie dla tymczasowego podglądu. Uruchomiono lokalnego hosta z testowym kodem PREVIEW, bez Relay i kontaktu z innymi graczami; brak wyjątków skryptów w logu. Zrzut ekranu w trybie batch nie powiódł się; normalny proces uruchomiony z ukrytym oknem zapisał czarny obraz. Z tego powodu nie uznano wyglądu IMGUI w działającym oknie za zweryfikowany wizualnie. Render sceny z API edytora był poprawny.
- Sprawdzono diff pod kątem błędów białych znaków oraz zasadę: CharacterController.Move nadal należy tylko do PlayerMotor. Nie wprowadzono komponentu sterującego kamerą ani zmian w ruchu.

Nie powtarzano testu czterech klientów, Relay, fizycznej klawiatury/pada ani ręcznego chodzenia i kolizji w Play Mode. Wyposażenie ma nowe collidery, więc ręczne obejście łóżek, stolików i ławek wymaga jeszcze sprawdzenia. Skrypty testowego podglądu, build, logi i obrazy były wyłącznie tymczasowe i nie należą do projektu.
Po zapisaniu wyników usunięto całą tymczasową kopię projektu, techniczne skrypty podglądu, build, pliki kompilacji, logi i obrazy wraz z ich metadanymi. W projekcie zostały wyłącznie docelowa scena, materiały, kod prezentacji i dokumentacja.

## 2026-11-25 — przebudowa przestrzeni i prezentacji lobby

Po porównaniu z obrazami Scene/Game i referencjami użytkownika przebudowano generowaną scenę na kompaktowy układ 2×2 ze wspólnym holem, salonem i pokojem gotowości. Kamera w osobnym LobbyCamera.prefab ma perspektywę 15°, nachylenie 45° i dopasowuje odległość do stałych granic planszy oraz proporcji okna. Nie śledzi graczy. Standardowe materiały URP Simple Lit, miękkie cienie, grubsze ściany i stojące tablice zastąpiły płaską prezentację. Osobny LobbyCameraRendering przywraca ustawienia renderowania po rysowaniu kamery.

LobbyPlayer.prefab otrzymał wizualnego rodzica poza Animatorem, wydzielone materiały stroju i pierścień przy stopach. PlayerNetworkAppearance replikuje paletę przypisanego pokoju, korzystając z osobnych bloków materiałów. Oryginalny OnlinePlayer, prefab Core/Camera, kontroler ruchu i globalny asset pipeline pozostały bez zmian. Odpowiedzialności są rozdzielone między generator geometrii, przygotowanie prefabu, kadrowanie, renderowanie i adapter wyglądu; CharacterController.Move nadal występuje wyłącznie w PlayerMotor.

Wykonano bez Computer Use, na izolowanej kopii projektu:

- Import i kompilację runtime oraz edytora w Unity batch, zakończone bez błędów. Końcową, wyłącznie edytorową zmianę dodającą menu Build Lobby Test sprawdzono ponownie kompilatorem C# Unity: zero błędów; pozostało wcześniejsze ostrzeżenie UAC0005 w OnlineServicesProjectLink.
- Render GPU 1400×1000 przez API Unity z trzema tymczasowymi postaciami i wizualną ocenę kadru, kolorów, proporcji oraz cieni. Postacie podglądu nie są zapisane w scenie. Sprawdzono wolne spawny, brak brakujących skryptów, środek pokoju wewnątrz strefy i korytarz poza nią.
- Walidację zachowania liczby wierzchołków, trójkątów i bind poses, kontrolera Animatora oraz ścieżek animacji po podziale materiałów. Rig i geometria źródłowa nie zostały przebudowane.
- Asercje przywrócenia natywnego ustawienia SRP Batcher i zasięgu cieni po renderowaniu. Hash oryginalnego PC_RPAsset pozostał identyczny. Nie pozostawiono eksperymentalnego shadera ani dodatkowego globalnego renderera.
- Udany build Windows Development oraz rzeczywisty test czterech procesów z grafiką D3D11 na lokalnym transporcie. Host i trzej klienci zakończyli wynikiem PASS. Sprawdzono cztery niezależne palety i kolory bloków materiałów na serwerze i klientach, brak startu solo, odliczanie dla dwóch i trzech osób, reset po wyjściu/dołączeniu, brak startu przy trzech z czterech przez ponad 5 sekund, wspólne przejście czterech do rundy po pełnym odliczaniu, flagę blokady dołączania oraz powrót samotnego hosta do przypisanego pokoju po rozłączeniu klientów.
- Pierwsza próba tego rozszerzonego testu z -nographics nie przeszła odczytu kolorów rendererów i nie jest uznana za sukces. Końcowy wynik PASS pochodzi z czterech procesów z aktywną grafiką.
- Sprawdzenie diffu pod kątem błędów białych znaków oraz zgodności skopiowanej sceny i prefabów z testowaną wersją.

Test techniczny używał jawnych teleportów. Nie wykonywano ręcznych testów Play Mode, sprzętowej klawiatury/pada, odczuwalnej płynności, obejścia wszystkich mebli ani połączenia nowej wersji lobby przez internet/Relay. Render świata nie potwierdza wizualnie całego interfejsu IMGUI w normalnym oknie gry. Nie zmieniono reguł ruchu i nie powtarzano ich pełnego zestawu testów. Po zmianie prefabów wszyscy uczestnicy wymagają nowego buildu.

Po porównaniu hashy potwierdzono zgodność docelowej sceny oraz prefabów gracza i kamery z testowaną kopią. Usunięto całą własną tymczasową kopię projektu, buildy, obrazy, logi i pliki kompilacji; potwierdzono brak katalogu tymczasowego. Nie zamykano edytora użytkownika ani nie zapisywano jego niezapisanej sceny.

## 2026-11-25 — Main Menu i wejście do istniejącego lobby

Dodano scenę `Assets/Project/Scenes/Debug/Kuba/Main Menu.unity`, moduły nawigacji, cztery wpisy Credits, opcjonalną muzykę oraz zapisywane Music/Sounds. Nowy adapter menu wykorzystuje istniejący prefab LobbyConnection. RelayConnection otrzymał dodatkowe HostInScene, zachowując dotychczasowe Host i Join. LobbySessionBootstrap może wracać do menu, a LobbyConnectionView ukrywa prototypowy HUD w tej scenie. Nie zapisywano ani nie przebudowywano istniejącej sceny Lobby; zachowano jej zastane zmiany. GameAudio zachowuje SFX, jego identyfikatory i przypisania graczy, a dodaje Music oraz dwa jawne parametry głośności.

Wykonano na izolowanej kopii projektu w Unity 6000.6.3f1:

- Import i kompilację kodu runtime oraz edytora bez błędów C#. Pierwsza próba generatora zakończyła się odmową Unity dodania sceny do nienazwanej sceny startowej; powtórzenie po otwarciu zapisanej sceny wygenerowało menu prawidłowo. Generator ma komunikat nakazujący zapisać nienazwaną scenę.
- Walidację trzech scen w buildzie, menu jako pierwszej włączonej sceny, braku brakujących skryptów i trwałych połączeń wszystkich dziesięciu przycisków.
- Render GPU czterech ekranów w 1920 × 1085 i ocenę wizualną. Ekran główny odwzorowuje Template.png. Po pierwszym renderze poprawiono wysokość uchwytów suwaków i ponownie obejrzano Options.
- Automatyczne testy Play Mode: Home/Options/Credits/Play, cztery edytowalne wpisy i odświeżenie nazwy, niezależne wartości Music 25% i Sounds 75%, odczyt obu parametrów miksera w dB, wyciszenie Sounds przy 0%, zapis PlayerPrefs, odrzucenie pustego kodu bez próby połączenia oraz przejście Lobby → Main Menu bez duplikacji miksera ustawień i NetworkManagera. Poprzednie ustawienia PlayerPrefs przywrócono po teście.
- Osobny test Play Mode z rzeczywistym Unity Relay: MainMenuOnlinePanel.Host utworzył przydział Relay, załadował lobby, uruchomił hosta, utworzył postać i udostępnił kod. Samotny host pozostał w lobby przez ponad pięć sekund. Disconnect wrócił do Main Menu.
- Końcowe powtórzenie testów UI po poprawce suwaków i wywołanie rzeczywistego przycisku Quit, który zakończył Play Mode.
- Przegląd odpowiedzialności: menu, audio i sieć są rozdzielone; kod ruchu pozostał bez zmian. Wykonanie CharacterController.Move nadal należy wyłącznie do PlayerMotor. Nie dodano zależności menu do komponentów gracza.

Podczas startu kopii edytor zgłaszał niezależny wyjątek UnityEditor.Search.SearchDatabase; nie zatrzymał on kompilacji, renderów ani powyższych testów. Nie traktuje się go jako błędu menu ani jako naprawionego problemu. Nie wykonano testu dołączania drugiej instancji przez nowe menu, ręcznych testów myszy/klawiatury/pada, odsłuchu nieprzypisanego jeszcze utworu, testu Quit w buildzie ani pełnej regresji ruchu. Gotowy kod dołączania korzysta z wcześniejszego RelayConnection.Join; pełny test dwóch osób pozostaje do wykonania na aktualnym buildzie.

Potwierdzono zgodność hashy docelowej sceny, sześciu zmienionych importerów grafik oraz wszystkich zmienionych skryptów i miksera z testowaną kopią. Usunięto całą tymczasową kopię projektu, skrypty testów, logi, obrazy podglądu i pliki żądań wraz z metadanymi. W projekcie pozostały wyłącznie docelowe assety, kod i dokumentacja. Zaktualizowano AGENTS.md o autoryzowany zakres menu i ustalone angielskie teksty oraz układ według Template.png.

## 2026-11-25 — jasne panele menu, autorzy i początek suwaków

Na prośbę użytkownika dodano MainMenu_Group do Home, Play, Options i Credits. Nagłówki oraz nazwiska korzystają z Jua, opisy i role z Madimi One. Credits ma uzgodnione cztery nazwiska i pełną listę ról w dwóch kolumnach; Role pozostaje wielowierszowym polem do edycji. Back ma fiolet istniejących Options/Credits, Host/Join turkus Play. Wypełnienie suwaków rozszerzono z 672 do pełnych 700 jednostek szerokości Track, usuwając ciemną szczelinę przy lewej krawędzi. Niewypełniona część jest jasna. Tło wypełnia również szerokie okna bez bocznych pasów. Nowy edytorowy MainMenuPresentationSetup aktualizuje prezentację istniejących elementów; generator nowej sceny używa tego samego układu. Nie zmieniono zasad połączeń, audio ani ruchu.

Wykonano w Unity 6000.6.3f1 na izolowanej kopii projektu:

- Import i kompilację zmienionych skryptów runtime oraz edytora bez błędów C#.
- Dwukrotne odświeżenie wyglądu bez duplikowania panelu Home i przycisków Back. Walidacja nadal potwierdza dziesięć podłączonych przycisków i brak brakujących skryptów.
- Sprawdzenie obecności MainMenu_Group na czterech ekranach oraz dokładnej zgodności czterech nazwisk i wszystkich ról z prośbą użytkownika.
- Kontrolę preferredWidth nazwisk i preferredHeight ról względem ich pól. Pierwszy test wykrył zbyt niskie pole czterech ról Adriany; wysokość zwiększono do 160 jednostek i końcowy test przeszedł dla wszystkich osób.
- Render GPU czterech ekranów przy 1920 × 1085, 1280 × 720 i 2400 × 1080. Obejrzano Home, Play, Options i Credits, a po usunięciu pasów tła także szeroki podgląd Options. Wszystkie role są widoczne; Back pozostaje w panelach.
- Asercje geometrii obu suwaków przy 0%, 50% i 100%: szerokość wypełnienia wynosi odpowiednio 0, 350 i 700 jednostek, a przy 100% lewa krawędź wypełnienia pokrywa się z lewą krawędzią Track.
- Porównanie hashy zmienionych źródeł z testowaną wersją oraz sprawdzenie, że oryginalna scena i importer grafiki nie zmieniły się w czasie pracy, zanim skopiowano wynik. Zachowano GUID sceny i grafiki oraz istniejące przypisania audio i przycisków. Sprawdzono diff pod kątem błędów białych znaków.

Nie powtarzano Play Mode, testów Relay, sprzętowej myszy/klawiatury/pada, rzeczywistego telefonu ani regresji ruchu. Zakres tej zmiany dotyczy prezentacji i danych Credits. Testy trzech rozmiarów renderu nie oznaczają testu fizycznych urządzeń mobilnych. CharacterController.Move i komponenty ruchu pozostały bez zmian. Kolory i rozmiary przyszłych PNG przycisków zapisano w Documentation/UI/MainMenu.md jako propozycję; same nowe ilustracje pozostają do wykonania przez użytkownika.

Po zakończeniu usunięto całą własną tymczasową kopię projektu, renderowane obrazy, skrypt weryfikacji, logi i pliki pomocnicze wraz z metadanymi. W projekcie pozostały docelowa scena, zmieniony importer MainMenu_Group, kod i dokumentacja.

## 2026-11-25 — kontrast Credits, animacje kontrolek i opcje wyświetlania

Dodano cztery jednolite ciemne karty CreditEntry1–4 z jasnymi nazwiskami i rolami; są placeholderami Image do późniejszego podmienienia Source Image. Istniejące etykiety i referencje MenuCredits zostały zachowane. MenuSelectableScale daje przyciskom wszystkich ekranów i nowym kontrolkom Options powiększenie po najechaniu/zaznaczeniu oraz zmniejszenie przy naciśnięciu wskaźnikiem. Suwaki nie zmieniają skali. MenuVideoSettings odpowiada wyłącznie za lokalne preferencje FPS/VSync/fullscreen, MenuVideoOptions za ich UI, a MainMenuOptionsSetup za utworzenie kontrolek w edytorze. Na PC limit wynosi 30/60/120/144 lub Unlimited, VSync blokuje ręczny wybór limitu; na urządzeniach mobilnych dostępne są 30/60 FPS, bez kontrolek trybu okna i VSync. Pełny ekran używa FullScreenWindow, a zwykłe okno Windowed. Tryb okna jest zmieniany wyłącznie w buildzie, bez przełączania okien edytora. Nie zmieniono profili Quality/URP ani komponentów ruchu i sieci.

Wykonano w Unity 6000.6.3f1 na izolowanej kopii projektu:

- Import i kompilację nowych klas runtime oraz edytora bez błędów C#.
- Powtórne odświeżenie wyglądu, walidację dziesięciu trwałych połączeń przycisków, jednej instancji konfiguracji i czterech kart autorów, bez brakujących skryptów. Sprawdzono obecność komponentu animacji na wszystkich przyciskach.
- Render GPU Credits i Options w 1280 × 720 oraz 2400 × 1080 i ocenę wizualną. Kontrola preferredWidth/preferredHeight potwierdziła, że nazwiska i komplet ról mieszczą się w kartach.
- Automatyczne testy Play Mode z syntetycznymi zdarzeniami UI: powiększenie po najechaniu także przy Time.timeScale = 0, zmniejszenie przy naciśnięciu, powrót do skali bazowej po opuszczeniu, powiększenie zaznaczonej kontrolki oraz reset po zablokowaniu i ukryciu ekranu.
- Testy Play Mode nowych opcji: wybór 30 FPS ustawia Application.targetFrameRate, VSync ustawia vSyncCount = 1 i blokuje listę FPS, wyłączenie VSync przywraca listę oraz zachowany limit. Checkbox pełnego ekranu zapisuje preferencję i nie zmienia trybu okna edytora. Sprawdzono zapis trzech kluczy PlayerPrefs oraz zachowanie limitu i jednej instancji ustawień po przejściu do Lobby i z powrotem do menu.
- Po testach przywrócono wcześniejsze wartości PlayerPrefs, vSyncCount i targetFrameRate. Potwierdzono zgodność hashy źródeł z kopią testową oraz niezmienność oryginalnej sceny podczas pracy przed skopiowaniem wyniku.

Podczas uruchomienia kopii edytora wystąpił znany wyjątek UnityEditor.Search.SearchDatabase, niezależny od menu; testy zakończyły się PASS. Nie wykonano fizycznego testu myszy/pada/dotyku, prawdziwego urządzenia mobilnego ani rzeczywistego przełączenia trybu okna w buildzie. Test pełnego ekranu obejmował zapis preferencji i ochronę okien edytora. Nie powtarzano Relay, testów wielu graczy ani regresji ruchu; kod tych systemów pozostał bez zmian, a CharacterController.Move nadal należy do PlayerMotor.

Po zakończeniu potwierdzono usunięcie całej własnej kopii testowej, renderów, logów, skryptu testów, plików pomocniczych i ich metadanych. W projekcie pozostawiono wyłącznie docelową scenę, komponenty menu, ich metadane i dokumentację.

## 2026-11-25 — tylko landscape na telefonach i poprawna obsługa Device Simulator

W Player Settings wyłączono Portrait i Portrait Upside Down. Auto Rotation pozostaje włączone z dozwolonymi wyłącznie Landscape Left i Landscape Right; konfiguracja jest wspólna dla Androida/iOS. Zasadę orientacji całej gry zapisano w AGENTS.md. Nie dodano logiki orientacji do graczy, ruchu ani poszczególnych scen.

Naprawiono uznawanie każdego uruchomienia w edytorze za PC: nowy MenuPlatform korzysta z UnityEngine.Device.Application.isMobilePlatform przed sprawdzeniem edytora. MenuVideoSettings wykrywa przełączenie aktywnej platformy podglądu i powiadamia MenuVideoOptions, które aktualizuje otwarty ekran, listę FPS oraz widoczność Fullscreen i VSync. Mobilny limit ma oddzielny klucz PlayerPrefs, z fallbackiem dla wcześniejszego wspólnego zapisu. MenuSelectableScale korzysta z tego samego rozpoznania telefonu i usuwa desktopowy hover także po przełączeniu widoku w trakcie Play Mode. Zachowano istniejące serialized fields i publiczne API właściwości SupportsDisplayOptions/FrameLimits.

Na izolowanej kopii projektu w Unity 6000.6.3f1 wykonano:

- Import i kompilację zmienionych klas runtime oraz tymczasowego testu edytorowego bez błędów C#; sprawdzenie istniejącej walidacji sceny menu.
- Kontrolę PlayerSettings.defaultInterfaceOrientation przez API edytora: AutoRotation, obie orientacje pionowe wyłączone, obie poziome włączone.
- Automatyczny test Play Mode z rzeczywistymi klasami ApplicationSimulation i ScreenSimulation modułu Unity Device Simulator, sterowanymi programowo przez test. Wykorzystano dostarczone przez Unity definicje Xiaomi Redmi Note 7 (Android) i Apple iPhone 12 (iOS).
- Dla każdego urządzenia sprawdzono cztery obroty 0/90/180/270 stopni: Device.Screen.orientation zawsze wskazywał Landscape Left/Right, a szerokość była większa niż wysokość.
- Przejście desktop → Android → iOS → desktop z otwartymi Options, bez restartu Play Mode. Na obu telefonach Fullscreen i VSync miały activeSelf=false, a lista zawierała dokładnie 30 FPS i 60 FPS. Po wyłączeniu symulacji wróciły opcje PC oraz wcześniejszy limit 144 FPS.
- Zmiana mobilnego limitu na 30 FPS nie nadpisała desktopowego klucza 144 FPS. Wywołanie zmiany Fullscreen podczas symulacji telefonu nie zmieniło preferencji pełnego ekranu. Ponowne otwarcie Options w iOS zachowało poprawną widoczność.
- Symulowane zaznaczenie i najechanie nie powiększały kontrolki telefonu, natomiast naciśnięcie zachowało reakcję dotykową. Po teście przywrócono wcześniejsze PlayerPrefs, vSyncCount i targetFrameRate.
- Porównanie hashy testowanych źródeł z oryginałem oraz sprawdzenie diffu i dwóch zmienionych flag Player Settings. Kod ruchu i jego jedyny CharacterController.Move pozostały bez zmian.

Test korzystał z prawdziwych mechanizmów symulatora Unity, ale nie z ręcznego obracania jego okna ani fizycznego telefonu. Nie wykonano buildu Android/iOS, testu OS-owej blokady obrotu na urządzeniu ani ponownego testu Relay/multiplayer i regresji ruchu. W logu startowym kopii wystąpił znany niezależny wyjątek UnityEditor.Search.SearchDatabase; końcowy test zakończył się PASS.

Po zakończeniu potwierdzono usunięcie własnej tymczasowej kopii projektu, testu symulatora, logów oraz pliku pomocniczego i jego metadanych. Pozostały wyłącznie poprawki kodu, ustawień projektu i dokumentacji.

## 2026-11-25 — szybszy hover, fokus startowy, checkboxy i dźwięki UI

MenuSelectableScale ma czas reakcji 0.035 s. Nowy MenuInputMode odróżnia mysz od celowej nawigacji UI. MainMenuNavigation i EventSystem nie wybierają Options automatycznie przy starcie; strzałki/WASD/pad nadal mogą rozpocząć nawigację. Frame limit nie ma komponentu skalującego, nie zmienia koloru po najechaniu/zaznaczeniu i nie odtwarza hoveru. Checkboxy Fullscreen/VSync mają turkusowe wypełnienie, ciemny obrys i powiększony ciemny znacznik.

Dodano opcjonalny MenuUISounds z polami Hover Clip i Click Clip, głośnością i wyjściem SFX. Jeden odtwarzacz 2D przeżywa zmianę sceny; MenuSoundFeedback przekazuje interakcje przycisków, checkboxów i listy. Klipy nie są przypisane za użytkownika. Sounds obejmuje także audio UI przez istniejący mikser. Osobny MainMenuFeedbackSetup aktualizuje tylko interakcję, zachowując układ, grafiki i istniejące zdarzenia. Odtwarzanie nie należy do komponentu animacji.

W OnlineServicesProjectLink zastąpiono AppDomain.CurrentDomain.GetAssemblies przez UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies. Zweryfikowano kod zainstalowanego AdaptiveResolution.OnDestroy: komunikat przywrócenia skali 1.0 jest wywołaniem diagnostycznym APLog.Debug, a nie wyjątkiem; pakietu nie wyłączano i nie wyciszano jego logów.

Sprawdzono w Unity 6000.6.3f1 na izolowanej kopii:

- Kompilację runtime i edytora oraz brak UAC0005 w końcowej kompilacji. Wyszukiwanie obu wewnętrznych typów UnityConnect/UnityConnectRequests nowym API zakończyło się poprawnie, bez uruchamiania powiązania konta lub usług.
- Powtórną aktualizację interakcji, zachowanie dziesięciu trwałych połączeń przycisków, pojedynczego odtwarzacza, przypisania SFX, braku domyślnego zaznaczenia EventSystem i wartości 0.035 na kontrolkach. Lista FPS nie ma skalowania ani odrębnej barwy hover/selected.
- Render GPU Options w 1280 × 720 ze stanem zaznaczonym i niezaznaczonym checkboxów oraz ocenę czytelności koloru, obrysu i znacznika.
- Automatyczne Play Mode: Options pozostaje w skali 1 i bez zaznaczenia przy uruchomieniu z kursorem poza menu; najechanie szybko powiększa przycisk, a samo zaznaczenie myszą nie zostawia powiększenia po jej odsunięciu.
- Test z wirtualną klawiaturą i myszą: wejście nawigacyjne rozpoczyna fokus z pustego zaznaczenia, a ruch myszy wyłącza tryb nawigacji. Ukryty edytor wymagał w samym teście dopuszczenia wirtualnego wejścia do Game View i aktualizacji Dynamic. Test listy wykonywano po jej Start; wcześniejsza próba otwarcia w tym samym wywołaniu co aktywacja ekranu była błędem czasu uruchomienia testu. Końcowy test zakończył się PASS.
- Sprawdzenie, że puste klipy pozostają ciche. Na wygenerowanych bezgłośnych klipach potwierdzono uruchomienie głosu AudioSource po hover i kliknięciu. Zaakceptowany klik zachował dźwięk także po ukryciu własnego ekranu przez trwały callback przycisku. Nieaktywne przyciski nie generowały dźwięków.
- Otwieranie i zamykanie listy FPS bez zmiany skali, koloru hover i dźwięku hover, bez duplikowania odtwarzacza po nawigacji. Suwaków nie objęto animacją ani dźwiękiem przeciągania.
- Zgodność hashy dziewięciu zmienionych/nowych plików źródłowych z wersją testową. Przed zapisem sprawdzono, że oryginalna scena nie została w międzyczasie zmieniona. Zachowano GUID sceny i skryptów już zaimportowanych w oryginalnym edytorze: w wyniku zamieniono jedynie 14 odwołań m_Script nowych komponentów na ich oryginalne GUID; kod i pozostałe dane sceny odpowiadają sprawdzonej wersji.

W logach startowych kopii wystąpił znany niezależny wyjątek UnityEditor.Search.SearchDatabase. Nie przeprowadzono ręcznego testu sprzętowej myszy/pada, odsłuchu docelowych nagrań użytkownika, buildu ani regresji Relay, ruchu i fizycznego telefonu. Test audio sprawdzał odtwarzanie głosów na syntetycznych cichych klipach i wyjście SFX, nie ocenę brzmienia. Ruch i jego pojedynczy CharacterController.Move pozostały bez zmian.

Po zakończeniu usunięto własną kopię testową, skrypty wirtualnego wejścia i audio, rendery, raporty, logi oraz pliki pomocnicze wraz z metadanymi. W projekcie pozostały docelowe zmiany sceny, kodu i dokumentacji.

## Main Menu — karty Credits, Play i checkboxy (2026-11-25)

Usunięto MenuSelectableScale z checkboxów Fullscreen/VSync oraz ich wyróżnienie koloru i dźwięk hover. Zmiana zaznaczenia zachowuje dźwięk kliknięcia. Generator Options nie przywraca już animacji. Credits używa czterech pomniejszonych MainMenu_Group z pastelowymi odcieniami i jasnymi RoleSurface; role są wyśrodkowane. Jakub: 2D Graphics, 3D Graphics, Programmer, UI/UX. Play ma osobne sekcje host/join, bardziej zaokrąglone pole kodu i pasek statusu. Opisano kolory i wymiary czterech grafik przycisków w Documentation/UI/MainMenu.md.

Sprawdzono w Unity 6000.6.3f1 na izolowanej kopii: kompilację, dwukrotne Refresh bez powielania paneli, Validate Main Menu (10 trwałych połączeń przycisków, brak brakujących skryptów i poprawne wpisy scen), brak skalowania i barwy hover/selected na checkboxach, ustawienie ich stanu przez SetIsOnWithoutNotify, przypisania MainMenu_Group, treść i granice tekstu Credits oraz zaokrąglenie pola kodu. Wygenerowano sześć renderów GPU: Play, Options i Credits w 1280×720 oraz 2400×1080. Oceniono Play i Credits w obu proporcjach oraz Options w 1280×720; brak nakładania tekstów i obcięcia paneli. Końcowe sprawdzenie zakończyło się PASS.

Pierwszy import kopii nie zarejestrował od razu klasy MainMenuOnlinePanel; ponowny import i restart kopii przywróciły mapowanie, bez zmiany skryptu sieciowego. Test referencji grafiki poprawiono, aby porównywał ścieżkę assetu bez rozróżniania wielkości liter, zamiast nazwy sprite. Końcowy wynik dotyczy poprawionego testu. Potwierdzono zgodność hashy czterech plików C# i ich metadanych z kopią testową. Scenę zapisano po sprawdzeniu, że oryginał nie zmienił się podczas kontroli; zachowano jego metadane.

Nie wykonywano w tym zadaniu Play Mode, ręcznych kliknięć checkboxów, odsłuchu, połączenia Relay, buildu ani testu fizycznego telefonu. Test stanu checkboxa był sprawdzeniem w edytorze, nie pełnym testem wejścia w grze. Nie zmieniano ruchu, PlayerMotor, danych graczy ani konfiguracji usług. Każdy zmieniony komponent zachowuje dotychczasową odpowiedzialność: narzędzia edytora stylizują scenę, a MenuSoundFeedback przekazuje wyłącznie interakcje dźwiękowe.

Po kontroli usunięto całą własną kopię testową, skrypt weryfikujący, sześć renderów, logi i pomocnicze pliki wraz z metadanymi. Kontrola diff sceny wskazuje standardowe spacje po pustych wartościach YAML zapisanych przez Unity; zachowano format serializera.

## Main Menu — nowe przyciski, pole kodu i rozdzielczość PC (2026-11-25)

Zmieniono UI/MainMenu/MenuVideoSettings.cs i MenuVideoOptions.cs: wybór rozdzielczości PC zapisuje szerokość/wysokość w PlayerPrefs, usuwa duplikaty częstotliwości z listy, odrzuca niepoprawne wybory i odtwarza poprawną wartość zastępczą dla nieobsługiwanego zapisu. Na telefonach i w Device Simulator lista i jej etykieta korzystają z tej samej bramki MenuPlatform co Fullscreen/VSync. Screen.SetResolution jest wywoływane wyłącznie w buildzie PC; borderless pozostaje dotychczasowym trybem pełnoekranowym. Istniejące pola i konsumenci ustawień pozostają zgodni, a nowe referencje UI są opcjonalne dla starszych scen.

MainMenuOptionsSetup ustawia nowy układ Display; MainMenuPresentationSetup przygotowuje jasne pole kodu i ciemny pasek statusu. Nowy MenuRoundedPanel odpowiada tylko za geometrię tła i obrysu, z wymaganym CanvasRenderer; nie przejmuje wejścia ani sieci. MainMenuArtworkSetup importuje i przypisuje nowe Back/HostGame/JoinGame/Cancel, bez dublowania napisów i bez podmieniania obiektów Button. Zapisano Main Menu.unity i metadane czterech grafik, zachowując GUID-y. Instrukcja i ostateczna paleta znajdują się w Documentation/UI/MainMenu.md. Nie zmieniano kodu ruchu, PlayerMotor ani kontraktów Relay.

Sprawdzono w Unity 6000.6.3f1 w izolowanej kopii projektu:

- Kompilację runtime i edytora; dwukrotne Refresh bez powielania elementów; Validate Main Menu, w tym 10 trwałych połączeń przycisków, brak brakujących skryptów i wpisy scen.
- Przypisanie nowych sprite, białą barwę Image i wyłączenie dodatkowych etykiet, obecność dwóch MenuRoundedPanel i ich CanvasRenderer.
- Cztery rendery GPU: Play i Options w 1280×720 oraz 2400×1080; oceniono oba ekrany w obu proporcjach. Sprawdzono wysokości opisu Join, placeholdera i wpisanego kodu; podgląd 2400×1080 zawiera AB12CD. Pierwsza kontrola wizualna ujawniła brak CanvasRenderer na nowym tle oraz zbyt małe pola tekstowe na szerokim ekranie; zostały poprawione, a końcowe rendery i testy przeszły.
- Automatyczne Play Mode: aktywność i liczba opcji Resolution na PC, brak duplikatów, przekazanie wyboru przez zdarzenie listy, zapis obu wymiarów, ponowny odczyt preferencji przez metodę ReadPreferences, fallback dla niepoprawnych zapisanych wymiarów, odrzucenie niepoprawnego żądania SetResolution, zachowanie rozdzielczości po SetFullscreen(false) i nawigacji Home/Options, działanie SetFrameLimit/SetVSync oraz przypisanie tekstu do pola kodu. Preferencje testowe używały osobnej nazwy firmy/produktu i zostały wyczyszczone.
- Zgodność hashy sześciu plików C# i ich metadanych z kopią testową. Przed zapisem zweryfikowano, że oryginalna scena i cztery metadane grafik nie zmieniły się w trakcie kontroli. Końcowy wynik: MENU ART CHECK PASS.

Nie wykonywano w tym zadaniu buildu ani rzeczywistej zmiany rozmiaru okna/rozdzielczości monitora, testu telefonu/Device Simulator, ręcznego wejścia sprzętowego, odsłuchu ani połączenia Relay. Play Mode testował zapis i logikę ustawień, a nie fizyczny tryb wyświetlania. Log startowy zawierał niezależny wyjątek UnityEditor.Search.SearchDatabase; nie zgłaszano czystej całej Console. Ruch i jego wcześniejsze testy pozostają poza zakresem tej zmiany.

Po ukończeniu usunięto całą własną kopię testową, skrypt kontrolny, cztery rendery, logi i pliki pomocnicze wraz z metadanymi. W projekcie pozostały wyłącznie docelowe pliki tej zmiany.

## Main Menu — rozmiar Game View i stały komunikat VSync (2026-11-25)

W MenuVideoSettings usunięto dopisywanie Screen.width/height do listy rozdzielczości. Wartości takie jak 1262×710 pochodziły z bieżącego okna Game View/gry, a nie z listy trybów monitora. Lista używa teraz wyłącznie zgłoszonych trybów monitora, grupowanych bez powtórzeń częstotliwości; przy pustej liście otrzymuje bieżący tryb monitora, awaryjnie 1280×720. Domyślny wybór i stary nieobsługiwany zapis preferują bieżący tryb monitora, jeżeli jest dostępny. MenuVideoOptions zachowuje stały tekst Your settings are saved automatically. po obu zmianach VSync; blokowanie listy FPS pozostało. Zachowano pola serializowane i kontrakty konsumentów, bez zmian ruchu i sieci.

Uzupełniono Documentation/UI/MainMenu.md oraz Documentation/Networking/README.md o aktualny test Main Menu: jeden edytor + jeden build, właściwa lista trzech scen, Host/Join kodem, dwie niezależne postacie i wspólna strefa gotowości. Sprawdzono plik EditorBuildSettings (Main Menu pierwszy, Lobby i Round/Kuba włączone), obecność pakietów w manifest.json oraz osobne profile Authentication editor/player w RelayConnection. Nie oznacza to potwierdzenia bieżącego działania chmury ani połączenia.

Wykonano kompilację całej bieżącej Assembly-CSharp kompilatorem Roslyn dostarczonym z Unity 6000.6.3f1, używając istniejącego pliku odpowiedzi Bee i zmieniając wyłącznie ścieżki wyjściowe na własny katalog tymczasowy. Kod wyjścia: 0. Sprawdzono kod wyboru wartości zastępczej i stały komunikat oraz kontrolę whitespace dokumentacji. Nie uruchamiano Unity ani Play Mode w tej zmianie, nie wykonano buildu, połączenia Relay ani testu fizycznego monitora/telefonu. Tymczasowy plik odpowiedzi i wszystkie pliki wyjściowe kompilatora usunięto po sprawdzeniu; nie dodano nowych testów dla tych małych zmian.

## Main Menu — krótka lista rozdzielczości i przewijanie zaznaczenia (2026-11-25)

MenuVideoSettings ogranicza listę do maksymalnie pięciu pozycji: do czterech typowych, obsługiwanych rozdzielczości o proporcjach zbliżonych do monitora i wymiarach nie większych niż bieżący tryb pulpitu oraz samego trybu pulpitu. Usuwane są duplikaty. Poprzednie zapisy wykluczonych rozmiarów korzystają z istniejącego mechanizmu wyboru wartości zastępczej.

Nowy MenuDropdownSelectionScroll ujawnia zaznaczony wiersz raz po otwarciu Resolution, w miarę możliwości na środku viewportu, i nie ingeruje w dalsze przewijanie. MenuVideoOptions zapewnia komponent w istniejącej scenie w runtime; MainMenuOptionsSetup dodaje go także podczas aktualizacji sceny w edytorze. Nie wymaga to ręcznego przebudowywania sceny ani zmiany jej referencji. Nie zmieniono dotychczasowych pól serializowanych, kodu ruchu ani sieci. Każdy zmieniony skrypt zachowuje jedną odpowiedzialność: polityka ustawień, powiązanie kontrolek, zachowanie przewijania albo przygotowanie UI w edytorze.

Sprawdzono w Unity 6000.6.3f1 na izolowanej kopii: kompilację runtime i edytora oraz automatyczne Play Mode na rzeczywistej liście Resolution z Main Menu. Lista monitora testowego zawierała dokładnie 1280×720, 1600×900, 1920×1080 i 2560×1440. Zweryfikowano limit pięciu, brak duplikatów, zgodność dodatkowych trybów z raportem monitora i proporcjami, obecność bieżącego trybu oraz liczbę pozycji w UI. Na testowo zmniejszonym viewporcie (88 jednostek, aby wymusić przewijanie) otwarto listę z wybraną pierwszą, środkową i ostatnią pozycją: cały zaznaczony wiersz był widoczny. Po ręcznym przestawieniu przewijania kontrola nie przyciągała go z powrotem. Powtórne otwarcia podążały za wyborem; nawigacja Home/Options nie powielała komponentu. Końcowy wynik: MENU SCROLL CHECK PASS.

Potwierdzono zgodność hashy czterech plików C# i ich metadanych z wersją testowaną. Preferencje testu używały osobnej nazwy firmy/produktu i zostały wyczyszczone. Nie wykonywano buildu, rzeczywistej zmiany rozdzielczości, ręcznego testu sprzętowego ani testu innych fizycznych monitorów, telefonu lub Relay.

Log kopii zawierał niezależny wyjątek UnityEditor.Search.SearchDatabase podczas indeksowania; nie zgłaszano pustej Console. Po kontroli usunięto całą własną kopię, skrypt testowy, logi i pliki pomocnicze wraz z metadanymi.

## Audyt Windows i Android — Main Menu i połączenie (2026-11-25)

Zakres: aktualna scena Main Menu, jej ustawienia, konfiguracja platform oraz istniejący przepływ Host/Join. Użytkownik potwierdził Android jako cel mobilny. Audyt wykonano na izolowanej kopii w Unity 6000.6.3f1, z obecnymi pakietami (m.in. Input System 1.20.0, NGO 2.14.0, Multiplayer Services 2.3.3 i URP 17.7.0). Nie oznacza to certyfikacji całej gry na każdym urządzeniu.

### Poprawka i znalezione braki

- W ProjectSettings/ProjectSettings.asset brakowało KUCHARZE_ONLINE dla Androida; symbol był tylko dla Standalone. Dodano go dla Androida po sprawdzeniu konfiguracji w kopii. Odwrócenie tej jednej dodanej linii daje dokładnie hash oryginalnego pliku sprzed audytu, więc inne ustawienia zachowano. Skrypty sieciowe i adaptery graczy nie będą teraz pomijane w Androidzie.
- W AGENTS.md zapisano bieżący cel mobilny Android, bez sugerowania gotowości iOS. Uzupełniono Documentation/UI/MainMenu.md i Documentation/Networking/README.md o granice gotowości telefonu.
- Lobby nie zawiera FloatingJoystick ani innego znalezionego ekranowego sterowania ruchem. Round/Kuba zawiera poprawnie przypisany FloatingJoystick, z kontrolą <Gamepad>/leftStick. W generowanych scenach nie znaleziono ekranowych przycisków Run/Dash. Telefon bez zewnętrznego kontrolera nie ma więc jeszcze całego sterowania potrzebnego do przejścia przez lobby i użycia wszystkich akcji. Nie dodawano nowych kontrolek ani nie zmieniano ruchu.
- Menu Music, Hover Clip i Click Clip w scenie są puste. Ustawienia głośności są zaimplementowane, ale menu nie ma docelowych nagrań.

### Wyniki wykonanych sprawdzeń

- Validate Main Menu: poprawne referencje i lista scen; Main Menu jest pierwszy, Lobby i Round/Kuba są włączone. Wspólna lista zawiera też niezależny SampleScene, którego nie usuwano. Testowe buildy zawierały trzy wymagane sceny, z Main Menu jako pierwszą.
- Play Mode PC i mobilny wariant: użyto rzeczywistego wewnętrznego ApplicationSimulation z modułu Device Simulator, z urządzeniem opisanym jako Android. Testował on rozpoznanie platformy, nie fizyczny ekran ani GPU telefonu. Resolution, ResolutionLabel, Fullscreen i VSync były ukryte w wariancie mobilnym, a limity zawierały 30/60 FPS. Desktop zachował listę do pięciu rozdzielczości i własny limit FPS po powrocie z wariantu Android. Potwierdzono stały komunikat przy VSync, zachowanie wartości audio i nawigację wszystkich ekranów.
- Wygenerowano osiem renderów GPU: Home/Play/Options/Credits w 1280×720 i 2400×1080. Oceniono Home w 2400×1080, Play w 1280×720, Options w 2400×1080 i Credits w 1280×720. Nie stwierdzono obcięcia paneli na tych podglądach. Dodatkowy końcowy test sprawdził raycasty w środki aktywnych kontrolek Selectable: przycisków, pola kodu, suwaków i listy FPS. Wynik: PLATFORM EDITOR AUDIT PASS. Nie był to fizyczny gest ani test systemowej klawiatury telefonu.
- Istniejące PlayerOnlineBoundaryTests uruchomiono przez Unity Test Runner w Edit Mode: 5/5 Passed, 0 Failed. Dotyczą niezależnych wejść/stanu graczy i granicy replik; nie zastępują sprzętowego testu chodzenia, Run, Dash i kolizji.
- Windows x64 Development: BuildResult.Succeeded, 0 błędów, 6 ostrzeżeń. W rzeczywiście uruchomionym programie sprawdzono start Main Menu, nawigację, rozmiar okna po SetResolution (porównanie Screen.width/height), przełączenie FullScreenWindow, ustawianie głośności i zakończenie procesu przez Quit. Wynik local.result: PASS. Nie wykonywano odsłuchu.
- Rzeczywisty Relay w dwóch procesach tego buildu Windows, z osobnymi testowymi profilami Authentication: Host wywołany przez istniejący przycisk, kod, Join przez istniejący przycisk, synchronizacja Lobby, co najmniej dwie PlayerNetworkInput po obu stronach, rozłączenie klienta i hosta oraz powrót obu do Main Menu. Wyniki host.result i client.result: PASS. Pierwszy klient trafił na wyścig odczytu pliku pomocniczego podczas jego zapisu; ponowiono klienta po zakończeniu zapisu. Nie była to poprawka ani awaria protokołu Relay. Nie testowano w tym scenariuszu ruchu sprzętowego ani wejścia obu graczy do strefy gotowości.
- Android ARM64 IL2CPP Development: zbudowano rzeczywiste APK, BuildResult.Succeeded, 0 błędów, 7 ostrzeżeń. Rozmiar APK 62 956 806 bajtów. Build zawierał włączony KUCHARZE_ONLINE. W wygenerowanym manifeście potwierdzono INTERNET i userLandscape; Player Settings zezwalają wyłącznie na obie orientacje poziome. Domyślny profil jakości Android to Mobile, Standalone to PC. Istniejący prefab NetworkManager ma RunInBackground = 1. Nie zmieniano XR ani profili jakości.
- Potwierdzono zgodność hashy wszystkich 111 produkcyjnych plików C# oraz sceny Main Menu z kopią używaną w audycie. Dwa tymczasowe skrypty kontrolne istniały wyłącznie w kopii; buildy były instrumentalnymi wersjami Development z osobną nazwą firmy/produktu i kontrolami uruchamianymi jawnym argumentem. Nie skopiowano do projektu tych skryptów ani ich danych testowych.

### Ograniczenia i logi

Nie instalowano APK na telefonie ani nie testowano połączenia PC–fizyczny Android. Nie sprawdzono systemowej klawiatury, sprzętowego dotyku/pada/myszy, odsłuchu, wycięć ekranu, utraty sieci, usypiania aplikacji, wydajności, temperatury i baterii urządzenia ani buildu Release. Dwa procesy Relay działały na jednym PC, przez rzeczywistą usługę, a nie na dwóch różnych łączach. Nie deklaruje się pełnej gotowości mobilnej gry ani iOS.

Logi kopii zawierały niezależny UnityEditor.Search.SearchDatabase podczas indeksowania. Build Windows zawierał ostrzeżenia CS0618 w tymczasowym kontrolerze o API FindObjectsByType z sortowaniem; dla testowej kompilacji Android użyto aktualnego przeciążenia. Pozostałe komunikaty obejmowały pragmę shadera TMP, dzielenie dużych metod TMP do osobnych plików C++ i brak konfiguracji XR Manager (XR nie był celem tego buildu). W uruchomionym PC URP ostrzegał o wyciętych/niedostępnych shaderach niektórych przejść postprocessingu. Testy menu i połączenia przeszły, ale nie zgłasza się pustej Console ani pełnego sprawdzenia wszystkich efektów graficznych.

Szczegóły tych wyników zastępują tymczasowe logi. Nie zmieniano PlayerMotor ani pojedynczego CharacterController.Move; test audytu nie dodaje nowych zdolności ani automatycznego końca rundy.

Po zakończeniu usunięto całą własną kopię projektu, testowe EXE i APK, dwa kontrolery audytu, rendery, XML/logi/wyniki, pliki pomocnicze wraz z metadanymi oraz utworzony podczas audytu katalog danych izolowanego produktu i jego klucze PlayerPrefs. Nie usuwano współdzielonych cache SDK ani pakietów użytkownika. Docelowo pozostała wyłącznie poprawka symbolu Androida, reguła celu mobilnego i dokumentacja wyników.

## 2026-11-25 — uporządkowanie folderów i scen

- Przeniesiono 113 jawnie wskazanych assetów przez Unity AssetDatabase. Kontrola przed migracją potwierdziła zapisane sceny i zgodne hashe źródeł; po migracji potwierdzono 113/113 zachowanych GUID-ów. Pliki graficzne, materiały, siatki i prefaby nie były regenerowane w oryginalnym projekcie. Zmiana zawartości LobbySession.prefab dotyczy ścieżek scen.
- Wspólna lista buildu zawiera Main Menu jako pierwszą włączoną scenę, następnie Lobby i Kuba, wszystkie bezpośrednio w Scenes/Debug/Kuba. Aktywna Kuba zachowała GUID be84c6ad8c0baf54eb4c7ebc0891e515; wcześniejsza lokalna Kuba ma własny GUID i trafiła do Prototypes razem z OnlineTest. Zachowano wyłączony wpis SampleScene. Sprawdzono zarówno API Unity, jak i zapis EditorBuildSettings.asset.
- W otwartym edytorze oryginału wykonano końcową kontrolę: pięć scen bez brakujących skryptów, 113 referencji GUID, podstawowe walidatory menu/lobby, ścieżki MainMenuOnlinePanel i LobbySession. Wynik: VALIDATED.
- Na osobnej kopii wykonano całą migrację, kompilację i kontrolę generatorów oprawy menu, postaci i lobby. Generatory użyły nowych folderów i nie odtworzyły starego Networking/Lobby/Generated. W Play Mode sprawdzono nawigację Credits/Options/Play, lokalny StartHost po załadowaniu lobby, utworzenie gracza z Input Action Asset, techniczne ładowanie sceny rozgrywki i powrót do menu po rozłączeniu. Wynik: ORGANIZATION CHECKS PASS. Techniczne ładowanie sceny nie sprawdzało gotowości 2–4 graczy ani nie dodaje trybu solo.
- Po wykryciu ścieżki kontrolera składanej z fragmentów poprawiono PlayerAnimation.FindDefaultController na Animations/Controllers. Osobny test w skompilowanej kopii potwierdził automatyczne znalezienie właściwego assetu: CONTROLLER LOOKUP AND FINAL LAYOUT PASS. Jawne przypisania i logika ruchu pozostały bez zmian.
- Próby na kopii ujawniły problemy kolejności rejestracji folderów i odczytu GUID-ów w trakcie batchowania oraz zbyt wczesne uruchamianie hosta w pomocniczym teście. Poprawiono migrator i test przed wykonaniem migracji oryginału. Kopia zgłosiła także wyjątek indeksowania Unity Search przy starcie; końcowe kontrole układu i Play Mode przeszły mimo tego komunikatu edytora.
- Ten zakres nie obejmował ponownych buildów Windows/Android, fizycznego telefonu, połączenia przez Relay ani testów chodzenia, pada, Run/Dash i kolizji. Nie zmieniano symulacji, autorytetu sieciowego, pakietów ani ustawień mobilnych. Wyniki wcześniejszych audytów pozostają osobnymi sprawdzeniami powyżej.
- Dodano Documentation/ProjectStructure.md, zaktualizowano instrukcje i generatory. AGENTS.md opisuje trwały podział folderów. Tymczasowy migrator, plan, żądania, raporty, logi i kopia testowa zostały usunięte po sprawdzeniach.