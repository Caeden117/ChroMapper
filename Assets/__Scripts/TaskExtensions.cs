using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;


public static class TaskExtensions
{
    // Used for waiting for tasks
    // C# 8 has IAsyncEnumerable
    // Reference: https://stackoverflow.com/questions/57207127/using-ienumerator-for-async-operation-on-items
    public static IEnumerator WaitTask(IAsyncResult task)
    {
        while (!task.IsCompleted)
            yield return null;
    }

    public static IEnumerator AsCoroutine(this Task task)
    {
        yield return WaitTask(task);
        var exception = task.Exception;
        if (exception == null) yield break;

        var innerExceptions = exception.InnerExceptions;

        foreach (var innerException in innerExceptions)
        {
            LogInnerExceptions(innerException);
        }
        
        LogInnerExceptions(exception);
        throw exception;
    }

    private static void LogInnerExceptions(Exception e)
    {
        while (true)
        {
            Debug.LogException(e);

            if (e.InnerException == e || e.InnerException == null) break;
            e = e.InnerException;
        }
    }
}
