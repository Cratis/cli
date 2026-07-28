using Bookshop;
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;

// Modes:
//   seed          — register artifacts and append the baseline story, then exit
//   serve         — stay connected and healthy, so observers report Active
//   serve-failing — stay connected with the overdue notice still failing, so one partition
//                   stays failed while everything around it keeps working
//   drip          — stay connected and append a trickle so a live dashboard visibly moves
//
// A client that has exited leaves every observer Disconnected, which looks alarming in a
// recording and is not what anyone's system looks like while it is running. The serve modes
// exist so the demos show a live system.
var server = args.Length > 0 ? args[0] : "chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35100/";
var storeName = args.Length > 1 ? args[1] : "Bookshop";
var seconds = args.Length > 2 ? int.Parse(args[2]) : 25;
var mode = args.Length > 3 ? args[3] : "seed";

// The book whose overdue notice cannot be sent. Armed BEFORE connecting: Chronicle retries a
// pending failed partition the moment a client reconnects, so arming it afterwards lets that
// first retry succeed and clears the very failure the triage clip is about.
const string FailingBook = "978-0131177055";

if (string.Equals(mode, "serve-failing", StringComparison.Ordinal))
{
    OverdueNotices.FailForEventSourceId = FailingBook;
}

var options = new ChronicleOptions(new ChronicleConnectionString(server));
using var client = new ChronicleClient(options);
var eventStore = await client.GetEventStore(storeName);

// Give the client time to register artifacts with the server.
await Task.Delay(TimeSpan.FromSeconds(5));

// Domain identifiers, not GUIDs — an ISBN keys a book and a handle keys a member. They are
// stable across re-renders and readable in every column the CLI prints.
var members = new[]
{
    ("ada.wong", "Ada Wong", "ada@example.com"),
    ("grace.miller", "Grace Miller", "grace@example.com"),
    ("kaito.mori", "Kaito Mori", "kaito@example.com"),
};

var books = new[]
{
    ("978-0135957059", "The Pragmatic Programmer", "Hunt & Thomas"),
    ("978-0321125217", "Domain-Driven Design", "Eric Evans"),
    ("978-1449373320", "Designing Data-Intensive Applications", "Martin Kleppmann"),
    ("978-0134757599", "Refactoring", "Martin Fowler"),
    ("978-0131177055", "Working Effectively with Legacy Code", "Michael Feathers"),
    ("978-1680502398", "Release It!", "Michael Nygard"),
    ("978-1942788331", "Accelerate", "Forsgren, Humble & Kim"),
    ("978-0201835953", "The Mythical Man-Month", "Fred Brooks"),
};

if (mode is "serve" or "serve-failing")
{
    Console.WriteLine($"{mode}: connected for {seconds}s");
    await Task.Delay(TimeSpan.FromSeconds(seconds));
    Console.WriteLine("done");
    return;
}

if (string.Equals(mode, "drip", StringComparison.Ordinal))
{
    // The overdue notice succeeds here — this mode exists to make a live dashboard move,
    // not to create failures.
    Console.WriteLine($"drip: appending for {seconds}s");
    var deadline = DateTimeOffset.UtcNow.AddSeconds(seconds);
    var n = 0;
    while (DateTimeOffset.UtcNow < deadline)
    {
        var book = books[(n + 6) % books.Length];
        var member = members[n % members.Length];
        await eventStore.EventLog.Append(book.Item1, new BookReservationPlaced(member.Item1));
        await Task.Delay(TimeSpan.FromMilliseconds(1500));
        await eventStore.EventLog.Append(book.Item1, new BookBorrowed(member.Item1, DateTimeOffset.UtcNow.AddDays(14)));
        await Task.Delay(TimeSpan.FromMilliseconds(1500));
        n++;
    }

    Console.WriteLine("drip done");
    return;
}

foreach (var (id, name, email) in members)
{
    await eventStore.EventLog.Append(id, new MemberRegistered(name, email));
}

foreach (var (id, title, author) in books)
{
    await eventStore.EventLog.Append(id, new BookAddedToInventory(title, author));
}

var dueIn = new[] { 9, 12, 6, -3, -11, 4 };
for (var i = 0; i < 6; i++)
{
    var member = members[i % members.Length];
    await eventStore.EventLog.Append(
        books[i].Item1,
        new BookBorrowed(member.Item1, DateTimeOffset.UtcNow.AddDays(dueIn[i])));
}

await eventStore.EventLog.Append(books[0].Item1, new BookReturned());
await eventStore.EventLog.Append(books[1].Item1, new BookReturned());

await eventStore.EventLog.Append(books[2].Item1, new BookReservationPlaced(members[0].Item1));
await eventStore.EventLog.Append(books[3].Item1, new BookReservationPlaced(members[2].Item1));

// Two books go overdue. The notice for the second one cannot be sent, which leaves a
// failed partition behind for the CLI to find.
OverdueNotices.FailForEventSourceId = FailingBook;
await eventStore.EventLog.Append(books[3].Item1, new BookMarkedOverdue(3));
await eventStore.EventLog.Append(books[4].Item1, new BookMarkedOverdue(11));

Console.WriteLine($"seeded. failing partition = {FailingBook} ({books[4].Item2})");

await Task.Delay(TimeSpan.FromSeconds(seconds));
Console.WriteLine("done");
