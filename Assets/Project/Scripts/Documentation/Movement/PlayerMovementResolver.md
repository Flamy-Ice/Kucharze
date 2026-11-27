# PlayerMovementResolver — granica integracji online

Resolver pozostaje ogólnym selektorem providerów. Sam nie zawiera Netcode ani nazw konkretnych umiejętności.
`SimulationEnabled` domyślnie wynosi `true`, zachowując zachowanie standalone. Wartość `false` zatrzymuje całą skoordynowaną symulację przed odczytem wejścia i wywołaniem motoru.
Adapter sieciowy ustawia ją przed pierwszym krokiem, a po spawnie dopuszcza symulację tylko na serwerze. Moduły Walk/Run/Dash pozostają zarejestrowane, ale repliki ich nie wykonują.

Tylko PlayerMotor wywołuje CharacterController.Move. NetworkTransform przesyła wynik pozycji, a osobny adapter może raz ustawić pozycję nowo utworzonej postaci.


### Wsparcie predykcji sieciowej — 2026-11-27

Resolver udostępnia ManualSimulation oraz Simulate(dt, snapshot) dla skoordynowanych kroków adaptera sieciowego. Domyślny tryb standalone pozostaje klatkowy. Jednorazowy SimulationSnapshot nadpisuje wyłącznie odczyt danego kroku i nie zastępuje ani nie wyłącza lokalnych akcji urządzenia. Właściciel może symulować przewidywany ruch, zatem jego PlayerStates.IsReplica jest false. Inne postacie nadal korzystają z ApplyReplica przy zablokowanej symulacji. PlayerMotor udostępnia CaptureSimulation/RestoreSimulation do punktów kontrolnych; korekta nie jest dodatkowym wykonawcą lokomocji. Run i Dash zapisują i odtwarzają własny stan. PlayerBus.SuppressEvents zapobiega powtórzeniu efektów podczas odtwarzania. Stan hosta jest ostateczny; rdzeń nie ma zależności od Netcode.
