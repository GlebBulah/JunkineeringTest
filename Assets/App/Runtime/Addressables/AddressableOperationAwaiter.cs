using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace JunkineeringTest.Runtime.Addressables
{
    internal static class AddressableOperationAwaiter
    {
        public static async Task<AsyncOperationHandle<T>> AwaitAsync<T>(this AsyncOperationHandle<T> handle, CancellationToken cancellationToken)
        {
            EnsureValid(handle);

            if (!handle.IsDone)
            {
                await CreateCompletionTask(handle, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return handle;
        }

        public static async Task<AsyncOperationHandle> AwaitAsync(this AsyncOperationHandle handle, CancellationToken cancellationToken)
        {
            EnsureValid(handle);

            if (!handle.IsDone)
            {
                await CreateCompletionTask(handle, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return handle;
        }

        private static Task CreateCompletionTask<T>(AsyncOperationHandle<T> handle, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration cancellationRegistration = default;

            void Complete(AsyncOperationHandle<T> completedHandle)
            {
                cancellationRegistration.Dispose();

                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                completion.TrySetResult(null);
            }

            handle.Completed += Complete;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.Register(() =>
                {
                    handle.Completed -= Complete;
                    completion.TrySetCanceled(cancellationToken);
                });
            }

            if (handle.IsDone)
            {
                handle.Completed -= Complete;
                Complete(handle);
            }

            return completion.Task;
        }

        private static Task CreateCompletionTask(AsyncOperationHandle handle, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration cancellationRegistration = default;

            void Complete(AsyncOperationHandle completedHandle)
            {
                cancellationRegistration.Dispose();

                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                completion.TrySetResult(null);
            }

            handle.Completed += Complete;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.Register(() =>
                {
                    handle.Completed -= Complete;
                    completion.TrySetCanceled(cancellationToken);
                });
            }

            if (handle.IsDone)
            {
                handle.Completed -= Complete;
                Complete(handle);
            }

            return completion.Task;
        }

        private static void EnsureValid<T>(AsyncOperationHandle<T> handle)
        {
            if (!handle.IsValid())
            {
                throw new InvalidOperationException("Addressables returned an invalid async operation handle.");
            }
        }

        private static void EnsureValid(AsyncOperationHandle handle)
        {
            if (!handle.IsValid())
            {
                throw new InvalidOperationException("Addressables returned an invalid async operation handle.");
            }
        }
    }
}
