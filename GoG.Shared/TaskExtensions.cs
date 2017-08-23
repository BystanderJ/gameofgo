using System.Threading.Tasks;

namespace GoG.Shared
{
    public static class TaskExtensions
    {
        public static void Forget(this Task task)
        {
            if (task == null)
                throw new System.ArgumentNullException(nameof(task));
        }
    }
}
