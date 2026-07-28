using Bookshop;
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;

// Modes:
//   seed  — register artifacts, append the baseline story, leave one failing partition behind
//   drip  — stay connected and append a trickle of events so a live dashboard visibly moves
var server = args.Length > 0 ? args[0] : "chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35100/";
var storeName = args.Length > 1 ? args[1] : "Bookshop";
var seconds = args.Length > 2 ? int.Parse(args[2]) : 25;
var mode = args.Length > 3 ? args[3] : "seed";

var options = new ChronicleOptions(new ChronicleConnectionString(server));
using var client = new ChronicleClient(options);
var eventStore = await client.GetEventStore(storeName);

// Give the client time to register artifacts with the server.
await Task.Delay(TimeSpan.FromSeconds(5));

// Deterministic ids so every re-render of the tapes shows the same identifiers.
static Guid Id(int n) => new($"0000{n:0000}-1111-4222-8333-444444444444");

var members = new[]
{
    (Id(1), "Ada Wong", "ada@example.com"),
    (Id(2), "Grace Miller", "grace@example.com"),
    (Id(3), "Kaito Mori", "kaito@example.com"),
};

var books = new[]
{
    (Id(10), "The Pragmatic Programmer", "Hunt & Thomas", "978-0135957059"),
    (Id(11), "Domain-Driven Design", "Eric Evans", "978-0321125217"),
    (Id(12), "Designing Data-Intensive Applications", "Martin Kleppmann", "978-1449373320"),
    (Id(13), "Refactoring", "Martin Fowler", "978-0134757599"),
    (Id(14), "Working Effectively with Legacy Code", "Michael Feathers", "978-0131177055"),
    (Id(15), "Release It!", "Michael Nygard", "978-1680502398"),
    (Id(16), "Accelerate", "Forsgren, Humble & Kim", "978-1942788331"),
    (Id(17), "The Mythical Man-Month", "Fred Brooks", "978-0201835953"),
};

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
        await eventStore.EventLog.Append(book.Item1.ToString(), new BookReservationPlaced(member.Item1));
        await Task.Delay(TimeSpan.FromMilliseconds(1500));
        await eventStore.EventLog.Append(book.Item1.ToString(), new BookBorrowed(member.Item1, DateTimeOffset.UtcNow.AddDays(14)));
        await Task.Delay(TimeSpan.FromMilliseconds(1500));
        n++;
    }

    Console.WriteLine("drip done");
    return;
}

foreach (var (id, name, email) in members)
{
    await eventStore.EventLog.Append(id.ToString(), new MemberRegistered(name, email));
}

foreach (var (id, title, author, isbn) in books)
{
    await eventStore.EventLog.Append(id.ToString(), new BookAddedToInventory(title, author, isbn));
}

var dueIn = new[] { 9, 12, 6, -3, -11, 4 };
for (var i = 0; i < 6; i++)
{
    var member = members[i % members.Length];
    await eventStore.EventLog.Append(
        books[i].Item1.ToString(),
        new BookBorrowed(member.Item1, DateTimeOffset.UtcNow.AddDays(dueIn[i])));
}

await eventStore.EventLog.Append(books[0].Item1.ToString(), new BookReturned());
await eventStore.EventLog.Append(books[1].Item1.ToString(), new BookReturned());

await eventStore.EventLog.Append(books[2].Item1.ToString(), new BookReservationPlaced(members[0].Item1));
await eventStore.EventLog.Append(books[3].Item1.ToString(), new BookReservationPlaced(members[2].Item1));

// Two books go overdue. The notice for the second one cannot be sent, which leaves a
// failed partition behind for the CLI to find.
OverdueNotices.FailForEventSourceId = books[4].Item1.ToString();
await eventStore.EventLog.Append(books[3].Item1.ToString(), new BookMarkedOverdue(3));
await eventStore.EventLog.Append(books[4].Item1.ToString(), new BookMarkedOverdue(11));

Console.WriteLine($"seeded. failing partition = {books[4].Item1} ({books[4].Item2})");

await Task.Delay(TimeSpan.FromSeconds(seconds));
Console.WriteLine("done");
