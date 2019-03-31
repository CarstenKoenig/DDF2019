---
author: Carsten König
title: Parser funktional implementiert mit C#
date: 06. April 2018
---

# Einleitung


:::notes:

OO intereressiert sich nicht für Kompositionen - FP umso mehr

Zeugen Jehovas / FP Witz einfügen

:::

## die Lego-Idee

![pixabay](../Images/Lego.jpg)

## Funktionen und Komposition

![Komposition](../Images/FunktionKomposition.png)


# Parser

## Parser *ok*

![](../Images/Parser.png)


## Parser *fail*

![](../Images/ParserFail.png)

## Definition `Parser`

Funktion Input-`String` &rarr; Output-`ParseResult`

```csharp
delegate ParseResult<T> Parser<T>(string input)
```

## `ParserResult`

eine von zwei Möglichkeiten:

- Parser konnte Eingabe *nicht* benutzen
- Parser hat einen *Teil* der Eingabe benutzt und einen *Ergebniswert* berechnet

## Beipiel


```csharp
Parser<int> P;

P ( "Hallo" )
``` 

schlägt fehl

---

```csharp
Parser<int> P;

P ( "42!" )
``` 

- liefert die Zahl `42`
- liefert den *Rest* der Eingabe `"!"`

## *SUM*-Typ

:::notes
- Produkt-Typen kennen wir alle - jeder Record, Tupel
- Enum ist schon nahe
:::

---

### funktional (F#)

```fsharp

type ParseResult<'a> =
   | Failure
   | Success of Value:'a * RemainingInput:string

```

---

```csharp
abstract class ParseResult<T> { .. }

class FailureResult<T> : ParseResult<T>
{
}

class SuccessResult<T> : ParseResult<T>
{
  T Value { get; }
  string RemainingInput { get; }
}

```

---

### Pattern Matching

```fsharp
match result with
| Failure -> ... 
| Success (value, remaining) -> ...
```

---

### C# 7

```csharp
switch (result)
{
   case FailureResult fail:
     ...
     break;
   case Success success:
     ... success.Value ...
     break;
}
```

---

### Alternative *Church-Encoding*

**Was** machen wir mit den Datentyp?

:::notes
- im Lambda-Calculus hat man nur Funktionen
- alles muss mit Funktionen ausgedrückt werden
:::

---

### Beispiel `Bool`

wird in *Entscheidungen* / `if` benutzt

```csharp
if (boolValue)
   return thenBranch;
else
   return elseBranch;
   
// oder

boolValue ? thenBranch : elseBranch;
```

---

### Beispiel `Bool`

Church-Encoding wäre

```csharp
T True<T>  (T thenBranch, T elseBranch) => thenBranch;
T False<T> (T thenBranch, T elseBranch) => elseBranch;

True ("Hallo", "Welt"); // = "Hallo"
False("Hallo", "Welt"); // = "Welt"
```

---

### `ParserResult`` / Pattern Matching

```csharp
abstract class ParseResult<T> 
{
   abstract Tres Match<Tres>( 
      Func<T, string, Tres> withSuccess, 
      Func<Tres> onError);
}
```

---

```csharp
class FailureResult<T> : ParseResult<T>
{
  override Tres Match<Tres> (
     Func<T, string, Tres> withSuccess, 
     Func<Tres> onError)
  {
    return onError();
  }
}
```

---

```csharp
class SuccessResult<T> : ParseResult<T>
{
  T Value { get; }
  string RemainingInput { get; }

  override Tres Match<Tres>(
     Func<T, string, Tres> withSuccess, 
     Func<Tres> onError)
  {
    return withSuccess(Value, RemainingInput);
  }
}
```

---

```csharp
abstract class ParseResult<T>
{
  bool IsSuccess => 
     Match((val, rem) => true, () => false);
}
```


# Atome unseres Universums

## `Failure`

- schlägt immmer fehl

```csharp
Parser<T> Failure<T>()
{
  return input => new FailureResult<T>();
}
```

## `Success`

- immer erfolgreich
- gibt *festen*  Wert zurück
- *konsumiert* nichts vom Input

```csharp
Parser<T> Succeed<T>(T withalue)
{
  return input => SuccessResult<T>(withValue, input);
}
```

## `Char` Parser

entscheided über ein Prädikat ob das erste Zeichen in der Eingabe erkannt wird

```csharp
Parser<char> Char(Func<char, bool> isValidChar)
{
  return input =>
    input.Length == 0 || !isValidChar(input[0])
    ? FailureResult<char>()
    : SuccessResult<char>(input[0], input.Substring(1));
}
```

# einfache Kombinatoren

## `or`

```csharp
Parser<T> Or<T>(this Parser<T> either, Parser<T> or)
{
  return input =>
    {
      switch (either(input))
      {
        case SuccessResult<T> success:
          return success;
        default: // Failed
          return or(input);
      }
    };
}
```

## `many`

```csharp
Parser<IEnumerable<T>> Many<T>(this Parser<T> parser) {
  return input => {
      var results = new List<T>();
      var nextInput = input;
      while (true) {
        switch (parser(nextInput)) {
          case SuccessResult<T> res:
            results.Add(res.Value);
            nextInput = res.RemainingInput;
            break;
          default:
            return SuccessResult<IEnumerable<T>>
               (results, nextInput);
}}};}
```

# Functor *what?*

---

`Map : (f: A->B) -> (P<A> -> P<B>)`

![`Map(ToString, IntParser)`](../Images/ParserFunctor.png)


## `map`

```csharp
Parser<TRes> Map<T, TRes>(
   this Parser<T> parser, 
   Func<T, TRes> map) 
{

  return input =>
     parser(input).Match(
        (valueT, remaining) =>
           new SuccessResult(map(valueT), remaining),
        () => new FailureResult<TRes>());
}
```

## `ParseResult.Map`

```csharp
ParseResult<Tres> Map<Tres>(Func<T, Tres> map)
{
  return Match(
    (value, remaining) => map(value).Success(remaining),
    () => ParseResult.Fail<Tres>());
}
```

---

```csharp
Parser<TRes> Map<T, TRes>(
   this Parser<T> parser, 
   Func<T, TRes> map)
{
  return input => parser(input).Map(map);
}
```

## Funktor Komposition

![](../Images/FunctorComp.png)

# Monad

## `andThen` 

![`P<A> -> (a -> P<B>) -> P<B>`](../Images/ParserBind.png)

## `andThen`

```csharp
Parser<TRes> AndThen<T, TRes>(
   this Parser<T> parser, 
   Func<T, Parser<TRes>> getNext)
{
  return input =>
    parser(input).Match(
        (value, remaining) => getNext(value)(remaining),
        () => new FailureResult<TRes>());
}
```

## LINQ*ify*

## Beispiel `ChainLeft1`

**Ziel:** `3 + 4 + 5 = (3 + 4) + 5`

```
<expr> ::= <operand> 
         | <operand> operator <expr>
```

:::notes
- **L**: `(3 + 4) + 5`
- **1**: `"3"` ok, `""` nicht
:::

---

```csharp
Parser<T> ChainLeft1<T>(
   this Parser<T> operandP, 
   Parser<Func<T, T, T>> operatorP)
{
  Parser<T> rest(T accum) =>
    (from op in operatorP
      from val in operandP
      from more in rest(op(accum, val))
      select more)
    .Or(Succeed(accum));

  return operandP.AndThen(rest);
}
```

## Select

```csharp
Parser<TResult> Select<TSource, TResult>(
   this Parser<TSource> source, 
   Func<TSource, TResult> selector)
{
  return source.Map(selector);
}
```

## Select Many

```csharp
Parser<TResult> SelectMany<TSource, TCollection, TResult>(
  this Parser<TSource> source,
  Func<TSource, Parser<TCollection>> collectionSelector,
  Func<TSource, TCollection, TResult> resultSelector)
{
  return source
      .AndThen(val =>
        collectionSelector(val)
        .Map(col => resultSelector(val, col)));
}
```

# Beispiel *Rechner*

## die Gramatik

```
<expr> ::= <prod> | <prod> ("+"|"-") <expr>
<prod> ::= <val>  | <val>  ("*"|"/") <prod>
<val>  ::= zahl   | "(" <expr> ")"

zahl   = [0|1|..|9]+
```

## Beispiel

(2+3) + 5 * 5

TODO: Baum einfügen

## Leerzeichen ignorieren

```csharp
var spacesP = Parsers
   .Char(Char.IsWhiteSpace)
   .Many()
   .Map(_ => Unit.Value);
```

## Zahl parsen

```csharp
var zahlP =
  Parsers
    .Char(Char.IsDigit)
    .Many1()
    .Map(ToString)
    .TryMap<string, double>(Double.TryParse)
    .LeftOf(spacesP);
```

## Operatoren

```csharp
Parser<Func<double, double, double>> OperatorP(
   char symbol, 
   Func<double, double, double> operation)
   => Parsers.Char(symbol).Map(_ => operation);

var addOpsP =
  OperatorP('+', Add)
  .Or(OperatorP('-', Subtract))
  .LeftOf(spacesP);

var multOpsP = ...
```

## Expression

```csharp
var expressionRef = Parsers.CreateForwardRef<double>();

var valueP = expressionRef.Parser
  .Between(Parsers.Char('('), Parsers.Char(')'))
  .Or(zahlP);

var prodP = valueP
   .ChainLeft1(multOpsP);

expressionRef.SetRef(
   prodP
   .ChainLeft1(addOpsP));
```


# andere Beispiele

## Diagrams

![Hilbert Curve](../Images/Hilbert.png)

---

## Diagrams

```haskell
hilbert 0 = mempty
hilbert n = hilbert' (n-1) # reflectY <> vrule 1
         <> hilbert  (n-1) <> hrule 1
         <> hilbert  (n-1) <> vrule (-1)
         <> hilbert' (n-1) # reflectX
  where
    hilbert' m = hilbert m # rotateBy (1/4)
```

## ELM - JSON Decoders

```haskell
type alias Info =
  { height : Float
  , age : Int
  }

infoDecoder : Decoder Info
infoDecoder =
  map2 Info
    (field "height" float)
    (field "age" int)
```

## Andere

- Form / Validation
- Folds / Projections (Eventsourcing)

# Fragen / Antworten?

## Vielen Dank

- **Slides/Demo** [github.com/CarstenKoenig](https://github.com/CarstenKoenig)
- **Twitter** @CarstenK_Dev
