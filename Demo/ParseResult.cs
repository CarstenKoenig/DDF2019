using System;

namespace ParserComb
{
  public abstract class ParseResult<T>
  {
    public abstract Tres Match<Tres>(Func<T, string, Tres> withSuccess, Func<Tres> onError);
    public void Match(Action<T, string> withSuccess, Action onError)
    {
      Match(
        (value, remaining) => { withSuccess(value, remaining); return 0; },
        () => { onError(); return -1; });
    }
    public bool IsSuccess => Match((val, rem) => true, () => false);

    public ParseResult<Tres> Map<Tres>(Func<T, Tres> map)
    {
      return Match(
        (value, remaining) => map(value).Success(remaining),
        () => ParseResult.Fail<Tres>());
    }
  }

  public class SuccessResult<T> : ParseResult<T>
  {
    public SuccessResult(T value, string remainingInput)
    {
      Value = value;
      RemainingInput = remainingInput;
    }
    public T Value { get; private set; }
    public string RemainingInput { get; private set; }
    public override Tres Match<Tres>(Func<T, string, Tres> withSuccess, Func<Tres> onError)
    {
      return withSuccess(Value, RemainingInput);
    }
  }
  public class FailureResult<T> : ParseResult<T>
  {
    public FailureResult()
    {
    }
    public override Tres Match<Tres>(Func<T, string, Tres> withSuccess, Func<Tres> onError)
    {
      return onError();
    }
  }

  public static class ParseResult
  {
    public static ParseResult<Tres> Select<T, Tres>(ParseResult<T> result, Func<T, Tres> map)
    {
      return result.Map(map);
    }

    public static ParseResult<T> Success<T>(this T value, string remaining)
    {
      return new SuccessResult<T>(value, remaining);
    }

    public static ParseResult<T> Fail<T>()
    {
      return new FailureResult<T>();
    }
  }
}