using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Bookshop;

[EventType]
public record MemberRegistered(string Name, string Email);

[EventType]
public record BookAddedToInventory(string Title, string Author, string Isbn);

[EventType]
public record BookBorrowed(Guid MemberId, DateTimeOffset DueBy);

[EventType]
public record BookReturned();

[EventType]
public record BookMarkedOverdue(int DaysLate);

[EventType]
public record BookReservationPlaced(Guid MemberId);

public record Book(Guid Id, string Title, string Author, string Isbn);

public record Member(Guid Id, string Name, string Email);

public record BorrowedBook(Guid Id)
{
    public string Title { get; set; } = string.Empty;
    public string Member { get; set; } = string.Empty;
    public DateTimeOffset Borrowed { get; set; }
    public DateTimeOffset DueBy { get; set; }
}

public record OverdueBook(Guid Id)
{
    public string Title { get; set; } = string.Empty;
    public string Member { get; set; } = string.Empty;
    public int DaysLate { get; set; }
}

public class Books : IReducerFor<Book>
{
    public Task<Book> Added(BookAddedToInventory @event, Book? initialState, EventContext context) =>
        Task.FromResult(new Book(Guid.Parse(context.EventSourceId), @event.Title, @event.Author, @event.Isbn));
}

public class Members : IReducerFor<Member>
{
    public Task<Member> Registered(MemberRegistered @event, Member? initialState, EventContext context) =>
        Task.FromResult(new Member(Guid.Parse(context.EventSourceId), @event.Name, @event.Email));
}

public class BorrowedBooks : IProjectionFor<BorrowedBook>
{
    public void Define(IProjectionBuilderFor<BorrowedBook> builder) => builder
        .From<BookBorrowed>(from => from
            .Set(m => m.DueBy).To(e => e.DueBy)
            .Set(m => m.Borrowed).ToEventContextProperty(c => c.Occurred))
        .Join<BookAddedToInventory>(join => join
            .On(m => m.Id)
            .Set(m => m.Title).To(e => e.Title))
        .RemovedWith<BookReturned>();
}

public class OverdueBooks : IProjectionFor<OverdueBook>
{
    public void Define(IProjectionBuilderFor<OverdueBook> builder) => builder
        .From<BookMarkedOverdue>(from => from
            .Set(m => m.DaysLate).To(e => e.DaysLate))
        .Join<BookAddedToInventory>(join => join
            .On(m => m.Id)
            .Set(m => m.Title).To(e => e.Title))
        .RemovedWith<BookReturned>();
}

/// <summary>
/// Sends the overdue notice. Deliberately fails for one book so the demo server has a
/// failed partition to inspect and retry.
/// </summary>
public class OverdueNotices : IReactor
{
    public static string? FailForEventSourceId { get; set; }

    public Task Overdue(BookMarkedOverdue @event, EventContext context)
    {
        if (FailForEventSourceId is not null && string.Equals(context.EventSourceId, FailForEventSourceId, StringComparison.Ordinal))
        {
            throw new SmtpUnavailable("smtp.bookshop.local: connection refused");
        }

        return Task.CompletedTask;
    }
}

public class SmtpUnavailable(string message) : Exception(message);
