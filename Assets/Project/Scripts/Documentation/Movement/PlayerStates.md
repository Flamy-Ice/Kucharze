# PlayerStates — stan prezentacji online

Stan autorytatywny nadal zapisują lokalny resolver i motor. `ApplyReplica` przyjmuje od adaptera sieciowego rzeczywistą prędkość, ruch, kontakt z podłożem, źródło ruchu i postęp dasha. Metoda odrzuca zapis, jeśli lokalna symulacja jest dozwolona lub brakuje resolvera.
`IsReplica` i `ReplicaDashProgress` pozwalają prezentacji czytać serwerowy postęp bez wykonywania umiejętności na kliencie. Zwykły wynik motoru usuwa flagę repliki.

PlayerBus.ReceiveReplicaDash przyjmuje wyłącznie sygnał dla repliki. Powiadamia Audio/VFX o zaakceptowanym dashu i nie uruchamia ruchu. Pozostałe zdarzenia przejść ruchu nie są obecnie odtwarzane na klientach; bieżące widoki czytają stan co klatkę.
