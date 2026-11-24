# Pierwsza gra online — host i kod Relay

## Konfiguracja przygotowana automatycznie w tym projekcie

Scena testowa: `Assets/Project/Scripts/Networking/Setup/OnlineTest.unity`.
Prefab online: `Assets/Project/Scripts/Networking/Setup/OnlinePlayer.prefab`.
Lista prefabów sieciowych: `Assets/Project/Scripts/Networking/Setup/OnlineNetworkPrefabs.asset`.
Scena jest kopią sceny Kuba; oryginalna scena i prefab Player pozostają osobnymi assetami. Jeśli oryginalna scena ma niezapisane zmiany, automat pozostawia ją otwartą. Zapisz swoją pracę przed otwarciem sceny OnlineTest dwuklikiem w Project.

Pakiety NGO 2.14.0 i Multiplayer Services 2.3.3 są już w manifest.json. KUCHARZE_ONLINE jest włączone dla Standalone. Project ID w Player Settings wskazuje projekt używany przez grę. Osobno edytor musi być zalogowany i powiązany z projektem oraz organizacją przez Unity Connect. Sam zapis Project ID nie tworzy tego pełnego powiązania.

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

Pierwszy test wykonujemy w jednej wspólnej scenie rozgrywki. Panel połączenia jest prostym panelem testowym, niezależnym od docelowego menu głównego. Stary skrypt UI/MainMenu.cs usunięto na prośbę użytkownika; menu będzie tworzone od nowa. Przyciski nowego menu można później podłączyć do RelayConnection.Host i RelayConnection.Join.

Poniższe kroki pozostają instrukcją konfiguracji od początku. W tym projekcie wykonano już lokalną konfigurację opisaną powyżej i sprawdzono Relay. Bez symbolu KUCHARZE_ONLINE pliki sieciowe są pomijane, a dotychczasowa gra bez adapterów sieciowych zachowuje tryb lokalny.

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
