# Trabajo Tarjeta 2026

## Integrantes del grupo
 - Leandro Caraballo
 - Facundo Naveira

## Aclaraciones
El siguiente trabajo es un enunciado iterativo. Regularmente se ampliará y/o modificará el enunciado.

[ENTREGA](https://forms.gle/NeM1adptvvtioRJH9) (El form cierra el 21/9)

- El trabajo está implementado en **.NET 8.0 (`net8.0`)**, usando Git para el control de versiones y NUnit como framework de testing.
- Los tests unitarios no deben depender de la base de datos real. Tienen [este ejercicio](https://github.com/mgonzalesips/Tienda) de ejemplo para ver como hacerlo
- **Todos** los métodos deben estar testeados con un test unitario, aunque no se aclare explícitamente en el enunciado.
- Para la nota final se tomará en cuenta no solo el código fuente de la implementación, sino también el uso de Git y las herramientas que este provee como commits, ramas y tags ademas de los tests.
- Cada clase de la implementación y de testing debe estar en un archivo aparte.

---

## Iteración 1

Escribir un programa con programación orientada a objetos que permita ilustrar el funcionamiento del transporte urbano de pasajeros de la ciudad de Rosario.
Las clases que interactúan en la simulación son: Colectivo, Tarjeta y Boleto.
Cuando un usuario viaja en colectivo con una tarjeta, se genera un boleto como resultado de la operación `colectivo.pagarCon(tarjeta)`.

Para esta iteración se consideran los siguientes supuestos:

- No hay medio boleto de ningún tipo.
- No hay transbordos.
- No hay saldo negativo.
- La tarifa básica de un pasaje es de: $1580
- Las cargas aceptadas de tarjetas son: (2000, 3000, 4000, 5000, 8000, 10000, 15000, 20000, 25000, 30000)
- El límite de saldo de una tarjeta es de $40000

Se pide:

- Completar los nombres de cada integrante al principio del enunciado. 
- Hacer un fork del repositorio.
- Crear un `DbContext` con los `DbSet` correspondientes a Tarjeta, Colectivo y Boleto.
- Implementar el código de las clases Tarjeta, Colectivo y Boleto.
- Hacer que el test Tarjeta.cs funcione correctamente con todos los montos de pago listados.
- Enviar el enlace del repositorio al mail del profesor con los integrantes del grupo: dos por grupo.

---

## Iteración 2

Para esta iteración hay 3 tareas principales. Crear un issue en GitHub copiando la descripción de cada tarea y completar cada uno en una rama diferente. Éstas serán mergeadas al validar, luego de una revisión cruzada (de ambos integrantes del grupo), que todo el código tiene sentido y está correctamente implementado.

No es necesario que todo el código para un issue esté funcionando al 100% antes de mergearlo, pueden crear pull requests que solucionen algún ítem particular del problema para avanzar más rápido.

Además de las tareas planteadas, cada grupo tiene tareas pendientes de la iteración anterior que debe finalizar antes de comenzar con la iteración 2.

### Descuento de saldos (Completado)
Cada vez que una tarjeta paga un boleto, descuenta el valor del monto gastado.
Si la tarjeta se queda sin saldo, la operación `colectivo.pagarCon(tarjeta)` devuelve `false`.
- **Contrato de `PagarCon`:** `bool PagarCon(Tarjeta tarjeta, out Boleto? boleto)` (y su alias `pagarCon`). Devuelve `true` y emite el `Boleto` cuando el saldo alcanza dentro del límite permitido; en caso contrario devuelve `false`, asigna `boleto = null` y no altera el saldo de la tarjeta.

### Saldo negativo (Completado)
Si la tarjeta se queda sin crédito, puede tener un saldo negativo de hasta $2000 (`Tarjeta.LimiteNegativo = -2000`).
Cuando se vuelve a cargar la tarjeta, se descuenta el saldo de lo que se haya consumido.
- [x] Escribir un test que valide que la tarjeta no pueda quedar con menos saldo que el permitido.
- [x] Escribir un test que valide que el saldo de la tarjeta descuenta correctamente el/los viaje/s plus otorgado/s.

### Franquicia de Boleto (Completado)
Existen dos tipos de franquicia en lo que refiere a tarjetas, las franquicias parciales, como el medio boleto estudiantil o el universitario, y las completas como las de jubilados (Notar que también existe boleto gratuito para estudiantes).

Crear la clase `TarjetaTipo` y relacionarla con `Tarjeta` mediante una clave foránea (`TarjetaTipoId`). Los tipos a representar son: Normal, Medio boleto estudiantil, Boleto gratuito estudiantil y Franquicia completa.
- **Modelo `TarjetaTipo`:** Se modela como una entidad parametrizada (`Id`, `Nombre`, `PorcentajeDescuento`), cumpliendo con el principio Abierto/Cerrado (OCP) para que agregar un nuevo tipo de tarjeta no requiera crear una subclase nueva.
  - `Normal`: `PorcentajeDescuento = 0` (paga el 100% de la tarifa básica).
  - `Medio boleto estudiantil`: `PorcentajeDescuento = 0.5m` (paga el 50% de la tarifa básica).
  - `Boleto gratuito estudiantil`: `PorcentajeDescuento = 1.0m` (costo $0).
  - `Franquicia completa`: `PorcentajeDescuento = 1.0m` (costo $0).
- [x] Agregar un tipo de tarjeta nuevo no debe requerir crear una clase nueva.
- [x] Para esta iteración considerar simplemente que cuando se paga con una tarjeta del tipo MedioBoleto el costo del pasaje vale la mitad, independientemente de cuántas veces se use y qué día de la semana sea.
- [x] Escribir un test que valide que una tarjeta de FranquiciaCompleta siempre puede pagar un boleto.
- [x] Escribir un test que valide que el monto del boleto pagado con medio boleto es siempre la mitad del normal.

---

## Iteración 3

Al igual que la iteración anterior, se pide mantener la mecánica de trabajo para ir añadiendo las nuevas funcionalidades y/o modificaciones (issue, una rama específica para cada tarea y finalmente el merge cuando todo funcione correctamente..., etc.)

En esta iteración daremos una introducción a la manipulación de fechas y horarios. Éstos serán necesarios en esta oportunidad para realizar las modificaciones pedidas.

### Tareas / Issues de la Iteración:
1. **Más datos sobre el boleto** (`feature/boleto-datos`):
   - La clase `Boleto` tendrá métodos/propiedades para conocer: Fecha, tipo de tarjeta, línea de colectivo, total abonado, saldo restante e ID de la tarjeta.
   - Informar si la transacción fue un viaje plus (`EsViajePlus`).
   - Desacople de la hora del sistema mediante `IProveedorDeFecha`.
2. **Refactor: reglas de viaje** (`refactor/reglas-de-viaje`):
   - Mover la lógica de validación y cálculo de cobro en `Colectivo.PagarCon` hacia reglas independientes (`IReglaDeViaje` y `ContextoDeViaje`).
3. **Limitación en el pago de medio boletos** (`feature/medio-boleto-limites`):
   - Mínimo 5 minutos entre viajes consecutivos con la misma tarjeta medio boleto.
   - Hasta 2 viajes por día con medio boleto; a partir del tercero se cobra tarifa normal.
4. **Limitación en el pago de franquicias completas** (`feature/gratuito-limites`):
   - Hasta 2 viajes gratuitos por día para Boleto Educativo Gratuito.
5. **Saldo de la tarjeta** (`feature/saldo-maximo`):
   - Límite máximo de saldo de $56000. El saldo excedente queda como pendiente de acreditación (`AcreditarCarga`).
6. **Cierre de Iteración 3** (`feature/cierre-iteracion3`):
   - Tests de integración de punta a punta y verificación de cobertura de métodos públicos.

### Supuestos y decisiones
- **Manejo de fechas y horarios:** Se introduce la abstracción `IProveedorDeFecha` (`ProveedorDeFechaSistema` y `ProveedorDeFechaFalso`). Se prohíbe el uso de `DateTime.Now` directo en la lógica de dominio para permitir testing temporal determinístico.
- **Datos inmutables en Boleto:** El `Boleto` funciona como comprobante histórico congelado al momento del pago (`Fecha`, `TipoTarjeta`, `Linea`, `TotalAbonado`, `SaldoRestante`, `TarjetaId`).
- **Total abonado y saldo negativo:** `TotalAbonado` refleja lo efectivamente descontado de la tarjeta en ese viaje. En caso de quedar con saldo negativo, se marca `EsViajePlus = true`.
- **Tope diario de beneficio:** Aplica al día calendario (`.Date`). Tras alcanzar el límite de 2 viajes con beneficio en el día (Medio Boleto o Gratuito Estudiantil), los viajes posteriores se cobran con tarifa completa ($1580). Al día siguiente el beneficio se reinicia automáticamente.
- **Saldo Máximo y Acreditación Progresiva:** Límite máximo de almacenamiento en la tarjeta de $56000. Los montos cargados que superen este valor se retienen en `SaldoPendiente` y se acreditan automáticamente tras cada uso (`Descontar`).

### Tabla de Reglas de Viaje
| Orden | Regla | Responsabilidad |
|-------|-------|-----------------|
| 20 | `IntervaloMinimoEntreViajesRegla` | Valida que pasen al menos 5 minutos entre viajes consecutivos si la tarjeta lo requiere. |
| 40 | `BeneficioPorTipoDeTarjetaRegla` | Aplica el descuento correspondiente según `TarjetaTipo` respetando el tope diario con beneficio. |




---

## Iteración 4

### Tareas / Issues de la Iteración:
1. **Actualización de tarifas** (`feature/tarifa-2150`):
   - La tarifa básica pasa de $1580 a $2150.
2. **Boleto de uso frecuente** (`feature/uso-frecuente`):
   - Beneficio de descuento mensual acumulativo para tarjetas de tipo Normal:
     - Viajes 1 al 29: Tarifa normal.
     - Viajes 30 al 79: 20% de descuento.
     - Viajes 80 en adelante: 25% de descuento.
3. **Franquicias: franja horaria** (`feature/franquicia-horario`):
   - Franja permitida para franquicias (no Normal): Lunes a viernes de 06:00 a 22:00. Fuera de ese rango no se permite viajar.
4. **Líneas interurbanas** (`feature/lineas-interurbanas`):
   - Implementar líneas de colectivo interurbanas con tarifa de $3000.
5. **Trasbordos** (`feature/trasbordos`):
   - Trasbordos sin costo entre líneas distintas dentro de 1 hora del primer boleto pagado, de lunes a sábado de 07:00 a 22:00.
6. **Cierre de Iteración 4** (`feature/cierre-iteracion4`):
   - Test de orden de reglas, integración E2E, README final y tag `v4.0-iteracion4`.

### Supuestos y decisiones
- **Tarifa básica:** La tarifa básica urbana se actualiza de $1580 a $2150. Con la nueva tarifa, un viaje con tarjeta en saldo 0 ya no puede debitarse en saldo negativo (0 - 2150 = -2150 < -2000), requiriendo saldo disponible previo para poder completarse.
