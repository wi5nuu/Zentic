using Zentic.Application.DTOs;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Zentic.Infrastructure.Repositories;
using Zentic.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Zentic.Tests.UnitTests
{
    public class NotificationServiceTests
    {
        private static (AppDbContext db, NotificationService service) CreateService()
        {
            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            var service = new NotificationService(db, new Repository<Notification>(db));
            return (db, service);
        }

        private static async Task SeedNotificationsAsync(AppDbContext db, int userId)
        {
            db.Notifications.AddRange(
                new Notification
                {
                    Id = 1, Title = "Test 1", Message = "Message 1",
                    Type = NotificationType.InApp, Priority = NotificationPriority.Normal,
                    RecipientId = userId, IsRead = false, IsSent = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Notification
                {
                    Id = 2, Title = "Test 2", Message = "Message 2",
                    Type = NotificationType.InApp, Priority = NotificationPriority.High,
                    RecipientId = userId, IsRead = true, IsSent = true,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                },
                new Notification
                {
                    Id = 3, Title = "Test 3", Message = "Message 3",
                    Type = NotificationType.InApp, Priority = NotificationPriority.Normal,
                    RecipientId = 999, IsRead = false, IsSent = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateAsync_ValidDto_CreatesNotification()
        {
            var (db, service) = CreateService();

            var dto = new CreateNotificationDto
            {
                Title = "New Notification",
                Message = "Test message",
                Type = NotificationType.InApp,
                Priority = NotificationPriority.Normal,
                RecipientId = 1
            };

            var result = await service.CreateAsync(dto, 1);

            Assert.Equal("New Notification", result.Title);
            Assert.Equal(1, db.Notifications.Count());
        }

        [Fact]
        public async Task GetUserNotificationsAsync_ReturnsUserNotifications()
        {
            var (db, service) = CreateService();
            await SeedNotificationsAsync(db, 1);

            var result = await service.GetUserNotificationsAsync(1);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetUserNotificationsAsync_UnreadOnly_ReturnsOnlyUnread()
        {
            var (db, service) = CreateService();
            await SeedNotificationsAsync(db, 1);

            var result = await service.GetUserNotificationsAsync(1, unreadOnly: true);

            Assert.Single(result);
            Assert.False(result[0].IsRead);
        }

        [Fact]
        public async Task GetUnreadCountAsync_ReturnsCorrectCount()
        {
            var (db, service) = CreateService();
            await SeedNotificationsAsync(db, 1);

            var count = await service.GetUnreadCountAsync(1);

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task MarkAsReadAsync_ExistingNotification_SetsIsRead()
        {
            var (db, service) = CreateService();
            await SeedNotificationsAsync(db, 1);

            await service.MarkAsReadAsync(1, 1);

            var notification = await db.Notifications.FindAsync(1);
            Assert.True(notification!.IsRead);
        }

        [Fact]
        public async Task MarkAsReadAsync_WrongUser_DoesNothing()
        {
            var (db, service) = CreateService();
            await SeedNotificationsAsync(db, 1);

            await service.MarkAsReadAsync(1, 999);

            var notification = await db.Notifications.FindAsync(1);
            Assert.False(notification!.IsRead);
        }

        [Fact]
        public async Task MarkAllAsReadAsync_SetsAllUserNotificationsAsRead()
        {
            var (db, service) = CreateService();
            await SeedNotificationsAsync(db, 1);

            await service.MarkAllAsReadAsync(1);

            var unreadCount = await service.GetUnreadCountAsync(1);
            Assert.Equal(0, unreadCount);
        }

        [Fact]
        public async Task DeleteAsync_ExistingNotification_RemovesNotification()
        {
            var (db, service) = CreateService();
            await SeedNotificationsAsync(db, 1);

            await service.DeleteAsync(1, 1);

            var count = await db.Notifications.CountAsync(n => n.Id == 1 && !n.IsDeleted);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task DeleteAsync_WrongUser_DoesNothing()
        {
            var (db, service) = CreateService();
            await SeedNotificationsAsync(db, 1);

            await service.DeleteAsync(1, 999);

            var count = await db.Notifications.CountAsync(n => n.Id == 1 && !n.IsDeleted);
            Assert.Equal(1, count);
        }
    }
}
