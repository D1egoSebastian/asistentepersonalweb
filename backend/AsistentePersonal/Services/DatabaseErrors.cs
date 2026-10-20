using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AsistentePersonal.Services;

public static class DatabaseErrors
{
    // True when PostgreSQL rejected the write because a unique index or primary key already had that value.
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    // True when PostgreSQL rejected the write because the referenced row (for example, the habit) no longer exists.
    public static bool IsForeignKeyViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
}
