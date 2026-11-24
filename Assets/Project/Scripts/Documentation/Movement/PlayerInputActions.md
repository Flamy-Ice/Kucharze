# PlayerInputActions — granica integracji online

Komponent zachowuje lokalny odczyt Input System, prywatną kopię assetu i wspólny Input Deadzone.
`CaptureSnapshot()` udostępnia kierunek, stan Run, liczniki naciśnięć i wersje resetów bez zależności od Netcode.
`SetExternalSource(IPlayerInputSource)` zastępuje urządzenia zewnętrznym źródłem tylko dla tego Playera. Zewnętrzny odczyt przechodzi walidację liczb, ograniczenie długości do 1 i wspólny deadzone. Mapa urządzeń pozostaje wtedy wyłączona. Fokus komputera hosta nie blokuje sterowania zdalnego gracza.
`SetExternalSource(null)` przywraca dotychczasową lokalną ścieżkę. `SetInputEnabled` steruje urządzeniami lokalnymi; adapter zarządza życiem zewnętrznego źródła osobno.

Snapshot nie zawiera prędkości, pozycji, czasu kroku ani cooldownu. Te wartości ustala serwer.
