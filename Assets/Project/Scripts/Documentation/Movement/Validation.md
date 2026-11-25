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
