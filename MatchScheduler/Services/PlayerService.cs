using CathermanCore.EntityFramework;
using MatchScheduler.Data;
using MatchScheduler.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchScheduler.Services;

public class PlayerService
{
    public async Task<List<Player>> GetAllAsync()
    {
        using var db = AppDatabase.CreateContext();
        return await db.Players.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<Player> AddAsync(Player player)
    {
        using var db = AppDatabase.CreateContext();
        db.Players.Add(player);
        await db.SaveChangesAsync();
        return player;
    }

    /// <summary>Copies the editable fields onto the stored player and returns the saved copy.</summary>
    public async Task<Player> UpdateAsync(Player edited)
    {
        using var db = AppDatabase.CreateContext();
        var stored = await db.Players.SingleAsync(p => p.Id == edited.Id);

        stored.Name = edited.Name;
        stored.DesiredPosition = edited.DesiredPosition;
        stored.PhoneNumber = edited.PhoneNumber;
        stored.Email = edited.Email;

        await db.SaveChangesAsync();
        return stored;
    }

    /// <summary>Soft delete: the player disappears from lists, saved schedules keep their name.</summary>
    public async Task DeleteAsync(int id)
    {
        using var db = AppDatabase.CreateContext();
        await db.DeleteAsync<Player>(id);
    }
}
