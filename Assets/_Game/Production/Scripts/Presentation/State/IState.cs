using System.Collections;

namespace Blobs.Presentation
{
    public interface IState<TContext>
    {
        BlobAnimationState State { get; }

        void Enter(TContext context);
        void Exit(TContext context);
        IEnumerator Update(TContext context);

    }
}