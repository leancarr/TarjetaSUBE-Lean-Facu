# Anotaciones y Decisiones de Diseño 

Anotaciones sobre las decisiones arquitectónicas tomadas en los Pasos 1 y 2 para mantener la misma línea en los pasos siguientes (3 y 4):

## 1. Sintaxis de Código (C# 9 / .NET 5)
A pesar de que el proyecto está configurado en `.NET 8`, acordamos escribir el código utilizando la **sintaxis clásica de .NET 5**. 
- Usar namespaces con llaves (`namespace Transporte { ... }`).
- NO usar *File-scoped namespaces*, ni *Global usings*, ni constructores primarios.
- NO usar la palabra reservada `required`.
- Instanciar arreglos y listas a la forma clásica.

## 2. Independencia de la Base de Datos (Clave para los tests)
Para cumplir con la regla del profesor ("Los tests unitarios no deben depender de la BD real"), decidimos que **toda la lógica de negocio viva adentro de las clases puras (POCOs)**. 
- En el Paso 2 ya metimos la lógica de validación de saldo adentro de `Tarjeta.cs`.
- En el Paso 3, cuando hagas `Colectivo.PagarCon()`, simplemente instanciá `var boleto = new Boleto(...)` y trabajá con los objetos en memoria. 
- Los tests se hacen con `new Tarjeta()` y `new Colectivo()`. NO uses el DbContext ni bases In-Memory para probar la lógica.

## 3. Límite de Saldo ($40.000)
Decidimos que si una carga supera el límite máximo de 40.000, **se rechaza la operación completa** lanzando una `LimiteSaldoExcedidoException`. No hacemos cargas parciales, tal cual un POS de kiosco real.

## 4. Estructura del Boleto (Paso 3)
Aunque el profe no lo pidió explícitamente, decidimos que el `Boleto` funcione como un comprobante histórico real (inmutable). Cuando implementes el Paso 3, asegurate de que al crear el `Boleto` le guardes:
- `Monto` (la constante de $1580)
- `SaldoRestante` (el saldo que le quedó a la tarjeta después de pagar)
- `FechaHora` (el momento exacto de la operación)

## 5. Excepciones Personalizadas
Ya te dejamos creadas 3 excepciones en `Excepciones.cs` que tenés que atajar o dejar propagar en el `Colectivo` y en tus tests:
- `MontoDeCargaInvalidoException`
- `LimiteSaldoExcedidoException`
- `SaldoInsuficienteException` (Esta es la que va a saltar cuando intentes debitar sin plata).
