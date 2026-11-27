# PlayerStates — stan prezentacji online

Stan autorytatywny nadal zapisują lokalny resolver i motor. `ApplyReplica` przyjmuje od adaptera sieciowego rzeczywistą prędkość, ruch, kontakt z podłożem, źródło ruchu i postęp dasha. Metoda odrzuca zapis, jeśli lokalna symulacja jest dozwolona lub brakuje resolvera.
`IsReplica` i `ReplicaDashProgress` pozwalają prezentacji czytać serwerowy postęp bez wykonywania umiejętności na kliencie. Zwykły wynik motoru usuwa flagę repliki.

PlayerBus.ReceiveReplicaDash przyjmuje wyłącznie sygnał dla repliki. Powiadamia Audio/VFX o zaakceptowanym dashu i nie uruchamia ruchu. Pozostałe zdarzenia przejść ruchu nie są obecnie odtwarzane na klientach; bieżące widoki czytają stan co klatkę.


### Wsparcie predykcji sieciowej — 2026-11-27

Resolver udostępnia ManualSimulation oraz Simulate(dt, snapshot) dla skoordynowanych kroków adaptera sieciowego. Domyślny tryb standalone pozostaje klatkowy. Jednorazowy SimulationSnapshot nadpisuje wyłącznie odczyt danego kroku i nie zastępuje ani nie wyłącza lokalnych akcji urządzenia. Właściciel może symulować przewidywany ruch, zatem jego PlayerStates.IsReplica jest false. Inne postacie nadal korzystają z ApplyReplica przy zablokowanej symulacji. PlayerMotor udostępnia CaptureSimulation/RestoreSimulation do punktów kontrolnych; korekta nie jest dodatkowym wykonawcą lokomocji. Run i Dash zapisują i odtwarzają własny stan. PlayerBus.SuppressEvents zapobiega powtórzeniu efektów podczas odtwarzania. Stan hosta jest ostateczny; rdzeń nie ma zależności od Netcode.
