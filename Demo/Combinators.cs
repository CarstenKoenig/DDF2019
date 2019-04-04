using System;
using System.Collections.Generic;

namespace ParserComb.Combinators
{
  public delegate ParseResult<T> Parser<T>(string input);
  public delegate bool TryPattern<T, Tout>(T input, out Tout output);

  public static class Parsers
  {
    public static ParseResult<T> Parse<T>(this Parser<T> p, string input) =>
      p(input);

    public static (Parser<T> Parser, Action<Parser<T>> SetRef) CreateForwardRef<T>()
    {
      object boxed = Fail<T>();
      return (input => ((Parser<T>)boxed)(input), p => boxed = p);
    }

    public static Parser<T> Succeed<T>(T withValue)
    {
      return input => ParseResult.Success(withValue, input);
    }

    public static Parser<T> Fail<T>()
    {
      return input => ParseResult.Fail<T>();
    }

    public static Parser<TRes> Map<T, TRes>(this Parser<T> parser, Func<T, TRes> map)
    {
      return input => parser(input).Map(map);
    }

    public static Parser<TRes> TryMap<T, TRes>(this Parser<T> parser, TryPattern<T, TRes> tryMap)
    {
      return input =>
        parser(input).Match(
          (value, remaining) => tryMap(value, out var result)
                                ? result.Success(remaining)
                                : ParseResult.Fail<TRes>(),
          () => ParseResult.Fail<TRes>()
        );
    }

    public static Parser<TRes> AndThen<T, TRes>(this Parser<T> parser, Func<T, Parser<TRes>> getNext)
    {
      return input =>
        parser(input).Match(
            (value, remaining) => getNext(value)(remaining),
            () => ParseResult.Fail<TRes>());
    }

    public static Parser<char> Char(Predicate<char> isValidChar)
    {
      return input =>
        input.Length == 0 || !isValidChar(input[0])
        ? ParseResult.Fail<char>()
        : ParseResult.Success(input[0], input.Substring(1));
    }

    public static Parser<Unit> Char(char expected)
    {
      return
        Char(found => found == expected)
        .Map(_ => Unit.Value);
    }

    public static Parser<IEnumerable<T>> Many<T>(this Parser<T> parser)
    {
      return input =>
        {
          var results = new List<T>();
          var nextInput = input;
          while (true)
          {
            switch (parser(nextInput))
            {
              case SuccessResult<T> res:
                results.Add(res.Value);
                nextInput = res.RemainingInput;
                break;
              default:
                return results.Success<IEnumerable<T>>(nextInput);
            }
          }
        };
    }

    public static Parser<IEnumerable<T>> Many1<T>(this Parser<T> parser)
    {
      return parser.AndThen(firstRes =>
        Many(parser).Map(more =>
          {
            var results = new List<T>(more);
            results.Insert(0, firstRes);
            return (IEnumerable<T>)results;
          }
        ));
    }

    public static Parser<T> Or<T>(this Parser<T> either, Parser<T> or)
    {
      return input =>
        {
          switch (either(input))
          {
            case SuccessResult<T> success:
              return success;
            default:
              return or(input);
          }
        };
    }

    public static Parser<T> LeftOf<T, Tign>(this Parser<T> p, Parser<Tign> ignoreP)
    {
      return p.AndThen(pVal => ignoreP.Map(_ => pVal));
    }

    public static Parser<T> RightOf<T, Tign>(this Parser<T> p, Parser<Tign> ignoreP)
    {
      return ignoreP.AndThen(_ => p);
    }

    public static Parser<T> Between<T, Tleft, Tright>(this Parser<T> p, Parser<Tleft> left, Parser<Tright> right)
    {
      return p.RightOf(left).LeftOf(right);
    }

    public static Parser<T> ChainLeft1<T>(this Parser<T> operandP, Parser<Func<T, T, T>> operatorP)
    {
      Parser<T> rest(T accum) =>
        (from op in operatorP
         from val in operandP
         from more in rest(op(accum, val))
         select more)
        .Or(Succeed(accum));

      return operandP.AndThen(rest);
    }

    public static Parser<TResult> Select<TSource, TResult>(this Parser<TSource> source, Func<TSource, TResult> selector)
    {
      return source.Map(selector);
    }

    public static Parser<TResult> SelectMany<TSource, TCollection, TResult>(
      this Parser<TSource> source,
      Func<TSource, Parser<TCollection>> collectionSelector,
      Func<TSource, TCollection, TResult> resultSelector)
    {
      return source
         .AndThen(val =>
            collectionSelector(val)
            .Map(col => resultSelector(val, col)));
    }
  }
}