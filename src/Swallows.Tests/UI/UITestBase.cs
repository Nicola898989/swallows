using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;
using Swallows.Core.Data;
using Swallows.Desktop;
using Xunit;

namespace Swallows.Tests.UI;

/// <summary>
/// Base class for all UI automation tests providing common setup/teardown and helper methods
/// </summary>
public abstract class UITestBase
{
    protected DbContextOptions<AppDbContext> DbOptions { get; private set; }
    protected Func<AppDbContext> ContextFactory { get; private set; }

    protected UITestBase()
    {
        // Create unique in-memory database for each test
        DbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"SwallowsUITest_{Guid.NewGuid()}")
            .Options;

        ContextFactory = () => new AppDbContext(DbOptions);

        // Ensure database is created
        using var context = ContextFactory();
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Helper to run UI operations on the dispatcher thread
    /// </summary>
    protected async Task<T> RunOnUIThread<T>(Func<T> func)
    {
        return await Dispatcher.UIThread.InvokeAsync(func);
    }

    /// <summary>
    /// Helper to run async UI operations on the dispatcher thread
    /// </summary>
    protected async Task<T> RunOnUIThreadAsync<T>(Func<Task<T>> func)
    {
        return await Dispatcher.UIThread.InvokeAsync(func);
    }

    /// <summary>
    /// Helper to run UI actions on the dispatcher thread
    /// </summary>
    protected async Task RunOnUIThread(Action action)
    {
        await Dispatcher.UIThread.InvokeAsync(action);
    }

    /// <summary>
    /// Find a control by name in the visual tree
    /// </summary>
    protected T? FindControl<T>(Control parent, string name) where T : Control
    {
        return parent.FindControl<T>(name);
    }

    /// <summary>
    /// Wait for a condition to be true with timeout
    /// </summary>
    protected async Task<bool> WaitForCondition(Func<bool> condition, TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < timeout)
        {
            if (condition())
                return true;
            await Task.Delay(100);
        }
        return false;
    }

    /// <summary>
    /// Wait for a condition to be true with default timeout of 5 seconds
    /// </summary>
    protected async Task<bool> WaitForCondition(Func<bool> condition)
    {
        return await WaitForCondition(condition, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Simulate button click
    /// </summary>
    protected async Task ClickButton(Button button)
    {
        await RunOnUIThread(() =>
        {
            button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        });
    }

    /// <summary>
    /// Set text in a TextBox
    /// </summary>
    protected async Task SetText(TextBox textBox, string text)
    {
        await RunOnUIThread(() =>
        {
            textBox.Text = text;
        });
    }

    /// <summary>
    /// Clean up test database
    /// </summary>
    protected void CleanupDatabase()
    {
        using var context = ContextFactory();
        context.Database.EnsureDeleted();
    }
}
