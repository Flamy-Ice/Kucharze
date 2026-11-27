# Main Menu

Scena: `Assets/Project/Scenes/Debug/Kuba/Main Menu.unity`.

Menu korzysta z drewnianego tła, logo i przycisków z `Assets/Project/MainMenu/UI/`, a na wszystkich czterech ekranach z jasnej grafiki `MainMenu_Group.png`, zgodnie z dodatkową referencją użytkownika. Home ma panel 760 × 758, a Play, Options i Credits szerszy panel 1000 × 758. Teksty pozostają po angielsku. Grafiki są osobnymi elementami UI, nie jednym nieaktywnym obrazem. Jua służy do tytułów i nazwisk, a Madimi One do opisów, ról i komunikatów. Ciemne teksty zachowują kontrast z jasnymi panelami. Canvas skaluje się względem 1920 × 1085; EventSystem korzysta z Input System. Drewniane tło wypełnia całe okno, także przy szerszych proporcjach ekranów telefonów.

## Ekrany i odpowiedzialności

- `MainMenuNavigation` przełącza HomeCanvas, PlayCanvas, OptionsCanvas i CreditsCanvas. Back, Escape oraz przycisk B / kółko pada wracają do ekranu głównego. Quit zamyka aplikację; w edytorze kończy Play Mode.
- `MenuCredits` zawiera cztery wpisy Team, każdy z Name i Role. Wpisy są danymi sceny, nie polami do wpisywania przez gracza.
- Credits ma układ dwóch kolumn: Jakub Lewandowski — 2D Graphics / 3D Graphics / Programmer / UI/UX; Mateusz Filipczak — Level Design / Sounds; Adriana Frankowska — 2D Graphics / 3D Graphics / Animations / UI/UX; Szymon Wosiek — 3D Graphics / VFX. Każda rola zajmuje osobną linię; pole Role w Inspectorze jest wielowierszowe.
- `MenuAudioSettings` przechowuje ustawienia lokalnego komputera w PlayerPrefs i utrzymuje je po zmianie sceny. Nie zmienia głośności pozostałym graczom. Oddzielne parametry MusicVolume i SoundsVolume w GameAudio sterują grupami Music oraz istniejącą SFX. Kroki i dash w obecnych prefabach już korzystają z SFX. Przyszłe efekty wymagają skierowania do tej samej grupy. Suwak 0 oznacza -80 dB, czyli praktyczne wyciszenie.
- `MenuAudioOptions` łączy dwa suwaki z ustawieniami. Zapis następuje przy opuszczeniu Options, utracie aktywności aplikacji i zamykaniu.
- `MenuVideoSettings` przechowuje lokalne ustawienia limitu FPS, VSync, pełnego ekranu i rozdzielczości PC w PlayerPrefs. Na PC dostępne są 30/60/120/144 FPS i Unlimited; na telefonie 30/60 FPS, bez kontrolek rozdzielczości, VSync i okna. Pełny ekran oznacza borderless fullscreen, wyłączenie — zwykłe okno. Zmiana trybu okna działa w buildzie, bez przełączania okien edytora. Te ustawienia nie są synchronizowane z innymi graczami i przeżywają zmianę sceny.
- `MenuVideoOptions` łączy listy rozdzielczości i FPS oraz dwa checkboxy z ustawieniami. Przy włączonym VSync blokuje listę FPS; zachowuje wybrany wcześniej limit na moment wyłączenia VSync. Zapis jest automatyczny przy opuszczeniu Options, zatrzymaniu aplikacji i zamknięciu. Nie zmienia assetów profili Quality ani URP. Nie dodaje wyboru profilu Mobile na PC ani profilu PC na telefonie.
- `MenuPlatform` rozpoznaje telefon przez `UnityEngine.Device.Application.isMobilePlatform`, dzięki czemu Device Simulator pokazuje wariant mobilny także wtedy, gdy sam edytor działa na Windows. Resolution, Fullscreen i VSync są wtedy ukryte, a lista FPS zawiera tylko 30/60. Zmiana aktywnego podglądu Game ↔ Simulator w Play Mode odświeża otwarte Options automatycznie. Limit FPS telefonu ma osobny klucz PlayerPrefs, aby test na telefonie nie nadpisywał limitu PC. Dotychczasowy wspólny klucz jest zachowany jako fallback dla wcześniejszych zapisów mobilnych.
- `MenuSelectableScale` animuje przyciski wszystkich ekranów: powiększenie do 104.5% po najechaniu lub celowym zaznaczeniu klawiaturą/padem i zmniejszenie do 97.5% podczas naciśnięcia wskaźnikiem. Czas reakcji skrócono z 0.09 do 0.035 s. Animacja używa czasu niezależnego od pauzy. Wyłączenie lub zablokowanie kontrolki przywraca jej skalę; na telefonie jest tylko reakcja na dotknięcie, bez trwałego powiększenia po tapnięciu. Checkboxy Fullscreen/VSync, suwaki i lista Frame limit nie są skalowane. Checkboxy nie zmieniają koloru po najechaniu/zaznaczeniu i nie odtwarzają hoveru; zmiana stanu nadal odtwarza klik. Frame limit nie zmienia również koloru przy najechaniu/zaznaczeniu i nie odtwarza dźwięku hover. Parametry animacji są na komponencie w Inspectorze.
- `MenuInputMode` odróżnia ruch lub kliknięcie myszy od użycia nawigacji UI. Po uruchomieniu gry nie ma automatycznego zaznaczenia Options. Pierwszy przycisk otrzymuje fokus dopiero po użyciu strzałek/WASD lub pada. Mysz nie zostawia efektu powiększenia na klikniętym przycisku po jej odsunięciu.
- `MenuUISounds` jest opcjonalnym wspólnym odtwarzaczem Hover Clip i Click Clip, utrzymywanym po przejściu do lobby. `MenuSoundFeedback` reaguje na rzeczywiste interakcje kontrolek: najechanie i fokus nawigacji odtwarzają hover, a aktywacja przycisku, zmiana checkboxa oraz otwarcie/wybór listy odtwarzają klik. Nie ma dźwięku hover po samym uruchomieniu ani przy dotknięciu ekranu telefonu. Puste pola nie generują błędów. Źródło jest 2D, kierowane do istniejącej grupy SFX, więc suwak Sounds obejmuje też dźwięki menu. Dźwięki nie należą do komponentu skalującego.
- Suwaki mają jasne tło paska, turkusowe wypełnienie i fioletowy uchwyt. FillArea obejmuje pełne 700 jednostek szerokości Track; wcześniej 672 jednostki pozostawiały odsłonięty ciemny fragment przy początku nawet przy 100%. Uchwyt porusza się po krótszym obszarze, aby pozostał wewnątrz paska.

W Credits każda osoba ma mniejszą kartę z MainMenu_Group: miętową (#C8EFE0), brzoskwiniową (#F7D4BC), lawendową (#E3D9FF) lub błękitną (#D2E9FF). Nazwiska są ciemne; role mają dodatkowe jasne tło RoleSurface, aby wzór nie utrudniał czytania. To edytowalne placeholdery Image: **UI > MainMenuCanvas > Screens > CreditsCanvas > CreditsCard > TeamMembers > CreditEntry1–4**. Aby później użyć własnej grafiki, przypisz ją do **Inspector > Image > Source Image** i ustaw **Color** na biały, jeśli chcesz zachować kolory pliku. Referencje do nazwisk i ról pozostają w MenuCredits.

Checkboxy Fullscreen i VSync mają turkusowe wypełnienie #059E94, ciemny obrys #064F4A i powiększony ciemny znacznik #252138, aby odróżniały się od jasnego panelu i miały czytelny stan zaznaczenia.

Unity stosuje limit FPS na PC przy wyłączonym VSync; na Androidzie/iOS limit może zostać dopasowany do odświeżania wyświetlacza. Wybrana wartość jest celem, nie obietnicą osiągnięcia tylu klatek na każdym urządzeniu. Szczegóły: [Application.targetFrameRate](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/application/targetframerate), [FullScreenMode](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/fullscreenmode).
- `MainMenuOnlinePanel`, pod `KUCHARZE_ONLINE`, korzysta z istniejącego LobbyConnection.prefab i RelayConnection. W czasie próby blokuje ponowne żądania i pozwala anulować połączenie. Błędy i timeout są widoczne na ekranie.

Host najpierw uzyskuje przydział i kod Relay, następnie ładuje istniejące Lobby i uruchamia serwer. Dzięki temu postać powstaje dopiero przy gotowych znacznikach spawnów. Klient wpisuje kod; po połączeniu Netcode synchronizuje scenę hosta. Kod jest prezentowany i kopiowany przez istniejący widok lobby. Nadal obowiązują 2–4 osoby i wspólne pięciosekundowe oczekiwanie w pokoju startowym. Menu nie wprowadza singleplayer ani automatycznego końca rundy.

Wejście przez menu ustawia powrót po opuszczeniu sesji lub utracie hosta na Main Menu. Bezpośrednio otwarta scena Lobby zachowuje dotychczasowy powrót do lobby. Prototypowy panel lobby jest ukryty w Main Menu. Nie przebudowuje się geometrii ani zapisanej sceny lobby.

## Uzupełnij muzykę i autorów

1. W oknie **Project** otwórz **Assets > Project > Scenes > Debug > Kuba**, a następnie dwukrotnie **Main Menu**.
2. W **Hierarchy** zaznacz **MenuAudio - assign your music here**. W **Inspector > Menu Audio Settings > Menu Music** przeciągnij plik muzyczny z Project. Jeśli plik nie jest jeszcze w projekcie, przeciągnij go najpierw z Eksploratora do wybranego folderu Assets. Puste Menu Music oznacza ciszę. Utwór zapętla się tylko w Main Menu i zatrzymuje po wejściu do lobby.
3. W **Hierarchy > MainMenuCanvas > CreditsCanvas** zaznacz CreditsCanvas. W **Inspector > Menu Credits > Team** rozwiń cztery elementy i wpisz **Name** oraz **Role**. Pozostaw cztery wpisy. Nie trzeba edytować referencji Name Labels i Role Labels.
4. Zapisz przez **File > Save** lub **Ctrl+S**. Uruchom **Play**, sprawdź Options i Credits. **Play > Host a game** uruchamia lobby; w drugim egzemplarzu gry wybierz **Play**, wpisz kod z lobby i kliknij **Join**.

### Dodaj dźwięki przycisków

1. W Main Menu zaznacz w **Hierarchy** obiekt **Menu UI Sounds - assign clips here**.
2. W **Inspector > Menu UI Sounds > Hover Clip** przeciągnij plik dźwięku najechania, a w **Click Clip** plik dźwięku kliknięcia. Jeśli plik jest poza projektem, przeciągnij go najpierw do wybranego folderu Assets w oknie Project.
3. **Volume** ustawia głośność tych nagrań w zakresie 0–1. **Minimum Hover Interval** ogranicza bardzo szybkie powtarzanie dźwięku podczas przesuwania kursora; domyślnie wynosi 0.04 s. **Output Mixer Group** jest już ustawione na SFX.
4. Zapisz scenę i uruchom Play. Najedź na przycisk, a potem kliknij. Suwak **Options > Sounds** reguluje także te dźwięki. Przypisuj pliki poza Play Mode, aby zachować je po zatrzymaniu testu.

## Sceny w buildzie i narzędzia

Generator **Tools > Kucharze > Main Menu > Create Main Menu** tworzy scenę tylko wtedy, gdy jeszcze nie istnieje. Nie nadpisuje ręcznych zmian. **Validate Main Menu** sprawdza brakujące skrypty, połączenia przycisków i wpisy scen. Nie jest testem połączenia internetowego.

**Tools > Kucharze > Main Menu > Refresh Main Menu Appearance** aktualizuje istniejące elementy: dodaje panel Home, zmienia tła pozostałych ekranów na MainMenu_Group, ustawia dwie czcionki, uzgodnioną listę i układ Credits, kolory i geometrię suwaków. Zachowuje tożsamość przycisków, ich zdarzenia i przypisania audio/sieci. Przed użyciem zapisz scenę i wyjdź z Play Mode. Nie jest narzędziem do zapisywania niezapisanej pracy użytkownika.

**Tools > Kucharze > Main Menu > Refresh Menu Interaction** aktualizuje wyłącznie szybkość animacji, zachowanie Frame limit, kolor checkboxów i konfigurację dźwięków, zachowując układ i przypisane klipy oraz grafiki. Użyj poza Play Mode, po zapisaniu sceny.

## Grafiki nowych przycisków — propozycja dla autora

To rozmiary plików PNG, a nie ustawienia rozdzielczości gry. Użyj przezroczystego tła i tego samego obrysu oraz zaokrągleń jak w istniejących przyciskach.

| Napis | Kolor wypełnienia | Rozmiar PNG |
| --- | --- | --- |
| Back | #4B4756, jak Options/Credits | 414 × 151 px |
| Host Game | #4B4756, zgodnie z dostarczoną grafiką | 414 × 151 px |
| Join Game | #4B4756, zgodnie z dostarczoną grafiką | 414 × 151 px |
| Cancel (grafika) | #4B4756, zgodnie z dostarczoną grafiką | 414 × 108 px |

Aktualnie używane są nowe ilustracje użytkownika: wszystkie cztery mają własny fioletowy kolor, kremowy napis i ciemny obrys. Image jest biały, a dodatkowy Text wyłączony. Back jest wewnątrz każdego podmenu. Pole kodu i procenty głośności pozostają edytowalnym tekstem Unity.

W **File > Build Profiles > Open Scene List** pierwszą włączoną sceną ma być **Main Menu**, dalej muszą być włączone **Scenes/Debug/Kuba/Lobby** oraz **Scenes/Debug/Kuba/Kuba**. Generator zachowuje inne wpisy. Jeżeli profil używa własnej listy zamiast wspólnej, dodaj te trzy sceny również do jego listy, z Main Menu na początku. Obaj gracze muszą korzystać z aktualnego buildu. Poprzednie **Build Lobby Test** nadal buduje test zaczynający się bezpośrednio w lobby.

Do działania online wymagane są istniejące pakiety, symbol KUCHARZE_ONLINE, powiązanie Unity Services i dostęp do internetu opisane w [instrukcji sieciowej](../Networking/README.md). Kod menu i audio nie zmienia konfiguracji usług ani kont.

## Telefon — wyłącznie poziomo

Cała gra na Androidzie i iOS ma działać poziomo. Wspólne Player Settings mają **Default Orientation: Auto Rotation**, z włączonymi wyłącznie **Landscape Left** i **Landscape Right**. **Portrait** oraz **Portrait Upside Down** są wyłączone. Nie wymaga to komponentu dodawanego do gracza ani osobnej logiki w ruchu.

1. Otwórz **Edit > Project Settings > Player**, wybierz zakładkę **Android** lub **iOS**, rozwiń **Resolution and Presentation > Orientation**.
2. **Default Orientation** ma wartość **Auto Rotation**. W sekcji **Allowed Orientations for Auto Rotation** zaznaczone są tylko **Landscape Left** i **Landscape Right**. Ustawienie jest wspólne dla Androida/iOS.
3. Po aktualizacji skryptów uruchom ponownie Play Mode. Otwórz **Window > General > Device Simulator**, wybierz telefon i wejdź w Options. Powinieneś zobaczyć tylko Music, Sounds i limit 30/60 FPS; bez Fullscreen i VSync. Po przełączeniu na zwykły Game view wrócą opcje PC.
4. Przycisk **Rotate** obraca fizyczną ramkę symulowanego telefonu. Gra nadal ma używać jednej z dwóch orientacji poziomych; pionowe trzymanie ramki nie oznacza zezwolenia na pionowy układ gry.

API symulacji: [UnityEngine.Device](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/cross-platform-features/device-simulator/simulated-classes). Podgląd nie zastępuje sprawdzenia końcowego buildu na urządzeniu.


Play ma dwie pastelowe sekcje: CREATE A LOBBY i JOIN YOUR CREW. Jasne pole Lobby code ma ciemny obrys i narożniki rysowane przez MenuRoundedPanel, bez skalowania małej tekstury. Tekst pozostaje edytowalny. Osobny pasek CONNECTION STATUS pokazuje komunikaty i przycisk Cancel Connection podczas łączenia. Istniejące przyciski i referencje sieciowe zachowano. Refresh Main Menu Appearance aktualizuje również ten układ i karty Credits; ponowne uruchomienie nie powiela dekoracji.

## Nowe grafiki i rozdzielczość PC

Przyciski Back (na trzech ekranach), Host Game, Join Game i Cancel korzystają z dostarczonych MainMenu_Button_Back/HostGame/JoinGame/Cancel.png. Grafiki mają już napisy, więc dodatkowe etykiety Text są wyłączone. Image pozostaje biały, aby nie przemnażać kolorów ilustracji; disabled przyciemnia grafikę. MainMenuArtworkSetup przygotowuje import Sprite bez mipmap i kompresji oraz zachowuje GUID-y i zdarzenia przycisków. Refresh Main Menu Appearance wywołuje ten krok również przy kolejnych aktualizacjach wyglądu.

Lobby code ma osobną etykietę, kremowe pole, ciemny tekst i placeholder „e.g. ABC123”. MenuRoundedPanel rysuje narożniki i obrys bez assetu rastrowego, z przezroczystym paskiem krawędzi. Connection status ma ciemne tło, miętowy nagłówek, jasny komunikat i miejsce na Cancel podczas łączenia. Komunikaty i logika Relay nie ulegają zmianie.

Options > Display > Resolution jest dostępne tylko na PC. Lista ma maksymalnie pięć pozycji: do czterech typowych, obsługiwanych rozdzielczości pasujących do proporcji monitora i nieprzekraczających jego wymiarów oraz bieżącą rozdzielczość monitora. Powtórzenia są usuwane. Dla 16:9 bazowe niższe rozmiary to 1280×720, 1600×900, 1920×1080 i 2560×1440; 4K pojawia się jako bieżący tryb monitora 4K. Inne proporcje mają osobne typowe rozmiary. Nie dopisuje się dowolnego rozmiaru Game View ani okna gry. Wybór zapisuje Kucharze.Video.Width/Height. Nieobsługiwany zapis, również wcześniejszy rozmiar Game View, wraca do bieżącej rozdzielczości monitora, jeżeli jest na liście, albo do poprawnej wartości zastępczej. Jeśli system nie udostępnia listy trybów, używany jest bieżący tryb monitora; awaryjnie 1280 × 720. Fullscreen zachowuje borderless; rozdzielczość określa rozmiar renderowania, a w trybie Windowed rozmiar okna. Zmiany są stosowane przez Screen.SetResolution w buildzie PC; edytor zapisuje wybór bez zmieniania własnych okien. Telefon i Device Simulator ukrywają Resolution oraz jej etykietę, Fullscreen i VSync, zachowując limit 30/60 FPS. Przy nieprzypisanym nowym polu Resolution starszy komponent MenuVideoOptions nadal obsługuje dotychczasowe ustawienia.

Aby sprawdzić rzeczywistą zmianę rozmiaru: zbuduj wersję PC przez File > Build Profiles, uruchom grę, otwórz Options, wyłącz Fullscreen i wybierz Resolution. Okno powinno zmienić rozmiar; po ponownym uruchomieniu wybór ma pozostać zapisany. W pełnym ekranie borderless monitor zachowuje fizyczną rozdzielczość pulpitu. API: [Screen.SetResolution](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/screen/setresolution).

Komunikat Your settings are saved automatically. pozostaje stały również po włączeniu VSync. Lista FPS nadal jest wtedy nieaktywna; wyłączenie VSync przywraca możliwość wyboru FPS.

MenuDropdownSelectionScroll pokazuje wybrany wiersz Resolution po otwarciu listy, w miarę możliwości na środku widoku. Potem pozostawia przewijanie graczowi, bez ciągłego przyciągania do zaznaczenia. Komponent jest zapewniany przez MenuVideoOptions w runtime także w istniejącej scenie oraz przez MainMenuOptionsSetup dla przyszłych aktualizacji sceny.

## Zakres gotowości Androida

Aktualnym celem mobilnym jest Android. Menu ma dotykowe bindingi Input System, wariant opcji mobilnych i układ poziomy. Sprawdzenie symulacji i kompilacji nie potwierdza zachowania systemowej klawiatury, wycięć ekranu, dźwięku, wydajności, utraty sieci ani przechodzenia aplikacji w tło na konkretnym urządzeniu.

Pełna gra wymaga dalszego przygotowania dotyku: obecna scena Lobby nie zawiera FloatingJoystick, podczas gdy Kuba ma skonfigurowany joystick wysyłający `<Gamepad>/leftStick`. Bez zewnętrznego kontrolera brakuje ruchu dotykowego w lobby, niezbędnego do wejścia do pokoju gotowości. W generowanych scenach nie znaleziono ekranowych przycisków Run/Dash. Ten audyt nie dodaje nowych kontrolek ani nie zmienia ruchu. Pola menuMusic, hoverClip i clickClip w scenie pozostają puste; suwaki działają, ale menu nie ma jeszcze docelowych nagrań.


## Hierarchy sceny — 2026-11-26

Scena jest uporządkowana za pomocą pustych GameObjectów. SceneSystems zawiera kamerę, EventSystem i MenuNavigation; UI zawiera MainMenuCanvas z BackgroundAndBranding oraz Screens. HomeCanvas ma grupę MenuButtons; PlayCard grupuje PanelBackgrounds, Header, HostControls, JoinControls oraz StatusAndNavigation; OptionsCard ma AudioSettings, Footer i DisplaySettings; CreditsCard grupuje wpisy w TeamMembers. PersistentServices zawiera MenuAudio, MenuVideoSettings i Menu UI Sounds. Każda usługa przed DontDestroyOnLoad odłącza własny Transform od folderu, zachowując dotychczasową pozycję i niezależny czas życia. Główne grupy sceny to SceneSystems, UI i PersistentServices. Grupy wewnątrz UI mają wyłącznie rozciągnięty RectTransform bez grafiki i komponentów layoutu. Zachowano kolejność rysowania, wszystkie dotychczasowe właściwości i referencje.


## Nick gracza przed Host/Join — 2026-11-26

Ekran Play zawiera grupę PlayerProfile z polem Nickname i podpisem NICKNAME nad panelami połączenia. Nick ma 2–16 znaków; dopuszczone są litery (w tym polskie), cyfry, pojedyncze spacje oraz _-. Zewnętrzne spacje, powtórzone spacje, znaczniki i znaki sterujące są usuwane. Puste lub jednoznakowe pole blokuje Host i Join; Join nadal wymaga kodu lobby. Nick zapisuje się lokalnie w PlayerPrefs pod Kucharze.Player.Nickname, jest odtwarzany przy kolejnym wejściu na ekran Play i można go tam edytować przed połączeniem. Zapis na dysk następuje po zakończeniu edycji, zatwierdzeniu połączenia lub opuszczeniu pola/ekranu. Nie dodano osobnego ekranu powitalnego ani konta gracza.

PlayerNicknameProfile odpowiada za zapis i wspólne reguły tekstu. MenuPlayerNickname obsługuje pole, a MainMenuOnlinePanel sprawdza nick przed Host/Join. LobbyPlayerNickname jest osobnym NetworkBehaviour na LobbyPlayer: właściciel wysyła swój nick, serwer sprawdza nadawcę i tekst, następnie replikuje go wszystkim uczestnikom. LobbyPlayerNameplate wyświetla podpis nad postacią tylko w scenie Lobby i zwraca samą etykietę w stronę kamery. Nick pozostaje na graczu podczas przejścia do rozgrywki i powrotu. Bezpośrednie testy lobby bez menu otrzymują zastępcze Player N. Nicki nie muszą być unikatowe.

Tools > Kucharze > Main Menu > Setup Player Nicknames aktualizuje tylko pole, jego podpięcia, font podpisu i prefab LobbyPlayer. Nie przebudowuje mapy ani innych ekranów. Podpis korzysta z osobnego fontu TMP NicknameFont utworzonego z Madimi One, ponieważ Jua nie zawiera polskich liter. Dodanie NetworkBehaviour zmienia układ prefabu sieciowego — wszyscy gracze muszą używać nowego, zgodnego buildu.
