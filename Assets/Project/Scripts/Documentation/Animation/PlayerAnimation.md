# PlayerAnimation — prezentacja online

Dotychczasowy adapter animacji pozostaje lokalnym widokiem i nie używa Netcode.
Replika wybiera animację z PlayerStates i odczytuje ReplicaDashProgress zamiast nieaktywnej symulacji PlayerDash. Serwer i standalone nadal używają lokalnego PlayerDash.DashProgress.
Prędkość odtwarzania Run wynika z rzeczywistej prędkości. Wszyscy uczestnicy muszą używać tej samej konfiguracji prefabu i kontrolera animacji.
Nie należy dodawać drugiego NetworkAnimator sterującego tymi samymi parametrami.
