using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AsistentePersonal.Tests;

// Runs an action once, right before EF Core saves. Tests use it to simulate another request
// that wins a race (deletes the habit, or takes the same name) between our checks and our save.
public sealed class BeforeSaveInterceptor(Func<Task> action) : SaveChangesInterceptor
{
    private bool _done;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!_done)
        {
            _done = true;
            await action();
        }

        return result;
    }
}
