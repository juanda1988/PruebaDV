using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PruebaDV.Data;
using PruebaDV.Models;
using TicketAPI.Controllers;

namespace TestProject
{
    public class TicketsControllerTests
    {
        private static DbContextOptions<AppDbContext> CreateNewContextOptions()
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TicketsTestDb_" + Guid.NewGuid())
                .Options;
        }

        private static TProp GetAnonymousProperty<TProp>(object anonObj, string propName)
        {
            var prop = anonObj.GetType().GetProperty(propName)
                ?? throw new InvalidOperationException($"Property '{propName}' not found on anonymous object.");
            return (TProp)prop.GetValue(anonObj)!;
        }

        [Fact]
        public async Task GetTickets_ReturnsPaginatedList()
        {
            var options = CreateNewContextOptions();

            using (var context = new AppDbContext(options))
            {
                for (int i = 0; i < 15; i++)
                {
                    context.Tickets.Add(new Ticket
                    {
                        Usuario = $"user{i}",
                        Estatus = "Open",
                        FechaCreacion = DateTime.UtcNow.AddMinutes(-i),
                        FechaActualizacion = DateTime.UtcNow.AddMinutes(-i)
                    });
                }
                await context.SaveChangesAsync();
            }

            using (var context = new AppDbContext(options))
            {
                var controller = new TicketsController(context);

                var result = await controller.GetTickets(page: 2, pageSize: 5);
                var ok = Assert.IsType<OkObjectResult>(result);

                var total = GetAnonymousProperty<int>(ok.Value!, "total");
                var page = GetAnonymousProperty<int>(ok.Value!, "page");
                var pageSize = GetAnonymousProperty<int>(ok.Value!, "pageSize");
                var data = GetAnonymousProperty<IEnumerable<Ticket>>(ok.Value!, "data").ToList();

                Assert.Equal(15, total);
                Assert.Equal(2, page);
                Assert.Equal(5, pageSize);
                Assert.Equal(5, data.Count);

                var expectedFirst = context.Tickets
                    .OrderByDescending(t => t.FechaCreacion)
                    .Skip((2 - 1) * 5)
                    .First()
                    .Id;
                Assert.Equal(expectedFirst, data[0].Id);
            }
        }

        [Fact]
        public async Task GetTicket_ReturnsTicket_WhenFound()
        {
            var options = CreateNewContextOptions();
            int id;

            using (var context = new AppDbContext(options))
            {
                var ticket = new Ticket
                {
                    Usuario = "alice",
                    Estatus = "Open",
                    FechaCreacion = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow
                };
                context.Tickets.Add(ticket);
                await context.SaveChangesAsync();
                id = ticket.Id;
            }

            using (var context = new AppDbContext(options))
            {
                var controller = new TicketsController(context);
                var result = await controller.GetTicket(id);
                var ok = Assert.IsType<OkObjectResult>(result);
                var returned = Assert.IsType<Ticket>(ok.Value);
                Assert.Equal(id, returned.Id);
                Assert.Equal("alice", returned.Usuario);
            }
        }

        [Fact]
        public async Task GetTicket_ReturnsNotFound_WhenMissing()
        {
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);
            var controller = new TicketsController(context);

            var result = await controller.GetTicket(9999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateTicket_CreatesAndReturnsCreatedAt()
        {
            var options = CreateNewContextOptions();

            using (var context = new AppDbContext(options))
            {
                var controller = new TicketsController(context);

                var newTicket = new Ticket
                {
                    Usuario = "bob",
                    Estatus = "New"
                };

                var result = await controller.CreateTicket(newTicket);
                var created = Assert.IsType<CreatedAtActionResult>(result);
                Assert.Equal(nameof(controller.GetTicket), created.ActionName);

                var createdTicket = Assert.IsType<Ticket>(created.Value);
                Assert.NotEqual(0, createdTicket.Id);
                Assert.Equal("bob", createdTicket.Usuario);
                Assert.Equal("New", createdTicket.Estatus);
                Assert.True(createdTicket.FechaCreacion <= DateTime.UtcNow && createdTicket.FechaActualizacion <= DateTime.UtcNow);

                var persisted = await context.Tickets.FindAsync(createdTicket.Id);
                Assert.NotNull(persisted);
            }
        }

        [Fact]
        public async Task UpdateTicket_ReturnsOkAndUpdates()
        {
            var options = CreateNewContextOptions();
            int id;
            DateTime beforeUpdate;

            using (var context = new AppDbContext(options))
            {
                var ticket = new Ticket
                {
                    Usuario = "carol",
                    Estatus = "Open",
                    FechaCreacion = DateTime.UtcNow.AddHours(-1),
                    FechaActualizacion = DateTime.UtcNow.AddHours(-1)
                };
                context.Tickets.Add(ticket);
                await context.SaveChangesAsync();
                id = ticket.Id;
                beforeUpdate = ticket.FechaActualizacion;
            }

            using (var context = new AppDbContext(options))
            {
                var controller = new TicketsController(context);
                var updated = new Ticket
                {
                    Usuario = "carol-updated",
                    Estatus = "Closed"
                };

                var result = await controller.UpdateTicket(id, updated);
                var ok = Assert.IsType<OkObjectResult>(result);
                var returned = Assert.IsType<Ticket>(ok.Value);

                Assert.Equal("carol-updated", returned.Usuario);
                Assert.Equal("Closed", returned.Estatus);
                Assert.True(returned.FechaActualizacion > beforeUpdate);

                var persisted = await context.Tickets.FindAsync(id);
                Assert.Equal("carol-updated", persisted!.Usuario);
                Assert.Equal("Closed", persisted!.Estatus);
            }
        }

        [Fact]
        public async Task UpdateTicket_ReturnsNotFound_WhenMissing()
        {
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);
            var controller = new TicketsController(context);

            var updated = new Ticket { Usuario = "x", Estatus = "y" };
            var result = await controller.UpdateTicket(9999, updated);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteTicket_ReturnsNoContentAndDeletes()
        {
            var options = CreateNewContextOptions();
            int id;

            using (var context = new AppDbContext(options))
            {
                var ticket = new Ticket
                {
                    Usuario = "dave",
                    Estatus = "Open",
                    FechaCreacion = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow
                };
                context.Tickets.Add(ticket);
                await context.SaveChangesAsync();
                id = ticket.Id;
            }

            using (var context = new AppDbContext(options))
            {
                var controller = new TicketsController(context);
                var result = await controller.DeleteTicket(id);
                Assert.IsType<NoContentResult>(result);

                var persisted = await context.Tickets.FindAsync(id);
                Assert.Null(persisted);
            }
        }

        [Fact]
        public async Task DeleteTicket_ReturnsNotFound_WhenMissing()
        {
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);
            var controller = new TicketsController(context);

            var result = await controller.DeleteTicket(9999);
            Assert.IsType<NotFoundResult>(result);
        }
    }
}