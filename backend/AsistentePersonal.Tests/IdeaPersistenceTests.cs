using AsistentePersonal.Data;
using AsistentePersonal.Models;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Tests;

public class IdeaPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Idea_KeepsItsTagsInTheSameOrder()
    {
        await using (var writer = await TestDatabase.CreateEmptyContextAsync())
        {
            AddOwner(writer);
            writer.Ideas.Add(new Idea
            {
                UserId = User.OwnerId,
                Text = "Una app para medir el sueño",
                Tags = ["app", "sueño"],
                CreatedAt = Now,
                UpdatedAt = Now,
            });
            await writer.SaveChangesAsync();
        }

        await using var reader = await TestDatabase.OpenAsync();
        var saved = await reader.Ideas.SingleAsync();

        Assert.Equal(new[] { "app", "sueño" }, saved.Tags);
    }

    [Fact]
    public async Task Idea_VersionChangesEveryTimeTheIdeaIsWritten()
    {
        await using (var writer = await TestDatabase.CreateEmptyContextAsync())
        {
            AddOwner(writer);
            writer.Ideas.Add(new Idea { UserId = User.OwnerId, Text = "Primera", CreatedAt = Now, UpdatedAt = Now });
            await writer.SaveChangesAsync();
        }

        uint firstVersion;
        await using (var reader = await TestDatabase.OpenAsync())
        {
            firstVersion = (await reader.Ideas.SingleAsync()).Version;
        }

        await using (var editor = await TestDatabase.OpenAsync())
        {
            var idea = await editor.Ideas.SingleAsync();
            idea.Text = "Segunda";
            await editor.SaveChangesAsync();
        }

        await using var check = await TestDatabase.OpenAsync();
        var edited = await check.Ideas.SingleAsync();

        Assert.Equal("Segunda", edited.Text);
        Assert.NotEqual(firstVersion, edited.Version);
    }

    private static void AddOwner(AppDbContext context)
    {
        context.Users.Add(new User
        {
            Id = User.OwnerId,
            Email = "dueno@ejemplo.com",
            PasswordHash = "hash-de-prueba",
            CreatedAt = Now,
        });
    }
}
