---
author: Carsten König
title: funktionale Parser in C#
date: 06. April 2018
---

# Einleitung

## die Lego-Idee

![pixabay](../Images/Lego.jpg)

## Funktionen und Komposition

![Komposition](../Images/FunktionKomposition.png)


# Parser

## Was ist ein Parser?

ein **Parser** versucht eine *Eingabe* in eine für die Weiterverarbeitung geeignete
*Ausgabe* umzuwandeln.

---

### 2 * 3 + 4

![Syntaxbaum](../Images/SyntaxBaum.png)

## Kombinator

![](../Images/LegoSteine.png)

*Funktionen* die *Parser* zu neuen *Parser*n zusammensetzen

---

## Dazu

- *Parser* als **Daten** representieren
- *Kombinatoren* als **Funktionen** zwischen diesen Daten

---

## Idee

![](../Images/ParserDef.png)

## Definition `Parser`

Input-`String` &rarr; Output-`ParseResult`

```csharp
delegate ParseResult<T> Parser<T>(string input)
```

## `ParserResult`

eine von zwei Möglichkeiten:

- Parser konnte Eingabe **nicht** erkennen
- Parser hat einen **Teil** der Eingabe erkannt und einen **Ergebniswert** berechnet

## algebraischer Datentyp

**F#**

```fsharp

type ParseResult<'a> =
   | Failure
   | Success of Value:'a * RemainingInput:string

```

---

**C#**

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

**F#**

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
   case FailureResult<T> fail:
     ...
     break;
   case SuccessResult<T> success:
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

**Church-Encoding**

```csharp
delegate T Bool<T>(T thenBranch, T elseBranch);

T True<T>  (T thenBranch, T elseBranch) => thenBranch;
T False<T> (T thenBranch, T elseBranch) => elseBranch;

True ("Hallo", "Welt"); // = "Hallo"
False("Hallo", "Welt"); // = "Welt"
```

---

### Beispiel Nat

```csharp
delegate T Nat<T>(Func<T,T> plus1, T zero);

T Null<T> (Func<T,T> plus1, T zero) => zero;
T Eins<T> (Func<T,T> plus1, T zero) => plus1(zero);
T Zwei<T> (Func<T,T> plus1, T zero) => plus1(plus1(zero));

Zwei (s => "*"+s, ""); // = "**"
```

---

### `ParserResult`

```csharp
abstract class ParseResult<T> 
{
   abstract Tres Match<Tres>( 
      Func<T, string, Tres> withSuccess, 
      Func<Tres> onError);
}
```

---

### Failure

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

### Success

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

### Anwendung

```csharp
abstract class ParseResult<T>
{
  bool IsSuccess => 
     Match((val, rem) => true, () => false);
}
```


# ![Atom](../Images/Atom.png)

## `Fail`

- schlägt immmer fehl

```csharp
Parser<T> Fail<T>()
{
  return input => new FailureResult<T>();
}
```

## `Succeed`

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
Parser<char> Char(Predicate<char> isValidChar)
{
  return input =>
    input.Length == 0 || !isValidChar(input[0])
    ? FailureResult<char>()
    : SuccessResult<char>(input[0], input.Substring(1));
}
```

# Parser kombinieren

## `or`

![A ok](../Images/OrParserA.png)

## `or`

![A fail, B ok](../Images/OrParserB.png)

## `or`

![beide fail](../Images/OrParserC.png)

## `or`

```csharp
Parser<T> Or<T>(this Parser<T> parserA, Parser<T> parserB)
{
  return input =>
    {
      switch (parserA(input))
      {
        case SuccessResult<T> success:
          return success;
        default: // Failed
          return parserB(input);
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

# Funktor

## *Erfolg*

![](../Images/ParserFunktorA.png)

## *Fehlschlag*

![](../Images/ParserFunktorB.png)

---

**C#**

```csharp
Parser<TRes> Map<T, TRes>(
   Parser<T> parser, 
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

**kombiniert**

```csharp
Parser<TRes> Map<T, TRes>(
   this Parser<T> parser, 
   Func<T, TRes> map)
{
  return input => parser(input).Map(map);
}
```

---

### Typ

map : (f: A &rarr; B)  &rarr;  (`P<A>`  &rarr;  `P<B>`)

---

### Gesetze


- `p.map(x=>x)` &#8801; `p`
- `p.map(x=>g(f(x)))` &#8801; `p.map(f).map(g)`

---

### Funktor Komposition

![](../Images/FunctorComp.png)

---

### andere "Funktoren"

- `IEnumerable` / `Select`
- `Task`
- ...

# Monad

## `andThen` 

`P<A>` &rarr; (`a` &rarr; `P<B>`) &rarr; `P<B>`

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
<expr>   ::= <term>    | <term> ("+"|"-") <expr>
<term>   ::= <factor>  | <factor>  ("*"|"/") <term>
<factor> ::= zahl      | "(" <expr> ")"

zahl   = [0|1|..|9]+
```

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

# Links

## Referenzen

- G. Hutton, E. Meijer [Monadic Parsing in Haskell](http://www.cs.nott.ac.uk/~pszgmh/pearl.pdf)
- G. Hutton, E. Meijer [Monadic Parser Combinators](http://www.cs.nott.ac.uk/~pszgmh/monparsing.pdf)

## Bibliotheken

- [Sprache](https://github.com/sprache/Sprache)
- [FParsec](http://www.quanttec.com/fparsec/)
- [Liste mit anderen Implementationen](https://wiki.haskell.org/Parsec#Parsec_clones_in_other_languages)

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
