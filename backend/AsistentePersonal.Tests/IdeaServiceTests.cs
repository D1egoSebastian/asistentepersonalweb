using AsistentePersonal.Data;
using AsistentePersonal.Models;
using AsistentePersonal.Services;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Tests;

public class IdeaServiceTests
{
    private const int UserId = 1;
    private const int OtherUserId = 2;

    // Fixed instants so the tests never depend on the real clock.
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo OwnerZone = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");

    // ---- T08: create ----

    [Fact]
    public async Task Create_StoresAnActiveIdea_WithEqualDates()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();

        var result = await new IdeaService(db).CreateAsync(UserId, "  Una app  ", new[] { "app" }, Now);

        Assert.Equal(ResultStatus.Ok, result.Status);
        var idea = await db.Ideas.SingleAsync();
        Assert.Equal("Una app", idea.Text);
        Assert.Equal(Now, idea.CreatedAt);
        Assert.Equal(Now, idea.UpdatedAt);
        Assert.Null(idea.ArchivedAt);
    }

    [Fact]
    public async Task Create_RejectsEmptyText_AndSavesNothing()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();

        var result = await new IdeaService(db).CreateAsync(UserId, "  \n ", null, Now);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(0, await db.Ideas.CountAsync());
    }

    [Fact]
    public async Task Create_UsesTheExistingSpelling_OfATagIgnoringCase()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await AddAsync(service, UserId, "Una app", Now, "app");

        var second = await AddAsync(service, UserId, "Otra", Now.AddMinutes(1), "App");

        Assert.Equal(new[] { "app" }, second.Tags);
    }

    // ---- T09: list order, summary and created date ----

    [Fact]
    public async Task List_ShowsTheNewestIdeaFirst()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await AddAsync(service, UserId, "Primera", Now.AddDays(-2));
        await AddAsync(service, UserId, "Segunda", Now.AddDays(-1));
        await AddAsync(service, UserId, "Tercera", Now);

        var list = await service.ListAsync(UserId, archived: false, search: null, tag: null, OwnerZone);

        Assert.Equal(new[] { "Tercera", "Segunda", "Primera" }, list.Select(i => i.Summary));
    }

    [Fact]
    public async Task List_SeparatesActiveAndArchivedIdeas()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await AddAsync(service, UserId, "Activa", Now.AddDays(-1));
        var archived = await AddAsync(service, UserId, "Archivada", Now);
        await service.ArchiveAsync(UserId, archived.Id, Now);

        var active = await service.ListAsync(UserId, archived: false, search: null, tag: null, OwnerZone);
        var archivedList = await service.ListAsync(UserId, archived: true, search: null, tag: null, OwnerZone);

        Assert.Equal(new[] { "Activa" }, active.Select(i => i.Summary));
        Assert.Equal(new[] { "Archivada" }, archivedList.Select(i => i.Summary));
    }

    [Fact]
    public async Task Editing_OrArchiving_DoesNotChangeTheOrder()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var older = await AddAsync(service, UserId, "Vieja", Now.AddDays(-1));
        await AddAsync(service, UserId, "Nueva", Now);

        var version = (await service.GetAsync(UserId, older.Id)).Value!.Version;
        await service.UpdateAsync(UserId, older.Id, "Vieja editada", null, version, Now.AddDays(1));
        await service.ArchiveAsync(UserId, older.Id, Now.AddDays(1));
        await service.RestoreAsync(UserId, older.Id);

        var list = await service.ListAsync(UserId, archived: false, search: null, tag: null, OwnerZone);

        Assert.Equal(new[] { "Nueva", "Vieja editada" }, list.Select(i => i.Summary));
    }

    [Fact]
    public async Task List_ReturnsTheSummary_NotTheFullText()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var text = new string('a', 200);
        await service.CreateAsync(UserId, text, null, Now);

        var item = Assert.Single(await service.ListAsync(UserId, archived: false, search: null, tag: null, OwnerZone));

        Assert.Equal(new string('a', 120) + "…", item.Summary);
    }

    [Fact]
    public async Task List_ComputesTheCreatedDate_InTheOwnerZone()
    {
        // 03:00 UTC on the 27th is still the 26th in La Paz (UTC-4).
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await service.CreateAsync(UserId, "Madrugada", null, new DateTimeOffset(2026, 10, 27, 3, 0, 0, TimeSpan.Zero));

        var item = Assert.Single(await service.ListAsync(UserId, archived: false, search: null, tag: null, OwnerZone));

        Assert.Equal(new DateOnly(2026, 10, 26), item.CreatedDate);
    }

    // ---- T10: search and tag filter ----

    [Fact]
    public async Task Search_IgnoresCaseAndAccents()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await AddAsync(service, UserId, "Canción de cuna", Now);
        await AddAsync(service, UserId, "Cancion simple", Now.AddMinutes(1));

        var found = await Search(service, "CANCION");
        var foundWithAccent = await Search(service, "canción");

        Assert.Equal(2, found.Count);
        Assert.Equal(2, foundWithAccent.Count);
    }

    [Fact]
    public async Task Search_TreatsPercentAndUnderscoreAsPlainText()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await AddAsync(service, UserId, "Llegué al 100% del plan", Now);
        await AddAsync(service, UserId, "Llegué al 1000 del plan", Now.AddMinutes(1));
        await AddAsync(service, UserId, "Probar a_b", Now.AddMinutes(2));
        await AddAsync(service, UserId, "Probar axb", Now.AddMinutes(3));

        var percent = await Search(service, "%");
        var underscore = await Search(service, "_");

        Assert.Equal(new[] { "Llegué al 100% del plan" }, percent.Select(i => i.Summary));
        Assert.Equal(new[] { "Probar a_b" }, underscore.Select(i => i.Summary));
    }

    [Fact]
    public async Task Search_IgnoresABlankSearch()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await AddAsync(service, UserId, "Una", Now);
        await AddAsync(service, UserId, "Otra", Now.AddMinutes(1));

        var list = await Search(service, "   ");

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task Search_MatchesEveryWord_InAnyOrder()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await AddAsync(service, UserId, "Una app para medir el sueño", Now);

        Assert.Single(await Search(service, "sueño app"));
        Assert.Empty(await Search(service, "sueño gato"));
    }

    [Fact]
    public async Task Search_FindsAnIdeaByItsTag()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await AddAsync(service, UserId, "Dormir mejor", Now, "salud");

        Assert.Single(await Search(service, "salud"));
    }

    [Fact]
    public async Task Search_MixesTextAndTag()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await AddAsync(service, UserId, "Dormir mejor", Now, "salud");

        Assert.Single(await Search(service, "dormir salud"));
        Assert.Empty(await Search(service, "dormir libros"));
    }

    [Fact]
    public async Task Filter_ByTag_IncludesBothSpellings()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await AddAsync(service, UserId, "Una", Now, "App");
        await AddAsync(service, UserId, "Otra", Now.AddMinutes(1), "app");
        await AddAsync(service, UserId, "Tercera", Now.AddMinutes(2), "cine");

        var list = await service.ListAsync(UserId, archived: false, search: null, tag: "app", OwnerZone);

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task Filter_CombinesWithTheSearch()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await AddAsync(service, UserId, "Dormir mejor", Now, "salud");
        await AddAsync(service, UserId, "Dormir temprano", Now.AddMinutes(1), "cine");

        var list = await service.ListAsync(UserId, archived: false, search: "dormir", tag: "salud", OwnerZone);

        Assert.Equal(new[] { "Dormir mejor" }, list.Select(i => i.Summary));
    }

    // ---- T11: update ----

    [Fact]
    public async Task Update_ChangesTextAndTags_AndKeepsCreatedAt()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var idea = await AddAsync(service, UserId, "Vieja", Now, "app");

        var result = await service.UpdateAsync(UserId, idea.Id, "Nueva", new[] { "libros" }, idea.Version, Now.AddDays(1));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Nueva", result.Value!.Text);
        Assert.Equal(new[] { "libros" }, result.Value.Tags);
        Assert.Equal(Now, result.Value.CreatedAt);
        Assert.Equal(Now.AddDays(1), result.Value.UpdatedAt);
    }

    [Fact]
    public async Task Update_WithoutChanges_DoesNotTouchUpdatedAt()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var idea = await AddAsync(service, UserId, "Igual", Now, "app");

        var result = await service.UpdateAsync(UserId, idea.Id, "Igual", new[] { "app" }, idea.Version, Now.AddDays(1));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(Now, result.Value!.UpdatedAt);
    }

    [Fact]
    public async Task Update_WithInvalidData_LeavesTheIdeaUnchanged()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var idea = await AddAsync(service, UserId, "Original", Now);

        var result = await service.UpdateAsync(UserId, idea.Id, "   ", null, idea.Version, Now.AddDays(1));

        Assert.Equal(ResultStatus.Invalid, result.Status);
        await using var reader = await TestDatabase.OpenAsync();
        Assert.Equal("Original", (await reader.Ideas.SingleAsync()).Text);
    }

    [Fact]
    public async Task Update_OfAnArchivedIdea_ReturnsConflict()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var idea = await AddAsync(service, UserId, "Archivada", Now);
        await service.ArchiveAsync(UserId, idea.Id, Now);
        var version = (await service.GetAsync(UserId, idea.Id)).Value!.Version;

        var result = await service.UpdateAsync(UserId, idea.Id, "Cambio", null, version, Now.AddDays(1));

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(IdeaService.ArchivedMessage, result.Message);
    }

    [Fact]
    public async Task Update_WithAnOldVersion_ReturnsConflict_AndKeepsTheNewerText()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var idea = await AddAsync(service, UserId, "Original", Now);
        var oldVersion = idea.Version;
        await service.UpdateAsync(UserId, idea.Id, "Primera edición", null, oldVersion, Now.AddDays(1));

        var result = await service.UpdateAsync(UserId, idea.Id, "Segunda edición", null, oldVersion, Now.AddDays(2));

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(IdeaService.ChangedMessage, result.Message);
        await using var reader = await TestDatabase.OpenAsync();
        Assert.Equal("Primera edición", (await reader.Ideas.SingleAsync()).Text);
    }

    [Fact]
    public async Task Update_WhenAnotherTabSavesFirst_ReturnsConflict_NotNotFound()
    {
        await SeedOwnersAsync();
        var idea = await SeedIdeaAsync("Original");
        var version = idea.Version;

        await using var db = await TestDatabase.OpenAsync(new BeforeSaveInterceptor(async () =>
        {
            await using var otherTab = await TestDatabase.OpenAsync();
            var other = await otherTab.Ideas.SingleAsync();
            other.Text = "Editada en otra pestaña";
            await otherTab.SaveChangesAsync();
        }));

        var result = await new IdeaService(db).UpdateAsync(UserId, idea.Id, "Mi edición", null, version, Now.AddDays(1));

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(IdeaService.ChangedMessage, result.Message);
        await using var reader = await TestDatabase.OpenAsync();
        Assert.Equal("Editada en otra pestaña", (await reader.Ideas.SingleAsync()).Text);
    }

    [Fact]
    public async Task Update_WhenDeletedBeforeSave_ReturnsNotFound()
    {
        await SeedOwnersAsync();
        var idea = await SeedIdeaAsync("Original");
        var version = idea.Version;

        await using var db = await TestDatabase.OpenAsync(new BeforeSaveInterceptor(async () =>
        {
            await using var otherTab = await TestDatabase.OpenAsync();
            otherTab.Ideas.Remove(await otherTab.Ideas.SingleAsync());
            await otherTab.SaveChangesAsync();
        }));

        var result = await new IdeaService(db).UpdateAsync(UserId, idea.Id, "Mi edición", null, version, Now.AddDays(1));

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal(IdeaService.NotFoundMessage, result.Message);
    }

    // ---- T12: get, archive, restore and delete ----

    [Fact]
    public async Task Get_OpensAnArchivedIdea_ForReading()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var idea = await AddAsync(service, UserId, "Texto completo que se puede leer", Now);
        await service.ArchiveAsync(UserId, idea.Id, Now);

        var result = await service.GetAsync(UserId, idea.Id);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Texto completo que se puede leer", result.Value!.Text);
        Assert.NotNull(result.Value.ArchivedAt);
    }

    [Fact]
    public async Task Archive_MovesTheIdeaOutOfTheActiveList()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var idea = await AddAsync(service, UserId, "Para archivar", Now);

        var result = await service.ArchiveAsync(UserId, idea.Id, Now.AddDays(1));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(await service.ListAsync(UserId, archived: false, search: null, tag: null, OwnerZone));
        Assert.Single(await service.ListAsync(UserId, archived: true, search: null, tag: null, OwnerZone));
    }

    [Fact]
    public async Task Archive_AfterAnotherTabEdited_ArchivesTheLatestText()
    {
        await SeedOwnersAsync();
        var idea = await SeedIdeaAsync("Original");

        await using (var otherTab = await TestDatabase.OpenAsync())
        {
            var other = await otherTab.Ideas.SingleAsync();
            other.Text = "Editada en otra pestaña";
            await otherTab.SaveChangesAsync();
        }

        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);

        var result = await service.ArchiveAsync(UserId, idea.Id, Now.AddDays(1));

        Assert.Equal(ResultStatus.Ok, result.Status);
        var archived = await service.GetAsync(UserId, idea.Id);
        Assert.Equal("Editada en otra pestaña", archived.Value!.Text);
        Assert.NotNull(archived.Value.ArchivedAt);
    }

    [Fact]
    public async Task Restore_KeepsTheTextTagsAndDates()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var idea = await AddAsync(service, UserId, "Conservar", Now, "app", "salud");
        await service.ArchiveAsync(UserId, idea.Id, Now.AddDays(1));

        var result = await service.RestoreAsync(UserId, idea.Id);

        Assert.Equal(ResultStatus.Ok, result.Status);
        var restored = await db.Ideas.SingleAsync();
        Assert.Null(restored.ArchivedAt);
        Assert.Equal("Conservar", restored.Text);
        Assert.Equal(new[] { "app", "salud" }, restored.Tags);
        Assert.Equal(Now, restored.CreatedAt);
        Assert.Equal(Now, restored.UpdatedAt);
    }

    [Fact]
    public async Task Archive_AndRestore_ChangeTheVersion()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var idea = await AddAsync(service, UserId, "Versiones", Now);
        var first = idea.Version;

        await service.ArchiveAsync(UserId, idea.Id, Now.AddDays(1));
        var archivedVersion = (await service.GetAsync(UserId, idea.Id)).Value!.Version;
        await service.RestoreAsync(UserId, idea.Id);
        var restoredVersion = (await service.GetAsync(UserId, idea.Id)).Value!.Version;

        Assert.NotEqual(first, archivedVersion);
        Assert.NotEqual(archivedVersion, restoredVersion);
    }

    [Fact]
    public async Task Delete_RemovesTheIdea()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var idea = await AddAsync(service, UserId, "Eliminar", Now);

        var result = await service.DeleteAsync(UserId, idea.Id);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(0, await db.Ideas.CountAsync());
    }

    [Fact]
    public async Task GetArchiveRestoreUpdateAndDelete_OfAMissingIdea_ReturnNotFound()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        const int missingId = 999;

        Assert.Equal(ResultStatus.NotFound, (await service.GetAsync(UserId, missingId)).Status);
        Assert.Equal(ResultStatus.NotFound, (await service.ArchiveAsync(UserId, missingId, Now)).Status);
        Assert.Equal(ResultStatus.NotFound, (await service.RestoreAsync(UserId, missingId)).Status);
        Assert.Equal(ResultStatus.NotFound, (await service.DeleteAsync(UserId, missingId)).Status);
        Assert.Equal(ResultStatus.NotFound, (await service.UpdateAsync(UserId, missingId, "x", null, 0, Now)).Status);
    }

    // ---- T13: tag counts and ownership ----

    [Fact]
    public async Task TagCounts_CountActiveAndArchivedIdeas()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        await AddAsync(service, UserId, "Activa", Now, "app", "salud");
        var archived = await AddAsync(service, UserId, "Archivada", Now.AddMinutes(1), "app");
        await service.ArchiveAsync(UserId, archived.Id, Now);

        var counts = await service.TagCountsAsync(UserId);

        Assert.Contains(counts, c => c.Name == "app" && c.ActiveCount == 1 && c.ArchivedCount == 1);
        Assert.Contains(counts, c => c.Name == "salud" && c.ActiveCount == 1 && c.ArchivedCount == 0);
    }

    [Fact]
    public async Task TagCounts_MergeCaseVariants_UnderTheMostUsedSpelling()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        // The service always saves the existing spelling, so case variants only appear when two tabs save
        // at the same time (spec CA-2.11). Insert them directly to model that.
        db.Ideas.AddRange(
            new Idea { UserId = UserId, Text = "Una", Tags = ["App"], CreatedAt = Now, UpdatedAt = Now },
            new Idea { UserId = UserId, Text = "Dos", Tags = ["app"], CreatedAt = Now, UpdatedAt = Now },
            new Idea { UserId = UserId, Text = "Tres", Tags = ["app"], CreatedAt = Now, UpdatedAt = Now });
        await db.SaveChangesAsync();

        var counts = await service.TagCountsAsync(UserId);

        var only = Assert.Single(counts);
        Assert.Equal("app", only.Name);
        Assert.Equal(3, only.ActiveCount);
    }

    [Fact]
    public async Task TagCounts_DropATag_WhenNoIdeaUsesIt()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var idea = await AddAsync(service, UserId, "Única", Now, "viejo");

        await service.DeleteAsync(UserId, idea.Id);

        Assert.Empty(await service.TagCountsAsync(UserId));
    }

    [Fact]
    public async Task OtherOwner_CannotSeeOrChangeTheseIdeas()
    {
        await SeedOwnersAsync();
        await using var db = await TestDatabase.OpenAsync();
        var service = new IdeaService(db);
        var idea = await AddAsync(service, UserId, "Privada", Now, "secreto");

        Assert.Empty(await service.ListAsync(OtherUserId, archived: false, search: null, tag: null, OwnerZone));
        Assert.Empty(await service.TagCountsAsync(OtherUserId));
        Assert.Equal(ResultStatus.NotFound, (await service.GetAsync(OtherUserId, idea.Id)).Status);
        Assert.Equal(ResultStatus.NotFound, (await service.UpdateAsync(OtherUserId, idea.Id, "x", null, idea.Version, Now)).Status);
        Assert.Equal(ResultStatus.NotFound, (await service.ArchiveAsync(OtherUserId, idea.Id, Now)).Status);
        Assert.Equal(ResultStatus.NotFound, (await service.DeleteAsync(OtherUserId, idea.Id)).Status);

        var stillThere = await db.Ideas.SingleAsync();
        Assert.Equal("Privada", stillThere.Text);
        Assert.Null(stillThere.ArchivedAt);
    }

    // ---- helpers ----

    private static User Owner(int id) => new()
    {
        Id = id,
        Email = $"dueno{id}@ejemplo.com",
        PasswordHash = "hash-de-prueba",
        CreatedAt = Now,
    };

    // Empties the test database and adds two owners, so ideas can belong to either one.
    private static async Task SeedOwnersAsync()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        db.Users.Add(Owner(UserId));
        db.Users.Add(Owner(OtherUserId));
        await db.SaveChangesAsync();
    }

    private static async Task<Idea> SeedIdeaAsync(string text)
    {
        await using var db = await TestDatabase.OpenAsync();
        var idea = new Idea { UserId = UserId, Text = text, CreatedAt = Now, UpdatedAt = Now };
        db.Ideas.Add(idea);
        await db.SaveChangesAsync();
        return idea;
    }

    private static async Task<Idea> AddAsync(IdeaService service, int userId, string text, DateTimeOffset at, params string[] tags)
    {
        var result = await service.CreateAsync(userId, text, tags.Length == 0 ? null : tags, at);
        return result.Value!;
    }

    private static Task<IReadOnlyList<IdeaListItem>> Search(IdeaService service, string search) =>
        service.ListAsync(UserId, archived: false, search: search, tag: null, OwnerZone);
}
