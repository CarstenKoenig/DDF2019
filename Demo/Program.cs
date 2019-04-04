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

    static Parser<double> ExpressionP()
    {
      var spacesP =
        Parsers.Char(Char.IsWhiteSpace).Many().Map(_ => Unit.Value);

      var doubleP =
        Parsers
          .Char(Char.IsDigit)
          .Many1()
          .Map(ToString)
          .TryMap<string, double>(Double.TryParse)
          .LeftOf(spacesP);

      Parser<Func<double, double, double>> OperatorP(char symbol, Func<double, double, double> operation)
        => Parsers.Char(symbol).Map(_ => operation);

      var addOpsP =
        OperatorP('+', Add)
        .Or(OperatorP('-', Subtract))
        .LeftOf(spacesP);

      var multOpsP =
        OperatorP('*', Multiply)
        .Or(OperatorP('/', Divide))
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

    static double Add(double a, double b) => a + b;
    static double Subtract(double a, double b) => a - b;
    static double Multiply(double a, double b) => a * b;
    static double Divide(double a, double b) => a / b;

    static string ToString(IEnumerable<char> chars) => new String(chars.ToArray());

  }
}
