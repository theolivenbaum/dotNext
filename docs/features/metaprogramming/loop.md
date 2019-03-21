Loops
====
Metaprogramming library provides construction of `for`, `foreach` and `while` loops. The scope object available inside of builder block of code provides two special parameterless methods for continuation and abortion of loop execution: `Continue()` and `Break()`. The same methods are available from any other scope object with one exception: these methods have single parameter receiving scope object of the loop to be aborted or continued. This feature allows to leave outer loop from inner loop. There is no equivalent instruction in C# except unconditional control transfer using `goto`.

# foreach Loop
`foreach` statement may accept any expression of type implementing `System.Collections.IEnumerable` or `System.Collections.Generic.IEnumerable<T>` interface, or having public instance parameterless method `GetEnumerator()` which return type has the public instance property `Current` and public instance parameterless method `MoveNext()`.

```csharp
using System;
using DotNext.Metaprogramming;

LambdaBuilder<Action<string>>.Build(fun => 
{
    fun.ForEach(fun.Parameters[0], loop =>
    {
        loop.Call(typeof(Console).GetMethod(nameof(Console.WriteLine), new[]{ typeof(char) }), loop.Element);
    });
});

//generated code is

new Action<string>(str => 
{
    foreach(char ch in str)
        Console.WriteLine(ch);
});
```

`Element` property of the loop statement builder provides access to loop variable.

# while Loop
`while` loop statement is supported in two forms: `while-do` or `do-while`. Both forms are representing regular `while` loop existing in most popular languages such as C#, C++ and C. 

```csharp
using System;
using DotNext.Metaprogramming;

LambdaBuilder<Fun<long, long>>(fun => 
{
    UniversalExpression arg = fun.Parameters[0];
    fun.Assign(fun.Result, 1L);  //result = 1L;
    fun.While(arg > 1L, whileBlock => {
        whileBlock.Assign(fun.Result, arg-- * fun.Result);  //result *= arg--;
    });
});

//generated code is

new Func<long, long>(arg => 
{
    var result = 1L;
    while(arg > 1L)
        result *= arg--;
    return result;
});
```

`do-while` loop can be constructed using `DoWhile` method.

# for Loop


# Plain Loop
Plain loop is similar to `while(true)` loop and doesn't have built-in loop control tools. Developer need to control loop execution by calling `Continue()` and `Break` manually.

```csharp
LambdaBuilder<Fun<long, long>>(fun => 
{
    UniversalExpression arg = fun.Parameters[0];
    fun.Assign(fun.Result, 1L);  //result = 1L;
    fun.Loop(loopBlock => 
    {
        loopBlock.If(arg > 1L)
            .Then(thenBlock => thenBlock.Assign(fun.Result, arg-- * fun.Result))
            .Else(elseBlock => elseBlock.Break(loopBlock))  //break;
            .End();
    });
});

//generated code is
new Func<long, long>(arg => 
{
    var result = 1L;
    while(true)
        if(arg > 1L)
            result *= arg--;
        else
            break;
    return true;
});
```