---
author: Carsten König
title: funktionale Parser in C#
date: 06. April 2018
---

# Einleitung

##

![](../Images/Franken.png)

:::notes
- technisch noch Franken / hört sich seltsam an
- Frankenwald/Thüringer Wald
- Infrastruktur eher schwach
- Markus Heimatministerium
- genau wie FP
:::

## Funktionen und Komposition

![](../Images/FunktionKomposition.png)

:::notes
- der FPler hat nicht viel - nur Funktionen
- die verknüpft er gerne
- Typen helfen dabei
- typische Mindset: Typen/Daten, Funktionen dazwischen + Verknüpfungen
:::

## die Lego-Idee

![](../Images/Lego.jpg)

:::notes
- kleine Bausteine / Daten
- Funktionen / Verknüpfungen geben wieder Bausteine (Kombinatoren)
- aus Einfachen &rarr; kompliziertes

- Beispiel heute: Parser
:::

# Parser

## Was ist ein Parser?

ein **Parser** versucht eine *Eingabe* in eine für die Weiterverarbeitung geeignete
*Ausgabe* umzuwandeln.

:::notes
- Eingabe = String (Stream von Charakteren)
- Ausgabe ein Wert
- Werte in FP immutable/typisiert
- Beispiel: `Int.Parse/TryParse`
- Compiler / Syntaxbaum (Demoprojekt: Rechner)
:::

---

### 2 * 3 + 4

![Syntaxbaum](../Images/SyntaxBaum.png)

:::notes
- Eingabe wird zerlegt in Symbole/Baum
- interpretiert durch Wertzuweisung
- Punkt vor Strich durch Struktur
:::

## Dazu

- *Parser* als **Daten** repräsentieren
- *Kombinatoren* als **Funktionen** zwischen diesen Daten

:::notes
- wir möchten Parser-Kombinatoren-Bibliothek
- Parser als Baustein/Daten/Typ darstellen
- Funktionen von Parser -> Parser finden
- Parser als Daten ..
:::

## Idee

![](../Images/ParserDef.png)

:::notes
- generischer Datentyp
- Eingabe: String
- Parser versucht Wert vom **Anfang** zu extrahieren
- gibt Rest und Wert **oder** Misserfolg zurück
:::

## Definition `Parser`

Input-`String` &rarr; Output-`ParseResult`

```csharp
delegate ParseResult<T> Parser<T>(string input)
```

:::notes
- als einfache Funktion dargestellt
- `delegate` gibt der Signatur einen Namen / C# Compiler kommt gut damit klar
- weniger Boilerplate `new CharParser(...)`
- *funktionaler*
- `ParserResult`?
:::

## `ParserResult`

eine von zwei Möglichkeiten:

- Parser konnte Eingabe **nicht** erkennen
- Parser hat einen **Teil** der Eingabe erkannt und einen **Ergebniswert** berechnet

:::notes
- auch wieder generisch
- zwei Fälle / entweder Erfolg oder Misserfolg
- im Erfolgsfall noch den erkannten Wert und den nicht-erkannten Rest
- unschön: Nullable über Flags ~> **ungültige Werte unrepräsentierbar**
:::

## algebraischer Datentyp

**F#**

```fsharp

type ParseResult<'a> =
   | Failure
   | Success of Value:'a * RemainingInput:string

```

:::notes
- in FP Sprachen kein Problem mit ADTs
- Beispiel erklären
- Vorteil: **nicht offen**
- algebraisch kurz erklären
:::

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

:::notes
- "direkte" Übersetzung des F# Codes
- ist aber "offen"
- wie arbeiten wir mit diesen Ergebnissen?
:::

---

### Pattern Matching

**F#**

```fsharp
match result with
| Failure -> ... 
| Success (value, remaining) -> ...
```

:::notes
- in F# zunächst über PM
- erklären
- Vorteile:
  - erkennt doppelte/fehlende Fälle
  - keine Fehlermöglichkeit wie bei Nullable (HasValue=false, Zugriff auf Value)
:::

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

:::notes
- PM in C# gibt es seit Ver 7
- erklären
- etwas mehr Code
- Problem: erkennt keine fehlenden Fälle
- gut durchatmen - Exkurs
:::

---

### Alternative *Church-Encoding*

**Was** machen wir mit den Datentyp?

:::notes
- Alonzo Church 1930 - Grundlagen der Mathematik
- im Lambda-Calculus hat man nur Funktionen
- alles muss mit Funktionen ausgedrückt werden
- nicht *wie sieht aus* sonder *was machen wir mit*
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

:::notes
- `bool = true | false` nutzen wir für Entscheidungen
- sieht man noch schönen im *ternären Ausdruck*
- falls `true` then sonst else - Branch
- übersetzen wir in zwei Funktionen!
:::

---

**Church-Encoding**

```csharp
delegate T Bool<T>(T thenBranch, T elseBranch);

T True<T>  (T thenBranch, T elseBranch) => thenBranch;
T False<T> (T thenBranch, T elseBranch) => elseBranch;

True ("Hallo", "Welt"); // = "Hallo"
False("Hallo", "Welt"); // = "Welt"
```

:::notes
- Code erklären
- Bool-Werte sind also Funktionen mit gleicher Signatur
- Tupel sehen übrigens genauso aus
:::

---

### Beispiel Nat

```csharp
delegate T Nat<T>(Func<T,T> plus1, T zero);

T Null<T> (Func<T,T> plus1, T zero) => zero;
T Eins<T> (Func<T,T> plus1, T zero) => plus1(zero);
T Zwei<T> (Func<T,T> plus1, T zero) => plus1(plus1(zero));

Zwei (s => "*"+s, ""); // = "**"
Zwei (n => n+1, 0);    // = 2
```

:::notes
- Was machen natürliche Zahlen
- Beispiel mit Finger zählen und Strichen auf Papier geben
- Code durchgehen und mit den Beispielen verdeutlichen
- keine Sorge - muss nicht verstanden werden
:::

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

:::notes
- wir benutzen die Results immer als Entscheidung / `match`
- wie beim bool / if
- Code erklären
- alle Fälle müssen angegeben werden, keiner wird vergessen
:::

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

:::notes
kurz erläutern
:::

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

:::notes
- kurz erläutern
- next: kleine Anwendung
:::

---

### Anwendung

```csharp
abstract class ParseResult<T>
{
  bool IsSuccess => 
     Match((val, rem) => true, () => false);
}
```

:::notes
- kurz erklären
- geschafft - zurück zu den Parsern
:::

# ![Atom](../Images/Atom.png)

## `Fail`

- schlägt immer fehl

```csharp
Parser<T> Fail<T>()
{
  return input => new FailureResult<T>();
}
```

:::notes
- der ultimative **Pessimist**
- einen für jeden Typ `T` (`Fail` ist ein Parser-Builder)
- benutzt nichts der Eingabe
:::

## `Succeed`

- immer erfolgreich
- gibt *festen*  Wert zurück
- *konsumiert* nichts vom Input

```csharp
Parser<T> Succeed<T>(T withValue)
{
  return input => SuccessResult<T>(withValue, input);
}
```

:::notes
- der ultimative **Optimist**
- einen für jeden Typ `T` und Wert `withValue`
- benutzt auch nichts der Eingabe
- immer noch kein *Fortschritt*
:::

## `Char` Parser

entscheidet über ein Prädikat ob das erste Zeichen in der Eingabe erkannt wird

```csharp
Parser<char> Char(Predicate<char> isValidChar)
{
  return input =>
    input.Length == 0 || !isValidChar(input[0])
    ? FailureResult<char>()
    : SuccessResult<char>(input[0], input.Substring(1));
}
```

:::notes
- akzeptiert das nächste Zeichen, falls es eine Eigenschaft erfüllt
- konsumiert ein Zeichen der Eingabe falls Erfolgreich
- dient als Basis für abgeleitete Parser (erkennt *bestimmtes Zeichen*, *jedes Zeichen*, ..)
:::

# Parser kombinieren

## `or`

![A ok](../Images/OrParserA.png)

:::notes
- verknüpft zwei Parser zu einem Neuem = Kombinator
- zunächst bekommt `A` die Chance zu parsen
- ist das erfolgreich, wird die Ausgabe übernommen
:::

## `or`

![A fail, B ok](../Images/OrParserB.png)

:::notes
- akzeptiert `A` nicht, bekommt `B` die gleiche Eingabe
- ist das erfolgreich, wird die Ausgabe übernommen
:::

## `or`

![beide fail](../Images/OrParserC.png)

:::notes
- akzeptieren beide nicht, schlägt der kombinierte Parser fehl
- beachten: `A` und `B` müssen den gleichen Typ zurückgeben!
:::

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

:::notes
- Code erklären
- hier mit switch
:::

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

:::notes
- Bild war komplizierter als der Code
- Idee: gib den gleichen Parser wiederholt den Rest der Eingabe und sammle die Ergebnissen
- schlägt **nie** fehl
- 
- Durchatmen - Besuch im Elfenbeinturm
:::

# Funktor

## ![](../Images/functor.jpg)

map : (f: A &rarr; B)  &rarr;  (`F<A>`  &rarr;  `F<B>`)

:::notes
- Bild aus schönem Buch (zeigen)
- Funktor hebt Pfeile (Funktionen) von einer Welt in eine andere
- Programmierer: in der Regel die gleiche Welt: Typen/Funktionen
- kennt jeder: `Map/Select` - oft auch Mappable
:::

## *Erfolg*

![](../Images/ParserFunktorA.png)

:::notes
- hier eine Variante
- ist Kombinator, der einen Parser und eine Funktion übergeben bekommt
- wenn der Parser erfolgreich ist, wird die Funktion noch auf den Wert angewendet
:::

## *Fehlschlag*

![](../Images/ParserFunktorB.png)

:::notes
- schlägt der Parser fehl, ist auch der *gemappte* Parser nicht erfolgreich
:::

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

:::notes
- Hinsehen: im Block wird nur noch mit dem Results gearbeitet...
:::

## `ParseResult.Map`

```csharp
ParseResult<Tres> Map<Tres>(Func<T, Tres> map)
{
  return Match(
    (value, remaining) => map(value).Success(remaining),
    () => ParseResult.Fail<Tres>());
}
```

:::notes
- das Result ist ebenfalls ein Funktor
- *viele* generische Typen sind Funktoren
- *wenn* Argument nur in Positiver-Position - Zusammenhang mit Co/contra Varianz kurz erklären
- damit ...
:::

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

:::notes
- einfacher dargestellt
:::

---

### Gesetze


- `p.map(x=>x)` &#8801; `p`
- `p.map(x=>g(f(x)))` &#8801; `p.map(f).map(g)`

:::notes
- erwähnen, Falls die *Lambda-Polizei* zusieht
- ist in C# schwierig zu garantieren - sehen wir einfach darüber weg
:::

---

### andere "Funktoren"

- `IEnumerable` / `Select`
- `Task`
- ...

:::notes
- Funktoren geschafft
- nächster Halt Monaden!
:::

# Monaden

## `andThen` 

`P<A>` &rarr; (`a` &rarr; `P<B>`) &rarr; `P<B>`

:::notes
- gibt auch Bilder im Buch - *pädagogisch hier fragwürdig*
- ähnlich Funktor
- können ins Ergebnis schauen und entscheiden, wie es weiter geht
- erklären wie der Ergebnis-Parser funktioniert
:::

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

:::notes
- Erklären
- C# hat *syntactic Sugar* dafür ...
:::

## LINQ*ify*

:::notes
- versteckt/vereinfacht Verknüpfungen mit `andThen`
- müssen uns nicht mehr um den `input` kümmern
- etwas blöde benannt ~> unsere DSL ist etwas anders als SQL
:::

## Beispiel `ChainLeft1`

**Ziel:** `3 + 4 + 5 = (3 + 4) + 5 = 7 + 5 = 12`

```
<expr> ::= <operand> 
         | <operand> operator <expr>
```

:::notes
- **kompliziertestes Beispiel** - keine Sorge
- soll Kette (*Chain*) von durch Operatoren getrennte Operanden parsen und falten
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

:::notes
- anhand Beispiel davor erklären
- gerne hin/her springen
- **übrigens:** Performance hier egal
:::

## Select

```csharp
Parser<TResult> Select<TSource, TResult>(
   this Parser<TSource> source, 
   Func<TSource, TResult> selector)
{
  return source.Map(selector);
}
```

:::notes
- damit das funktioniert brauchen wir
- `Select` = `Map`
- und ...
:::

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

:::notes
- `SelectMany` ~ `andThen`
- schöne Übung
:::


# Beispiel *Rechner*

## DEMO

```shell
$ dotnet run
input? 5+5*5
30
input? (5+5)*5
50
input? 2-2-2
-2
input? Hallo
parse error
input? 
```

## die Grammatik

```
<expr>   ::= <term>    | <term> ("+"|"-") <expr>
<term>   ::= <factor>  | <factor>  ("*"|"/") <term>
<factor> ::= zahl      | "(" <expr> ")"

zahl   = [0|1|..|9]+
```

:::notes
- kurz erklären
- nur für Namen wichtig
:::

## Leerzeichen ignorieren

```csharp
var spacesP = Parsers
   .Char(Char.IsWhiteSpace)
   .Many()
   .Map(_ => Unit.Value);
```

:::notes
- `Unit` weil `void` kein Typ ist - hat einfach nur einen Wert `Unit.Value`
:::

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

:::notes
- `Many1` muss mindestens einen Wert liefern
- `TryMap` geht schief, wenn `TryParse` `false` liefert
- `LeftOf` Kombinator, der zwei Parser nacheinander benutzt und nur das Ergebnis des ersten zurück gibt
:::

## Operatoren

```csharp
Parser<Func<double, double, double>> 
   OperatorP( char symbol, 
              Func<double, double, double> operation )
      => Parsers.Char(symbol).Map(_ => operation);

var strichOperatorP =
  OperatorP('+', Add)
  .Or(OperatorP('-', Subtract))
  .LeftOf(spacesP);

var punktOperator = ...
```

:::notes
- Achtung: `OperatorP` liefert einen Parser, der einen Funktionswert zurück gibt
:::

## Expression

```csharp
var expressionRef = Parsers.CreateForwardRef<double>();

var factorP = expressionRef.Parser
  .Between(Parsers.Char('('), Parsers.Char(')'))
  .Or(zahlP);

var termP = factorP
   .ChainLeft1(punktOperator);

expressionRef.SetRef(
   termP
   .ChainLeft1(strichOperator));
```

:::notes
- `CreateForwardRef` technisch - zunächst ignorieren
- `Between` ist weiterer Kombinator
- `LeftOf(spaceP)` weggelassen
- benötigt, weil hier eine gegenseitige Rekursion vorliegt - führt leicht zu Stackoverflow-Exs
:::

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

## Fragen / Antworten?

## Vielen Dank

- **Slides/Demo** [github.com/CarstenKoenig/DDF2019](https://github.com/CarstenKoenig/DDF2019)
- **Twitter** @CarstenK_Dev
