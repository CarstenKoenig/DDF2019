using System;
using System.Collections.Generic;
using System.Linq;
using ParserComb.Combinators;

namespace ParserComb
{
  class Program
  {
    static void Main(string[] args)
    {
      var parser = ExpressionP();

      while (true)
      {
        Console.Write("input? ");

        var input = Console.ReadLine();
        var result = parser.Parse(input);

        result.Match(
                (value, _) => System.Console.WriteLine(value),
                () => System.Console.WriteLine("parse error"));
      }
    }

    private static Parser<double> ExpressionP()
    {
      var spacesP =
        Parsers.Char(Char.IsWhiteSpace).Many().Map(_ => Unit.Value);

      var doubleP =
        Parsers
          .Char(Char.IsDigit)
          .Many1()
          .TryMap((IEnumerable<char> chrs, out double output) => Double.TryParse(new String(chrs.ToArray()), out output))
          .LeftOf(spacesP);

      var addOpsP =
        Parsers.Char('+').Map<Unit, Func<double, double, double>>(_ => ((a, b) => a + b))
        .Or(Parsers.Char('-').Map<Unit, Func<double, double, double>>(_ => ((a, b) => a - b)))
        .LeftOf(spacesP);

      var multOpsP =
        Parsers.Char('*').Map<Unit, Func<double, double, double>>(_ => ((a, b) => a * b))
        .Or(Parsers.Char('/').Map<Unit, Func<double, double, double>>(_ => ((a, b) => a / b)))
        .LeftOf(spacesP);

      var expressionRef = Parsers.CreateForwardRef<double>();

      var valueP = expressionRef.Parser
        .Between(Parsers.Char('(').LeftOf(spacesP), Parsers.Char(')').LeftOf(spacesP))
        .Or(doubleP);

      var multExprP =
        valueP.ChainLeft1(multOpsP);

      expressionRef.SetRef(multExprP.ChainLeft1(addOpsP).RightOf(spacesP));
      return expressionRef.Parser;
    }
  }
}
