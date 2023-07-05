using System;
using System.Collections;

namespace inscryption_multiplayer
{
    public static class Utils
    {
        public static IEnumerator CallbackRoutine(IEnumerator coroutine, Action callback)
        {
            yield return coroutine;
            callback();
        }

        public static IEnumerator JoinCoroutines(params IEnumerator[] coroutines)
        {
            foreach (var coroutine in coroutines)
                yield return coroutine;
        }
    }
}