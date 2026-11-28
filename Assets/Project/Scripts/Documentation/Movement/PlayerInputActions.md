# PlayerInputActions — granica integracji online

Komponent zachowuje lokalny odczyt Input System, prywatną kopię assetu i wspólny Input Deadzone.
`CaptureSnapshot()` udostępnia kierunek, stan Run, liczniki naciśnięć i wersje resetów bez zależności od Netcode.
`SetExternalSource(IPlayerInputSource)` zastępuje urządzenia zewnętrznym źródłem tylko dla tego Playera. Zewnętrzny odczyt przechodzi walidację liczb, ograniczenie długości do 1 i wspólny deadzone. Mapa urządzeń pozostaje wtedy wyłączona. Fokus komputera hosta nie blokuje sterowania zdalnego gracza.
`SetExternalSource(null)` przywraca dotychczasową lokalną ścieżkę. `SetInputEnabled` steruje urządzeniami lokalnymi; adapter zarządza życiem zewnętrznego źródła osobno.

Snapshot nie zawiera prędkości, pozycji, czasu kroku ani cooldownu. Te wartości ustala serwer.


### Wsparcie predykcji sieciowej — 2026-11-27

Resolver udostępnia ManualSimulation oraz Simulate(dt, snapshot) dla skoordynowanych kroków adaptera sieciowego. Domyślny tryb standalone pozostaje klatkowy. Jednorazowy SimulationSnapshot nadpisuje wyłącznie odczyt danego kroku i nie zastępuje ani nie wyłącza lokalnych akcji urządzenia. Właściciel może symulować przewidywany ruch, zatem jego PlayerStates.IsReplica jest false. Inne postacie nadal korzystają z ApplyReplica przy zablokowanej symulacji. PlayerMotor udostępnia CaptureSimulation/RestoreSimulation do punktów kontrolnych; korekta nie jest dodatkowym wykonawcą lokomocji. Run i Dash zapisują i odtwarzają własny stan. PlayerBus.SuppressEvents zapobiega powtórzeniu efektów podczas odtwarzania. Stan hosta jest ostateczny; rdzeń nie ma zależności od Netcode.


### Local menu input blocking

`SetControlsBlocked` discards new local movement, Run and Dash controls while preserving the Dash press baseline and reset version. A burst already accepted by the resolver therefore finishes normally through PlayerMotor, on both the owner and server. Hiding touch controls may remove the virtual gamepad; this device removal does not reset input while menu blocking is active. Run toggle state is cleared. Full `SetInputEnabled(false)`, focus loss and application suspension keep the existing hard-reset behavior. External and simulation snapshots remain independent of this local UI gate.
