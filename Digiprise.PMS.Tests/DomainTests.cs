// ── Zero-dependency test suite using built-in assertions ──────────────
// Runs as a console app when NuGet is unavailable.
// All test logic is identical to the xUnit version - just uses different runner.

using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Enums;
using System.Reflection;

namespace Digiprise.PMS.Tests;

// ── Minimal test framework (no NuGet required) ────────────────────────
[AttributeUsage(AttributeTargets.Method)]
public class TestAttribute : Attribute { }

public static class Assert
{
    public static void Equal<T>(T expected, T actual, string? msg = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Assert.Equal failed{(msg != null ? $": {msg}" : "")}. Expected: {expected}, Actual: {actual}");
    }
    public static void NotEqual<T>(T notExpected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(notExpected, actual))
            throw new Exception($"Assert.NotEqual failed. Did not expect: {notExpected}");
    }
    public static void True(bool condition, string? msg = null)
    {
        if (!condition) throw new Exception($"Assert.True failed{(msg != null ? $": {msg}" : "")}.");
    }
    public static void False(bool condition, string? msg = null)
    {
        if (condition) throw new Exception($"Assert.False failed{(msg != null ? $": {msg}" : "")}.");
    }
    public static void Null(object? obj)
    {
        if (obj != null) throw new Exception($"Assert.Null failed. Was: {obj}");
    }
    public static void NotNull(object? obj)
    {
        if (obj == null) throw new Exception("Assert.NotNull failed. Was null.");
    }
    public static T Throws<T>(Action action) where T : Exception
    {
        try { action(); throw new Exception($"Assert.Throws<{typeof(T).Name}> failed: no exception was thrown."); }
        catch (T ex) { return ex; }
        catch (Exception ex) { throw new Exception($"Assert.Throws<{typeof(T).Name}> failed: got {ex.GetType().Name} instead."); }
    }
}

// ── Test Classes ──────────────────────────────────────────────────────
public class ProjectEntityTests
{
    [Test] public void Create_ValidKey_ShouldSucceed()
    {
        var p = Project.Create(1, "DIG", "Digiprise", 1, BoardType.Scrum);
        Assert.Equal("DIG", p.Key);
        Assert.Equal(ProjectStatus.Active, p.Status);
    }
    [Test] public void Create_KeyTooShort_ShouldThrow() =>
        Assert.Throws<ArgumentException>(() => Project.Create(1, "D", "Short", 1));
    [Test] public void Create_KeyTooLong_ShouldThrow() =>
        Assert.Throws<ArgumentException>(() => Project.Create(1, "VERYLONGKEY", "Long", 1));
    [Test] public void Create_KeyWithSpaces_ShouldThrow() =>
        Assert.Throws<ArgumentException>(() => Project.Create(1, "MY KEY", "Bad", 1));
    [Test] public void Archive_ShouldSetArchived()
    {
        var p = Project.Create(1, "DIG", "Test", 1); p.Archive();
        Assert.Equal(ProjectStatus.Archived, p.Status);
    }
    [Test] public void Delete_ShouldSetDeleted()
    {
        var p = Project.Create(1, "DIG", "Test", 1); p.Delete();
        Assert.Equal(ProjectStatus.Deleted, p.Status);
    }
    [Test] public void Create_LowercaseKey_ShouldBeUppercased()
    {
        var p = Project.Create(1, "dig", "Test", 1);
        Assert.Equal("DIG", p.Key);
    }
}

public class IssueEntityTests
{
    [Test] public void Create_ValidIssue_ShouldSucceed()
    {
        var i = Issue.Create(1, 1, "DIG-1", IssueType.Story, "My story", 1, 1);
        Assert.Equal("DIG-1", i.IssueKey);
        Assert.Equal(IssueType.Story, i.IssueType);
        Assert.Equal(Priority.Major, i.Priority);
    }
    [Test] public void Create_EmptySummary_ShouldThrow() =>
        Assert.Throws<ArgumentException>(() => Issue.Create(1, 1, "DIG-1", IssueType.Story, "", 1, 1));
    [Test] public void Create_LongSummary_ShouldThrow() =>
        Assert.Throws<ArgumentException>(() => Issue.Create(1, 1, "DIG-1", IssueType.Story, new string('x', 256), 1, 1));
    [Test] public void Assign_ShouldUpdateAssignee()
    {
        var i = Issue.Create(1, 1, "DIG-1", IssueType.Story, "Test", 1, 1);
        i.Assign(42, 1); Assert.Equal(42, i.AssigneeId);
    }
    [Test] public void Unassign_ShouldClearAssignee()
    {
        var i = Issue.Create(1, 1, "DIG-1", IssueType.Story, "Test", 1, 1);
        i.Assign(42, 1); i.Assign(null, 1); Assert.Null(i.AssigneeId);
    }
    [Test] public void ChangePriority_ShouldUpdate()
    {
        var i = Issue.Create(1, 1, "DIG-1", IssueType.Bug, "Bug", 1, 1);
        i.ChangePriority(Priority.Blocker, 1); Assert.Equal(Priority.Blocker, i.Priority);
    }
    [Test] public void SetStoryPoints_ShouldUpdate()
    {
        var i = Issue.Create(1, 1, "DIG-1", IssueType.Story, "Story", 1, 1);
        i.SetStoryPoints(8, 1); Assert.Equal(8, i.StoryPoints);
    }
    [Test] public void LogTime_ShouldAccumulate()
    {
        var i = Issue.Create(1, 1, "DIG-1", IssueType.Story, "Story", 1, 1);
        i.LogTime(60); i.LogTime(30); Assert.Equal(90, i.TimeSpentMinutes);
    }
}

public class SprintEntityTests
{
    [Test] public void Create_ShouldBeCreatedState()
    {
        var s = Sprint.Create(1, 1, "Sprint 1", DateTime.UtcNow, DateTime.UtcNow.AddDays(14));
        Assert.Equal(SprintState.Created, s.State);
    }
    [Test] public void Start_ShouldBeActive()
    {
        var s = Sprint.Create(1, 1, "Sprint 1", DateTime.UtcNow, DateTime.UtcNow.AddDays(14));
        s.Start(); Assert.Equal(SprintState.Active, s.State);
    }
    [Test] public void Start_AlreadyActive_ShouldThrow()
    {
        var s = Sprint.Create(1, 1, "Sprint 1", DateTime.UtcNow, DateTime.UtcNow.AddDays(14));
        s.Start(); Assert.Throws<InvalidOperationException>(() => s.Start());
    }
    [Test] public void Close_Active_ShouldBeClosed()
    {
        var s = Sprint.Create(1, 1, "Sprint 1", DateTime.UtcNow, DateTime.UtcNow.AddDays(14));
        s.Start(); s.Close(34);
        Assert.Equal(SprintState.Closed, s.State);
        Assert.Equal(34, s.VelocityPoints);
    }
    [Test] public void Close_Created_ShouldThrow()
    {
        var s = Sprint.Create(1, 1, "Sprint 1", DateTime.UtcNow, DateTime.UtcNow.AddDays(14));
        Assert.Throws<InvalidOperationException>(() => s.Close(0));
    }
}

public class AttachmentEntityTests
{
    [Test] public void Create_Valid_ShouldSucceed()
    {
        var a = Attachment.Create(1, 1, "file.pdf", 1024 * 1024, "application/pdf", "/uploads/file.pdf");
        Assert.Equal("file.pdf", a.FileName);
    }
    [Test] public void Create_TooLarge_ShouldThrow() =>
        Assert.Throws<InvalidOperationException>(() =>
            Attachment.Create(1, 1, "big.zip", 26L * 1024 * 1024, "application/zip", "/uploads/big.zip"));
}

public class IssueLinkEntityTests
{
    [Test] public void Create_SelfLink_ShouldThrow() =>
        Assert.Throws<InvalidOperationException>(() => IssueLink.Create(5, 5, IssueLinkType.Blocks, 1));
    [Test] public void Create_Valid_ShouldSucceed()
    {
        var l = IssueLink.Create(1, 2, IssueLinkType.Blocks, 1);
        Assert.Equal(1, l.SourceIssueId);
        Assert.Equal(2, l.TargetIssueId);
    }
}

public class TenantEntityTests
{
    [Test] public void Create_ShouldLowercaseSubdomain()
    {
        var t = Tenant.Create("Test", "MyTenant");
        Assert.Equal("mytenant", t.Subdomain);
    }
    [Test] public void Deactivate_ShouldSetFalse()
    {
        var t = Tenant.Create("Test", "test"); t.Deactivate();
        Assert.False(t.IsActive);
    }
}

public class PasswordHasherTests
{
    private readonly Digiprise.PMS.Application.Services.PasswordHasher _hasher = new();

    [Test] public void Hash_ProducesDifferentEachTime()
    {
        var h1 = _hasher.Hash("pass123"); var h2 = _hasher.Hash("pass123");
        Assert.NotEqual(h1, h2);
    }
    [Test] public void Verify_Correct_ReturnsTrue()
    {
        var h = _hasher.Hash("SecurePass@99");
        Assert.True(_hasher.Verify("SecurePass@99", h));
    }
    [Test] public void Verify_Wrong_ReturnsFalse()
    {
        var h = _hasher.Hash("CorrectPass");
        Assert.False(_hasher.Verify("WrongPass", h));
    }
    [Test] public void Verify_Tampered_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("password", "invalid:hash"));
    }
}

// ── Test Runner ───────────────────────────────────────────────────────
public static class TestRunner
{
    public static int RunAll()
    {
        var testClasses = new object[]
        {
            new ProjectEntityTests(), new IssueEntityTests(), new SprintEntityTests(),
            new AttachmentEntityTests(), new IssueLinkEntityTests(),
            new TenantEntityTests(), new PasswordHasherTests()
        };

        int passed = 0, failed = 0;
        Console.WriteLine("\n🧪 Digiprise PMS — Domain Test Suite");
        Console.WriteLine(new string('─', 60));

        foreach (var instance in testClasses)
        {
            var type = instance.GetType();
            Console.WriteLine($"\n  📦 {type.Name}");

            foreach (var method in type.GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null))
            {
                try
                {
                    method.Invoke(instance, null);
                    Console.WriteLine($"    ✅ {method.Name}");
                    passed++;
                }
                catch (TargetInvocationException ex)
                {
                    Console.WriteLine($"    ❌ {method.Name}");
                    Console.WriteLine($"       {ex.InnerException?.Message}");
                    failed++;
                }
            }
        }

        Console.WriteLine($"\n{new string('─', 60)}");
        Console.WriteLine($"Results: {passed} passed, {failed} failed");
        Console.WriteLine(failed == 0 ? "✅ All tests passed!" : $"❌ {failed} test(s) failed.");
        return failed;
    }
}
