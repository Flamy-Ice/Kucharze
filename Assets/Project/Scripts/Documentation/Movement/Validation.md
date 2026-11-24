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
