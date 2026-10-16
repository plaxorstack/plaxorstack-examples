using System.IO.Compression;
using System.Text;

namespace DatasetBuilder;

// A web shop's support inbox, one ticket per team. Written in the everyday, untidy way customers
// write: the model should learn the vocabulary of a problem, not a template.
internal static class SupportTickets
{
    private const int TicketsPerTeam = 80;

    private static readonly string[] Openers =
    [
        "Hi,", "Hello,", "Hi there,", "Good morning,", "Hey,", "Dear support team,", "Hello support,", "", "", "",
    ];

    private static readonly string[] Closers =
    [
        "Thanks.", "Thank you!", "Please help.", "Can someone look into this?", "Regards", "Thanks in advance.",
        "This is urgent.", "Cheers", "", "", "",
    ];

    private static readonly Dictionary<string, string[]> Issues = new()
    {
        ["billing"] =
        [
            "I was charged twice for order #{order}.",
            "My card was billed {amount} but the invoice says {amount2}.",
            "I cancelled my subscription last month and I was still charged.",
            "Can you send me a VAT invoice for my last payment?",
            "The discount code {code} was not applied at checkout.",
            "I see a pending charge of {amount} that I don't recognise.",
            "How do I change the credit card on my account?",
            "I need a refund for the duplicate payment.",
            "Why did my monthly price go up?",
            "The payment failed but the money left my bank account.",
            "Please update the company name on my invoices.",
            "I was promised a refund {days} days ago and haven't received it.",
            "Can I pay by bank transfer instead of card?",
            "My invoice shows the wrong billing address.",
            "I was charged for the annual plan but I selected monthly.",
            "Do you offer a receipt in PDF? I need it for expenses.",
            "My PayPal payment went through twice.",
            "The total at checkout was different from the price on the product page.",
        ],
        ["technical"] =
        [
            "The app crashes as soon as I open it.",
            "I get error {error} when I try to upload a photo.",
            "The website is extremely slow today.",
            "The checkout page just keeps loading forever.",
            "Push notifications stopped working after the last update.",
            "The search returns no results even for products I know you sell.",
            "I can't play the product videos, the player stays black.",
            "The page shows a blank white screen on Safari.",
            "Your API returns {error} for every request since this morning.",
            "The app logs me out every few minutes.",
            "Images on the product page don't load.",
            "The Android app freezes on the order history screen.",
            "Syncing between my phone and laptop doesn't work.",
            "The export to CSV button does nothing.",
            "I keep getting 'something went wrong' when I add items to the basket.",
            "The barcode scanner in the app won't focus.",
            "Dark mode makes all the text unreadable.",
            "After updating to version {version} the app won't start.",
        ],
        ["account"] =
        [
            "I forgot my password and the reset email never arrives.",
            "How do I change the email address on my account?",
            "Please delete my account and all my data.",
            "I can't log in, it says my account is locked.",
            "Someone else may have accessed my account.",
            "I'd like to turn on two-factor authentication.",
            "My verification code doesn't work.",
            "How can I add a colleague to our business account?",
            "I have two accounts by mistake, can you merge them?",
            "I want to change my username.",
            "The 'sign in with Google' button gives me an error.",
            "I lost my phone and can't receive the login code.",
            "Please unsubscribe me from all marketing emails.",
            "How do I download a copy of my personal data?",
            "My account shows someone else's name.",
            "I never received the account activation link.",
            "Can I transfer my account to my new company email?",
            "I'd like to close my account but keep my order history.",
        ],
        ["shipping"] =
        [
            "My order #{order} still hasn't arrived.",
            "The tracking number doesn't work on the courier's website.",
            "The parcel was delivered to the wrong address.",
            "The box arrived damaged and the item is broken.",
            "Can I change the delivery address for order #{order}?",
            "Tracking says delivered but I never got the package.",
            "Do you ship to {country}?",
            "Why is delivery taking {days} days?",
            "I received the wrong item in my parcel.",
            "Part of my order is missing.",
            "Can I pick up my order from a locker instead?",
            "The courier left the parcel outside in the rain.",
            "How much is express shipping to {country}?",
            "My parcel has been stuck in customs for a week.",
            "I need the order delivered before Friday, is that possible?",
            "The driver said nobody was home but I was there all day.",
            "Can I return the item by post or do I need to drop it off?",
            "The delivery date keeps getting pushed back.",
        ],
    };

    private static readonly Dictionary<string, string[]> Context = new()
    {
        ["billing"] =
        [
            "I've attached a screenshot of my bank statement.", "This is the second time it happened.",
            "My accountant needs this by the end of the month.", "I'm on the Business plan.",
            "The charge appeared on {date}.",
        ],
        ["technical"] =
        [
            "I'm using an iPhone 14 with the latest iOS.", "I already tried clearing the cache and reinstalling.",
            "It happens on both Chrome and Firefox.", "It started after the update yesterday.",
            "My internet connection is fine, other sites work.",
        ],
        ["account"] =
        [
            "I've tried resetting it three times.", "I checked my spam folder too.",
            "My old email address no longer exists.", "I need access today for work.",
            "The account is registered under {name}.",
        ],
        ["shipping"] =
        [
            "I ordered on {date}.", "It was supposed to be a birthday present.",
            "The courier is DHL.", "I paid for next-day delivery.", "My neighbours didn't take it either.",
        ],
    };

    private static readonly string[] Countries = ["Canada", "Norway", "Australia", "Switzerland", "Japan", "Brazil", "Ireland", "Poland"];
    private static readonly string[] Errors = ["500", "403", "ERR_UPLOAD_7", "0x80070005", "timeout", "502 Bad Gateway"];
    private static readonly string[] Names = ["Anna Kovacs", "Tom Becker", "Lena Silva", "Marco Rossi", "Sara Ahmed", "James Okafor"];

    public static async Task BuildAsync(string repoRoot)
    {
        var random = new Random(2026);
        var tickets = new List<(string Team, string Text)>();
        var seen = new HashSet<string>();

        foreach (var team in Issues.Keys)
        {
            var made = 0;

            while (made < TicketsPerTeam)
            {
                var text = Compose(random, team);

                if (seen.Add(text))
                {
                    tickets.Add((team, text));
                    made++;
                }
            }
        }

        var data = RepoPaths.DataFolder(repoRoot, "text/route-support-tickets");

        Csv.Write(Path.Combine(data, "tickets.csv"), ["ticket_text", "team"],
            tickets.OrderBy(_ => random.Next()).Select(t => new object[] { t.Text, t.Team }));

        var zipPath = Path.Combine(data, "tickets-by-team.zip");
        File.Delete(zipPath);

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        foreach (var team in tickets.GroupBy(t => t.Team))
        {
            var n = 0;

            foreach (var ticket in team)
            {
                var entry = zip.CreateEntry($"{team.Key}/ticket-{++n:000}.txt");
                await using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                await writer.WriteAsync(ticket.Text);
            }
        }

        Console.WriteLine($"  {tickets.Count} tickets in {Issues.Count} teams");
    }

    private static string Compose(Random random, string team)
    {
        var parts = new List<string>();
        var opener = Pick(random, Openers);

        if (opener.Length > 0)
            parts.Add(opener);

        var issues = Issues[team];
        var first = random.Next(issues.Length);
        parts.Add(Fill(random, issues[first]));

        if (random.NextDouble() < 0.3)
            parts.Add(Fill(random, issues[(first + 1 + random.Next(issues.Length - 1)) % issues.Length]));

        if (random.NextDouble() < 0.5)
            parts.Add(Fill(random, Pick(random, Context[team])));

        var closer = Pick(random, Closers);

        if (closer.Length > 0)
            parts.Add(closer);

        return string.Join(' ', parts);
    }

    private static string Fill(Random random, string template) => template
        .Replace("{order}", random.Next(10000, 99999).ToString())
        .Replace("{amount2}", $"${random.Next(9, 120)}.{random.Next(0, 99):00}")
        .Replace("{amount}", $"${random.Next(9, 120)}.{random.Next(0, 99):00}")
        .Replace("{code}", Pick(random, ["SPRING15", "WELCOME10", "FREESHIP", "VIP20"]))
        .Replace("{days}", random.Next(5, 21).ToString())
        .Replace("{error}", Pick(random, Errors))
        .Replace("{version}", $"4.{random.Next(1, 9)}.{random.Next(0, 5)}")
        .Replace("{country}", Pick(random, Countries))
        .Replace("{date}", $"{Pick(random, ["March", "April", "May", "June"])} {random.Next(1, 29)}")
        .Replace("{name}", Pick(random, Names));

    private static string Pick(Random random, string[] items) => items[random.Next(items.Length)];
}
