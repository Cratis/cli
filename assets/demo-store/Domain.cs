using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Bookshop;

// Event source ids here are the domain's own identifiers — an ISBN for a book, a handle for a
// member — rather than generated GUIDs. Chronicle takes any string, and it keeps every column
// of CLI output readable instead of 36 characters of hex.

[EventType]
public record MemberRegistered(string Name, string Email);

[EventType]
public record BookAddedToInventory(string Title, string Author);

[EventType]
public record BookBorrowed(string MemberId, DateTimeOffset DueBy);

[EventType]
public record BookReturned();

[EventType]
public record BookMarkedOverdue(int DaysLate);

[EventType]
public record BookReservationPlaced(string MemberId);

public record Book(string Id, string Title, string Author);

public record Member(string Id, string Name, string Email);

public record BorrowedBook(string Id)
{
    public string Title { get; set; } = string.Empty;
    public string Borrower { get; set; } = string.Empty;
    public DateTimeOffset DueBy { get; set; }
}

public record OverdueBook(string Id)
{
    public string Title { get; set; } = string.Empty;
    public int DaysLate { get; set; }
}

public class Books : IReducerFor<Book>
{
    public Task<Book> Added(BookAddedToInventory @event, Book? initialState, EventContext context) =>
        Task.FromResult(new Book(context.EventSourceId.ToString(), @event.Title, @event.Author));
}

public class Members : IReducerFor<Member>
{
    public Task<Member> Registered(MemberRegistered @event, Member? initialState, EventContext context) =>
        Task.FromResult(new Member(context.EventSourceId.ToString(), @event.Name, @event.Email));
}

public class BorrowedBooks : IProjectionFor<BorrowedBook>
{
    public void Define(IProjectionBuilderFor<BorrowedBook> builder) => builder
        .From<BookBorrowed>(from => from
            .Set(m => m.Borrower).To(e => e.MemberId)
            .Set(m => m.DueBy).To(e => e.DueBy))
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
/// Sends the overdue notice. Fails for one book on demand, so the triage clip has a real
/// exception to follow rather than a contrived one.
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
