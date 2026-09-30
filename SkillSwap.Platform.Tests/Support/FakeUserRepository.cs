using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Repositories;

namespace SkillSwap.Platform.Tests.Support;

public class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];
    private int _nextId = 1;

    public IReadOnlyList<User> Users => _users;

    public Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        TestData.SetId(entity, _nextId++);
        _users.Add(entity);
        return Task.CompletedTask;
    }

    public Task<User?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
    }

    public void Update(User entity)
    {
    }

    public void Remove(User entity)
    {
        _users.Remove(entity);
    }

    public Task<IEnumerable<User>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<User>>(_users.ToList());
    }

    public Task<User?> FindByUsernameAsync(Username username, CancellationToken cancellationToken)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Username == username));
    }

    public Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Email == email));
    }

    public Task<bool> ExistsByUsernameAsync(Username username, CancellationToken cancellationToken)
    {
        return Task.FromResult(_users.Any(u => u.Username == username));
    }

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken)
    {
        return Task.FromResult(_users.Any(u => u.Email == email));
    }
}