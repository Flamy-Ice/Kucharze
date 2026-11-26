# PlayerAnimation — prezentacja online

Dotychczasowy adapter animacji pozostaje lokalnym widokiem i nie używa Netcode.
Replika wybiera animację z PlayerStates i odczytuje ReplicaDashProgress zamiast nieaktywnej symulacji PlayerDash. Serwer i standalone nadal używają lokalnego PlayerDash.DashProgress.
Prędkość odtwarzania Run wynika z rzeczywistej prędkości. Wszyscy uczestnicy muszą używać tej samej konfiguracji prefabu i kontrolera animacji.
Nie należy dodawać drugiego NetworkAnimator sterującego tymi samymi parametrami.

Domyślny kontroler znajduje się w `Assets/Project/Animations/Controllers/Player.controller`. Automatyczne przypisanie w edytorze korzysta z tej lokalizacji względem folderu projektu; jawnie przypisane kontrolery pozostają zachowane.

## Wizualny wariant gracza lobby

LobbyPlayer ma rodzica PlayerVisualScale nad PlayerModel. Zmiana proporcji odbywa się poza Animatorem, więc klipy nie nadpisują tej skali. Przy rozdzieleniu materiałów zachowano wierzchołki, wagi i bind poses oryginalnego mesha; zmieniają się wyłącznie grupy trójkątów. Kontroler, root motion i ścieżki do kości pozostają zgodne z oryginałem. Kolor stroju jest prezentacją sieciową w PlayerNetworkAppearance; PlayerAnimation nadal odpowiada tylko za wybór animacji.
