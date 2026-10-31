# Trabajo Tarjeta 2026

## Integrantes del grupo
 - Leandro Caraballo
 - Facundo Naveira

## Aclaraciones
El siguiente trabajo es un enunciado iterativo. Regularmente se ampliará y/o modificará el enunciado.

[ENTREGA](https://forms.gle/NeM1adptvvtioRJH9) (El form cierra el 21/9)

- El trabajo está implementado en **.NET 8.0 (`net8.0`)**, usando Git para el control de versiones y NUnit como framework de testing.
- Los tests unitarios no dependen de una base de datos real externa (se emplea SQLite en memoria para tests de mapeo EF Core).
- **Todos** los métodos públicos y reglas de negocio están testeados con pruebas unitarias e integrales (153 tests en verde sin fallos).
- Cada clase de la implementación y de testing se encuentra en un archivo separado.

---

## Cómo ejecutar los tests

Para compilar la solución y ejecutar la suite completa de pruebas unitarias e integrales:

```bash
dotnet test Transporte.Tests/Transporte.Tests.csproj
```

Para ejecutar los tests con recolección de cobertura de código:

```bash
dotnet test Transporte.Tests/Transporte.Tests.csproj --collect:"XPlat Code Coverage"
```

---

## Iteración 1

Escribir un programa con programación orientada a objetos que permita ilustrar el funcionamiento del transporte urbano de pasajeros de la ciudad de Rosario.
Las clases que interactúan en la simulación son: `Colectivo`, `Tarjeta` y `Boleto`.
Cuando un usuario viaja en colectivo con una tarjeta, se genera un boleto como resultado de la operación `colectivo.pagarCon(tarjeta)`.

Para esta iteración se consideraron los siguientes supuestos iniciales:
- No hay medio boleto de ningún tipo.
- No hay transbordos.
- No hay saldo negativo.
- Tarifa inicial histórica: $1580.
- Montos de carga aceptados: (2000, 3000, 4000, 5000, 8000, 10000, 15000, 20000, 25000, 30000).
- Límite de saldo inicial: $40000.

---

## Iteración 2

### Descuento de saldos (Completado)
Cada vez que una tarjeta paga un boleto, descuenta el valor del monto gastado.
Si la tarjeta se queda sin saldo dentro del límite permitido, `colectivo.PagarCon(tarjeta, out boleto)` devuelve `false`.

### Saldo negativo (Completado)
Se permite viajar hasta un descubierto de -$2000 (`Tarjeta.LimiteNegativo = -2000`).
Al realizar una nueva carga, se cancela automáticamente el saldo negativo consumido.

### Franquicias y Tipos de Tarjeta (Completado)
Se incorpora `TarjetaTipo` relacionada por clave foránea (`TarjetaTipoId`) con `Tarjeta` y seed en `TransporteDbContext`:
- `Normal`: `PorcentajeDescuento = 0m`.
- `Medio boleto estudiantil`: `PorcentajeDescuento = 0.5m`.
- `Boleto gratuito estudiantil`: `PorcentajeDescuento = 1.0m`.
- `Franquicia completa`: `PorcentajeDescuento = 1.0m`.

---

## Iteración 3

### Resumen de Tareas:
1. **Datos inmutables en Boleto** (`feature/boleto-datos`): Comprobante inmutable con fecha, tipo de tarjeta, línea, total abonado, saldo restante, ID de tarjeta y flag de viaje plus (`EsViajePlus`). Desacople de tiempo mediante `IProveedorDeFecha`.
2. **Motor de Reglas de Viaje** (`refactor/reglas-de-viaje`): Pipeline desacoplado mediante la interfaz `IReglaDeViaje` y `ContextoDeViaje`, permitiendo agregar o modificar reglas sin alterar `Colectivo` (OCP/LSP).
3. **Limitación de Medio Boleto** (`feature/medio-boleto-limites`): Intervalo mínimo de 5 minutos entre viajes consecutivos y tope de 2 viajes con descuento por día calendario.
4. **Limitación de Boleto Gratuito** (`feature/gratuito-limites`): Tope de 2 viajes gratuitos por día para Boleto Gratuito Estudiantil. La franquicia de jubilados se mantiene sin tope diario.
5. **Saldo Máximo y Acreditación Progresiva** (`feature/saldo-maximo`): Límite físico en tarjeta de $56000 (`SaldoMaximo`). Montos excedentes se retienen en `SaldoPendiente` y se acreditan tras cada uso (`Descontar`).
6. **Cierre de Iteración 3** (`feature/cierre-iteracion3`): Tests de integración de punta a punta.

---

## Iteración 4

### Tareas / Issues de la Iteración:
1. **Actualización de tarifas** (`feature/tarifa-2150`):
   - La tarifa básica urbana se actualiza de $1580 a $2150 (`Colectivo.TarifaBasica = 2150`).
2. **Boleto de uso frecuente** (`feature/uso-frecuente`):
   - Descuento acumulativo mensual en tarjetas de tipo Normal (no franquicias):
     - Viajes 1 al 29: Tarifa normal ($2150).
     - Viajes 30 al 79: 20% de descuento ($1720).
     - Viajes 80 en adelante: 25% de descuento ($1612,50).
3. **Franquicias: franja horaria** (`feature/franquicia-horario`):
   - Las tarjetas con franquicia (`EsFranquicia = true`) únicamente pueden viajar de lunes a viernes entre las 06:00:00 y las 21:59:59 ([06:00, 22:00)). Sábados, domingos o fuera de franja horaria devuelven `false`.
4. **Líneas interurbanas** (`feature/lineas-interurbanas`):
   - `ColectivoInterurbano : Colectivo` con tarifa base de $3000 (`TarifaInterurbana`). Mapeo como herencia (TPH) en `TransporteDbContext`. Mismos criterios y reglas que las líneas urbanas.
5. **Trasbordos sin costo** (`feature/trasbordos`):
   - Trasbordo gratuito sin límite de combinaciones dentro de 1 hora del último boleto pagado (`!EsTrasbordo`), entre líneas distintas, de lunes a sábado de 07:00 a 22:00 ([07:00, 22:00)).
6. **Cierre de Iteración 4** (`feature/cierre-iteracion4`):
   - Test que fija el orden canónico de reglas, tests de integración E2E punta a punta, documentación completa y tag `v4.0-iteracion4`.

### Supuestos y Decisiones de Diseño (Iteración 4)

- **Tarifa básica y descubierto:** Con la tarifa urbana de $2150 y la interurbana de $3000, una tarjeta en saldo $0 no puede abonar el viaje en descubierto ($0 - $2150 = -$2150 < -$2000; $0 - $3000 = -$3000 < -$2000), requiriendo saldo previo para viajar.
- **Pipeline de Reglas Extensible (OCP / LSP):** Las reglas de viaje implementan `IReglaDeViaje` y son ordenadas por su propiedad `Orden`. `Colectivo` y `ColectivoInterurbano` inicializan `ContextoDeViaje.Tarifa` en su `TarifaBase`, y las reglas mutan dicho contexto sin recalcular tarifas fijas, permitiendo que todas las reglas apliquen tanto a colectivos urbanos como interurbanos de manera transparente.
- **Ventana de Trasbordos:**
  - Se cuenta desde el *último boleto pagado* (`!EsTrasbordo`) y **no se reinicia** con trasbordos intermedios.
  - La condición de línea distinta compara con la línea del *boleto anterior inmediato*.
  - Los trasbordos no consumen viajes con beneficio del tope diario de Medio Boleto (`ViajesDeHoy` filtra `!b.EsTrasbordo`) ni suman al conteo mensual de Uso Frecuente (`viajesMes` filtra `!b.EsTrasbordo`).
  - Las reglas previas de franja horaria (10) e intervalo de 5 minutos (20) se evalúan antes que el trasbordo (30), impidiendo que un medio boleto trasborde antes de 5 minutos o que una franquicia trasborde en días no laborales.

---

## Tabla Canónica de Reglas de Viaje

| Orden | Clase | Responsabilidad |
|:-----:|-------|-----------------|
| **10** | `FranjaHorariaFranquiciaRegla` | Valida que las franquicias (`EsFranquicia = true`) solo viajen de lunes a viernes entre las 06:00 y las 22:00. Fuera de ese rango devuelve `false`. |
| **20** | `IntervaloMinimoEntreViajesRegla` | Exige un tiempo mínimo de 5 minutos entre viajes consecutivos para tarjetas que lo requieran (Medio Boleto). Si no transcurrió el tiempo, devuelve `false`. |
| **30** | `TrasbordoRegla` | Aplica costo $0 y marca `EsTrasbordo = true` si el viaje es a una línea distinta dentro de 1 hora del último boleto pagado, de lunes a sábado entre las 07:00 y las 22:00. Devuelve `true`. |
| **40** | `BeneficioPorTipoDeTarjetaRegla` | Aplica el descuento correspondiente a la tarjeta (50% Medio Boleto, 100% Gratuito/Jubilado) respetando el tope diario de 2 viajes con beneficio (excluyendo trasbordos). Devuelve `true`. |
| **50** | `UsoFrecuenteRegla` | Aplica descuentos acumulativos en el mes calendario para tarjetas Normales: 20% a partir del viaje 30 y 25% a partir del viaje 80 (excluyendo trasbordos). Devuelve `true`. |

---

## Iteración 5

Integración con el sistema de bicicletas públicas **Mi Bici Tu Bici**.
Los usuarios pueden retirar bicicletas públicas en estaciones pagando la tarifa de servicio mediante su tarjeta SUBE.

### Tareas / Issues de la Iteración:
1. **Mi Bici Tu Bici: estación, retiro y pago** (`feature/bici-retiro-devolucion`):
   - Modelado de `EstacionBicicletas` y `RetiroDeBici`.
   - Cobro de la tarifa diaria de $2418,75 (`TarifaDiaria`).
   - Pase diario: si en el día calendario (`.Date`) ya se abonó la tarifa diaria (`TarifaCobrada > 0`), los retiros posteriores de ese mismo día no se cobran.
   - Restricción de saldo: no se admite saldo negativo para retirar bicicletas (`TieneSaldoPara(monto)` exige `Saldo >= monto`).
   - Límite de 1 bicicleta en curso por tarjeta a la vez.
   - Control de tiempo máximo de uso de 1 hora (`TiempoMaximoDeUso`) y registro de mora (`TuvoDemora`).
2. **Multas por demora** (`feature/bici-multas`):
   - Multa fija de $1250 (`MultaPorDemora`) por uso mayor a 1 hora.
   - Acumulación de multas pendientes (`MultaPendiente(tarjeta)`): retiros con demora que aún no hayan sido saldados (`TuvoDemora && !MultaSaldada`).
   - Cobro atómico de multas acumuladas en el primer retiro que requiera pago (tarifa diaria + multas).
   - Si el día ya está pago, los retiros posteriores de ese día no cobran multas y las mismas continúan acumuladas.
   - Principio "todo o nada": si el saldo no alcanza para tarifa + multas, el retiro se rechaza y tanto el saldo como las multas quedan intactos.
3. **Escenarios de prueba y aislamiento** (`feature/bici-tests`):
   - Suite `MiBiciTuBiciEscenariosTest` cubriendo los 3 escenarios mínimos del enunciado (retiro exitoso a tiempo, rechazo por saldo insuficiente con/sin multas, cobro acumulado de multas de uno o varios usos tardíos).
   - Pruebas de tarifa plana universal (todas las franquicias pagan tarifa completa de bici).
   - Pruebas de aislamiento total entre Colectivos y Mi Bici Tu Bici (un retiro de bici no es un `Boleto`, no consume intervalos de 5 min ni topes diarios de 2 viajes, ni altera el conteo de Uso Frecuente).
4. **Cierre de Iteración 5** (`feature/cierre-iteracion5`):
   - Regresión completa de 153 tests en verde.
   - Verificación de cobertura de todos los métodos y propiedades públicas nuevas.
   - Actualización integral de documentación y creación del tag `v5.0-iteracion5`.

### Supuestos y Decisiones de Diseño (Iteración 5):
- **SRP y desacoplamiento:**
  - `Tarjeta` gestiona exclusivamente su saldo, saldos pendientes y el historial de comprobantes (`Boletos` y `RetirosDeBici`).
  - `RetiroDeBici` es un registro inmutable de auditoría y ciclo de vida de un uso de bicicleta pública (no es un `Boleto`).
  - `EstacionBicicletas` orquesta la transacción de retiro, control de pases diarios, liquidación de multas acumuladas y devolución con verificación de mora.
- **DIP y desacople temporal:**
  - `EstacionBicicletas` recibe `IProveedorDeFecha`, eliminando cualquier llamado directo a `DateTime.Now` en la lógica de negocio y permitiendo testing determinístico mediante `ProveedorDeFechaFalso`.
- **Tarifa plana universal:** Todas las tarjetas pagan la tarifa completa de $2418,75 sin descuentos por franquicias de colectivo (Medio Boleto, Boleto Gratuito y Franquicia Completa abonan tarifa diaria completa).
- **Disponibilidad horaria:** El sistema de bicicletas públicas no posee restricciones de franja horaria laboral (opera las 24 horas, incluso domingos a la noche).
- **Sin descubierto:** A diferencia del transporte en colectivo que permite descubierto de hasta -$2000, para retirar una bicicleta se exige saldo disponible previo suficiente para cubrir el total (`tarjeta.TieneSaldoPara(total)`).
- **Aislamiento completo de dominios:** Los retiros de bicicleta no interfieren con las reglas de viaje en colectivo (intervalo de 5 min, tope diario de 2 viajes gratis/medio boleto, ni uso frecuente).

---

## Trazabilidad de Tareas y Revisión Cruzada

| Iteración | Prompt | Tarea / Rama | Autor | Revisor |
|:---------:|:------:|--------------|:-----:|:-------:|
| **Iteración 2** | **Prompt 1** | `feature/descuento-saldos` (Descuento de saldos y `bool PagarCon`) | Facundo | Leandro |
| **Iteración 2** | **Prompt 2** | `feature/saldo-negativo` (Límite negativo -$2000 y cancelación con carga) | Leandro | Facundo |
| **Iteración 2** | **Prompt 3** | `feature/franquicia-tarjeta` (TarjetaTipo, seed en DbContext y cálculo de tarifas) | Facundo | Leandro |
| **Iteración 2** | **Prompt 4** | `feature/cierre-iteracion2` (Test de integración, README y tag `v2.0-iteracion2`) | Leandro | Facundo |
| **Iteración 3** | **Prompt 1** | `feature/boleto-datos` (Datos inmutables en Boleto, `IProveedorDeFecha`) | Facundo | Leandro |
| **Iteración 3** | **Prompt 2** | `refactor/reglas-de-viaje` (Motor de reglas desacoplado `IReglaDeViaje`) | Facundo | Leandro |
| **Iteración 3** | **Prompt 3** | `feature/medio-boleto-limites` (Intervalo 5 min y tope 2 viajes diarios medio boleto) | Facundo | Leandro |
| **Iteración 3** | **Prompt 4** | `feature/gratuito-limites` (Tope de 2 viajes diarios para Boleto Gratuito) | Leandro | Facundo |
| **Iteración 3** | **Prompt 5** | `feature/saldo-maximo` (Saldo máximo $56000 y acreditación diferida) | Facundo | Leandro |
| **Iteración 3** | **Prompt 6** | `feature/cierre-iteracion3` (Tests de integración E2E y cierre Iteración 3) | Leandro | Facundo |
| **Iteración 4** | **Prompt 7** | `feature/tarifa-2150` (Actualización de tarifa básica urbana a $2150) | Facundo | Leandro |
| **Iteración 4** | **Prompt 8** | `feature/uso-frecuente` (Boleto de uso frecuente con descuentos de 20% y 25%) | Leandro | Facundo |
| **Iteración 4** | **Prompt 9** | `feature/franquicia-horario` (Franja horaria para franquicias de lun a vie 06-22) | Facundo | Leandro |
| **Iteración 4** | **Prompt 10** | `feature/lineas-interurbanas` (`ColectivoInterurbano` con tarifa $3000 y herencia EF) | Leandro | Facundo |
| **Iteración 4** | **Prompt 11** | `feature/trasbordos` (Trasbordos gratuitos entre líneas distintas dentro de 1 hora) | Facundo | Leandro |
| **Iteración 4** | **Prompt 12** | `feature/cierre-iteracion4` (Test de orden, integración E2E, README final y tag `v4.0-iteracion4`) | Leandro | Facundo |
| **Iteración 5** | **Prompt 13 (P1)** | `feature/bici-retiro-devolucion` (Estación, retiro, pase diario y devolución) | Facundo | Leandro |
| **Iteración 5** | **Prompt 14 (P2)** | `feature/bici-multas` (Multa por demora $1250, acumulación y cobro al pagar) | Leandro | Facundo |
| **Iteración 5** | **Prompt 15 (P3)** | `feature/bici-tests` (Escenarios mínimos de aceptación y aislamiento colectivo) | Facundo | Leandro |
| **Iteración 5** | **Prompt 16 (P4)** | `feature/cierre-iteracion5` (Regresión general, cobertura, README y tag `v5.0-iteracion5`) | Leandro | Facundo |
