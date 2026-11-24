# PlayerMovementResolver — granica integracji online

Resolver pozostaje ogólnym selektorem providerów. Sam nie zawiera Netcode ani nazw konkretnych umiejętności.
`SimulationEnabled` domyślnie wynosi `true`, zachowując zachowanie standalone. Wartość `false` zatrzymuje całą skoordynowaną symulację przed odczytem wejścia i wywołaniem motoru.
Adapter sieciowy ustawia ją przed pierwszym krokiem, a po spawnie dopuszcza symulację tylko na serwerze. Moduły Walk/Run/Dash pozostają zarejestrowane, ale repliki ich nie wykonują.

Tylko PlayerMotor wywołuje CharacterController.Move. NetworkTransform przesyła wynik pozycji, a osobny adapter może raz ustawić pozycję nowo utworzonej postaci.
