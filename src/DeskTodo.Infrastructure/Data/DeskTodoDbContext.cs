using DeskTodo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeskTodo.Infrastructure.Data;

public sealed class DeskTodoDbContext(DbContextOptions<DeskTodoDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    public DbSet<TaskTemplate> TaskTemplates => Set<TaskTemplate>();

    public DbSet<TaskGroup> TaskGroups => Set<TaskGroup>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();

    public DbSet<TaskRelationship> TaskRelationships => Set<TaskRelationship>();

    public DbSet<Goal> Goals => Set<Goal>();

    public DbSet<GoalCompletion> GoalCompletions => Set<GoalCompletion>();

    public DbSet<Milestone> Milestones => Set<Milestone>();

    public DbSet<FocusSession> FocusSessions => Set<FocusSession>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<TaskHistory> TaskHistories => Set<TaskHistory>();

    public DbSet<TaskVersion> TaskVersions => Set<TaskVersion>();

    public DbSet<InboxItem> InboxItems => Set<InboxItem>();

    public DbSet<Decision> Decisions => Set<Decision>();

    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

    public DbSet<FocusContext> FocusContexts => Set<FocusContext>();

    public DbSet<Distraction> Distractions => Set<Distraction>();

    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();

    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DeskTodoDbContext).Assembly);
    }
}
